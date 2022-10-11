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
            IProxyProvider provider = new SameTypeProxyProvider();

            Assert.True(provider.CanProxy<string, string>("asdf"));
            Assert.True(provider.CanProxy<int, int>(3));
            Assert.True(provider.CanProxy<long, long>(3L));
            Assert.True(provider.CanProxy<StringBuilder, StringBuilder>(new()));
        }

        [Test]
        public void TestCanProxy_ShouldReturnFalse_WhenDifferentType()
        {
            IProxyProvider provider = new SameTypeProxyProvider();

            Assert.False(provider.CanProxy<string, int>("asdf"));
            Assert.False(provider.CanProxy<int, string>(3));
            Assert.False(provider.CanProxy<long, object>(3L));
            Assert.False(provider.CanProxy<StringBuilder, string>(new()));
        }

        [Test]
        public void TestObtainProxy_ShouldReturnSameValue()
        {
            IProxyProvider provider = new SameTypeProxyProvider();

            Assert.AreEqual("asdf", provider.ObtainProxy<string, string>("asdf"));
            Assert.AreEqual(3, provider.ObtainProxy<int, int>(3));
            Assert.AreEqual(3L, provider.ObtainProxy<long, long>(3L));

            var stringBuilder = new StringBuilder();
            Assert.AreSame(stringBuilder, provider.ObtainProxy<StringBuilder, StringBuilder>(stringBuilder));
        }
    }
}