﻿using System;
using System.Diagnostics.CodeAnalysis;

using Xunit;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClient.UnitTests
{
    [ExcludeFromCodeCoverage]
    public class TestHttpWebClientContainer : IDisposable
    {
        public interface ITestInterface
        {
        }

        public class TestClass : ITestInterface
        {
            private string _value;

            public TestClass() { }
            public TestClass(string value) { _value = value; }
            public TestClass(string v1, string v2) { _value = v1 + v2; }
            public TestClass(string v1, string v2, string v3) { _value = v1 + v2 + v3; }
            public TestClass(string v1, string v2, string v3, string v4) { _value = v1 + v2 + v3 + v4; }
            public TestClass(string v1, string v2, string v3, string v4, string v5) { _value = v1 + v2 + v3 + v4 + v5; }

            public override string ToString()
            {
                return _value;
            }
        }

        public void Dispose()
        {
            HttpWebClientContainer.Clear();
        }

        [Fact]
        public void TestResolveString()
        {
            HttpWebClientContainer.Register<object, string>();

            var result = HttpWebClientContainer.Resolve<object>('x', 10);
            Assert.IsType(typeof(string), result);
            Assert.Equal("xxxxxxxxxx", result.ToString());
        }

        [Fact]
        public void TestResolveInterface()
        {
            HttpWebClientContainer.Register<ITestInterface, TestClass>();

            var result = HttpWebClientContainer.Resolve<ITestInterface>();
            Assert.IsType(typeof(TestClass), result);
            Assert.Null(result.ToString());
        }

        [Fact]
        public void TestResolveInterfaceWithDelegate()
        {
            HttpWebClientContainer.Register<ITestInterface>(() => { return new TestClass(); });

            var result = HttpWebClientContainer.Resolve<ITestInterface>();
            Assert.IsType(typeof(TestClass), result);
            Assert.Null(result.ToString());
        }

        [Fact]
        public void TestResolveInterfaceWithDelegateWithOneParameter()
        {
            HttpWebClientContainer.Register<ITestInterface>((v) => { return new TestClass((string)v); });

            var result = HttpWebClientContainer.Resolve<ITestInterface>("hello");
            Assert.IsType(typeof(TestClass), result);
            Assert.Equal("hello", result.ToString());
        }

        [Fact]
        public void TestResolveInterfaceWithDelegateWithTwoParameters()
        {
            HttpWebClientContainer.Register<ITestInterface>((v1, v2) => { return new TestClass((string)v1, (string)v2); });

            var result = HttpWebClientContainer.Resolve<ITestInterface>("1", "2");
            Assert.IsType(typeof(TestClass), result);
            Assert.Equal("12", result.ToString());
        }

        [Fact]
        public void TestResolveInterfaceWithDelegateWithThreeParameters()
        {
            HttpWebClientContainer.Register<ITestInterface>((v1, v2, v3) => { return new TestClass((string)v1, (string)v2, (string)v3); });

            var result = HttpWebClientContainer.Resolve<ITestInterface>("1", "2", "3");
            Assert.IsType(typeof(TestClass), result);
            Assert.Equal("123", result.ToString());
        }

        [Fact]
        public void TestResolveInterfaceWithDelegateWithFourParameters()
        {
            HttpWebClientContainer.Register<ITestInterface>((v1, v2, v3, v4) => { return new TestClass((string)v1, (string)v2, (string)v3, (string)v4); });

            var result = HttpWebClientContainer.Resolve<ITestInterface>("1", "2", "3", "4");
            Assert.IsType(typeof(TestClass), result);
            Assert.Equal("1234", result.ToString());
        }

        [Fact]
        public void TestResolveInterfaceWithDelegateWithFiveParameters()
        {
            HttpWebClientContainer.Register<ITestInterface>((v1, v2, v3, v4, v5) => { return new TestClass((string)v1, (string)v2, (string)v3, (string)v4, (string)v5); });

            var result = HttpWebClientContainer.Resolve<ITestInterface>("1", "2", "3", "4", "5");
            Assert.IsType(typeof(TestClass), result);
            Assert.Equal("12345", result.ToString());
        }

        [Fact]
        public void TestResolveWithMissingType()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => HttpWebClientContainer.Resolve<int>());
            Assert.Throws<ArgumentOutOfRangeException>(() => HttpWebClientContainer.Resolve<int>(42));
        }
    }
}
