using System;
using System.Text;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class NullableProxyProviderTests
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

        [Test]
        public void TestCanProxy_ReturnsTrue_WhenSameNullable()
        {
            IProxyProvider provider = new NullableProxyProvider();
            IProxyProvider rootProvider = new CompoundProxyProvider(
                new SameTypeProxyProvider(),
                new NumericEnumProxyProvider()
            );

            Assert.True(provider.CanProxy<int?, int?>(null, rootProvider));
            Assert.True(provider.CanProxy<int?, int?>(123, rootProvider));
        }

        [Test]
        public void TestCanProxy_ReturnsTrue_WhenProxyableNullable()
        {
            IProxyProvider provider = new NullableProxyProvider();
            IProxyProvider rootProvider = new CompoundProxyProvider(
                new SameTypeProxyProvider(),
                new NumericEnumProxyProvider()
            );

            Assert.True(provider.CanProxy<IntA?, IntB?>(null, rootProvider));
            Assert.True(provider.CanProxy<IntA?, IntB?>(IntA.Three, rootProvider));
        }

        [Test]
        public void TestCanProxy_ReturnsFalse_WhenUnproxyableNullable()
        {
            IProxyProvider provider = new NullableProxyProvider();
            IProxyProvider rootProvider = new CompoundProxyProvider(
                new SameTypeProxyProvider(),
                new NumericEnumProxyProvider()
            );

            Assert.False(provider.CanProxy<IntA?, int?>(IntA.Three, rootProvider));
        }

        [Test]
        public void TestObtainProxy_ReturnsValidResults()
        {
            IProxyProvider provider = new NullableProxyProvider();
            IProxyProvider rootProvider = new CompoundProxyProvider(
                new SameTypeProxyProvider(),
                new NumericEnumProxyProvider()
            );

            Assert.IsNull(provider.ObtainProxy<int?, int?>(null, rootProvider));
            Assert.AreEqual(new Nullable<IntB>(IntB.Zero), provider.ObtainProxy<IntA?, IntB?>(IntA.Zero, rootProvider));
        }
    }
}