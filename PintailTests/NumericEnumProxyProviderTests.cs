using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class NumericEnumProxyProviderTests
    {
        private enum IntA : int
        {
            Zero = 0,
            One = 1,
            Two = 2,
            Three = 3,
            Four = 4
        }

        private enum IntB : int
        {
            Zero = 0,
            FirstFlag = 2 << 0,
            SecondFlag = 2 << 1,
            ThirdFlag = 2 << 2,
            FourthFlag = 2 << 3
        }

        private enum LongA : long
        {
            A, B, C, D, E, F, G, H, I, J
        }

        [Test]
        public void TestCanProxy_ShouldReturnTrue_WhenSameEnum()
        {
            IProxyProvider provider = new NumericEnumProxyProvider();

            Assert.True(provider.CanProxy<IntA, IntA>(IntA.Zero, out _));
        }

        [Test]
        public void TestCanProxy_ShouldReturnTrue_WhenSameEnumUnderlyingType()
        {
            IProxyProvider provider = new NumericEnumProxyProvider();

            Assert.True(provider.CanProxy<IntB, IntA>(IntB.FourthFlag, out _));
        }

        [Test]
        public void TestCanProxy_ShouldReturnFalse_WhenDifferentEnumUnderlyingType()
        {
            IProxyProvider provider = new NumericEnumProxyProvider();

            Assert.False(provider.CanProxy<IntA, LongA>(IntA.Zero, out _));
        }

        [Test]
        public void TestCanProxyProcessorObtainProxy_ShouldReturnSameNumericValue()
        {
            IProxyProvider provider = new NumericEnumProxyProvider();

            Assert.True(provider.CanProxy<IntA, IntB>(IntA.Zero, out var intbProcessor));
            Assert.AreEqual(IntB.Zero, intbProcessor!.ObtainProxy());

            Assert.True(provider.CanProxy((IntA)999, out intbProcessor));
            Assert.AreEqual((IntB)999, intbProcessor!.ObtainProxy());
        }
    }
}