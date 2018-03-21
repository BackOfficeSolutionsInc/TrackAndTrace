using RadialReview.Crosscutting.EventAnalyzers;
using RadialReview.Crosscutting.EventAnalyzers.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.App_Start {
	public class EventConfig {

		public static void RegisterEvents() {

			EventRegistry.RegisterEventAnalyzer(new DaysWithoutL10());

		}
	}
}