using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RSMaster;
using RSMaster.Api;

namespace RSMaster.Tests
{
    [TestClass]
    public class RegexTests
    {
        [TestMethod]
        public void Replace_Plus_Increment_From_Email_Test()
        {
            var expect = "master@gmail.com";
            var mail = "master+1@gmail.com";

            var result = Regex.Replace(mail, @"\+.*(?=\@)", "");
            Assert.AreEqual(result, expect);
        }
    }
}
