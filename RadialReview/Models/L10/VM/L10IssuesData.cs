using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Issues;

namespace RadialReview.Models.L10.VM
{
	public class IssuesData
	{
		public long recurrence_issue { get; set; }
		public long issue { get; set; }
		public long createtime { get; set; }
		public IssuesData[] children { get; set; }
		public String message { get; set; }
		public String details { get; set; }
		public bool @checked { get; set; }
		public String owner { get; set; }
		public long? accountable { get; set; }
		public String imageUrl { get; set; }
		public String padid { get; set; }
        public long? createdDuringMeetingId { get; set; }
        public int priority { get; set; }
        public int rank { get; set; }

		public static IssuesData FromIssueRecurrence(IssueModel.IssueModel_Recurrence recur)
		{
			var issue = new IssuesData(){
                priority = recur.Priority,
				@checked = recur.CloseTime != null,
				createtime = recur.CreateTime.NotNull(x=>x.ToJavascriptMilliseconds()),
				details = recur.Issue.Description,
				message = recur.Issue.Message,
				recurrence_issue = recur.Id,
				issue = recur.Issue.Id,
				padid = recur.Issue.PadId,
				owner = recur.Owner.NotNull(x => x.GetName()),
				imageUrl	= recur.Owner.NotNull(x=>x.ImageUrl(true,ImageSize._64))??"/i/placeholder",
				createdDuringMeetingId = recur.Issue.CreatedDuringMeetingId,
                rank = recur.Rank
			};
			if (recur.Owner!=null){
				issue.accountable = recur.Owner.Id;
			}
			return issue;
		}
	}


	public class IssueEdit
	{
		public long? ParentRecurrenceIssueId { get; set; }
		public long RecurrenceIssueId { get; set; }
		public int Order { get; set; }
	}


	public class IssuesDataList
	{
		public IssuesData[] issues { get; set; }

		public string connectionId { get; set; }

		public string orderby { get; set; }

		public List<long> GetAllIds()
		{
			return idsRecurse(issues).Distinct().ToList();
		}
		private IEnumerable<long> idsRecurse(IssuesData[] data)
		{
			if (data==null)
				return new List<long>();
			var output = data.Select(x => x.recurrence_issue).ToList();
			foreach (var d in data){
				output.AddRange(idsRecurse(d.children));
			}
			return output;
		} 

		public List<IssueEdit> GetIssueEdits()
		{
			return issuesRecurse(null, issues).ToList();
		}

		private IEnumerable<IssueEdit> issuesRecurse(long? parentIssueId,IssuesData[] data)
		{
			if (data == null)
				return new List<IssueEdit>();
			var output = data.Select((x,i) => new IssueEdit(){
				RecurrenceIssueId = x.recurrence_issue,
				ParentRecurrenceIssueId = parentIssueId,
				Order = i
			}).ToList();
			foreach (var d in data){
				output.AddRange(issuesRecurse(d.recurrence_issue, d.children));
			}
			return output;
		}

	}
}