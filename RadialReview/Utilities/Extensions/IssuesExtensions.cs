using System.Threading.Tasks;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
	public static class IssuesExtensions
	{
		public async static Task<string> IssueMessage(this IIssue self)
		{
			if (self == null)
				return "Not entered.";
			return await self.GetIssueMessage();

		}

		public async static Task<string> IssueDetails(this IIssue self)
		{
			if (self == null)
				return "Not entered.";
			return await self.GetIssueDetails();
		}
	}
}