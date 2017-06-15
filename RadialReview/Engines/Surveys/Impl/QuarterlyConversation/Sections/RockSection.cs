using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Engines.Surveys.Interfaces;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Engines.Surveys.Impl.QuarterlyConversation.Sections {

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
        }

        public ISection InitializeSection(ISectionInitializerData data) {
            return new SurveySection(data, "Rocks", SurveySectionType.Rocks);
        }

        public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
            var about = data.Survey.GetAbout();
            if (about.ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
                var rockLookup = data.Lookup.GetList<RockModel>();
                return rockLookup.Where(x => x.ForUserId == about.ModelId).Select(x => new RockItems(x));
            }
            return new List<IItemInitializer>();
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