using NHibernate;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
	public interface IRecurrenceSettings :IHook{
		Task ChangePeopleAnalyzerSharing(ISession s, long forUser, long recurrenceId, L10Recurrence.SharePeopleAnalyzer share);
	}
}
