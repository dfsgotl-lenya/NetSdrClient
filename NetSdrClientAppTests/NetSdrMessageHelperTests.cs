using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetSdrClientApp.Messages
{
    //TODO: analyze possible use of [StructLayout] for better performance and readability
    public static class NetSdrMessageHelper
    {
        // Константы
        private const short _maxMessageLength = 8191;
        private const short _maxDataItemMessageLength = 8194;
        private const short _msgHeaderLength = 2; // 2 byte, 16 bit
        private const short _msgControlItemLength = 2; // 2 byte, 16 bit
        private const short _msgSequenceNumberLength = 2; // 2 byte, 16 bit

        public enum MsgTypes : ushort // Явно задаємо тип для безпеки
        {
            SetControlItem,
            CurrentControlItem,
            ControlItemRange,
            Ack,
            DataItem0,
            DataItem1,
            DataItem2,
            DataItem3
        }

        public enum ControlItemCodes : ushort
        {
            None = 0,
            IQOutputDataSampleRate = 0x00B8,
            RFFilter = 0x0044,
            ADModes = 0x008A,
            ReceiverState = 0x0018,
            ReceiverFrequency = 0x0020
        }

        public static byte[] GetControlItemMessage(MsgTypes type, ControlItemCodes itemCode, byte[] parameters)
        {
            // Перевірка на null
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));
            
            return GetMessage(type, itemCode, parameters);
        }

        public static byte[] GetDataItemMessage(MsgTypes type, byte[] parameters)
        {
            // Перевірка на null
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            return GetMessage(type, ControlItemCodes.None, parameters);
        }

        private static byte[] GetMessage(MsgTypes type, ControlItemCodes itemCode, byte[] parameters)
        {
            var itemCodeBytes = Array.Empty<byte>();
            if (itemCode != ControlItemCodes.None)
            {
                // Конвертація itemCode у байти
                itemCodeBytes = BitConverter.GetBytes((ushort)itemCode);
            }

            // Довжина тіла повідомлення (itemCodeBytes + parameters)
            var bodyLength = itemCodeBytes.Length + parameters.Length;

            // Отримання заголовка
            var headerBytes = GetHeader(type, bodyLength);

            // Оптимізована конкатенація
            byte[] msg = new byte[headerBytes.Length + bodyLength];

            Buffer.BlockCopy(headerBytes, 0, msg, 0, headerBytes.Length);
            Buffer.BlockCopy(itemCodeBytes, 0, msg, headerBytes.Length, itemCodeBytes.Length);
            Buffer.BlockCopy(parameters, 0, msg, headerBytes.Length + itemCodeBytes.Length, parameters.Length);

            return msg;
        }

        public static bool TranslateMessage(byte[] msg, out MsgTypes type, out ControlItemCodes itemCode, out ushort sequenceNumber, out byte[] body)
        {
            itemCode = ControlItemCodes.None;
            sequenceNumber = 0;
            type = 0;
            body = Array.Empty<byte>();

            if (msg is null || msg.Length < _msgHeaderLength)
                return false;

            int offset = 0;

            // 1. Отримання та переклад заголовка
            TranslateHeader(msg.Take(_msgHeaderLength).ToArray(), out type, out int msgLength);
            offset += _msgHeaderLength;
            
            // Якщо довжина повідомлення не відповідає фактичній, це помилка
            if (msgLength != msg.Length)
                return false;

            // Довжина даних, що залишилися
            int remainingLength = msgLength - _msgHeaderLength;

            // 2. Отримання коду елемента управління або порядкового номера
            if (type < MsgTypes.DataItem0) // get item code (Control messages)
            {
                if (remainingLength < _msgControlItemLength) return false;

                // Використовуємо Subarray для безпечного копіювання
                var itemCodeBytes = msg.Skip(offset).Take(_msgControlItemLength).ToArray();
                var value = BitConverter.ToUInt16(itemCodeBytes, 0);
                offset += _msgControlItemLength;
                remainingLength -= _msgControlItemLength;

                if (Enum.IsDefined(typeof(ControlItemCodes), value))
                {
                    itemCode = (ControlItemCodes)value;
                }
                else
                {
                    return false; // Невідомий код елемента
                }
            }
            else // get sequenceNumber (Data messages)
            {
                if (remainingLength < _msgSequenceNumberLength) return false;

                // Використовуємо Subarray для безпечного копіювання
                var sequenceBytes = msg.Skip(offset).Take(_msgSequenceNumberLength).ToArray();
                sequenceNumber = BitConverter.ToUInt16(sequenceBytes, 0);
                offset += _msgSequenceNumberLength;
                remainingLength -= _msgSequenceNumberLength;
            }

            // 3. Отримання тіла
            if (remainingLength > 0)
            {
                body = msg.Skip(offset).ToArray();
            }
            else
            {
                body = Array.Empty<byte>();
            }

            // Перевірка, чи збігається фактично прочитана довжина з очікуваною
            return body.Length == remainingLength;
        }

        public static IEnumerable<int> GetSamples(ushort sampleSize, byte[] body)
        {
            if (body is null) throw new ArgumentNullException(nameof(body));

            sampleSize /= 8; // to bytes

            // Sonar S4456: Краще розділити перевірку параметрів від ітератора (yield)
            if (sampleSize == 0 || sampleSize > 4)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sampleSize),
                    $"Sample size in bytes ({sampleSize}) must be between 1 and 4."
                );
            }

            // Коректне визначення префіксних байтів (заповнення нулями)
            var prefixBytes = Enumerable.Repeat((byte)0, 4 - sampleSize).ToArray();

            // Використання циклу for для ефективності
            for (int i = 0; i <= body.Length - sampleSize; i += sampleSize)
            {
                // Виділяємо необхідну частину з body
                var sampleBytes = body.Skip(i).Take(sampleSize);

                // Конкатенація та перетворення
                yield return BitConverter.ToInt32(
                    sampleBytes.Concat(prefixBytes).ToArray(), 0
                );
            }
        }

        private static byte[] GetHeader(MsgTypes type, int msgLength)
        {
            // Перевірка на від'ємну довжину
            if (msgLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(msgLength), "Message body length cannot be negative.");
            }

            int lengthWithHeader = msgLength + _msgHeaderLength;

            // Data Items edge case: якщо довжина = _maxDataItemMessageLength, то у полі заголовка записується 0
            if (type >= MsgTypes.DataItem0 && lengthWithHeader == _maxDataItemMessageLength)
            {
                lengthWithHeader = 0;
            }

            // Перевірка на перевищення максимальної довжини
            if (lengthWithHeader > _maxMessageLength && lengthWithHeader != 0)
            {
                throw new ArgumentException($"Message length ({lengthWithHeader}) exceeds allowed value ({_maxMessageLength})");
            }

            // Формування 16-бітного заголовка: Type (3 біти) + Length (13 бітів)
            return BitConverter.GetBytes((ushort)(lengthWithHeader + ((ushort)type << 13)));
        }

        private static void TranslateHeader(byte[] header, out MsgTypes type, out int msgLength)
        {
            if (header.Length < _msgHeaderLength)
                throw new ArgumentException("Header byte array is too short.", nameof(header));

            var num = BitConverter.ToUInt16(header, 0); // Використовуємо перевантаження з offset=0
            
            // Витягуємо тип (3 біти)
            type = (MsgTypes)(num >> 13);
            
            // Витягуємо довжину (13 бітів)
            // Використовуємо бітову маску 0x1FFF (13 одиниць)
            msgLength = num & 0x1FFF;

            // Data Items edge case: якщо Length = 0, то фактична довжина = _maxDataItemMessageLength
            if (type >= MsgTypes.DataItem0 && msgLength == 0)
            {
                msgLength = _maxDataItemMessageLength;
            }
        }
    }
}
