using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;
using RadialReview.Models.VTO;
using RadialReview.Models.Components;

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
		public virtual ForModel ForModel { get; set; }
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
				Component(x=>x.ForModel).ColumnPrefix("ForModel_");
			}
		}
	}

	public class L10AuditModel : AuditModel, ITimeline
	{
		public virtual L10Recurrence Recurrence { get; set; }
		public virtual String Action { get; set; }
		public virtual String Notes { get; set; }

		public virtual String CustomAnchor()
		{
			if (ForModel == null)
				return null;
			switch (Action)
			{
				case "CreateTodo": return "TodoModel-" + ForModel.ModelId;
				case "CreateIssue": return "IssueModel-" + ForModel.ModelId;
				default: return null;
			}
		}

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
	public class VtoAuditModel : AuditModel
	{
		public virtual VtoModel Vto { get; set; }
		public virtual String Action { get; set; }
		public virtual String Notes { get; set; }



		public class VtoAuditMap : SubclassMap<VtoAuditModel>
		{
			public VtoAuditMap()
			{
				References(x => x.Vto).Column("VtoId");
				Map(x => x.Action);
				Map(x => x.Notes).Length(1000);
			}
		}
	}
}