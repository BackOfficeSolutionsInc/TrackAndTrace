using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using RadialReview;
using RadialReview.Areas.People.Models.Survey;
using System.Diagnostics;

namespace RadialReview {
	[DebuggerDisplay("{FriendlyType()}: {ModelId}")]
	public class ForModel : IForModel {
		public virtual long ModelId { get; set; }
		public virtual string ModelType { get; set; }
		public virtual string _PrettyString { get; set; }

		public virtual ForModel Clone() {
			return new ForModel {
				ModelId = ModelId,
				ModelType = ModelType,
				_PrettyString= _PrettyString
			};
		}

		public class ForModelMap : ComponentMap<ForModel> {
			public ForModelMap() {
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

		public static ForModel Create<T>(long id) where T : ILongIdentifiable {
			return new ForModel() {
				ModelId = id,
				ModelType = GetModelType<T>()
			};
		}	

		public virtual string FriendlyType() {
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

		// override object.Equals
		public override bool Equals(object obj) {
			var found = (obj as IForModel);
			if (found == null)
				return false;
			return found.ModelId == ModelId && found.ModelType == ModelType;

		}

		// override object.GetHashCode
		public override int GetHashCode() {
			return HashUtil.Merge(ModelId.GetHashCode(), ModelType.GetHashCode());
		}

		public static string GetModelType(ILongIdentifiable creator) {
			return HibernateSession.GetDatabaseSessionFactory().GetClassMetadata(creator.GetType()).EntityName;
		}
		public static string GetModelType<T>() where T : ILongIdentifiable {
#pragma warning disable CS0618 // Type or member is obsolete
			return GetModelType(typeof(T));
#pragma warning restore CS0618 // Type or member is obsolete
		}
		[Obsolete("Use other methods")]
		public static string GetModelType(Type t) {
			return HibernateSession.GetDatabaseSessionFactory().GetClassMetadata(t).EntityName;

		}

		public bool Is<T>() {
#pragma warning disable CS0618 // Type or member is obsolete
			return ModelType == GetModelType(typeof(T));
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public string ToPrettyString() {
			return _PrettyString;
		}
	}

	public static class ForModelExtensions {
		public static ForModel ToImpl(this IForModel obj) {
			return ForModel.From(obj);
		}

		public static string ToKey(this IForModel obj) {
			return obj.NotNull(x=>x.ModelType + "_" + x.ModelId);
		}

		public static string ToKey(this IByAbout byAbout) {
			return byAbout.GetBy().ToKey() + "-" + byAbout.GetAbout().ToKey();
		}
	}
}

namespace RadialReview.Extensions {

	public static class ForModelExtensions {
		public static IForModel ForModelFromKey(this string obj) {
			if (obj == null)
				throw new ArgumentNullException("obj");
			var split = obj.Split('_');
			if (split.Length < 2)
				throw new ArgumentOutOfRangeException("Requires a string in the format <ModelType>_<ModelId>");

			return new ForModel() {
				ModelId = split.Last().ToLong(),
				ModelType = string.Join("_", split.Take(split.Length - 1))
			};
		}

		public static IByAbout ByAboutFromKey(this string obj) {
			if (obj == null)
				throw new ArgumentNullException("obj");
			var split = obj.Split('-');
			if (split.Length != 2)
				throw new ArgumentOutOfRangeException("Requires a string in the format <ByModelType>_<ByModelId>-<AboutModelType>_<AboutModelId>");

			return new ByAbout(ForModelFromKey(split[0]), ForModelFromKey(split[1]));
		}		
	}
}