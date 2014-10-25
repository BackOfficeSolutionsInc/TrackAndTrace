using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Responsibilities {
	public class ResponsibilityModel : Askable, ILongIdentifiable {
		public virtual long ForOrganizationId { get; set; }
		public virtual long ForResponsibilityGroup { get; set; }
		public virtual String Responsibility { get; set; }

		protected virtual QuestionType? QuestionType { get; set; }

		public ResponsibilityModel(): base() {
		}

		public override QuestionType GetQuestionType()
		{
			if (QuestionType == null)
				QuestionType = Enums.QuestionType.Slider;

			return QuestionType.Value;
		}
		public virtual void SetQuestionType(QuestionType questionType)
		{
			QuestionType = questionType;
		}

		public override string GetQuestion() {
			return Responsibility;
		}
		public class ResponsibilityModelMap : SubclassMap<ResponsibilityModel> {
			public ResponsibilityModelMap() {
				Map(x => x.Responsibility).Length(65000);
				Map(x => x.ForOrganizationId);
				Map(x => x.ForResponsibilityGroup);
				Map(x => x.QuestionType).CustomType<QuestionType>().Default(""+(int)Enums.QuestionType.Slider);
				References(x => x.Category).Not.LazyLoad();
			}
		}
	}
}