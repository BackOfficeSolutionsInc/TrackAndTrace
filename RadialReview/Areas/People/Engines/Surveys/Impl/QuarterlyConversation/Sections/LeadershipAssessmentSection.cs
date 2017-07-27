using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Enums;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
	public class LeadershipAssessmentSection : ISectionInitializer {
		public bool SelfAssessment { get; private set; }

		public LeadershipAssessmentSection(bool selfAssessment) {
			SelfAssessment = selfAssessment;
		}

		public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
			yield break;
		}

		public IEnumerable<IItemInitializer> GetItemBuildersSupervisorAssessment(IItemInitializerData data) {

			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"I am giving clear direction",
					"Creating the opening",
					"I have shared a compelling vision that you are honestly excited about",
					"Our V/TO™ is clear and my actions are aligned with it"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"I am providing the necessary tools",
					"Resources",
					"Training",
					"Technology",
					"People",
					"Time and attention"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"I am letting go of the vine",
					"I am delegating work appropriately",
					"I am not micromanaging",
					"Our team members Get, Want, and have the Capacity to perform their roles well"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"I act with the greater good in mind",
					"My actions and decisions are aligned with the company vision",
					"I walk the talk",
					"I put the company’s needs first"
			);
			//yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
			//	"I act with the greater good in mind",
			//		"My actions and decisions are aligned with the company vision",
			//		"I walk the talk",
			//		"I put the companies needs first"
			//);
		}

		public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
			//Not reviewing our manager
			if (data.SurveyContainer.GetSurveyType() == SurveyType.QuarterlyConversation && data.About.Is<SurveyUserNode>()) {
				if (!(data.About as SurveyUserNode)._Relationship[data.By.ToKey()].HasFlag(AboutType.Manager)) {
					return new List<IItemInitializer>();
				}
			}

			if (SelfAssessment) {
				throw new NotImplementedException();
			} else {
				return GetItemBuildersSupervisorAssessment(data);
			}
		}

		public ISection InitializeSection(ISectionInitializerData data) {
			return new SurveySection(data, "Leadership Assessment", SurveySectionType.LeadershipAssessment, "leadership-assessment");
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}

	public class AssessmentItem : IItemInitializer {
		public string Help { get; private set; }
		public string Name { get; private set; }
		public SurveyQuestionIdentifier QuestionIdentifier { get; private set; }

		public string Bullets(params string[] items) {
			return string.Join("\n", items.Select(x => "• " + x));
		}

		public AssessmentItem(SurveyQuestionIdentifier questionIdentifier, string name, params string[] bullets) {
			Name = name;
			Help = Bullets(bullets);
			QuestionIdentifier = questionIdentifier;
		}

		public IItem InitializeItem(IItemInitializerData data) {
			return new SurveyItem(data, Name, null, Name,Help);
		}

		public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
			var options = new Dictionary<string, string>() {
					{ "yes","Yes" },
					{ "no","No" },
			};
			return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(ctx, QuestionIdentifier, options),Name);
		}

		public bool HasResponse(IResponseInitializerCtx data) {
			return true;
		}

		public IResponse InitializeResponse(IResponseInitializerCtx data, IItemFormat format) {
			return new SurveyResponse(data, format);
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}
}