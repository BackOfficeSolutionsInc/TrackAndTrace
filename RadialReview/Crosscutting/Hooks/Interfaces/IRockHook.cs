using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public class IRockHookUpdates {
		public bool MessageChanged { get; set; }
		public bool DueDateChanged { get; set; }
		public bool StatusChanged { get; set; }
		public bool AccountableUserChanged { get; set; }

		public long OriginalAccountableUserId { get; set; }
		//public static IRockHookUpdates Diff(RockModel old, RockModel
	}



	public interface IRockHook : IHook {
		Task CreateRock(ISession s, RockModel rock);
		Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates);
		Task ArchiveRock(ISession s, RockModel rock, bool deleted);
		Task UnArchiveRock(ISession s, RockModel rock, bool v);
	}
}
