using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;

namespace RadialReview.Models.Components
{
	public class ForModel : IForModel
	{
		public virtual long ModelId { get; set; }
		public virtual string ModelType { get; set; }


		public virtual ForModel Clone() {
			return new ForModel{
				ModelId= ModelId,
				ModelType=ModelType,
			};
		}
        
		public class ForModelMap : ComponentMap<ForModel>
		{
			public ForModelMap(){
				Map(x => x.ModelId);
				Map(x => x.ModelType);
			}
		}

        public static ForModel Create(ILongIdentifiable creator) {
            return new ForModel() {
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

		public virtual string FriendlyType()
		{
			if (ModelType == null)
				return null;
			return ModelType.Split('.').Last();
		}

        public static ForModel From(IForModel model) {
            return new ForModel() {
                ModelId = model.ModelId,
                ModelType = model.ModelType

            };
        }

        public static string GetModelType(ILongIdentifiable creator)
		{
			return HibernateSession.GetDatabaseSessionFactory().GetClassMetadata(creator.GetType()).EntityName;
		}
		public static string GetModelType<T>() where T : ILongIdentifiable
		{
            return GetModelType(typeof(T));
		}
        [Obsolete("Use other methods")]
        public static string GetModelType(Type t) {
			return HibernateSession.GetDatabaseSessionFactory().GetClassMetadata(t).EntityName;

        }

        public bool Is<T>() {            
            return ModelType == GetModelType(typeof(T));
        }
    }
}