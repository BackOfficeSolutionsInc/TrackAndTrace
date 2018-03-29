using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Crosscutting.EventAnalyzers.Events;
using Newtonsoft.Json;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TractionTools.Tests.Crosscutting.Events {
	[TestClass]
	public class EventSubscriptionTests {

		[TestMethod]
		public void TestEventSerialize() {

			IEventAnalyzer a = new AverageMeetingRatingBelowForWeeksInARow(1);

			var s = JsonConvert.SerializeObject(a);

			Console.WriteLine(a);

			var result = JsonConvert.DeserializeObject<AverageMeetingRatingBelowForWeeksInARow>(s);

			int b = 0;
			Assert.AreEqual(2, result.WeeksInARow);
			Assert.AreEqual(7, result.RatingTheshold);
			Assert.AreEqual(LessGreater.LessThanOrEqual, result.Direction);
			Assert.AreEqual(1, result.RecurrenceId);

		}

		[TestMethod]
		public async Task TestEditorFieldProperty() {

			var m = new AverageMeetingRatingBelowForWeeksInARow(1);

			var egs = new BaseEventGeneratorSettings(null, null, null, 0, new[] { new KeyValuePair<string, long>("M1", 1) });
			var fields = await m.GetSettingsFields(egs);

			int a = 0;


		}

	}
}
