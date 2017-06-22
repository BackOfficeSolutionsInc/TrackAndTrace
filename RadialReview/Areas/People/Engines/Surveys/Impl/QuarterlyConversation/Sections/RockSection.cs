using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Accountability;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {

    public class RockSection : ISectionInitializer {
        //private IEnumerable<RockModel> rockLookup = new List<RockModel>();

        public RockSection() {
        }

        public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
            return new[] { new RockItems(null) };
        }

        public void Prelookup(IInitializerLookupData data) {
			var rocks = data.Session.QueryOver<RockModel>().Where(x => x.DeleteTime == null && x.OrganizationId == data.OrgId).Future();
			data.Lookup.AddList(rocks);

			var nodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<AccountabilityNode>()).Select(x => x.ModelId).ToArray();
			if (nodeIds.Any()) {
				data.Lookup.AddList( data.Session.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(nodeIds).Future());
			}			
		}

        public ISection InitializeSection(ISectionInitializerData data) {
            return new SurveySection(data, "Rocks", SurveySectionType.Rocks);
        }

        public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
			var items = new List<IItemInitializer>();
            var about = data.Survey.GetAbout();
			if (about.ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
				var rockLookup = data.Lookup.GetList<RockModel>();
				var rocks = rockLookup.Where(x => x.ForUserId == about.ModelId).Select(x => new RockItems(x));
				items.AddRange(rocks);
			} else if (about.ModelType == ForModel.GetModelType<AccountabilityNode>()) {
				var rockLookup = data.Lookup.GetList<RockModel>();
				var accNodeLookup = data.Lookup.GetList<AccountabilityNode>();

				var node = accNodeLookup.FirstOrDefault(x => x.Id == about.ModelId);
				if (node != null) {
					var userId = node.UserId;
					var rocks = rockLookup.Where(x => x.ForUserId == userId).Select(x => new RockItems(x));
					items.AddRange(rocks);
					
				}
			}
			if (!items.Any())
				items.Add(new TextItemIntializer("No rocks.",true));
			items.Add(new TextAreaItemIntializer("General Comments"));
			return items;
        }
    }

    public class RockItems : IItemInitializer {
        private RockModel Rock;

        public RockItems(RockModel rock) {
            Rock = rock;
        }

        public IItem InitializeItem(IItemInitializerData data) {
            return new SurveyItem(data, Rock.Rock, ForModel.Create(Rock));
        }

        public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
            var options = new Dictionary<string, string>() {
                    { "done","Done" },
                    { "not-done","Not Done" },
            };
            return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(ctx,options));
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