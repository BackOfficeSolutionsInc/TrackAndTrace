using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Api.V0;
using TractionTools.Tests.TestUtils;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using RadialReview.Utilities;
using RadialReview.Accessors;
using RadialReview.Models.Scorecard;
using System.Linq;
using RadialReview;

namespace TractionTools.Tests.API.v0 {
	[TestClass]
	public class ScorecardApiTests_v0 : BaseTest {
		[TestMethod]
		[TestCategory("Api_V0")]
		public void TestCurrentWeek() {
			var ctx = new Ctx();
			var c = new WeekController();
			c.MockUser(ctx.E1);

			var week = c.Get();

			var actualWeek = TimingUtility.GetWeekSinceEpoch(DateTime.UtcNow) + 1;

			Assert.AreEqual(week.DataContract_Weeks, actualWeek);

		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public void GetScore() {
			var ctx = new Ctx();
			var m1 = new MeasurableModel() {
				AccountableUserId = ctx.E1.Id,
				AdminUserId = ctx.E1.Id,
				Title = "Meas1",
				OrganizationId = ctx.Org.Organization.Id
			};
			ScorecardAccessor.CreateMeasurable(ctx.Manager, m1 , false);

			var score = L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2000L, (decimal?)null,null);

			var c = new ScoresController();
			ctx.E1._ClientTimestamp = DateTime.UtcNow.ToJavascriptMilliseconds();
			ctx.Manager._ClientTimestamp = DateTime.UtcNow.ToJavascriptMilliseconds();
			c.MockUser(ctx.E1);

			
			var s = c.Get(score.Id);

			Assert.AreEqual(s.Measurable.Title, m1.Title);
			Assert.AreEqual(s.Value, null);


			score = L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2000L, 3.14m, null);

			var s2 = c.Get(score.Id);
			Assert.AreEqual(s2.Value, 3.14m);

		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public void PutScoreWeek() {
			var ctx = new Ctx();
			var m1 = new MeasurableModel() {
				AccountableUserId = ctx.E1.Id,
				AdminUserId = ctx.E1.Id,
				Title = "Meas1",
				OrganizationId = ctx.Org.Organization.Id
			};
			ScorecardAccessor.CreateMeasurable(ctx.Manager, m1, false);
			

			var c = new ScoresController();
			c.MockUser(ctx.E1);

			c.Put(m1.Id, 2000L, null);
			var score = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(score.Measured, null);

			c.Put(m1.Id, 2000L, 3.14m);
			score = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(score.Measured, 3.14m);

			c.Put(m1.Id, 2001L, 6.14m);
			score = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(score.Measured, 3.14m);
			score = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2001L);
			Assert.AreEqual(score.Measured, 6.14m);

		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public void PutScore() {
			MockHttpContext();
			var ctx = new Ctx();

			var m1 = new MeasurableModel() {
				AccountableUserId = ctx.E1.Id,
				AdminUserId = ctx.E1.Id,
				Title = "Meas1",
				OrganizationId = ctx.Org.Organization.Id
			};
			ScorecardAccessor.CreateMeasurable(ctx.Manager, m1, false);

			var score = L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2000L, (decimal?)null, null);

			var c = new ScoresController();

			c.MockUser(ctx.E1);
			c.Put(score.Id,null);
			var s = ScorecardAccessor.GetScore(ctx.Manager, score.Id);
			Assert.AreEqual(score.Measured, null);

			c.MockUser(ctx.E1);
			c.Put(score.Id, 3.14m);
			s = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(s.Measured, 3.14m);


			var score2 = L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2001L, (decimal?)6.14, null);
			
			s = ScorecardAccessor.GetScore(ctx.Manager, score.Id);
			Assert.AreEqual(s.Measured, 3.14m);
			s = ScorecardAccessor.GetScore(ctx.Manager, score2.Id);
			Assert.AreEqual(s.Measured, 6.14m);

		}

	}
}
