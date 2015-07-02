using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Audit
{
	public enum LogType
	{
		Unspecified = 0,
		ChangeProducts = 1,


	}

	public class LogModel : ILongIdentifiable, IDeletable
	{
		public virtual long Id { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual ForModel Creator { get; set; }
		public virtual ForModel About { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual LogType LogType { get; set; }
		public virtual String Message { get; set; }
		public virtual OrganizationModel Organization { get; set; }

		[Obsolete("Use Static Constructor")]
		public LogModel()
		{
			CreateTime = DateTime.UtcNow;
		}

		public static LogModel Create(UserOrganizationModel creator, LogType logtype, ILongIdentifiable forModel, String message = null, DateTime? now = null)
		{
			return new LogModel(){
				About = ForModel.Create(forModel),
				Creator = ForModel.Create(creator),
				LogType = logtype,
				Message = message,
				Organization = creator.Organization,
				CreateTime = now ?? DateTime.UtcNow,
			};
		}

		public class LogModelMap : ClassMap<LogModel>
		{
			public LogModelMap()
			{
				Id(x => x.Id);
				Map(x => x.DeleteTime);
				Component(x => x.Creator).ColumnPrefix("Creator_");
				Component(x => x.About).ColumnPrefix("About_");
				Map(x => x.CreateTime);
				Map(x => x.LogType).CustomType<LogType>();
				Map(x => x.Message);
				References(x => x.Organization).Nullable().Column("OrganizationId");
			}
		}
	}
}