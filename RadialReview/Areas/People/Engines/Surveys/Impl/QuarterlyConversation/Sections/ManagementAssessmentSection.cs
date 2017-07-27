﻿using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
	public class ManagementAssessmentSection : ISectionInitializer {

		public bool SelfAssessment { get; private set; }

		public ManagementAssessmentSection(bool selfAssessment) {
			SelfAssessment = selfAssessment;
		}

		public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
			yield break;
		}

		public IEnumerable<IItemInitializer> GetItemBuildersSupervisorAssessment(IItemInitializerData data) {
			yield return new AssessmentItem(SurveyQuestionIdentifier.ManagementAssessment,
				"I keep expectations clear",
					"Mine and yours",
					"Roles, core values, rocks, and measurables"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.ManagementAssessment,
				"I am communicating well",
					"We know what's on each other's minds.",
					"We're not making assumptions",
					"I have a good question-to-statement ratio"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.ManagementAssessment,
				"We have the right meeting pulse",
					"We are meeting frequently enough",
					"We have an even exchange of dialogue",
					"We are keeping the circles connected"
			);
			yield return new AssessmentItem(SurveyQuestionIdentifier.ManagementAssessment,
				"I am rewarding and recognizing",
					"I give positive and negative feedback quickly, within 24 hours",
					"I criticize in private, praise in public"
			);

		}

		public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
			//Not reviewing our manager
			if (data.SurveyContainer.GetSurveyType()==SurveyType.QuarterlyConversation && data.About.Is<SurveyUserNode>()) {
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
			return new SurveySection(data, "Management Assessment", SurveySectionType.ManagementAssessment, "management-assessment");
		}

		public void Prelookup(IInitializerLookupData data) {
			//nothing to do
		}
	}
}
