using System;
using System.Text;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using RipcordSoftware.HttpWebClient;

namespace HttpWebClient.UnitTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TestHttpWebClientHeaders
    {
        [TestMethod]
        public void TestDefaultState()
        {
            var expectedHeaderText = "GET / HTTP/1.1\r\nHost: localhost:80\r\n\r\n";

            var headers = new HttpWebClientHeaders();
            Assert.AreEqual("GET", headers.Method);
            Assert.AreEqual("localhost", headers.Hostname);
            Assert.AreEqual(80, headers.Port);
            Assert.AreEqual("/", headers.Uri);
            Assert.IsFalse(headers.Secure);
            Assert.IsNull(headers.ContentLength);
            Assert.AreEqual(expectedHeaderText, headers.GetHeaders());
            Assert.IsTrue(Encoding.ASCII.GetBytes(expectedHeaderText).SequenceEqual(headers.GetHeaderBytes()));
        }

        [TestMethod]
        public void TestNonDefaultState()
        {
            var expectedHeaderText = "POST /someuri HTTP/1.1\r\nHost: somehost:8080\r\nContent-Length: 1024\r\n\r\n";

            var headers = new HttpWebClientHeaders();
            headers.Method = "POST";
            headers.Hostname = "somehost";
            headers.Port = 8080;
            headers.Uri = "/someuri";
            headers.ContentLength = 1024;

            Assert.AreEqual("POST", headers.Method);
            Assert.AreEqual("somehost", headers.Hostname);
            Assert.AreEqual(8080, headers.Port);
            Assert.AreEqual("/someuri", headers.Uri);
            Assert.IsFalse(headers.Secure);
            Assert.AreEqual(1024, headers.ContentLength.Value);
            Assert.AreEqual(expectedHeaderText, headers.GetHeaders());
            Assert.IsTrue(Encoding.ASCII.GetBytes(expectedHeaderText).SequenceEqual(headers.GetHeaderBytes()));
        }

        [TestMethod]
        public void TestNonDefaultByIndexerState()
        {
            var expectedHeaderText1 = "GET / HTTP/1.1\r\nHost: localhost:80\r\nMy-Header: somevalue\r\n\r\n";
            var expectedHeaderText2 = "GET / HTTP/1.1\r\nHost: localhost:80\r\n\r\n";
            var expectedHeaderText3 = "GET / HTTP/1.1\r\nHost: localhost:80\r\nMy-Header1: somevalue1\r\nMy-Header2: somevalue2\r\n\r\n";
            var expectedHeaderText4 = "GET / HTTP/1.1\r\nHost: localhost:80\r\nMy-Header2: somevalue2\r\n\r\n";
            var expectedHeaderText5 = "GET / HTTP/1.1\r\nHost: localhost:80\r\nMy-Header2: updatedsomevalue2\r\n\r\n";

            var headers = new HttpWebClientHeaders();
            headers["My-Header"] = "somevalue";

            Assert.AreEqual("GET", headers.Method);
            Assert.AreEqual("localhost", headers.Hostname);
            Assert.AreEqual(80, headers.Port);
            Assert.AreEqual("/", headers.Uri);
            Assert.IsFalse(headers.Secure);
            Assert.IsNull(headers.ContentLength);
            Assert.AreEqual("somevalue", headers["My-Header"]);
            Assert.AreEqual(expectedHeaderText1, headers.GetHeaders());
            Assert.IsTrue(Encoding.ASCII.GetBytes(expectedHeaderText1).SequenceEqual(headers.GetHeaderBytes()));

            headers["My-Header"] = null;
            Assert.IsNull(headers["My-Header"]);
            Assert.AreEqual(expectedHeaderText2, headers.GetHeaders());
            Assert.IsTrue(Encoding.ASCII.GetBytes(expectedHeaderText2).SequenceEqual(headers.GetHeaderBytes()));

            headers["My-Header1"] = "somevalue1";
            headers["My-Header2"] = "somevalue2";
            Assert.AreEqual(expectedHeaderText3, headers.GetHeaders());
            Assert.IsTrue(Encoding.ASCII.GetBytes(expectedHeaderText3).SequenceEqual(headers.GetHeaderBytes()));

            headers["My-Header1"] = null;
            Assert.AreEqual(expectedHeaderText4, headers.GetHeaders());
            Assert.IsTrue(Encoding.ASCII.GetBytes(expectedHeaderText4).SequenceEqual(headers.GetHeaderBytes()));

            headers["My-Header2"] = "updatedsomevalue2";
            Assert.AreEqual(expectedHeaderText5, headers.GetHeaders());
            Assert.IsTrue(Encoding.ASCII.GetBytes(expectedHeaderText5).SequenceEqual(headers.GetHeaderBytes()));
        }

    }
}
