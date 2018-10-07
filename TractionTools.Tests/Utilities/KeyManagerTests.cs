using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities.Constants;
using Newtonsoft.Json;

namespace TractionTools.Tests.Utilities {
    [TestClass]
    public class KeyManagerTests {
        [TestMethod]
        public void TestParseKey() {
            var keys = new KeyManager.Key("a",null,"{username:'uname',password:'pass'}");

            var o = keys.GetJsonValue("username");
            Assert.IsTrue(o == "uname");
            o = keys.GetJsonValue("password");
            Assert.IsTrue(o == "pass");


        }
    }
}
