using NHibernate;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Areas.People.Angular.Survey.SurveyAbout;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Traverse {
	public class TraverseBuildAboutAngular : TraverseBuild {
		private Action<AngularSurveyAboutContainer> SetSurveyContainer { get; set; }
		//private Dictionary<long, AngularSurvey> Surveys = new Dictionary<long, AngularSurvey>();
		private Dictionary<long, AngularSurveySection> Sections = new Dictionary<long, AngularSurveySection>();
		public string TemplateModifier { get; set; }
		public ISession Session { get; set; }
		public IForModel About { get; set; }

		public TraverseBuildAboutAngular(ISession s,IForModel about,Action<AngularSurveyAboutContainer> setter, string templateModifier = AngularSurveyItemFormat.DEFAULT_TEMPLATE_MODIFIER) {
			SetSurveyContainer = setter;
			TemplateModifier = templateModifier;
			Session = s;
			About = about;
		}

		private Dictionary<string, string> GenLookups(ISurveyContainer surveyContainer) {
			var allForModels = surveyContainer.GetSurveys().SelectMany(x => {
				var o = new List<IForModel>();
				o.Add(x.GetBy());
				o.Add(x.GetAbout());
				var others = x.GetSections().SelectMany(y => y.GetItemContainers()).Where(y => y.HasResponse()).SelectMany(y => {
					var byAbout = y.GetResponse().GetByAbout();
					return new[] { byAbout.GetBy(), byAbout.GetAbout() };
				});

				o.AddRange(others);
				return o;
			}).Distinct(x => x.ToKey()).ToList();

			var userIds = allForModels.Where(x => x.Is<UserOrganizationModel>()).Select(x => x.ModelId).ToArray();
			var nodeIds = allForModels.Where(x => x.Is<AccountabilityNode>()).Select(x => x.ModelId).ToArray();

			var users = Session.QueryOver<UserOrganizationModel>()
				.Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.Id).IsIn(userIds)
				.Future();

			var nodes = Session.QueryOver<AccountabilityNode>()
				.Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.Id).IsIn(nodeIds)
				.Fetch(x => x.User).Eager
				.Future();

			var output = new Dictionary<string, string>();
			foreach (var u in users) {
				var name = u.GetName();
				output[ForModel.Create(u).ToKey()] = name;
			}
			foreach (var u in nodes) {
				var name = u.User.NotNull(x=>x.GetName());
				output[ForModel.Create(u).ToKey()] = name;
			}

			//var userNames = users.ToList().ToDictionary(x => ForModel.Create(x).ToKey(), x => x.GetName());
			//var nodeNames = nodes.ToList().ToDictionary(x => ForModel.Create(x).ToKey(), x => x.User.NotNull(y => y.GetName()));


			//output.AddRange(userNames, x => x.Key, x => x.Value);
			//output.AddRange(nodeNames, x => x.Key, x => x.Value);
			
			return output;
		}

		private void AdjPrettyName(Dictionary<string, string> dict, AngularForModel forModel) {
			var name = dict[forModel.ToKey()];
			forModel.SetPrettyString(name);
		}


		public override void OnComplete(ISurveyContainer surveyContainer) {
			var lookup = GenLookups(surveyContainer);
			var allSurveys = surveyContainer.GetSurveys();
			var surveysAbout = allSurveys.GroupBy(x => x.GetAbout())
				.Where(x=>x.Key.ToKey()==About.ToKey()) // filter out extra surveys
				.ToList();
			var container = AngularSurveyAboutContainer.ConstructShallow(surveyContainer);
			foreach (var surveyAbouts in surveysAbout) {
				var allSectionsAbout = surveyAbouts.SelectMany(x => x.GetSections()).GroupBy(x=>x.GetSectionMergerKey()).ToList();
				var survey = AngularSurveyAbout.ConstructShallow(surveyAbouts.First());
				AdjPrettyName(lookup, survey.About);
				foreach (var sectionAbout in allSectionsAbout) {
					var section = AngularSurveyAboutSection.ConstructShallow(sectionAbout.First());					
					var allItemContainers = sectionAbout.SelectMany(x => x.GetItemContainers()).GroupBy(x => x.GetItemMergerKey()).ToList();
					foreach (var items in allItemContainers) {
						var itemContainer = AngularSurveyItemContainerAbout.ConstructShallow(items.First());
						foreach (var response in items) {
							if (response.HasResponse()){
								var r = new AngularSurveyResponse(response.GetResponse());
								AdjPrettyName(lookup, r.By);
								AdjPrettyName(lookup, r.About);
								itemContainer.Responses.Add(r);
							}
						}
						section.AppendItem(itemContainer);
					}
					survey.AppendSection(section);
				}
				container.AppendSurvey(survey);
			}
			SetSurveyContainer(container);
		}

		//public void AtSurveyContainer(ISurveyContainer child) {
		//	SurveyContainer.Id = child.Id;
		//	SurveyContainer.Name = child.GetName();
		//	SurveyContainer.Ordering = child.GetOrdering();
		//	SurveyContainer.SurveyType = child.GetSurveyType();
		//	SurveyContainer.Help = child.GetHelp();
		//	SurveyAboutDictionary = new Dictionary<IForModel, ISurveyAbout>();
		//}
		//protected Dictionary<IForModel, ISurveyAbout> SurveyAboutDictionary = new Dictionary<IForModel, ISurveyAbout>();
		//protected Dictionary<IForModel,Dictionary<string, ISectionAbout>> SectionGuidDictionary = new Dictionary<IForModel, Dictionary<string, ISectionAbout>>();
		//public void SurveyContainerToSurvey(ISurveyContainer parent, ISurvey child) {
		//	if (!SurveyAboutDictionary.ContainsKey(child.GetAbout())) {
		//		var survey = new AngularSurveyAbout(child);
		//		SurveyAboutDictionary[child.GetAbout()] = survey;
		//		SurveyContainer.AppendSurvey(survey);
		//	} else {
		//		SurveyAboutDictionary[child.GetAbout()].MergeWith(child);
		//	}
		//	//Surveys[child.Id]= survey;
		//}
		//public void SurveyToSection(ISurvey parent, ISection child) {
		//	//Get Appropriate survey
		//	var survey = SurveyAboutDictionary[parent.GetAbout()];
		//	var sections = survey.GetSections();
		//	var section = SectionGuidDictionary
		//					.GetOrAddDefault(parent.GetAbout(), x => new Dictionary<string, ISectionAbout>())
		//					.GetOrDefault(child.GetSectionMergerKey(), null);
		//	if (section==null) {
		//		var sectionAbout = new AngularSurveySectionAbout(child);
		//		survey.AppendSection(sectionAbout);
		//		SectionGuidDictionary[parent.GetAbout()][child.GetSectionMergerKey()] = sectionAbout;
		//	} else {
		//		section.MergeWith(child);
		//	}
		//	//Merge section
		//	var section = new AngularSurveySection(child);
		//	if (!SectionGuidDictionary.ContainsKey(child.GetSectionMergerKey())) {
		//		SectionGuidDictionary[child.GetSectionMergerKey()] = child;
		//	}
		//	var survey = Surveys[parent.Id];
		//	survey.AppendSection(section);
		//	Sections[child.Id] = section;
		//}
		//public void SectionToItem(ISection parent, IItemContainer child) {
		//	var itemContainer = new AngularSurveyItemContainer(child);
		//	var section = Sections[parent.Id];
		//	section.AppendItem(itemContainer);
		//}
	}
}