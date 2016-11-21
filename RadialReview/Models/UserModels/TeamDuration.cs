using FluentNHibernate.Mapping;
using RadialReview.Exceptions;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.UserModels
{
	public class TeamDurationModel : IDeletable, ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }
		public virtual long TeamId { get; set; }
		public virtual long OrganizationId { get; set; }

		public virtual UserOrganizationModel User { get; set; }
		public virtual OrganizationTeamModel Team { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long? DeletedBy { get; set; }
		public virtual long AddedBy { get; set; }

		//public virtual DateTime CreateTime { get { return CreateTime; } set { CreateTime = value; } }
		//public virtual long OrganizationId { get; set; }

		public TeamDurationModel() {
		}

		public TeamDurationModel(UserOrganizationModel forUser, OrganizationTeamModel team, long addedBy) {
			OrganizationId = forUser.Organization.Id;

			if (forUser.Organization.Id != team.Organization.Id)
				throw new PermissionsException("User and team must be at the same organization");


			TeamId = team.Id;
			UserId = forUser.Id;
			Team = team;
			AddedBy = addedBy;
			CreateTime = DateTime.UtcNow;
			User = forUser;
		}


		public class TeamDurationMap : ClassMap<TeamDurationModel> {
			public TeamDurationMap() {
				Id(x => x.Id);
				Map(x => x.CreateTime).Column("Start");
				Map(x => x.AddedBy);
				Map(x => x.DeletedBy);
				Map(x => x.DeleteTime);

				Map(x => x.OrganizationId);

				Map(x => x.UserId).Column("User_id");
				Map(x => x.TeamId).Column("Team_id");

				References(x => x.User)
					.Column("User_id")
					.Not.LazyLoad()
					.ReadOnly();
				References(x => x.Team)
					.Column("Team_id")
					.Not.LazyLoad()
					.ReadOnly();
			}
		}
	}
}