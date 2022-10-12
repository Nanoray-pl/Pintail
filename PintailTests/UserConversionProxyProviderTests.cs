using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class UserConversionProxyProviderTests
    {
        private struct BasicStruct
        {
            internal double X;
            internal double Y;
        }

        private struct ImplicitStruct
        {
            internal double X;
            internal double Y;

            public static implicit operator ImplicitStruct(BasicStruct value)
                => new() { X = value.X, Y = value.Y };

            public static implicit operator BasicStruct(ImplicitStruct value)
                => new() { X = value.X, Y = value.Y };
        }

        private struct ExplicitStruct
        {
            internal double X;
            internal double Y;

            public static explicit operator ExplicitStruct(BasicStruct value)
                => new() { X = value.X, Y = value.Y };

            public static explicit operator BasicStruct(ExplicitStruct value)
                => new() { X = value.X, Y = value.Y };
        }

        [Test]
        public void TestCanProxy_ShouldReturnTrue_WhenImplicitOperatorExistsAndIsSetup()
        {
            IProxyProvider provider = new UserConversionProxyProvider(UserConversionProxyProviderConversionType.Implicit);

            Assert.True(provider.CanProxy<BasicStruct, ImplicitStruct>(new() { X = 2, Y = 3 }, out _));
            Assert.True(provider.CanProxy<ImplicitStruct, BasicStruct>(new() { X = 5, Y = 7 }, out _));
        }

        [Test]
        public void TestCanProxy_ShouldReturnTrue_WhenExplicitOperatorExistsAndIsSetup()
        {
            IProxyProvider provider = new UserConversionProxyProvider(UserConversionProxyProviderConversionType.Explicit);

            Assert.True(provider.CanProxy<BasicStruct, ExplicitStruct>(new() { X = 2, Y = 3 }, out _));
            Assert.True(provider.CanProxy<ExplicitStruct, BasicStruct>(new() { X = 5, Y = 7 }, out _));
        }

        [Test]
        public void TestCanProxy_ShouldReturnFalse_WhenImplicitOperatorDoesNotExistButIsSetup()
        {
            IProxyProvider provider = new UserConversionProxyProvider(UserConversionProxyProviderConversionType.Implicit);

            Assert.False(provider.CanProxy<BasicStruct, ExplicitStruct>(new() { X = 2, Y = 3 }, out _));
            Assert.False(provider.CanProxy<ExplicitStruct, BasicStruct>(new() { X = 5, Y = 7 }, out _));
        }

        [Test]
        public void TestCanProxy_ShouldReturnFalse_WhenExplicitOperatorDoesNotExistButIsSetup()
        {
            IProxyProvider provider = new UserConversionProxyProvider(UserConversionProxyProviderConversionType.Explicit);

            Assert.False(provider.CanProxy<BasicStruct, ImplicitStruct>(new() { X = 2, Y = 3 }, out _));
            Assert.False(provider.CanProxy<ImplicitStruct, BasicStruct>(new() { X = 5, Y = 7 }, out _));
        }

        [Test]
        public void TestCanProxy_ShouldReturnTrue_WhenAnyOperatorExistsAndIsSetup()
        {
            IProxyProvider provider = new UserConversionProxyProvider(UserConversionProxyProviderConversionType.Any);

            Assert.True(provider.CanProxy<BasicStruct, ImplicitStruct>(new() { X = 2, Y = 3 }, out _));
            Assert.True(provider.CanProxy<ImplicitStruct, BasicStruct>(new() { X = 5, Y = 7 }, out _));
            Assert.True(provider.CanProxy<BasicStruct, ExplicitStruct>(new() { X = 2, Y = 3 }, out _));
            Assert.True(provider.CanProxy<ExplicitStruct, BasicStruct>(new() { X = 5, Y = 7 }, out _));
        }

        [Test]
        public void TestCanProxy_ShouldReturnFalse_WhenOperatorDoesNotExist()
        {
            IProxyProvider provider = new UserConversionProxyProvider(UserConversionProxyProviderConversionType.Any);

            Assert.False(provider.CanProxy<ImplicitStruct, ExplicitStruct>(new() { X = 2, Y = 3 }, out _));
            Assert.False(provider.CanProxy<ExplicitStruct, ImplicitStruct>(new() { X = 5, Y = 7 }, out _));
        }

        [Test]
        public void TestCanProxyProcessorObtainProxy_ShouldReturnValidResults()
        {
            IProxyProvider provider = new UserConversionProxyProvider(UserConversionProxyProviderConversionType.Any);

            Assert.True(provider.CanProxy<BasicStruct, ImplicitStruct>(new() { X = 2, Y = 3 }, out var implicitStructProcessor));
            Assert.AreEqual(2, implicitStructProcessor!.ObtainProxy().X);
            Assert.AreEqual(3, implicitStructProcessor!.ObtainProxy().Y);

            Assert.True(provider.CanProxy<BasicStruct, ExplicitStruct>(new() { X = 2, Y = 3 }, out var explicitStructProcessor));
            Assert.AreEqual(2, explicitStructProcessor!.ObtainProxy().X);
            Assert.AreEqual(3, explicitStructProcessor!.ObtainProxy().Y);

            Assert.True(provider.CanProxy<ImplicitStruct, BasicStruct>(new() { X = 2, Y = 3 }, out var basicStructProcessor1));
            Assert.AreEqual(2, basicStructProcessor1!.ObtainProxy().X);
            Assert.AreEqual(3, basicStructProcessor1!.ObtainProxy().Y);

            Assert.True(provider.CanProxy<ExplicitStruct, BasicStruct>(new() { X = 2, Y = 3 }, out var basicStructProcessor2));
            Assert.AreEqual(2, basicStructProcessor2!.ObtainProxy().X);
            Assert.AreEqual(3, basicStructProcessor2!.ObtainProxy().Y);
        }
    }
}