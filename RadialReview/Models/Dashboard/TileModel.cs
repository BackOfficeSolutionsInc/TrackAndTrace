using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System.Web.Script.Serialization;
using System.Runtime.Serialization;

namespace RadialReview.Models.Dashboard {
	public enum TileType {
		//DO NOT REORDER
		Invalid = 0,
		Profile,
		Scorecard,
		Todo,
		Roles,
		Rocks,
		Values,
		Manage,
		Url,
		L10Todos,
		L10Scorecard,
		L10Rocks,
		Headlines,
		L10Issues,
		FAQGuide,
		Notifications,
		L10SolvedIssues,
		Tasks,
		CoreProcesses,
		Milestones
	}


	public class TileTypeBuilder {


		public TileType Type { get; private set; }
		public string DataUrl { get; private set; }
		public string KeyId { get; private set; }

		private TileTypeBuilder(TileType type, string dataUrl, string keyId = null) {
			Type = type;
			DataUrl = dataUrl;
			KeyId = keyId;
		}

		public static TileTypeBuilder L10Scorecard(long recurrenceId) {
			return new TileTypeBuilder(TileType.L10Scorecard, "/TileData/L10Scorecard/" + recurrenceId, "" + recurrenceId);
		}
		public static TileTypeBuilder L10Rocks(long recurrenceId) {
			return new TileTypeBuilder(TileType.L10Rocks, "/TileData/L10Rocks/" + recurrenceId, "" + recurrenceId);
		}
		public static TileTypeBuilder L10Todos(long recurrenceId) {
			return new TileTypeBuilder(TileType.L10Todos, "/TileData/L10Todos/" + recurrenceId, "" + recurrenceId);
		}
		public static TileTypeBuilder L10Issues(long recurrenceId) {
			return new TileTypeBuilder(TileType.L10Issues, "/TileData/L10Issues/" + recurrenceId, "" + recurrenceId);
		}
		public static TileTypeBuilder L10PeopleHeadlines(long recurrenceId) {
			return new TileTypeBuilder(TileType.L10Issues, "/TileData/L10Headlines/" + recurrenceId, "" + recurrenceId);
		}

	}


	public class TileModel : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual bool Hidden { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual string DataUrl { get; set; }
		public virtual string Title { get; set; }
		public virtual int Width { get; set; }
		public virtual int Height { get; set; }
		public virtual int X { get; set; }
		public virtual int Y { get; set; }
		public virtual TileType Type { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual Dashboard Dashboard { get; set; }
		[ScriptIgnore]
		[IgnoreDataMember]
		public virtual UserModel ForUser { get; set; }
		public virtual string KeyId { get; set; }
		public virtual bool ShowPrintButton { get; set; }

		public TileModel() {
			CreateTime = DateTime.UtcNow;
		}

		public TileModel(int x, int y, int width, int height, string title, TileTypeBuilder type, Dashboard dashboard, DateTime createTime) {
			CreateTime = createTime;
			Dashboard = dashboard;
			Title = title;
			Width = width;
			Height = height;
			X = x;
			Y = y;
			Type = type.Type;
			KeyId = type.KeyId;
			DataUrl = type.DataUrl;
		}

		public class TileMap : ClassMap<TileModel> {
			public TileMap() {
				Id(x => x.Id);
				Map(x => x.KeyId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.DataUrl);
				Map(x => x.Hidden);
				Map(x => x.Title);
				Map(x => x.Width);
				Map(x => x.Height);
				Map(x => x.X);
				Map(x => x.Y);
				Map(x => x.Type).CustomType<TileType>();
				References(x => x.ForUser).LazyLoad();
				References(x => x.Dashboard).LazyLoad();

			}
		}

	}
}