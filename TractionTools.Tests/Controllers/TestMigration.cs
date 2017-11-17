using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Controllers;
using System.Collections.Generic;
using RadialReview.Models.Scorecard;

namespace TractionTools.Tests.Controllers {
	[TestClass]
	public class TestMigration {
		[TestMethod]
		public void FixScoresDecide() {

			var mc = new MigrationController();

			var thresh = TimeSpan.FromSeconds(6);

			var one = TimeSpan.FromSeconds(1);
			//var minus = TimeSpan.FromSeconds(-1);

			var a = new DateTime(2015, 01, 01);
			var b = a.Add(thresh).AddSeconds(1);
			var c = a.Add(thresh + thresh).AddSeconds(1);
			//before
			{
				var scores = new List<ScoreModel> {
					new ScoreModel() { Id =1 , DateEntered = a, Measured =1 },
					new ScoreModel() { Id =2 , DateEntered = a - one , Measured =1.1m },
				};

				Assert.AreEqual(2, mc.DecideOnScores(scores, thresh).Id);
			}

			//after
			{
				var scores = new List<ScoreModel> {
					new ScoreModel() { Id =1 , DateEntered = a, Measured =1 },
					new ScoreModel() { Id =2 , DateEntered = a + one , Measured =1.1m },
				};

				Assert.AreEqual(2, mc.DecideOnScores(scores, thresh).Id);
			}


			//much after
			{
				var scores = new List<ScoreModel> {
					new ScoreModel() { Id =1 , DateEntered = a, Measured =1 },
					new ScoreModel() { Id =2 , DateEntered = a + one , Measured =1.1m },
					new ScoreModel() { Id =3 , DateEntered = b + one , Measured =6m },
				};

				Assert.AreEqual(3, mc.DecideOnScores(scores, thresh).Id);
			}

			//much after, same time, different score
			{
				var scores = new List<ScoreModel> {
					new ScoreModel() { Id =1 , DateEntered = a, Measured =1 },
					new ScoreModel() { Id =2 , DateEntered = a + one , Measured =1.1m },
					new ScoreModel() { Id =3 , DateEntered = b + one , Measured =60m },
					new ScoreModel() { Id =4 , DateEntered = b + one , Measured =6m },
				};

				Assert.AreEqual(3, mc.DecideOnScores(scores, thresh).Id);
			}

			//add a Null
			{
				var scores = new List<ScoreModel> {
					new ScoreModel() { Id =1 , DateEntered = null, Measured =null },
					new ScoreModel() { Id =2 , DateEntered = a + one , Measured =1.1m },
					new ScoreModel() { Id =3 , DateEntered = b + one , Measured =60m },
					new ScoreModel() { Id =4 , DateEntered = b + one , Measured =6m },
				};

				Assert.AreEqual(3, mc.DecideOnScores(scores, thresh).Id);
			}
			//only a Null
			{
				var scores = new List<ScoreModel> {
					new ScoreModel() { Id =1 , DateEntered = null, Measured =null },
				};

				Assert.AreEqual(1, mc.DecideOnScores(scores, thresh).Id);
			}
			//empty
			{
				var scores = new List<ScoreModel> {};

				Assert.IsNull(mc.DecideOnScores(scores, thresh));
			}

		}
	}
}
