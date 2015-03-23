using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Scorecard
{
	public class ScoreModel : ILongIdentifiable, IDeletable, IIssue,ITodo
	{
		public virtual long Id { get; set; }
		public virtual DateTime? DateEntered { get; set; }
		public virtual DateTime DateDue { get; set; }
		public virtual DateTime ForWeek { get; set; }
		public virtual long MeasurableId { get; set; }
		public virtual MeasurableModel Measurable { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long AccountableUserId { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }
		public virtual decimal? Measured { get; set; }

		public virtual DateTime? DeleteTime { get; set; }

		public ScoreModel()
		{
		}

		public virtual string GetIssueMessage()
		{
			var name = "'" + Measurable.Title + "'";

			if (!Measured.HasValue)
			{
				return name + " was not entered.";
			}
			var v = Measured.Value;
			var g = Measurable.Goal;
			var dir = (decimal) Measurable.GoalDirection;
			if (MetGoal()){
				var diff = (Measured - Measurable.Goal) * (decimal)Measurable.GoalDirection;
				if (diff == 0)
					return name + " goal was met at " +g;
				if (v == Math.Floor(v) && g == Math.Floor(g))
					return name + " goal was exceeded by " + ((v - g)*dir).ToString("0.####") ;
				return name + " goal was exceeded by " + ((v - g)/g*dir*100).ToString("0.####") + "%";
			}else{
				return name + " goal was missed by "+ ((g - v) * dir).ToString("0.####");
			}
		}

		public virtual string GetIssueDetails()
		{
			var week = ForWeek.ToString("d");
			var accountable = Measurable.AccountableUser.GetName();
			var admin = Measurable.AdminUser.GetName();
			if (admin != accountable){
				accountable += "/" + admin;
			}
			var footer = "Week of " + week + "\nOwner: " + accountable;
			if (Measured.HasValue){

				var goal = "GOAL: " + Measurable.GoalDirection.GetDisplayName() + " " + Measurable.Goal.ToString("0.####");
				var recorded = "RECORDED: " + Measured.Value.ToString("0.####");
				return goal + "\n" + recorded + "\n\n" + footer;
			}
			return footer ;
		}

		public virtual string GetTodoMessage()
		{
			var name = "'" + Measurable.Title + "'";

			if (!Measured.HasValue)
			{
				return "Enter "+name ;
			}
			var v = Measured.Value;
			var g = Measurable.Goal;
			var dir = (decimal)Measurable.GoalDirection;
			if (MetGoal())
			{
				var diff = (Measured - Measurable.Goal) * (decimal)Measurable.GoalDirection;
				if (diff == 0)
					return name + " goal was met at " + g;
				if (v == Math.Floor(v) && g == Math.Floor(g))
					return name + " goal was exceeded by " + ((v - g) * dir).ToString("0.####");
				return name + " goal was exceeded by " + ((v - g) / g * dir * 100).ToString("0.####") + "%";
			}
			else
			{
				return name + " goal was missed by " + ((g - v) * dir).ToString("0.####");
			}
		}

		public virtual string GetTodoDetails()
		{
			var week = ForWeek.ToString("d");
			var accountable = Measurable.AccountableUser.GetName();
			var admin = Measurable.AdminUser.GetName();
			if (admin != accountable)
			{
				accountable += "/" + admin;
			}
			var footer = "Week of " + week + "\nOwner: " + accountable;
			if (Measured.HasValue)
			{

				var goal = "GOAL: " + Measurable.GoalDirection.GetDisplayName() + " " + Measurable.Goal.ToString("0.####");
				var recorded = "RECORDED: " + Measured.Value.ToString("0.####");
				return goal + "\n" + recorded + "\n\n" + footer;
			}
			return footer;
		}

		public virtual bool MetGoal()
		{
			switch (Measurable.GoalDirection)
			{
				case LessGreater.LessThan:
					return Measured < Measurable.Goal;
				case LessGreater.GreaterThan:
					return Measured >= Measurable.Goal;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}



		public class ScoreMap : ClassMap<ScoreModel>
		{
			public ScoreMap()
			{
				Id(x => x.Id);
				Map(x => x.DateEntered);
				Map(x => x.DateDue);
				Map(x => x.ForWeek);
				Map(x => x.Measured);
				Map(x => x.OrganizationId);
				Map(x => x.AccountableUserId).Column("AccountableUserId");
				References(x => x.AccountableUser).Column("AccountableUserId").LazyLoad().ReadOnly();

				Map(x => x.MeasurableId).Column("MeasureableId");
				References(x => x.Measurable).Column("MeasureableId").Not.LazyLoad().ReadOnly();
				Map(x => x.DeleteTime);
			}
		}



		
	}
}