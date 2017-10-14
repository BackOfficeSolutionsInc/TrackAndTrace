using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Api.V1;
using TractionTools.Tests.TestUtils;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using RadialReview.Utilities;
using RadialReview.Accessors;
using RadialReview.Models.Scorecard;
using System.Linq;
using RadialReview;
using System.Threading.Tasks;
using TractionTools.Tests.Properties;

namespace TractionTools.Tests.API.v1 {
	[TestClass]
	public class ScorecardApiTests_v1 : BaseApiTest {
		public ScorecardApiTests_v1() : base(VERSION_1) { }

		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task TestCurrentWeek() {
			var ctx = await Ctx.Build();
			var c = new Week_Controller();
			c.MockUser(ctx.E1);
			var week = c.Get();
			var actualWeek = TimingUtility.GetWeekSinceEpoch(DateTime.UtcNow.AddMinutes(+ctx.Org.Organization.GetTimezoneOffset())) + 1;
			Assert.AreEqual(week.ForWeekNumber, actualWeek);
		}


		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task GetScore() {
			var ctx = await Ctx.Build();
			var m1 = new MeasurableModel() {
				AccountableUserId = ctx.E1.Id,
				AdminUserId = ctx.E1.Id,
				Title = "Meas1",
				OrganizationId = ctx.Org.Organization.Id
			};
			MockHttpContext();
			await ScorecardAccessor.CreateMeasurable(ctx.Manager, m1 , false);
			var score = await L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2000L, (decimal?)null,null);

			var c = new Scores_Controller();
			ctx.E1._ClientTimestamp = DateTime.UtcNow.ToJavascriptMilliseconds();
			ctx.Manager._ClientTimestamp = DateTime.UtcNow.ToJavascriptMilliseconds();
			c.MockUser(ctx.E1);
			
			var s = c.Get(score.Id);
            CompareModelProperties(/*APIResult.ScorecardApiTests_v0_GetScore*/ s);

            Assert.AreEqual(s.Measurable.Name, m1.Title);
			Assert.AreEqual(s.Measured, null);

			score = await L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2000L, 3.14m, null);
			var s2 = c.Get(score.Id);
			Assert.AreEqual(s2.Measured, 3.14m);
		}


		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task PutScoreWeek() {
			var ctx = await Ctx.Build();
			var m1 = new MeasurableModel() {
				AccountableUserId = ctx.E1.Id,
				AdminUserId = ctx.E1.Id,
				Title = "Meas1",
				OrganizationId = ctx.Org.Organization.Id
			};
			MockHttpContext();
			await ScorecardAccessor.CreateMeasurable(ctx.Manager, m1, false);			

			var c = new Measurables_Controller();
			c.MockUser(ctx.E1);

			c.UpdateScore(m1.Id, 2000L, new Scores_Controller.UpdateScoreModel { value = null });
			var score = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(score.Measured, null);

			c.UpdateScore(m1.Id, 2000L, new Scores_Controller.UpdateScoreModel { value = 3.14m });
			score = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(score.Measured, 3.14m);

			c.UpdateScore(m1.Id, 2001L, new Scores_Controller.UpdateScoreModel { value = 6.14m });
			score = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(score.Measured, 3.14m);
			score = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2001L);
			Assert.AreEqual(score.Measured, 6.14m);

		}

		[TestMethod]
		[TestCategory("Api_V1")]
		public async Task PutScore() {
			MockHttpContext();
			var ctx = await Ctx.Build();

			var m1 = new MeasurableModel() {
				AccountableUserId = ctx.E1.Id,
				AdminUserId = ctx.E1.Id,
				Title = "Meas1",
				OrganizationId = ctx.Org.Organization.Id
			};
			await ScorecardAccessor.CreateMeasurable(ctx.Manager, m1, false);
			var score = await L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2000L, (decimal?)null, null);

			var c = new Scores_Controller();
			c.MockUser(ctx.E1);
			c.Put(score.Id,new Scores_Controller.UpdateScoreModel { value = null });
			var s = ScorecardAccessor.GetScore(ctx.Manager, score.Id);
			Assert.AreEqual(score.Measured, null);

			c.MockUser(ctx.E1);
			c.Put(score.Id, new Scores_Controller.UpdateScoreModel { value = 3.14m });
			s = ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(s.Measured, 3.14m);

			var score2 = await L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2001L, (decimal?)6.14, null);			
			s = ScorecardAccessor.GetScore(ctx.Manager, score.Id);
			Assert.AreEqual(s.Measured, 3.14m);
			s = ScorecardAccessor.GetScore(ctx.Manager, score2.Id);
			Assert.AreEqual(s.Measured, 6.14m);
		}
	}
}
