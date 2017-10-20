using NHibernate;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {


	public class IScoreHookUpdates {
		public DateTime AbsoluteUpdateTime { get; internal set; }
		public bool ValueChanged { get; set; }		
	}

	public interface IScoreHook : IHook {

		Task UpdateScore(ISession s, ScoreModel score, IScoreHookUpdates updates);
	}
}
