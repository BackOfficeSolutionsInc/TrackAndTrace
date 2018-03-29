using NHibernate;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
	public interface IEventHook : IHook {
		Task HandleEventTriggered(ISession s, IEventAnalyzer analyzer, IEventSettings settings);
	}
}