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
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {

    public class RockSection : ISectionInitializer {
        //private IEnumerable<RockModel> rockLookup = new List<RockModel>();
        public static String RockCommentHeading = "Rock Quality/Comments";
        public DateRange SearchRange { get; set; }


        public RockSection(DateRange searchRange) {
            SearchRange = searchRange;
        }

        public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
            return new[] { new RockItems(null) };
        }

        public void Prelookup(IInitializerLookupData data) {
            var rocks = data.Session.QueryOver<RockModel>()
                .Where(x=>x.CreateTime>=SearchRange.StartTime && x.CreateTime<=SearchRange.EndTime)
                .Where(x => x.OrganizationId == data.OrgId)
                .Future();

            data.Lookup.AddList(rocks);

            var nodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<AccountabilityNode>()).Select(x => x.ModelId).ToArray();
            if (nodeIds.Any()) {
                data.Lookup.AddList(data.Session.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(nodeIds).Future());
            }

            var surveyUserNodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<SurveyUserNode>()).Select(x => x.ModelId).ToArray();
            if (surveyUserNodeIds.Any()) {
                data.Lookup.AddList(
                    data.Session.QueryOver<SurveyUserNode>()
                        .WhereRestrictionOn(x => x.Id).IsIn(surveyUserNodeIds)
                        .Fetch(x => x.AccountabilityNode).Eager
                        .Fetch(x => x.User).Eager
                        .Future()
                );
                //data.Lookup.AddList(data.Session.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(surveyUserNodeIds).Future());
            }
        }

        public ISection InitializeSection(ISectionInitializerData data) {
            return new SurveySection(data, "Rocks", SurveySectionType.Rocks, "mk-rocks");
        }

        private List<IItemInitializer> GetRocksForAccountabilityNode(IItemInitializerData data, AccountabilityNode about) {
            var accNodeLookup = data.Lookup.GetList<AccountabilityNode>();

            var items = new List<IItemInitializer>();
            var node = accNodeLookup.FirstOrDefault(x => x.Id == about.ModelId);
            if (node != null) {
                items.AddRange(GetRockForUserId(data, node.UserId));
            }
            return items;
        }

        private static IEnumerable<IItemInitializer> GetRockForUserId(IItemInitializerData data, long? userId) {
            var rockLookup = data.Lookup.GetList<RockModel>();
            return rockLookup.Where(x => x.ForUserId == userId).Select(x => new RockItems(x));
        }

        public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {

            var dict = data.Lookup.GetOrAdd("RockSectionAlreadyGenerated", (_str) => new DefaultDictionary<string, bool>(x => false));
            var byAboutKey = data.By.ToKey() + "-" + data.About.ToKey();
            if (data.About.Is<SurveyUserNode>()) {
                byAboutKey = data.By.ToKey() + "-" + ((SurveyUserNode)data.About).User.ToKey();
            }
            var alreadyGenerated = dict[byAboutKey];

            if (!alreadyGenerated) {
                dict[byAboutKey] = true;

                //only ask if they are not our manager
                if (data.SurveyContainer.GetSurveyType() == SurveyType.QuarterlyConversation && data.About.Is<SurveyUserNode>()) {

                    if (data.SurveyContainer.GetCreator().ToKey() == ((SurveyUserNode)data.About).User.ToKey())
                        return new List<IItemInitializer>();

                    if ((data.About as SurveyUserNode)._Relationship[data.By.ToKey()] == AboutType.Manager)
                        return new List<IItemInitializer>();

                }

                var items = new List<IItemInitializer>();
                var about = data.Survey.GetAbout();
                if (about.ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
                    var rockLookup = data.Lookup.GetList<RockModel>();
                    var rocks = rockLookup.Where(x => x.ForUserId == about.ModelId).Select(x => new RockItems(x));
                    items.AddRange(rocks);
                } else if (about.ModelType == ForModel.GetModelType<AccountabilityNode>()) {
                    items.AddRange(GetRocksForAccountabilityNode(data, (AccountabilityNode)about));
                } else if (about.ModelType == ForModel.GetModelType<SurveyUserNode>()) {
                    var node = data.Lookup.GetList<SurveyUserNode>().First(x => x.Id == about.ModelId).AccountabilityNode;
                    items.AddRange(GetRockForUserId(data, node.UserId));
                }
                if (!items.Any())
                    items.Add(new TextItemIntializer("No rocks.", true));
                items.Add(new InputItemIntializer(RockCommentHeading, SurveyQuestionIdentifier.GeneralComment));
                return items;
            }
            return new IItemInitializer[] { };
        }
    }

    public class RockItems : IItemInitializer {
        private RockModel Rock;

        public RockItems(RockModel rock) {
            Rock = rock;
        }

        public IItem InitializeItem(IItemInitializerData data) {
            var forModel = ForModel.Create(Rock);
            return new SurveyItem(data, Rock.Rock, forModel, forModel.ToKey());
        }

        public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
            var options = new Dictionary<string, string>() {
                    { "done","Done" },
                    { "not-done","Not Done" },
            };
            return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(ctx, SurveyQuestionIdentifier.Rock, options));
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