using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Components;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections {
    public class ValueSection : ISectionInitializer {
        public IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts) {
            yield return new ValueItem();
        }

        public IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data) {
            var values = data.Lookup.GetList<CompanyValueModel>();
			var genComments = new TextAreaItemIntializer("General Comments");
			var items = values.Select(x => (IItemInitializer) new ValueItem(x)).ToList();
			items.Add(genComments);
			return items;
        }

        public ISection InitializeSection(ISectionInitializerData data) {
            return new SurveySection(data, "Values", SurveySectionType.Values);
        }

        public void Prelookup(IInitializerLookupData data) {
            data.Lookup.AddList(OrganizationAccessor.GetCompanyValues_Unsafe(data.Session, data.OrgId));
        }
    }

    public class ValueItem : IItemInitializer {
        private CompanyValueModel CompanyValue;

        [Obsolete("Use other constructor")]
        public ValueItem() {
        }

        public ValueItem(CompanyValueModel value) {
            CompanyValue = value;
        }

        public IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx) {
            var options = new Dictionary<string, string> {
				{"often"    ,"Often"     },
                {"sometimes","Sometimes" },
				{"not-often","Not often" },
			};
            return ctx.RegistrationItemFormat(true, () => SurveyItemFormat.GenerateRadio(ctx,options));
        }

        public bool HasResponse(IResponseInitializerCtx data) {
            return true;
        }

        public IItem InitializeItem(IItemInitializerData data) {
            return new SurveyItem(data, CompanyValue.CompanyValue, ForModel.Create(CompanyValue)) {
                Help = CompanyValue.CompanyValueDetails
            };
        }

        public IResponse InitializeResponse(IResponseInitializerCtx ctx, IItemFormat format) {
            return new SurveyResponse(ctx, format);
        }

        public void Prelookup(IInitializerLookupData data) {
            //not needed
        }
    }
}