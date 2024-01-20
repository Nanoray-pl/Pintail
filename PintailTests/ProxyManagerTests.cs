using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Nanoray.Pintail.Tests.Consumer;
using Nanoray.Pintail.Tests.Private;
using Nanoray.Pintail.Tests.Provider;
using NUnit.Framework;

namespace Nanoray.Pintail.Tests
{
    [TestFixture]
    public class ProxyManagerTests
    {
        private ProxyManager<Nothing> CreateProxyManager(ProxyManagerConfiguration<Nothing>? configuration = null)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"Nanoray.Pintail.Proxies, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule($"Proxies");
            var manager = new ProxyManager<Nothing>(moduleBuilder, configuration);
            return manager;
        }

        [Test]
        public void TestSuccessfulBasicApi()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new SimpleProviderApi();

            var consumerApi = manager.ObtainProxy<ISimpleConsumerApi>(providerApi)!;
            Assert.DoesNotThrow(() => consumerApi.VoidMethod());
            Assert.AreEqual(123, consumerApi.IntMethod(123));
            Assert.AreEqual(144, consumerApi.DefaultMethod(12));
            Assert.AreEqual(42, consumerApi.IntProperty);
            Assert.AreEqual("asdf", consumerApi["asdf"]);
            Assert.AreEqual(5, consumerApi.MapperMethod("word.", (t) => t.Length));
        }

        [Test]
        public void TestOutNonProxyApi()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new SimpleProviderApi();

            var consumerApi = manager.ObtainProxy<ISimpleConsumerApi>(providerApi)!;
            consumerApi.OutIntMethod(out int num);
            Assert.AreEqual(1, num);
            consumerApi.OutObjectMethod(out object? obj);
            Assert.IsNotNull(obj);
            Assert.AreEqual(obj!.GetType(), typeof(StringBuilder));
        }

        [Test]
        public void TestRefNonProxyApi()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new SimpleProviderApi();

            int num = 0;
            object? obj = null;
            var consumerApi = manager.ObtainProxy<ISimpleConsumerApi>(providerApi)!;
            consumerApi.RefIntMethod(ref num);
            Assert.AreEqual(1, num);
            consumerApi.RefObjectMethod(ref obj);
            Assert.IsNotNull(obj);
            Assert.AreEqual(obj!.GetType(), typeof(StringBuilder));
        }

        /*
        [Test]
        public void TestIsAssignable()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApi();

            var consumerApi = manager.ObtainProxy<IConsumerApi>(providerApi)!;
            object? obj = null;
            Assert.DoesNotThrow(() => obj = consumerApi.IsAssignableTest("testing"));
            Assert.AreEqual("testing", obj);
        }*/

        [Test]
        public void TestEnum()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            Assert.AreEqual(Consumer.StateEnum.State1, consumerApi.GetStateEnum());
        }

        [Test]
        public void TestOutEnum()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            consumerApi.GetOutStateEnum(out Consumer.StateEnum state);
            Assert.AreEqual(Consumer.StateEnum.State1, state);
        }

        [Test]
        public void TestReturnSameEnum()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var state = consumerApi.GetSameEnumState(Consumer.StateEnum.State2);
            Assert.AreEqual(Consumer.StateEnum.State2, state);
        }

        [Test]
        public void TestReturnArray()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.GetArray();
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("0", result[0].Text);
        }

        [Test]
        public void TestReturnJaggedArray()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.GetJaggedArray();
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1, result[0].Length);
            Assert.AreEqual("0", result[0][0].Text);
        }

        [Test]
        public void TestReturn2DArray()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.Get2DArray();
            Assert.AreEqual(1, result.GetLength(0));
            Assert.AreEqual(2, result.GetLength(1));
            Assert.AreEqual("0, 0", result[0, 0].Text);
            Assert.AreEqual("0, 1", result[0, 1].Text);
        }

        [Test]
        public void TestMatchingArrayParameter()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var input = new Consumer.IApiResult[] { new Consumer.ApiResult("0"), new Consumer.ApiResult("1") };
            consumerApi.ArrayMethod(input);
            Assert.AreEqual(2, input.Length);
            Assert.AreEqual("modified", input[0].Text);
            Assert.AreEqual("1", input[1].Text);
        }

        [Test]
        public void TestMismatchedArrayParameterAndThrow()
        {
            var manager = this.CreateProxyManager(new(
                mismatchedArrayMappingBehavior: ProxyManagerMismatchedArrayMappingBehavior.Throw
            ));
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var input = new Consumer.ApiResult[] { new Consumer.ApiResult("0"), new Consumer.ApiResult("1") };
            Assert.Throws<ArgumentException>(() => consumerApi.ArrayMethod(input));
        }

        [Test]
        public void TestMismatchedArrayParameterAndAllowAndDontMapBack()
        {
            var manager = this.CreateProxyManager(new(
                mismatchedArrayMappingBehavior: ProxyManagerMismatchedArrayMappingBehavior.AllowAndDontMapBack
            ));
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var input = new Consumer.ApiResult[] { new Consumer.ApiResult("0"), new Consumer.ApiResult("1") };
            consumerApi.ArrayMethod(input);
            Assert.AreEqual(2, input.Length);
            Assert.AreEqual("0", input[0].Text);
            Assert.AreEqual("1", input[1].Text);
        }

        [Test]
        public void TestReturnNullableObject()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.GetNullableObject("asdf");
            Assert.IsNotNull(result);
            Assert.AreEqual("asdf", result!.Text);
        }

        [Test]
        public void TestReturnNullableEnum()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.GetNullableEnum();
            Assert.IsNotNull(result);
            Assert.AreEqual(Consumer.StateEnum.State0, result!.Value);
        }

        [Test]
        public void TestReturnNullEnum()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.GetNullEnum();
            Assert.IsNull(result);
        }

        [Test]
        public void TestReturnValueTuple()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.GetValueTuple((new Consumer.ApiResult("asdf"), Consumer.StateEnum.State2));
            Assert.AreEqual("asdf", result.Item1.Text);
            Assert.AreEqual(Consumer.StateEnum.State2, result.Item2);
        }

        [Test]
        public void TestReturnTuple()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.GetTuple(Tuple.Create<Consumer.IApiResult, Consumer.StateEnum>(new Consumer.ApiResult("asdf"), Consumer.StateEnum.State2));
            Assert.AreEqual("asdf", result.Item1.Text);
            Assert.AreEqual(Consumer.StateEnum.State2, result.Item2);
        }

        [Test]
        public void TestReturnList()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.GetList();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("0", result[0].Text);
        }

        [Test]
        public void TestOutParameters()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            consumerApi.GetOutResult("testing", out Consumer.IApiResult result);
            Assert.AreEqual("testing", result.Text);
        }

        [Test]
        public void TestInputOutputApi()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            {
                Consumer.IApiResult input = new Consumer.ApiResult("input");
                Consumer.IApiResult output = consumerApi.GetSameResult(input);
                Assert.AreEqual(input.Text, output.Text);
                Assert.IsTrue(ReferenceEquals(input, output));
            }

            {
                Consumer.IApiResult input = new Consumer.ApiResult("input");
                Consumer.IApiResult output = consumerApi.GetModifiedResult(input);
                Assert.AreEqual(input.Text, output.Text);
                Assert.IsFalse(ReferenceEquals(input, output));
            }
        }

        [Test]
        public void TestComplexType()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();

            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;
            var result = consumerApi.GetComplexType();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(Consumer.StateEnum.State0, result.Keys.ToList()[0]);
            Assert.AreEqual(1, result[Consumer.StateEnum.State0].Count);
            Assert.AreEqual("0", result[Consumer.StateEnum.State0].ToList()[0].Text);
        }

        [Test]
        public void TestSystemDelegates()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            var result1 = new Consumer.ApiResult("asdf");
            var result2 = consumerApi.GetMapper()(result1);
            Assert.AreEqual(result1.Text, result2.Text);
            Assert.AreEqual(result1, result2);

            consumerApi.SetMapper(r => new Consumer.ApiResult($"{r.Text}{r.Text}"));
            var result3 = consumerApi.GetMapper()(result2);
            Assert.AreEqual($"{result2.Text}{result2.Text}", result3.Text);
            Assert.AreNotEqual(result2, result3);
        }

        [Test]
        public void TestCustomGenericDelegate()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            consumerApi.SetCustomDelegate(list => new Consumer.ApiResult($"{string.Join(" ", list.Select(r => r.Text))}"));
            var result = consumerApi.CallCustomDelegate(new List<Consumer.ApiResult> { new("42"), new("asdf") });

            Assert.AreEqual("42 asdf", result.Text);
        }

        [Test]
        public void TestCustomGenericOutDelegate()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            consumerApi.GetCustomOutDelegate()(out Consumer.StateEnum result1);
            Assert.AreEqual(Consumer.StateEnum.State0, result1);
            consumerApi.SetCustomOutDelegate((out Consumer.StateEnum p) => p = Consumer.StateEnum.State2);
            consumerApi.GetCustomOutDelegate()(out Consumer.StateEnum result2);
            Assert.AreEqual(Consumer.StateEnum.State2, result2);
        }

        [Test]
        public void TestTryProxy()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new SimpleProviderApi();
            Assert.IsTrue(manager.TryProxy(providerApi, out ISimpleConsumerApi? consumerApi));
            Assert.NotNull(consumerApi);
        }

        [Test]
        public void TestNonExistentApiMethodByThrowingOnPrepare()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowExceptionNoMatchingMethodHandler
            ));
            var providerApi = new SimpleProviderApi();
            Assert.Throws<ArgumentException>(() => manager.ObtainProxy<IInvalidConsumerApi>(providerApi));
        }

        [Test]
        public void TestNonExistentApiMethodByThrowingImplementation()
        {
            var manager = this.CreateProxyManager(new(
                noMatchingMethodHandler: ProxyManagerConfiguration<Nothing>.ThrowingImplementationNoMatchingMethodHandler
            ));
            var providerApi = new SimpleProviderApi();
            IInvalidConsumerApi consumerApi = null!;
            Assert.DoesNotThrow(() => consumerApi = manager.ObtainProxy<IInvalidConsumerApi>(providerApi)!);
            Assert.Throws<NotImplementedException>(() => consumerApi.NonExistentApiMethod());
        }

        [Test]
        public void TestMarkerInterfaceWithoutProperty()
        {
            var manager = this.CreateProxyManager(new(
                proxyObjectInterfaceMarking: ProxyObjectInterfaceMarking.Marker
            ));
            var providerApi = new SimpleProviderApi();
            var consumerApi = manager.ObtainProxy<ISimpleConsumerApi>(providerApi)!;
            Assert.IsTrue(consumerApi is IProxyObject);
            Assert.IsFalse(consumerApi is IProxyObject.IWithProxyTargetInstanceProperty);
        }

        [Test]
        public void TestMarkerInterfaceWithProperty()
        {
            var manager = this.CreateProxyManager(new(
                proxyObjectInterfaceMarking: ProxyObjectInterfaceMarking.MarkerWithProperty
            ));
            var providerApi = new SimpleProviderApi();
            var consumerApi = manager.ObtainProxy<ISimpleConsumerApi>(providerApi)!;
            Assert.IsTrue(consumerApi is IProxyObject);
            Assert.IsTrue(consumerApi is IProxyObject.IWithProxyTargetInstanceProperty);
            Assert.IsTrue(ReferenceEquals(providerApi, ((IProxyObject.IWithProxyTargetInstanceProperty)consumerApi).ProxyTargetInstance));
        }

        [Test]
        public void TestGMCM()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            consumerApi.RegisterSimpleOption(new Consumer.ApiResult(""), "optionName", "optionDesc", () => true, (b) => { });
            consumerApi.RegisterSimpleOption(new Consumer.ApiResult(""), "optionName", "optionDesc", () => "value", (s) => { });
        }

        [Test]
        public void TestComplexGenericMethod()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            var result = consumerApi.ComplexGenericMethod<string>("");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestEnumConstrainedGenericMethod()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            var result = consumerApi.EnumConstrainedGenericMethod<ProxyManagerEnumMappingBehavior>("Strict");
            Assert.AreEqual(ProxyManagerEnumMappingBehavior.Strict, result);
        }

        [Test]
        public void TestConstructorConstrainedGenericMethod()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            var result = consumerApi.ConstructorConstrainedGenericMethod<List<string>>();
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestStringEvent()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            string? output = null;
            consumerApi.StringEvent += (s) => output = s;
            Assert.IsNull(output);

            consumerApi.FireStringEvent("test");
            Assert.AreEqual("test", output);
        }

        [Test]
        public void TestApiResultEvent()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            Consumer.IApiResult? output = null;
            consumerApi.ApiResultEvent += (v) => output = v;
            Assert.IsNull(output);

            Consumer.IApiResult input = new Consumer.ApiResult("test");
            consumerApi.FireApiResultEvent(input);
            Assert.IsNotNull(output);
            Assert.AreSame(input, output);
            Assert.AreEqual("test", output!.Text);
        }

        [Test]
        public void TestGenericInterfaceMultipleProxies()
        {
            var manager = this.CreateProxyManager();
            var stringProviderApi = new SimpleProviderApi<string>();
            var intProviderApi = new SimpleProviderApi<int>();
            var objectProviderApi = new SimpleProviderApi<object>();

            var stringConsumerApi = manager.ObtainProxy<ISimpleConsumerApi<string>>(stringProviderApi)!;
            stringConsumerApi.SetValue("asdf");
            Assert.AreEqual("asdf", stringConsumerApi.GetValue());

            var intConsumerApi = manager.ObtainProxy<ISimpleConsumerApi<int>>(intProviderApi)!;
            intConsumerApi.SetValue(13);
            Assert.AreEqual(13, intConsumerApi.GetValue());

            var objectConsumerApi = manager.ObtainProxy<ISimpleConsumerApi<object>>(objectProviderApi)!;
            var @object = new StringBuilder();
            objectConsumerApi.SetValue(@object);
            Assert.AreSame(@object, objectConsumerApi.GetValue());
        }

        [Test]
        public void TestSimpleOverloadedMethods()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new SimpleProviderApiWithOverloads();
            var consumerApi = manager.ObtainProxy<ISimpleConsumerApiWithOverloads>(providerApi)!;
            Type baseType = consumerApi.MethodWithOverload(new object());
            Assert.AreSame(baseType, typeof(object));

            Type valueType = consumerApi.MethodWithOverload(1);
            Assert.AreEqual(valueType, typeof(int));

            Type referenceType = consumerApi.MethodWithOverload(new StringBuilder());
            Assert.AreEqual(referenceType, typeof(StringBuilder));

            Type enumType = consumerApi.MethodWithOverload(DayOfWeek.Sunday);
            Assert.AreEqual(enumType, typeof(DayOfWeek));

            Type outTestType = consumerApi.MethodWithOverload(out int test);
            Assert.AreEqual(outTestType, typeof(int));
            Assert.AreEqual(test, 5);

            string intTest = consumerApi.MethodWithOverload(5.0);
            Assert.AreEqual(intTest, "5");
        }

        [Test]
        public void TestComplexOverloadedMethods()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApiWithOverloads();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApiWithOverloads>(providerApi)!;
            ProxiedInput proxiedInput = new("HIIIIII!");
            ProxiedInput2 proxiedInput2 = new("BYEEEEE!");

            string proxied = consumerApi.MethodWithOverload(proxiedInput);
            Assert.AreEqual(proxied, "proxy");

            string callback = consumerApi.MethodWithOverload(() => proxiedInput);
            Assert.AreEqual(callback, proxiedInput.teststring);

            string othercallback = consumerApi.MethodWithOverload(() => proxiedInput2);
            Assert.AreEqual(othercallback, proxiedInput2.otherteststring);
        }

        [Test]
        public void TestArrayOverloadedMethods()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApiWithOverloads();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApiWithOverloads>(providerApi)!;

            string localbuilders = consumerApi.MethodWithArrayOverload(Array.Empty<LocalBuilder>());
            Assert.AreEqual("LocalBuilder array!", localbuilders);

            string localvars = consumerApi.MethodWithArrayOverload(Array.Empty<LocalVariableInfo>());
            Assert.AreEqual("LocalVariableInfo array!", localvars);

            string ints = consumerApi.MethodWithArrayOverload(new[] { 1, 2, 3 });
            Assert.AreEqual("int array!", ints);

            string proxied = consumerApi.MethodWithArrayOverload(Array.Empty<Consumer.IProxiedInput>());
            Assert.AreEqual("proxied array!", proxied);
        }

        [Test]
        public void TestOverloadsWithGenerics()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApiWithOverloadsWithGenerics();
            var consumerApi = manager.ObtainProxy<IConsumerApiWithOverloadsWithGenerics>(providerApi)!;

            InputWithGeneric<string> stringGeneric = new();
            InputWithTwoGenerics<string, int> twoGenerics = new();

            Assert.AreEqual("string?", consumerApi.MethodWithOverload(stringGeneric));
            Assert.AreEqual("string, int", consumerApi.MethodWithOverload(twoGenerics));
        }

        [Test]
        public void TestProxiedInputOverloads()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderWithTwoProxiedInputs();
            var consumerApi = manager.ObtainProxy<IConsumerWithTwoProxiedInputs>(providerApi)!;

            ProxyInputA a = new("HI!");
            ProxyInputB b = new("BYE!");

            Assert.DoesNotThrow(() => consumerApi.MethodWithNoOverload(a));
            Assert.DoesNotThrow(() => consumerApi.MethodWithTwoInputs(a, b));

            Assert.AreEqual(a.hi, consumerApi.MethodWithProxiedOverload(a));
            Assert.AreEqual(b.bye, consumerApi.MethodWithProxiedOverload(b));
        }

        [Test]
        public void TestProxiedOverrides()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApiWithOverrides();
            var consumerApi = manager.ObtainProxy<IConsumerApiWithOverrides>(providerApi)!;

            Assert.AreEqual("BASESTRING", consumerApi.MethodWithOverride());
            Assert.AreEqual("heya", consumerApi.MethodWithoutOverride());

            var overriddenProviderApi = new ProviderApiWithOverridesMeow();
            var overriddenConsumerApi = manager.ObtainProxy<IConsumerApiWithOverrides>(overriddenProviderApi)!;

            Assert.AreEqual("MEOW", overriddenConsumerApi.MethodWithOverride());
            Assert.AreEqual("heya", overriddenConsumerApi.MethodWithoutOverride());
        }

        [Test]
        public void TestComplexProxiedOverrides()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderWithComplexProxiedInputs();
            var consumerApi = manager.ObtainProxy<IConsumerApiWithComplexProxiedInputs>(providerApi)!;

            ProxyInputA a = new("HI!");
            ProxyInputB b = new("BYE!");

            Assert.AreEqual(a.hi, consumerApi.MethodWithProxiedOverload(() => a));
            Assert.AreEqual(b.bye, consumerApi.MethodWithProxiedOverload(() => b));

            int x = 4;
            consumerApi.FancyEvent += (Consumer.IProxyInputA a) => x = 5;
            consumerApi.FireEvent(a);
            Assert.AreEqual(5, x);
        }

        [Test]
        public void TestFluentApi()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new SimpleFluentProviderApi();
            var consumerApi = manager.ObtainProxy<ISimpleConsumerFluentAPI>(providerApi)!;

            consumerApi.state = 5;
            Assert.AreEqual(consumerApi, consumerApi.method());
            Assert.AreEqual(10, consumerApi.state);
            Assert.AreEqual(1337, consumerApi.GetOtherState());

            var otherconsumer = manager.ObtainProxy<ISimpleConsumerFluentAPI>(providerApi)!;
            otherconsumer.state = 7;
            Assert.AreEqual(7, otherconsumer.state);
            // Assert.AreEqual(10, consumerApi.state); // currently fails.
        }

        [Test]
        public void TestWithAbstractClass()
        {
            var manager = this.CreateProxyManager(new(
                proxyObjectInterfaceMarking: ProxyObjectInterfaceMarking.MarkerWithProperty
            ));
            var providerApi = new ATestClassImpl();

            var consumerApi = manager.ObtainProxy<IATestClass>(providerApi)!;

            //manager.TryProxy<IATestClass>(providerApi, out var consumerApi);

            Assert.AreEqual("Hi!", consumerApi.Name);
            Assert.AreEqual("sigh", consumerApi.inner[0].sigh);

            manager.TryProxy(providerApi, out consumerApi);

            Assert.AreEqual("Hi!", consumerApi!.Name);
            Assert.AreEqual("sigh", consumerApi.inner[0].sigh);
        }

        [Test]
        public void TestNestedCalls()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new Nesting.Provider();
            var consumerApi = manager.ObtainProxy<Nesting.Consumer>(providerApi)!;

            Assert.AreEqual("lorem ipsum", consumerApi!.Text);
            Assert.AreEqual("lorem ipsum", consumerApi!.Inner.Text);
            Assert.AreEqual("lorem ipsum", consumerApi!.Inner.Inner.Text);
            Assert.AreEqual("lorem ipsum", consumerApi!.Inner.Inner.Inner.Text);
        }

        [Test]
        public void TestKeyValuePairWithProxiedValue()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ComplexProviderApi();
            var consumerApi = manager.ObtainProxy<IComplexConsumerApi>(providerApi)!;

            var kvp = consumerApi.GetKeyValuePairWithProxiedValue(true, "asdf");
            Assert.AreEqual(true, kvp.Key);
            Assert.AreEqual("asdf", kvp.Value.teststring);
        }

        [Test]
        public void TestDefaultMethodsInReverseProxy()
        {
            var manager = this.CreateProxyManager();
            var providerApi = new ProviderApiWithDefaultMethods();
            Assert.DoesNotThrow(() =>
            {
                var consumerApi = manager.ObtainProxy<IConsumerApiWithDefaultMethods>(providerApi);
                var hook = new IConsumerApiWithDefaultMethods.HookImplementation();
                consumerApi.SetHook(hook);
                Assert.AreEqual(456, consumerApi.CallHookNewMethod(hook));
                Assert.AreEqual(456, providerApi.CallHookNewMethod());
            });
        }
    }
}
