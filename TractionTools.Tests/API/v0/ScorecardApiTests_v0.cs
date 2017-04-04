using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Api.V0;
using TractionTools.Tests.TestUtils;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using Smocks;

namespace TractionTools.Tests.API.v0 {
	[TestClass]
	public class ScorecardApiTests_v0 : BaseTest{
		[TestMethod]
		public void TestCurrentWeek() {
			var ctx = new Ctx();
			Smock.Run(context =>
			{
				context.Setup(() => DateTime.Now).Returns(new DateTime(2017, 4,3));
				var c = new WeekController();
				c.MockUser(ctx.E1);

				var week = c.Get();

				//Assert.IsTrue(week.DataContract_Weeks,
			});
		

		}
	}
}
