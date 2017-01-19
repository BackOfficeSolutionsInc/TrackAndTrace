using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Askables {
	public class ResponsibilityModel : Askable, ILongIdentifiable {
		public virtual long ForOrganizationId { get; set; }
		public virtual long ForResponsibilityGroup { get; set; }
		public virtual String Responsibility { get; set; }
		public virtual bool Anonymous { get; set; }
		public virtual String Arguments { get; set; }

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
				Map(x => x.Anonymous);
				Map(x => x.QuestionType).CustomType<QuestionType>().Default("" + (int)Enums.QuestionType.Slider);
				References(x => x.Category).Not.LazyLoad();

				Map(x => x.Arguments).Length(1024);
			}
		}
	}
}