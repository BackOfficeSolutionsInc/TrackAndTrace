using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Impl;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Events;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Traverse;

namespace RadialReview.Areas.People.Engines.Surveys {
    public class SurveyReconstructionEngine {
        protected IOuterLookup OuterLookup { get; set; }
        protected ISurveyReconstructionAggregator Aggregator { get; set; }
        protected ISurveyReconstructorEvents EventHandler { get; set; }

        public long OrgId { get; protected set; }
        public long SurveyContainerId { get; protected set; }
        public Cache CacheData { get; private set; }

        public SurveyReconstructionEngine(long surveyContainerId, long orgId, ISurveyReconstructionAggregator aggregator,ISurveyReconstructorEvents events, IOuterLookup outerLookup = null) {
            Aggregator = aggregator;
            EventHandler = events ?? new SurveyReconstructionEventsNoOp();
            OuterLookup = outerLookup ?? new OuterLookup();

            OrgId = orgId;
            SurveyContainerId = surveyContainerId;
            EventHandler.OnBegin(OuterLookup);

            //Prelookups
            var data = new Data(this);
            data.SetLookup(Aggregator.GetType());
            Aggregator.Prelookup(data);

            CacheData = new Cache();
            //Gather all data
            CacheData.SurveyContainer = Aggregator.GetSurveysContainer(data);
            CacheData.AllSurveys = Aggregator.GetAllSurveys(data).Where(x=>x.GetSurveyContainerId()==SurveyContainerId);
            CacheData.AllSections = Aggregator.GetAllSections(data);
            CacheData.AllItems = Aggregator.GetAllItems(data);
            CacheData.AllItemFormats = Aggregator.GetAllItemFormats(data);
            CacheData.AllResponses = Aggregator.GetAllResponses(data);

        }

        public ISurveyContainer ReconstructSurveyContainer() {
            //Build it 
            var builder = new TraverseBuild();
            Traverse(builder);
            return builder.SurveyContainer;
        }

        public void Traverse(ISurveyTraverse traverse) {

            //Gather all data
            var surveyContainer = CacheData.SurveyContainer;// Aggregator.GetSurveysContainer(data);
            var surveys = CacheData.AllSurveys;// Aggregator.GetAllSurveys(data).Where(x => x.SurveyContainerId == SurveyContainerId);
            var allSections = CacheData.AllSections;// Aggregator.GetAllSections(data);
            var allItems = CacheData.AllItems;//Aggregator.GetAllItems(data);
            var allItemFormats = CacheData.AllItemFormats;//Aggregator.GetAllItemFormats(data);
            var allResponses = CacheData.AllResponses;//Aggregator.GetAllResponses(data);

            //Start at the container
            traverse.AtSurveyContainer(surveyContainer);
            //Traverse surveys
            foreach (var survey in surveys) {
                traverse.SurveyContainerToSurvey(surveyContainer,survey);
                var surveySections = allSections.Where(x => x.GetSurveyId() == survey.Id);
                //Compile sections
                foreach (var section in surveySections) {
                    traverse.SurveyToSection(survey, section);
                    var sectionItems = allItems.Where(x => x.GetSectionId() == section.Id);
                    //Add Items
                    foreach (var item in sectionItems) {
                        var format = allItemFormats.FirstOrDefault(fmat => fmat.Id == item.GetItemFormatId());
                        var response = allResponses.FirstOrDefault(resp => resp.GetItemId() == item.Id);
                        var itemContainer = new SurveyItemContainer(item, response, format);
                        traverse.SectionToItem(section, itemContainer);
                    }
                }
            }
			traverse.OnComplete(surveyContainer);
        }

        public class Cache {
            public ISurveyContainer SurveyContainer { get; internal set; }
            public IEnumerable<ISurvey> AllSurveys { get; internal set; }
            public IEnumerable<ISection> AllSections { get; internal set; }
            public IEnumerable<IItemFormat> AllItemFormats { get; internal set; }
            public IEnumerable<IItem> AllItems { get; internal set; }
            public IEnumerable<IResponse> AllResponses { get; internal set; }
        }

        public class Data : IReconstructionData {

            protected SurveyReconstructionEngine Engine;
            public Data(SurveyReconstructionEngine engine) {
                Engine = engine;
            }

            public IInnerLookup Lookup { get; private set; }

            public void SetLookup(Type type) {
                Lookup = Engine.OuterLookup.GetInnerLookup(type);
            }


            public long OrgId { get { return Engine.OrgId; } }            

            public long SurveyContainerId { get { return Engine.SurveyContainerId; } }
        }
    }

}