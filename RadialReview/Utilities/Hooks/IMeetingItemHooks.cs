using NHibernate;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IMeetingRockHooks : IHook {
		Task AttachRock(ISession s, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock);
		Task DetatchRock(ISession s, RockModel rock,long recurrenceId);
		Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock);
	}
}
