using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.L10.VM
{
	public class IssuesData
	{
		public long issue { get; set; }
		public IssuesData[] children { get; set; }
		public String message { get; set; }
	}


	public class IssueEdit
	{
		public long? ParentIssueId { get; set; }
		public long IssueId { get; set; }
		public int Order { get; set; }
	}


	public class IssuesDataList
	{
		public IssuesData[] issues { get; set; }

		public string connectionId { get; set; }

		public List<long> GetAllIds()
		{
			return idsRecurse(issues).Distinct().ToList();
		}
		private IEnumerable<long> idsRecurse(IssuesData[] data)
		{
			if (data==null)
				return new List<long>();
			var output = data.Select(x=>x.issue).ToList();
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
				IssueId = x.issue,
				ParentIssueId = parentIssueId,
				Order = i
			}).ToList();
			foreach (var d in data){
				output.AddRange(issuesRecurse(d.issue,d.children));
			}
			return output;
		}

	}
}