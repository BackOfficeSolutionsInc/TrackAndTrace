using System;
using System.Runtime.Serialization;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Scorecard
{
	[DataContract]
	public class MeasurableModel :ILongIdentifiable,IDeletable
	{
		[DataMember]
		public virtual long Id { get; set; }
		[DataMember]
		public virtual string Title { get; set; }
		[DataMember]
		public virtual LessGreater GoalDirection { get; set; }
		[DataMember]
        public virtual decimal Goal { get; set; }
		[DataMember]
		public virtual UnitType UnitType { get; set; }
		[DataMember(Name = "AccountableUser")]
		public virtual UserOrganizationModel.DataContract DataContract_AccountableUser { get { return AccountableUser.GetUserDataContract(); } }
		[DataMember(Name = "AdminUser")]
		public virtual UserOrganizationModel.DataContract DataContract_AdminUser { get { return AdminUser.GetUserDataContract(); }}

		public virtual long? FromTemplateItemId { get; set; }
		public virtual long AccountableUserId { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }
		public virtual long AdminUserId { get; set; }
		public virtual UserOrganizationModel AdminUser { get; set; }
        public virtual decimal? AlternateGoal { get; set; }
        public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual DateTime NextGeneration { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual DayOfWeek DueDate { get; set; }
		public virtual TimeSpan DueTime { get; set; }

		public virtual DateTime? CumulativeRange { get; set; }
		public virtual bool ShowCumulative { get; set; }
		public virtual decimal? _Cumulative { get; set; }


		public virtual bool _Editable { get; set; }
        public virtual int? _Ordering { get; set; }
        public virtual long? _Grouping { get; set; }
        public virtual string _GroupingName { get; set; }

      
		public virtual bool Archived { get; set; }

		public MeasurableModel()
		{
            _Editable = true;
            CreateTime = DateTime.UtcNow;
			CumulativeRange = DateTime.UtcNow;
			NextGeneration = CreateTime - TimeSpan.FromDays(7);
            DueDate = DayOfWeek.Friday;
			
		}

		public MeasurableModel(OrganizationModel forOrganization):this()
		{
			var now = DateTime.UtcNow;
			CreateTime = now;
			NextGeneration =now- TimeSpan.FromDays(7);
			DueDate = DayOfWeek.Friday;

			DueTime = forOrganization.ConvertToUTC(TimeSpan.FromHours(12));
				
			//	TimeSpan.FromHours(12).Add(TimeSpan.FromMinutes(-1*forOrganization.Settings.TimeZoneOffsetMinutes));
			Organization = forOrganization;
			OrganizationId = forOrganization.Id;
		}

		public class MeasurableMap : ClassMap<MeasurableModel>
		{
			public MeasurableMap()
			{
				//Table("measurablemodel");
				Id(x => x.Id);
				Map(x => x.Title);
				Map(x => x.NextGeneration);
				Map(x => x.AccountableUserId).Column("AccountableUserId");
				References(x => x.AccountableUser).Column("AccountableUserId").LazyLoad().ReadOnly();
				Map(x => x.AdminUserId).Column("AdminUserId");
				References(x => x.AdminUser).Column("AdminUserId").LazyLoad().ReadOnly();
                Map(x => x.Goal);
                Map(x => x.AlternateGoal);
                Map(x => x.GoalDirection);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);

				Map(x => x.UnitType);

				Map(x => x.DueDate);
				Map(x => x.DueTime);

				Map(x => x.Archived);

				Map(x => x.FromTemplateItemId);

				Map(x => x.ShowCumulative);
				Map(x => x.CumulativeRange);
			} 
		}
		
		public virtual string ToSymbolString(){
			return GoalDirection.ToSymbol()+" "+Goal.ToString("0.#####");
		}


	}
}