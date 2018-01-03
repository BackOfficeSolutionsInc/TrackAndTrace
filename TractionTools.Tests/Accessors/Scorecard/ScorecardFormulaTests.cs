using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dangl.Calculator;
using RadialReview.Accessors;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;
using RadialReview.Utilities;
using RadialReview.Models.Scorecard;
using TractionTools.Tests.TestUtils;
using RadialReview;

namespace TractionTools.Tests.Accessors.Scorecard {
    [TestClass]
    public class ScorecardFormulaTests : BaseTest{
        [TestMethod]
        public void TestCalculator() {

            var formula = "5+5";
            var calculation = Calculator.Calculate(formula);

            Assert.AreEqual(10, calculation.Result);
        }
        [TestMethod]
        public async Task TestSetFormula() {
            var scores = new decimal?[] { 0.0m, null, 1.0m, 2m, 4m, null, 8m };

            var o = await OrgUtil.CreateOrganization();
            var m = await ScorecardAccessor.CreateMeasurable(o.Manager, MeasurableBuilder.Build("m", o.Employee.Id));

            o.Employee._ClientTimestamp = DateTime.UtcNow.ToJsMs();

            for (var i = 0; i < scores.Length; i++)
                await ScorecardAccessor.UpdateScore(o.Employee, m.Id, TimingUtility.ToScorecardDate(TimingUtility.PeriodsFromNow(DateTime.UtcNow, i-scores.Length, ScorecardPeriod.Weekly)), scores[i]);

            var calc = await ScorecardAccessor.CreateMeasurable(o.Manager, MeasurableBuilder.Build("calc", o.Employee.Id));

            await ScorecardAccessor.SetFormula(o.Employee, calc.Id, "["+m.Id+"(0)]+2");


            var calcScores=  ScorecardAccessor.GetMeasurableScores(o.Employee, calc.Id);

            var expected = new decimal?[] { 2, 2, 3, 4, 6, 2, 10 };

            for (var i = 0; i < expected.Length; i++) {
                Assert.AreEqual(expected[i], calcScores[i].Measured);
            }
        }
    }
}
