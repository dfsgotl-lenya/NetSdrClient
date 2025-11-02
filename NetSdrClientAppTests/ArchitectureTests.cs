using NetArchTest.Rules;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Test]
        public void UI_ShouldNotDependOn_EchoTspServer()
        {
            var assembly = typeof(NetSdrClientApp.NetSdrClient).Assembly;

            var result = Types.InAssembly(assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp")
                .ShouldNot()
                .HaveDependencyOn("EchoTspServer")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, "NetSdrClientApp should not depend directly on EchoTspServer layer.");
        }
    }
}
