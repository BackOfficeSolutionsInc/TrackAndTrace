using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Attach;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Periods;

namespace RadialReview.Models.UserTemplate
{
	public class UserTemplate: ILongIdentifiable,IDeletable
	{
		public virtual long Id { get; set; }

		public virtual long AttachId { get; set; }
		public virtual AttachType AttachType { get; set; }
		public virtual AttachModel _Attach { get; set; }

		public virtual DateTime? DeleteTime { get; set; }
		public virtual String JobDescription { get; set; }
		public virtual List<UT_Role> _Roles { get; set; }
		public virtual List<UT_User> _Members { get; set; }
		public virtual List<UT_Rock> _Rocks { get; set; }
		public virtual List<UT_Measurable> _Measurables { get; set; }

		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }

		public class UserTemplateMap : ClassMap<UserTemplate>
		{
			public UserTemplateMap()
			{
				Id(x => x.Id);
				Map(x => x.DeleteTime);
				Map(x => x.JobDescription);
				Map(x => x.AttachId);
				Map(x => x.AttachType);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly(); ;
			}
		}

		public class UT_Role : ILongIdentifiable, IDeletable,IUserTemplateItem
		{
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual String Role { get; set; }
			public virtual long TemplateId { get; set; }
			public virtual UserTemplate Template { get; set; }
			public class UT_RoleMap : ClassMap<UT_Role>
			{
				public UT_RoleMap()
				{
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.Role);
					Map(x => x.TemplateId).Column("TemplateId");
					References(x => x.Template).Column("TemplateId").LazyLoad().ReadOnly();
				}
			}

		}

		public class UT_User : ILongIdentifiable, IDeletable, IUserTemplateItem
		{
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual UserOrganizationModel User { get; set; }
			public virtual long TemplateId { get; set; }
			public virtual UserTemplate Template { get; set; }
			public class UT_UserMap : ClassMap<UT_User>
			{
				public UT_UserMap()
				{
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					References(x => x.User).Column("UserId");
					Map(x => x.TemplateId).Column("TemplateId");
					References(x => x.Template).Column("TemplateId").LazyLoad().ReadOnly();
				}
			}
		}

		public class UT_Rock : ILongIdentifiable, IDeletable, IUserTemplateItem
		{
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual String Rock { get; set; }
			public virtual long PeriodId { get; set; }
			public virtual PeriodModel Period { get; set; }
			public virtual long TemplateId { get; set; }
			public virtual UserTemplate Template { get; set; }
			public class UT_RockMap : ClassMap<UT_Rock>
			{
				public UT_RockMap()
				{
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.Rock);
					Map(x => x.PeriodId).Column("PeriodId");
					References(x => x.Period).Column("PeriodId").LazyLoad().ReadOnly();
					Map(x => x.TemplateId).Column("TemplateId");
					References(x => x.Template).Column("TemplateId").LazyLoad().ReadOnly();
				}
			}

		}

		public class UT_Measurable : ILongIdentifiable, IDeletable, IUserTemplateItem
		{
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual String Measurable { get; set; }
			public virtual LessGreater GoalDirection { get; set; }
			public virtual decimal Goal { get; set; }
			public virtual long TemplateId { get; set; }
			public virtual UserTemplate Template { get; set; }
			public class UT_MeasurableMap : ClassMap<UT_Measurable>
			{
				public UT_MeasurableMap()
				{
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.Measurable);
					Map(x => x.GoalDirection);
					Map(x => x.Goal);
					Map(x => x.TemplateId).Column("TemplateId");
					References(x => x.Template).Column("TemplateId").LazyLoad().ReadOnly();
				}
			}
		}
	}
}