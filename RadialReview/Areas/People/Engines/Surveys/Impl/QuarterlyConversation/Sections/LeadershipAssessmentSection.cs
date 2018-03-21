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
				"They are giving clear direction",
					"They are creating the opening",
					"They have shared a compelling vision that they are honestly excited about",
					"The V/TO™ is clear and their actions are aligned with it"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"They are providing the necessary tools",
					"Resources",
					"Training",
					"Technology",
					"People",
					"Time and attention"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"They are letting go of the vine",
					"They are delegating work appropriately",
					"They are not micromanaging",
					"Our team members Get, Want, and have the Capacity to perform their roles well"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"They act with the greater good in mind",
					"Their actions and decisions are aligned with the company vision",
					"They walk the talk",
					"They put the company’s needs first"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"They are taking Clarity Breaks™",
					"They're focused \"On\" the business"
				);
		}

		public IEnumerable<IItemInitializer> GetItemBuildersSupervisorSelfAssessment(IItemInitializerData data) {

			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"I am giving clear direction",
					"I am creating the opening",
					"I have a compelling vision",
					"The V/TO™ is clear and my actions are aligned with it"
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
					"I Delegate and Elevate™",
					"GWC™"
			//"Our team members Get, Want, and have the Capacity to perform their roles well"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"I act with the greater good in mind",
					"Company vision (V/TO™)",
					"My actions",
					"My decisions",
					"I walk the talk",
					"I put the company's needs first"

			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.LeadershipAssessment,
				"I am taking Clarity Breaks™",
					"Focused \"On\" the business",
					"Creating clarity",
					"Protecting my confidence",
					"With the right frequency (daily, weekly, or monthly)",
					"Using a blank legal pad"
				);
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
				if (data.FirstSeenByAbout()) {
					var sun = (data.About as SurveyUserNode);
					if (sun != null && sun._Relationship != null && sun._Relationship[data.By.ToKey()].HasFlag(AboutType.Self))
						return GetItemBuildersSupervisorSelfAssessment(data);
					return GetItemBuildersSupervisorSelfAssessment(data); //GetItemBuildersSupervisorAssessment(data);
				}
				return new List<IItemInitializer>();
			}
		}

		public ISection InitializeSection(ISectionInitializerData data) {
			var sun = data.About as SurveyUserNode;
			var help = "Would you say your boss could say yes to...";
			if (sun != null && sun._Relationship != null && sun._Relationship[data.Survey.GetBy().ToKey()].HasFlag(AboutType.Self))
				help = "";

			return new SurveySection(data, "Leadership Self-Assessment", SurveySectionType.LeadershipAssessment, "leadership-assessment") {
				Help = help
			};
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
			return new SurveyItem(data, Name, null, Name, Help);
		}

		public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
			var options = new Dictionary<string, string>() {
					{ "yes","Yes" },
					{ "no","No" },
			};
			return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(ctx, QuestionIdentifier, options), Name);
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
