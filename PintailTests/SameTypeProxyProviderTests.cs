using System;
using System.Text;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class SameTypeProxyProviderTests
    {
        [Test]
        public void TestCanProxy_ShouldReturnTrue_WhenSameType()
        {
            IProxyProvider provider = SameTypeProxyProvider.Instance;

            Assert.True(provider.CanProxy<string, string>("asdf", out _));
            Assert.True(provider.CanProxy<int, int>(3, out _));
            Assert.True(provider.CanProxy<long, long>(3L, out _));
            Assert.True(provider.CanProxy<StringBuilder, StringBuilder>(new(), out _));
        }

        [Test]
        public void TestCanProxy_ShouldReturnFalse_WhenDifferentType()
        {
            IProxyProvider provider = SameTypeProxyProvider.Instance;

            Assert.False(provider.CanProxy<string, int>("asdf", out _));
            Assert.False(provider.CanProxy<int, string>(3, out _));
            Assert.False(provider.CanProxy<long, object>(3L, out _));
            Assert.False(provider.CanProxy<StringBuilder, string>(new(), out _));
        }

        [Test]
        public void TestCanProxyProcessorObtainProxy_ShouldReturnSameValue()
        {
            IProxyProvider provider = SameTypeProxyProvider.Instance;

            Assert.True(provider.CanProxy<string, string>("asdf", out var stringProcessor));
            Assert.AreEqual("asdf", stringProcessor!.ObtainProxy());

            Assert.True(provider.CanProxy<int, int>(3, out var intProcessor));
            Assert.AreEqual(3, intProcessor!.ObtainProxy());

            Assert.True(provider.CanProxy<long, long>(4L, out var longProcessor));
            Assert.AreEqual(4L, longProcessor!.ObtainProxy());

            var stringBuilder = new StringBuilder();
            Assert.True(provider.CanProxy<StringBuilder, StringBuilder>(stringBuilder, out var stringBuilderProcessor));
            Assert.AreSame(stringBuilder, stringBuilderProcessor!.ObtainProxy());
        }
    }
}