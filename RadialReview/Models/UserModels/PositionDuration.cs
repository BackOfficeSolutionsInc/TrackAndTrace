using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.UserModels
{
	public class PositionDurationModel : IHistorical, ILongIdentifiable {
        [Obsolete("Did you mean Position.Id?")]
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }
		public virtual OrganizationPositionModel Position { get; set; }
        //public virtual long PositionId { get; set;}                       /// Issues with the Map(PositionId) and Reference(Position). Use caution.
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long? DeletedBy { get; set; }
		public virtual long PromotedBy { get; set; }
		public virtual long OrganizationId { get; set; }

		//public virtual DateTime CreateTime { get { return Start; } set { Start = value; } }

		public PositionDurationModel() {

		}

		public PositionDurationModel(OrganizationPositionModel position, long promotedBy, long forUserId) {
			Position = position;
			PromotedBy = promotedBy;
			CreateTime = DateTime.UtcNow;
			UserId = forUserId;
			OrganizationId = position.Organization.Id;

		}
		public class PositionDurationMap : ClassMap<PositionDurationModel> {
			public PositionDurationMap() {
#pragma warning disable CS0618 // Type or member is obsolete
				Id(x => x.Id);
#pragma warning restore CS0618 // Type or member is obsolete
				Map(x => x.CreateTime).Column("Start");
				Map(x => x.UserId);
				Map(x => x.DeletedBy);
				Map(x => x.DeleteTime);
				Map(x => x.PromotedBy);
				Map(x => x.OrganizationId);
				References(x => x.Position).Not.LazyLoad();
			}
		}
	} 
}