using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using NHibernate.Type;
using RadialReview.Utilities;

namespace RadialReview.Models.Synchronize
{
	public class Sync : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }
		/// <summary>
		/// The client timestamp
		/// </summary>
		public virtual long Timestamp { get; set; }
		public virtual String Action { get; set; }
		[Obsolete("Not accurate, use DbTimestamp instead")]
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime DbTimestamp{ get; set; }


		public Sync(){
			CreateTime = DateTime.UtcNow;
		}

		public class SyncMap : ClassMap<Sync>
		{
			public SyncMap()
			{
				Id(x => x.Id);
				Map(x => x.Timestamp);
				Map(x => x.UserId);
				Map(x => x.Action);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				switch (Config.GetDatabaseType()) {
					case Config.DbType.MySql:
						Map(c => c.DbTimestamp).CustomSqlType("timestamp").Length(3).Generated.Insert();
						break;
					case Config.DbType.Sqlite:
						Map(c => c.DbTimestamp).Default("CURRENT_TIMESTAMP").Generated.Insert();
						break;
					default: throw new Exception("Unknown Db Type. Cannot Create Sync model");
				}

			}
		}
	}
}

//"strftime(""%Y-%m-%d %H:%M:%f"", ""now"")"