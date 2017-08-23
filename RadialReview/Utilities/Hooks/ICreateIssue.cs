using NHibernate;
using RadialReview.Models.Issues;
using RadialReview.Models.Todo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface IIssueHook : IHook {

		Task CreateIssue(ISession s, IssueModel.IssueModel_Recurrence issue);
		Task UpdateMessage(ISession s, IssueModel.IssueModel_Recurrence issue);
		Task UpdateCompletion(ISession s, IssueModel.IssueModel_Recurrence issue);
	}
}
