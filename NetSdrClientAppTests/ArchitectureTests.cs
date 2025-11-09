using System.Reflection;
using NetArchTest.Rules;
using NUnit.Framework;

// Підключаємо простори імен, щоб отримати доступ до класів Program
using NetSdrClientApp;
using EchoTspServer;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        // 1. Завантажуємо "збірку" (.dll) нашого основного проєкту
        private static readonly Assembly ApplicationAssembly = typeof(NetSdrClientApp.Program).Assembly;

        // 2. Визначаємо назву простору імен (namespace),
        //    від якого ми НЕ хочемо залежати
        private const string EchoServerNamespace = "EchoTspServer";

        [Test]
        public void App_ShouldNot_DependOn_EchoServer()
        {
            // ARRANGE (Підготовка)
            // Отримуємо всі типи (класи, інтерфейси і т.д.)
            // з нашого основного проєкту NetSdrClientApp
            var types = Types.InAssembly(ApplicationAssembly);

            // ACT (Дія)
            // Створюємо правило:
            // "Типи з нашої збірки...
            var result = types
                .ShouldNot() // ...не повинні
                .HaveDependencyOn(EchoServerNamespace) // ...мати залежність від 'EchoTspServer'"
                .GetResult(); // Отримуємо результат перевірки

            // ASSERT (Перевірка)
            // Переконуємося, що правило не порушено
            Assert.That(result.IsSuccessful, Is.True, 
                "Проєкт 'NetSdrClientApp' не має посилатися на 'EchoTspServer'.");
        }
    }
}
