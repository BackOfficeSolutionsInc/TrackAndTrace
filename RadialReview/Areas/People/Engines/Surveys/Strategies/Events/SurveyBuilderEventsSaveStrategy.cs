using NHibernate;
using RadialReview.Areas.People.Engines.Surveys.Impl;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Events {
    public class SurveyBuilderEventsSaveStrategy : ISurveyBuilderEvents {

        protected ISession Session;
        public SurveyBuilderEventsSaveStrategy(ISession session) {
            Session = session;
        }

        public void OnInitialize(IComponent component) {
            Session.Save(component);
        }

        public void OnBegin(ISurveyInitializer builder,long orgId,IOuterLookup outerLookup, IEnumerable<IByAbout> byAbouts) {
            var surveyBuilder = builder;

            var data = new PrelookupData(Session, orgId, outerLookup, byAbouts);
            data.SetLookup(surveyBuilder.GetType());
            surveyBuilder.Prelookup(data);
            foreach (var sectionBuilder in surveyBuilder.GetAllPossibleSectionBuilders(byAbouts)) {
                data.SetLookup(sectionBuilder.GetType());
                sectionBuilder.Prelookup(data);
                foreach (var itemBuilder in sectionBuilder.GetAllPossibleItemBuilders(byAbouts)) {
                    data.SetLookup(itemBuilder.GetType());
                    itemBuilder.Prelookup(data);
                }
            }
        }

        public void OnEnd(ISurveyContainer container) {
        }

        public class PrelookupData : IInitializerLookupData {
            public IEnumerable<IByAbout> ByAbouts { get; private set; }
            public IInnerLookup Lookup { get; private set; }
            public long OrgId { get; private set; }
            public ISession Session { get; private set; }
            protected IOuterLookup OuterLookup { get; private set; }

			private DateTime _Now = DateTime.UtcNow;
			public DateTime Now {get { return _Now; }}

			public PrelookupData(ISession session, long orgId, IOuterLookup outerLookup, IEnumerable<IByAbout> byAbouts) {
                ByAbouts = byAbouts;
                OrgId = orgId;
                Session = session;
                OuterLookup = outerLookup;
                Lookup = null;
            }

            public void SetLookup(Type type) {
                Lookup = OuterLookup.GetInnerLookup(type);
            }

        }
    }
}

//protected void InitSurvey(AbstractSurveyContainerBuilder containerBuilder, ISurveyBuilder surveyBuilder) {
//    surveyBuilder.Prequery(Session);
//    var fakeSurveyContainer = containerBuilder.InitializeSurveyContainer();
//    var fakeSurvey = containerBuilder.InitializeSurvey(fakeSurveyContainer,)
//    foreach (var survey in surveyBuilder.GetSectionBuilders()) { }

//}
//  }