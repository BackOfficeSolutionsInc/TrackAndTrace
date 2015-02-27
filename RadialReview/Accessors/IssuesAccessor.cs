using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class IssuesAccessor
	{

		public static void CreateIssue(UserOrganizationModel caller,long recurrenceId, IssueModel issue)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
					perms.ViewL10Recurrence(recurrenceId);

					if (issue.Id!=0)
						throw new PermissionsException("Id was not zero");


					perms.ConfirmAndFix(issue,
						x => x.CreatedDuringMeetingId,
						x => x.CreatedDuringMeeting,
						x => x.ViewL10Meeting);

					perms.ConfirmAndFix(issue,
						x => x.OrganizationId,
						x => x.Organization,
						x => x.ViewOrganization);
					
					perms.ConfirmAndFix(issue,
						x => x.CreatedById,
						x => x.CreatedBy,
						x => y=>x.ManagesUserOrganization(y,false) );
					/*if (issue.CreatedDuringMeetingId != null)
						issue.CreatedDuringMeeting = s.Get<L10Meeting>(issue.CreatedDuringMeetingId);
					issue.MeetingRecurrence = s.Get<L10Recurrence>(issue.MeetingRecurrenceId);
					issue.CreatedBy = s.Get<UserOrganizationModel>(issue.CreatedById);
					*/
					s.Save(issue);
					var recur=new IssueModel.IssueModel_Recurrence(){
						CopiedFrom = null,
						Issue = issue,
						CreatedBy = issue.CreatedBy,
						Recurrence = s.Load<L10Recurrence>(recurrenceId),
						CreateTime = issue.CreateTime,
					};
					s.Save(recur);

					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}