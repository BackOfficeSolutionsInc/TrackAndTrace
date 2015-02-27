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
		public static string IssueMessage(this IIssue self)
		{
			if (self == null)
				return "Not entered.";
			return self.GetIssueMessage();

		}

		public static string IssueDetails(this IIssue self)
		{
			if (self == null)
				return "Not entered.";
			return self.GetIssueDetails();
		}
	}
}