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
        public void TestCanProxy_ShouldReturnTrue_WhenSameNullable()
        {
            IProxyProvider provider = new NullableProxyProvider();
            IProxyProvider rootProvider = new CompoundProxyProvider(
                SameTypeProxyProvider.Instance,
                new NumericEnumProxyProvider()
            );

            Assert.True(provider.CanProxy<int?, int?>(null, out _, rootProvider));
            Assert.True(provider.CanProxy<int?, int?>(123, out _, rootProvider));
        }

        [Test]
        public void TestCanProxy_ShouldReturnTrue_WhenProxyableNullable()
        {
            IProxyProvider provider = new NullableProxyProvider();
            IProxyProvider rootProvider = new CompoundProxyProvider(
                SameTypeProxyProvider.Instance,
                new NumericEnumProxyProvider()
            );

            Assert.True(provider.CanProxy<IntA?, IntB?>(null, out _, rootProvider));
            Assert.True(provider.CanProxy<IntA?, IntB?>(IntA.Three, out _, rootProvider));
        }

        [Test]
        public void TestCanProxy_ShouldReturnFalse_WhenUnproxyableNullable()
        {
            IProxyProvider provider = new NullableProxyProvider();
            IProxyProvider rootProvider = new CompoundProxyProvider(
                SameTypeProxyProvider.Instance,
                new NumericEnumProxyProvider()
            );

            Assert.False(provider.CanProxy<IntA?, int?>(IntA.Three, out _, rootProvider));
        }

        [Test]
        public void TestCanProxyProcessorObtainProxy_ShouldReturnValidResults()
        {
            IProxyProvider provider = new NullableProxyProvider();
            IProxyProvider rootProvider = new CompoundProxyProvider(
                SameTypeProxyProvider.Instance,
                new NumericEnumProxyProvider()
            );

            Assert.True(provider.CanProxy<int?, int?>(null, out var intProcessor, rootProvider));
            Assert.IsNull(intProcessor!.ObtainProxy());

            Assert.True(provider.CanProxy<IntA?, IntB?>(IntA.Zero, out var intbProcessor, rootProvider));
            Assert.AreEqual(new IntB?(IntB.Zero), intbProcessor!.ObtainProxy());
        }
    }
}