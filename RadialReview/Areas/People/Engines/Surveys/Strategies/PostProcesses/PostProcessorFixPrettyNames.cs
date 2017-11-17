using NHibernate;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.PostProcesses {
	public class PostProcessorFixPrettyNames : IPostProcessor {
		public ISession Session { get; set; }

		public PostProcessorFixPrettyNames(ISession s) {
			Session = s;
		}

		public void Process(ISurveyContainer surveyContainer) {
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

			var userNames = users.ToDictionary(x => ForModel.Create(x).ToKey(), x => x.GetName());
			var nodeNames = nodes.ToDictionary(x => ForModel.Create(x).ToKey(), x => x.User.NotNull(y => y.GetName()));




		}
	}
}