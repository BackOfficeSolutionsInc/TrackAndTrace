using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Engines;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.TestUtils;

namespace TractionTools.Tests.Accessors {
	[TestClass]
	public class EvalScorerTests : BaseTest {

		[TestMethod]
		public void AboveTheBar() {

			var scores = new[] {
				PositiveNegativeNeutral.Positive ,
				PositiveNegativeNeutral.Positive ,
				PositiveNegativeNeutral.Positive,
				PositiveNegativeNeutral.Neutral,
				PositiveNegativeNeutral.Neutral
			}.ToList();

			var res = ChartsEngine.ScatterScorer.MergeValueScores(scores, new CompanyValueModel() { MinimumPercentage = 60 });

			Assert.AreEqual(PositiveNegativeNeutral.Positive, res.Merged);
			Assert.AreEqual(true, res.Above);


			res = ChartsEngine.ScatterScorer.MergeValueScores(scores, new CompanyValueModel() { MinimumPercentage = 59 });
			Assert.AreEqual(PositiveNegativeNeutral.Positive, res.Merged);
			Assert.AreEqual(true, res.Above);

			res = ChartsEngine.ScatterScorer.MergeValueScores(scores, new CompanyValueModel() { MinimumPercentage = 61 });
			Assert.AreEqual(PositiveNegativeNeutral.Negative, res.Merged);
			Assert.AreEqual(false, res.Above);
		}
	}
}


