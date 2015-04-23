using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;

namespace RadialReview.Models.Components
{
	public class ForModel
	{
		public virtual long ModelId { get; set; }
		public virtual string ModelType { get; set; }

		public class ForModelMap : ComponentMap<ForModel>
		{
			public ForModelMap(){
				Map(x => x.ModelId);
				Map(x => x.ModelType);
			}
		}

		public static ForModel Create(ILongIdentifiable creator)
		{
			return new ForModel(){
				ModelId = creator.Id,
				ModelType = GetModelType(creator)
			};
		}

		public static ForModel Create<T>(long id) where T : ILongIdentifiable
		{
			return new ForModel(){
				ModelId = id,
				ModelType = GetModelType<T>()
			};
		}

		public static string GetModelType(ILongIdentifiable creator)
		{
			return HibernateSession.GetDatabaseSessionFactory().GetClassMetadata(creator.GetType()).EntityName;
		}
		public static string GetModelType<T>() where T : ILongIdentifiable
		{
			return HibernateSession.GetDatabaseSessionFactory().GetClassMetadata(typeof(T)).EntityName;
		}
	}
}