using System;
using RadialReview.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;
using TractionTools.Tests.TestUtils;

namespace TractionTools.Tests.Controllers {
    [TestClass]
    public class PasswordTests : BaseTest{
        [TestMethod]
        public void TestSpecialCharacter() {
            //All types
            Assert.IsFalse(Regex.IsMatch("!", PasswordConstants.PasswordRegex));
            Assert.IsFalse(Regex.IsMatch("a", PasswordConstants.PasswordRegex));
            Assert.IsFalse(Regex.IsMatch("A", PasswordConstants.PasswordRegex));
            Assert.IsFalse(Regex.IsMatch("aA", PasswordConstants.PasswordRegex));
            Assert.IsFalse(Regex.IsMatch("a!", PasswordConstants.PasswordRegex));
            Assert.IsFalse(Regex.IsMatch("Aa", PasswordConstants.PasswordRegex));
            Assert.IsFalse(Regex.IsMatch("A!", PasswordConstants.PasswordRegex));
            Assert.IsFalse(Regex.IsMatch("!a", PasswordConstants.PasswordRegex));
            Assert.IsFalse(Regex.IsMatch("!A", PasswordConstants.PasswordRegex));

            //Ordering
            Assert.IsTrue(Regex.IsMatch("aA!", PasswordConstants.PasswordRegex));
            Assert.IsTrue(Regex.IsMatch("!aA", PasswordConstants.PasswordRegex));
            Assert.IsTrue(Regex.IsMatch("a!A", PasswordConstants.PasswordRegex));
            Assert.IsTrue(Regex.IsMatch("A!a", PasswordConstants.PasswordRegex));

            //Test all special characters
            var specialChars = new[] {
                "`", "~", "!", "@", "#", "$", "%", "^", "&",
                "*", "(", ")", "_", "|", "+", "\\", "-", "=",
                "?", ";", ":", "'", "\"", ",", ".", "<", ">", 
                "{", "}","[","]","\\","/"
            };
            foreach (var sc in specialChars) {
                Assert.IsTrue(Regex.IsMatch("Aa"+sc, PasswordConstants.PasswordRegex),"Did not work with '"+sc+"'");
            }


        }
    }
}
