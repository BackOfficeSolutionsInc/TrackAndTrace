using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Dashboard
{
	public enum TileType 
	{
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
		L10Issues,
		FAQGuide,
		Notifications,
		L10SolvedIssues,
        Tasks,
        CoreProcesses
	}

	public class TileModel : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual bool Hidden { get; set; }
		public virtual string DataUrl { get; set; }
		public virtual string Title { get; set; }
		public virtual int Width { get; set; }
		public virtual int Height { get; set; }
		public virtual int X { get; set; }
		public virtual int Y { get; set; }
		public virtual TileType Type { get; set; }
		public virtual UserModel ForUser { get; set; }
		public virtual Dashboard Dashboard { get; set; }
        public virtual string KeyId { get; set; }

		public TileModel()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class TileMap : ClassMap<TileModel>
		{
			public TileMap()
			{
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