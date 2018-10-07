using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities;
using System.Collections.Generic;

namespace TractionTools.Tests.Algorithms {
	[TestClass]
	public class JenksFischerBreakTests {
		[TestMethod]
		public void TestBreaks() {
			var arr = new List<double> { 2.0, 2.0, 2.0, 2.0, 20, 20, 20, 20 };
			var result = JenksFisher.CreateJenksFisherBreaksArray(arr, 1);
			arr = new List<double> { 2.0, 3.0, 4.0, 5.0, 20, 21, 22, 23 };
			result = JenksFisher.CreateJenksFisherBreaksArray(arr, 2);
			int a = 0;
		}
	}
}
