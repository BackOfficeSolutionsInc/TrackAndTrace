using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System.Diagnostics;

namespace RadialReview.Models.Scorecard
{
    [DataContract]
    [DebuggerDisplay("{Id} = '{Measured}' @ {DataContract_ForWeek}")]
	public class ScoreModel : ILongIdentifiable, IDeletable, IIssue,ITodo
	{
		[DataMember(Order = 0)]
		public virtual long Id { get; set; }
		[DataMember(Name = "MeasurableId", Order = 1)]
		public virtual long MeasurableId { get; set; }
		[DataMember(Name = "ForWeekNumber",Order = 2)]	
		public virtual long DataContract_ForWeek{get { return TimingUtility.GetWeekSinceEpoch(ForWeek); }}
		[DataMember(Name = "Value", Order = 3)]
		public virtual decimal? Measured { get; set; }


		public virtual DateTime ForWeek { get; set; }
		public virtual DateTime? DateEntered { get; set; }
		public virtual DateTime DateDue { get; set; }



		public virtual MeasurableModel Measurable { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long AccountableUserId { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }
		

		public virtual DateTime? DeleteTime { get; set; }


		public virtual bool _Editable { get; set; }


		public ScoreModel()
		{
			_Editable = true;
		}

		public virtual async Task<string> GetIssueMessage()
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
					return name + " goal was exceeded by " + Measurable.UnitType.Format((v - g)*dir);
				return name + " goal was exceeded by " + ((v - g)/g*dir*100).ToString("0.####") + "%";
			}else{
				return name + " goal was missed by " + Measurable.UnitType.Format((g - v) * dir);
			}
		}

		public virtual async Task<string> GetIssueDetails()
		{
			var week = ForWeek.AddDays(-7).ToString("d");
			var accountable = Measurable.AccountableUser.NotNull(x => x.GetName());
			var admin = Measurable.AdminUser.NotNull(x=>x.GetName());
			if (admin != accountable){
				accountable += "/" + admin;
			}
			var footer = "Week: " + week + "\nOwner: " + accountable;
			if (Measured.HasValue){

				var goal = "GOAL: " + Measurable.GoalDirection.GetDisplayName() + " " + Measurable.UnitType.Format(Measurable.Goal);
				var recorded = "RECORDED: " + Measurable.UnitType.Format(Measured.Value);
				return goal + "\n" + recorded + "\n\n" + footer;
			}
			return footer ;
		}

		public virtual async Task<string> GetTodoMessage()
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
					return name + " goal was exceeded by " + Measurable.UnitType.Format((v - g)*dir);
				return name + " goal was exceeded by " + ((v - g) / g * dir * 100).ToString("0.####") + "%";
			}
			else
			{
				return name + " goal was missed by " + Measurable.UnitType.Format((g - v) * dir);
			}
		}

		public virtual async Task<string> GetTodoDetails()
		{
			var week = ForWeek.AddDays(-7).ToString("d");
			var accountable = Measurable.AccountableUser.GetName();
			var admin = Measurable.AdminUser.GetName();
			if (admin != accountable)
			{
				accountable += "/" + admin;
			}
			var footer = "Week:" + week + "\nOwner: " + accountable;
			if (Measured.HasValue)
			{

				var goal = "GOAL: " + Measurable.GoalDirection.GetDisplayName() + " " + Measurable.UnitType.Format(Measurable.Goal);
				var recorded = "RECORDED: " + Measurable.UnitType.Format(Measured.Value);
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


		public class DataContract
		{
			public virtual long Id { get; set; }
			public virtual decimal? Value { get; set; }
			public virtual long ForWeek { get; set; }
			public virtual MeasurableModel Measurable { get; set; }

			public virtual UserOrganizationModel.DataContract AccountableUser { get { return Measurable.AccountableUser.GetUserDataContract(); } }
			public virtual UserOrganizationModel.DataContract AdminUser { get { return Measurable.AdminUser.GetUserDataContract(); } }

			public DataContract(ScoreModel self)
			{
				Id = self.Id;
				Value = self.Measured;
				ForWeek = self.DataContract_ForWeek;
				Measurable = self.Measurable;
			}
		}
		
	}
}