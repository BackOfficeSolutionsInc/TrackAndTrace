using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
	public static class IssuesExtensions
	{
		public static string IssueMessage(this ScoreModel self)
		{
			if (self == null)
				return "Not entered.";

			var name = "'(" + self.ForWeek.ToString("d") + ") " + self.Measurable.Title + "'";
			var accountable = self.Measurable.AccountableUser.GetName();
			var admin = self.Measurable.AdminUser.GetName();
			if (admin != accountable){
				accountable += "/"+admin;
			}


			if (!self.Measured.HasValue){
				return name + " was not entered. "+accountable+".";
			}else if (self.MetGoal()){
				return name + accountable;
			}else{
				return name + " goal was not met. Requires " +
					self.Measurable.GoalDirection + " " + 
					self.Measurable.Goal.ToString("0.####") + ". Recorded as " +
					self.Measured.Value.ToString("0.####") + ". " + accountable + ".";
			}
		}
	}
}