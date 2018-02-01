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
using System.Threading.Tasks;
using TractionTools.Tests.Properties;

namespace TractionTools.Tests.API.v0 {
	[TestClass]
	public class ScorecardApiTests_v0 : BaseApiTest {
		public ScorecardApiTests_v0() : base(VERSION_0) { }
		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task TestCurrentWeek() {
			var ctx = await Ctx.Build();
			var c = new WeekController();
			c.MockUser(ctx.E1);
			var week = c.Get();
			var actualWeek = TimingUtility.GetWeekSinceEpoch(DateTime.UtcNow.AddMinutes(+ctx.Org.Organization.GetTimezoneOffset())) + 1;
			Assert.AreEqual(week.DataContract_Weeks, actualWeek);
		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task GetScore_V0() {
			var ctx = await Ctx.Build();
			//var m1 = new MeasurableModel() {
			//	AccountableUserId = ctx.E1.Id,
			//	AdminUserId = ctx.E1.Id,
			//	Title = "Meas1",
			//	OrganizationId = ctx.Org.Organization.Id
			//};
			//MockHttpContext();
			//await ScorecardAccessor.CreateMeasurable(ctx.Manager, m1 , false);
			
			MockHttpContext();
			var builder = MeasurableBuilder.Build("Meas1", ctx.E1.Id);
			var m1 = await ScorecardAccessor.CreateMeasurable(ctx.Manager, builder);
			MockNoSyncException();
            ctx.Manager.SetClientTimeStamp(DateTime.UtcNow.ToJsMs());
			var score = await ScorecardAccessor.UpdateScore(ctx.Manager, m1.Id, TimingUtility.GetDateSinceEpoch(2000L), null);

			//var score = await L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2000L, (decimal?)null,null);

			var c = new ScoresController();
			ctx.E1._ClientTimestamp = DateTime.UtcNow.ToJavascriptMilliseconds();
			ctx.Manager._ClientTimestamp = DateTime.UtcNow.ToJavascriptMilliseconds();
			c.MockUser(ctx.E1);

			
			var s = c.Get(score.Id);
            CompareModelProperties(/*APIResult.ScorecardApiTests_v0_GetScore*/ s);

            Assert.AreEqual(s.Measurable.Title, m1.Title);
			Assert.AreEqual(s.Value, null);

			score = await ScorecardAccessor.UpdateScore(ctx.Manager, m1.Id, TimingUtility.GetDateSinceEpoch(2000L), 3.14m);
			//score = await L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2000L, 3.14m, null);

			var s2 = c.Get(score.Id);
			Assert.AreEqual(s2.Value, 3.14m);

		}


		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task PutScoreWeek() {
			var ctx = await Ctx.Build();
			//var m1 = new MeasurableModel() {
			//	AccountableUserId = ctx.E1.Id,
			//	AdminUserId = ctx.E1.Id,
			//	Title = "Meas1",
			//	OrganizationId = ctx.Org.Organization.Id
			//};
			//MockHttpContext();
			//await ScorecardAccessor.CreateMeasurable(ctx.Manager, m1, false);

			MockHttpContext();
			var builder = MeasurableBuilder.Build("Meas1", ctx.E1.Id);
			var m1 = await ScorecardAccessor.CreateMeasurable(ctx.Manager, builder);
            
			var c = new ScoresController();
			c.MockUser(ctx.E1);

            ctx.E1.IncrementClientTimestamp();
            await c.Put(m1.Id, 2000L, null);
			var score = await ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(score.Measured, null);

            ctx.E1.IncrementClientTimestamp();
            await c.Put(m1.Id, 2000L, 3.14m);
			score = await ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(score.Measured, 3.14m);

            ctx.E1.IncrementClientTimestamp();
            await c.Put(m1.Id, 2001L, 6.14m);
			score = await ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(score.Measured, 3.14m);
			score = await ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2001L);
			Assert.AreEqual(score.Measured, 6.14m);

		}

		[TestMethod]
		[TestCategory("Api_V0")]
		public async Task PutScore() {
			MockHttpContext();
			var ctx = await Ctx.Build();

			//var m1 = new MeasurableModel() {
			//	AccountableUserId = ctx.E1.Id,
			//	AdminUserId = ctx.E1.Id,
			//	Title = "Meas1",
			//	OrganizationId = ctx.Org.Organization.Id
			//};
			//await ScorecardAccessor.CreateMeasurable(ctx.Manager, m1, false);

			MockHttpContext();
			var builder = MeasurableBuilder.Build("Meas1", ctx.E1.Id);
			var m1 = await ScorecardAccessor.CreateMeasurable(ctx.Manager, builder);

			MockNoSyncException();
            ctx.Manager.IncrementClientTimestamp();
            //var score = await L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2000L, (decimal?)null, null);
            var score = await ScorecardAccessor.UpdateScore(ctx.Manager, m1.Id, TimingUtility.GetDateSinceEpoch(2000L), null);

			var c = new ScoresController();
			c.MockUser(ctx.E1);
            ctx.Manager.IncrementClientTimestamp();

            await c.Put(score.Id,null);
			var s = ScorecardAccessor.GetScore(ctx.Manager, score.Id);
			Assert.AreEqual(score.Measured, null);

			c.MockUser(ctx.E1);
            ctx.Manager.IncrementClientTimestamp();
            await c.Put(score.Id, 3.14m);
			s = await ScorecardAccessor.GetScore(ctx.Manager, m1.Id, 2000L);
			Assert.AreEqual(s.Measured, 3.14m);

            //var score2 = await L10Accessor.UpdateScore(ctx.Manager, m1.Id, 2001L, (decimal?)6.14, null);
            ctx.Manager.IncrementClientTimestamp();

            var score2 = await ScorecardAccessor.UpdateScore(ctx.Manager, m1.Id, TimingUtility.GetDateSinceEpoch(2001L), 6.14m);
			s = ScorecardAccessor.GetScore(ctx.Manager, score.Id);
			Assert.AreEqual(s.Measured, 3.14m);
			s = ScorecardAccessor.GetScore(ctx.Manager, score2.Id);
			Assert.AreEqual(s.Measured, 6.14m);

		}

	}
}
