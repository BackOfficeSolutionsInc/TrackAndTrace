using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;
using System.Threading.Tasks;
using RadialReview.Models.Enums;
using System.Linq;
using RadialReview.Utilities;
using RadialReview;

namespace TractionTools.Tests.Accessors.Scorecard {
    [TestClass]
    public class ScorecardAccessorTests : BaseTest {
        [TestMethod]
        [TestCategory("Accessors")]
        public async Task CreateMeasurable() {
            var o = await OrgUtil.CreateOrganization();
            var m = await ScorecardAccessor.CreateMeasurable(o.Manager, MeasurableBuilder.Build("m", o.Employee.Id));


            var found = ScorecardAccessor.GetMeasurable(o.Manager, m.Id);

            Assert.AreEqual(found.Id, m.Id);
            Assert.AreEqual("m", found.Title);
            Assert.AreEqual(o.Id, found.OrganizationId);
            Assert.AreEqual(o.Employee.Id, found.AccountableUserId);
            Assert.AreEqual(o.Employee.Id, found.AdminUserId);
            Assert.AreEqual(null, found.AlternateGoal);
            Assert.AreEqual(false, found.Archived);
            Assert.IsTrue(found.CreateTime >= new DateTime(2017,1,1));
            Assert.IsTrue(found.CreateTime <= DateTime.UtcNow);
            Assert.AreEqual(null, found.CumulativeRange);
            Assert.AreEqual(null, found.FromTemplateItemId);
            Assert.AreEqual(0m, found.Goal);
            Assert.AreEqual(LessGreater.GreaterThan, found.GoalDirection);
            Assert.AreEqual(false, found.ShowCumulative);
            Assert.AreEqual(UnitType.None, found.UnitType);
            Assert.IsNotNull(found.AdminUser);
            Assert.IsNotNull(found.AccountableUser);
            Assert.IsNotNull(found.Organization);
        }

        [TestMethod]
        [TestCategory("Accessors")]
        public async Task UpdateScore() {
            MockNoSyncException();
            var o = await OrgUtil.CreateOrganization();
            var m = await ScorecardAccessor.CreateMeasurable(o.Manager, MeasurableBuilder.Build("m", o.Employee.Id));
            {
                var scores = ScorecardAccessor.GetMeasurableScores(o.Manager, m.Id);
                Assert.AreEqual(0, scores.Count);
            }
            var weekId1 = 2000L;
            var weekId2 = 2001L;

            {
                var score = await ScorecardAccessor.GetScore(o.Manager, m.Id, weekId1);
                var scores = ScorecardAccessor.GetMeasurableScores(o.Manager, m.Id);
                Assert.AreEqual(1, scores.Count);

                var found = scores.First();

                Assert.AreEqual(score.Id, found.Id);
                Assert.AreEqual(null, found.Measured);
                Assert.AreEqual(null, score.Measured);
                Assert.IsNull(score.DateEntered);

                Assert.AreEqual(score.ForWeek, TimingUtility.GetDateSinceEpoch(weekId1));

                await ScorecardAccessor.UpdateScore(o.Manager, score.Id, 1);
                var scoreAgain = ScorecardAccessor.GetScore(o.Manager, score.Id);

                Assert.AreEqual(1, scoreAgain.Measured);
                Assert.AreEqual(score.Id, scoreAgain.Id);
                Assert.IsNotNull(scoreAgain.DateEntered);
            }

            {
                //Test updating a second score
                {
                    var scores = ScorecardAccessor.GetMeasurableScores(o.Manager, m.Id);
                    Assert.AreEqual(1, scores.Count);
                }
                await ScorecardAccessor.UpdateScore(o.Manager, m.Id, TimingUtility.GetDateSinceEpoch(weekId2), 2.1m);
                {
                    var scores = ScorecardAccessor.GetMeasurableScores(o.Manager, m.Id);
                    Assert.AreEqual(2, scores.Count);

                    var w1Score = scores.First(x => x.ForWeek == TimingUtility.GetDateSinceEpoch(weekId1));
                    var w2Score = scores.First(x => x.ForWeek == TimingUtility.GetDateSinceEpoch(weekId2));

                    Assert.IsTrue(w1Score.Measured == 1);
                    Assert.IsTrue(w2Score.Measured == 2.1m);


                    //Test priority of ScoreId,MeasurableId,WeekId
                    {
                        //(ScoreId) from w1Score, (MeasurableId,WeekId) from w2Score
                        await ScorecardAccessor.UpdateScore(o.Manager,w1Score.Id, m.Id, TimingUtility.GetDateSinceEpoch(weekId2), 3.35m);
                        //Should use w1Score as priority
                        var newW1Score = ScorecardAccessor.GetScore(o.Manager, w1Score.Id);
                        var newW2Score = ScorecardAccessor.GetScore(o.Manager, w2Score.Id);
                        Assert.IsTrue(newW1Score.Measured == 3.35m);
                        Assert.IsTrue(newW2Score.Measured == 2.1m);                        
                    }

                    //Test setting to null
                    {
                        await ScorecardAccessor.UpdateScore(o.Manager, m.Id, TimingUtility.GetDateSinceEpoch(weekId2), null);
                        var newScore = ScorecardAccessor.GetScore(o.Manager, w2Score.Id);

                        Assert.IsTrue(newScore.Measured == null);
                        Assert.IsTrue(newScore.DateEntered == null);
                    }
                }               

            }



        }

        
    }
}
