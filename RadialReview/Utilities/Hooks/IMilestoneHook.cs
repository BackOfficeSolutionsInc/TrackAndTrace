using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Rocks;
using RadialReview.Models.Todo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public class IMilestoneHookUpdates {
		public bool MessageChanged { get; set; }
		public bool DueDateChanged { get; set; }
		public bool CompletionChanged { get; set; }
		public bool IsDeleted { get; set; }
	}

    public interface IMilestoneHook : IHook {
        Task CreateMilestone(ISession s, Milestone milestone);
		Task UpdateMilestone(ISession s, UserOrganizationModel caller, Milestone milestone, IMilestoneHookUpdates updates);		
	}
}
