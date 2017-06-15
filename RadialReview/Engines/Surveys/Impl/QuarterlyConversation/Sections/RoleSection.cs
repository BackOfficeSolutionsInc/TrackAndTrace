using RadialReview.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models;
using RadialReview.Accessors;
using static RadialReview.Accessors.RoleAccessor;
using RadialReview.Areas.People.Models.Survey;

namespace RadialReview.Engines.Surveys.Impl.QuarterlyConversation.Sections {
    public class RoleSection : ISectionInitializer {
        public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
            yield return new RoleListItem();
            yield return new RoleResponseItem();
        }

        public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
            if (data.Survey.GetAbout().ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
                var query = data.Lookup.Get<RoleLinksQuery>("RoleQuery");
                if (query != null) {
                    var roles = query.GetRoleDetailsForUser(data.Survey.GetAbout().ModelId);

                    var roleItems = roles.Select(x => (IItemInitializer)new RoleListItem(x));
                    var roleReponses = new[] {
                    new RoleResponseItem("Gets it",             "get"   ),
                    new RoleResponseItem("Wants it",            "want"  ),
                    new RoleResponseItem("Capacity to do it",   "cap"   ),
                };

                    return roleItems.Union(roleReponses);
                }
                return new List<IItemInitializer>();
            }
            return new List<IItemInitializer>();
        }

        public ISection InitializeSection(ISectionInitializerData data) {
            return new SurveySection(data, "Roles", SurveySectionType.Roles);
        }

        public void Prelookup(IInitializerLookupData data) {
            data.Lookup.Add("RoleQuery", RoleAccessor.GetRolesForOrganization_Unsafe(data.Session, data.OrgId));
        }
    }

    public class RoleResponseItem : IItemInitializer {
        public String Title { get; set; }
        public String Value { get; set; }

        public RoleResponseItem(string title, string value) {
            Title = title;
            Value = value;
        }
        [Obsolete("Use other constructor.")]
        public RoleResponseItem() {
        }

        public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx data) {
            var options = new Dictionary<string, string>() {
                {"yes", "Yes" },
                {"no",  "No" },
            };

            return data.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(data,options), Value);
        }

        public bool HasResponse(IResponseInitializerCtx data) {
            return true;
        }

        public IItem InitializeItem(IItemInitializerData data) {
            return new SurveyItem(data, Title, null);
        }

        public IResponse InitializeResponse(IResponseInitializerCtx ctx, IItemFormat format) {
            return new SurveyResponse(ctx, format);
        }

        public void Prelookup(IInitializerLookupData data) {
            //nothing to do
        }
    }

    public class RoleListItem : IItemInitializer {
        private RoleLinksQuery.RoleDetails RoleDetails;

        [Obsolete("Use other constructor")]
        public RoleListItem() {
        }

        public RoleListItem(RoleLinksQuery.RoleDetails x) {
            RoleDetails = x;
        }

        public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {            
            return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateText(ctx));
        }

        public bool HasResponse(IResponseInitializerCtx data) {
            return false;
        }

        public IItem InitializeItem(IItemInitializerData data) {
            return new SurveyItem(data, RoleDetails.Role.Role,ForModel.Create(RoleDetails.Role));
        }

        public IResponse InitializeResponse(IResponseInitializerCtx data, IItemFormat format) {
            throw new NotImplementedException();
        }

        public void Prelookup(IInitializerLookupData data) {
            //nothing to do
        }
    }
}