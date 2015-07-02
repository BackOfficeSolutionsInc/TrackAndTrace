using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;

namespace RadialReview.Models.Audit
{
	public class AuditModel: ILongIdentifiable,IHistorical
	{
		public virtual long Id { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual UserModel User { get; set; }
		public virtual UserOrganizationModel UserOrganization { get; set; }

		public virtual String Path { get; set; }

		public virtual String Query { get; set; }

		public virtual String Data { get; set; }
		public virtual String Method { get; set; }

		public virtual String UserAgent { get; set; }

		public AuditModel()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class AuditMap : ClassMap<AuditModel>
		{
			public AuditMap()
			{
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Path);
				Map(x => x.Query);
				Map(x => x.Data).Length(10000);
				Map(x => x.Method);
				Map(x => x.UserAgent);
				References(x => x.User).Column("UserId").Nullable();
				References(x => x.UserOrganization).Column("UserOrganizationId").Nullable();
			}
		}
	}

	public class L10AuditModel : AuditModel
	{
		public virtual L10Recurrence Recurrence { get; set; }
		public virtual String Action { get; set; }
		public virtual String Notes { get; set; }



		public class L10AuditMap : SubclassMap<L10AuditModel>
		{
			public L10AuditMap()
			{
				References(x => x.Recurrence).Column("RecurrenceId");
				Map(x => x.Action);
				Map(x => x.Notes).Length(1000);
			}	
		}
	}
}