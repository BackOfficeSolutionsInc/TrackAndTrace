using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Impl;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Areas.People.Engines.Surveys.Strategies.Events;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Areas.People.Engines.Surveys.Strategies.PostProcesses;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Areas.People.Engines.Surveys {

    /// <summary>
    /// INSTRUCTIONS:
    /// 
    /// Create a CustomSurveyContainer class implementing the ISurveyInitializer and populate the interface.
    /// 
    /// </summary>
    public class SurveyBuilderEngine {
        public ISurveyBuilderEvents EventHandler { get; protected set; }
        public ISurveyInitializer SurveyBuilder { get; protected set; }
        public IOuterLookup OuterLookup { get; protected set; }
		public ITransformByAbout Transformer { get; protected set; }
//		public IPostProcessor Postprocessor { get; protected set; }

		public long OrgId { get { return SurveyBuilder.OrgId; } }

        protected Data data;

        public SurveyBuilderEngine(ISurveyInitializer builder, ISurveyBuilderEvents eventHandler, ITransformByAbout transformer, IOuterLookup outerLookup = null) {
            SurveyBuilder = builder;
            EventHandler = eventHandler ?? new SurveyBuilderEventsNoOp();
            OuterLookup = outerLookup ?? new OuterLookup();
			Transformer = transformer;

		}

        public ISurveyContainer BuildSurveyContainer(IEnumerable<IByAbout> byAbout) {
			var byAboutTransformed = Transformer.TransformForCreation(byAbout);
            OnBegin(byAboutTransformed);
			

            //Build Container
            var data = new Data(this);
            data.SurveyContainer = SurveyBuilder.BuildSurveyContainer();
            OnInitialize(data.SurveyContainer);

			var any = false;
            foreach (var surveyByAbouts in byAboutTransformed.GroupBy(x => UniqueKey(x.GetBy()))) {
                data.By = surveyByAbouts.First().GetBy();
                var abouts = surveyByAbouts.Select(x => x.GetAbout());
                foreach (var about in abouts) {
                    data.About = about;
                    //Build individual surveys , About = about };
                    data.Survey = BuildSurvey(SurveyBuilder, data);
					if (data.Survey.GetSections().Any() && data.Survey.GetSections().Any(x=>x.GetItems().Any())) {
						data.SurveyContainer.AppendSurvey(data.Survey);
						any = true;
					}
                }
            }
			AfterInitialized(data.SurveyContainer, any);


            OnEnd(data.SurveyContainer);
            return data.SurveyContainer;

        }

        #region Events
        protected void OnBegin(IEnumerable<IByAbout> byAbout) {
            EventHandler.OnBegin(SurveyBuilder, OrgId, OuterLookup, byAbout);
        }
        protected void OnEnd(ISurveyContainer container) {
            EventHandler.OnEnd(container);
		}
		protected void OnInitialize(IComponent component) {
			EventHandler.OnInitialize(component);
		}
		protected void AfterInitialized(IComponent component,bool hasElements) {
			EventHandler.AfterInitialized(component, hasElements);
		}
		#endregion

		#region Build Survey
		protected Tuple<long, string> UniqueKey(IForModel forModel) {
            return Tuple.Create(forModel.ModelId, forModel.ModelType);
        }

        protected ISurvey BuildSurvey(ISurveyInitializer surveyBuilder, Data data) {

            data.SetLookup(surveyBuilder.GetType());

            data.Survey = surveyBuilder.InitializeSurvey(data);
            OnInitialize(data.Survey);

            var sectionBuilders = surveyBuilder.GetSectionBuilders(data);
			var any = false;
            foreach (var sectionBuilder in sectionBuilders) {
                data.Section = BuildSection(sectionBuilder, data);
				if (data.Section.GetItemContainers().Any()) {
					data.Survey.AppendSection(data.Section);
					any = true;
				}
            }

			AfterInitialized(data.Survey, any);

			return data.Survey;
        }
        protected ISection BuildSection(ISectionInitializer sectionBuilder, Data data) {
            data.SetLookup(sectionBuilder.GetType());
            data.Section = sectionBuilder.InitializeSection(data);
            OnInitialize(data.Section);

            var itemBuilders = sectionBuilder.GetItemBuilders(data);
			var any = false;
            foreach (var itemBuilder in itemBuilders) {
                var itemResponse = BuildItemResponse(itemBuilder, data);
                data.Item = itemResponse.GetItem();
                data.Response = itemResponse.GetResponse();
                data.Section.AppendItem(itemResponse);
				any = true;
			}
			AfterInitialized(data.Section, any);

			return data.Section;
        }
        protected IItemContainer BuildItemResponse(IItemInitializer itemBuilder, Data data) {
            data.SetLookup(itemBuilder.GetType());

            //ItemFormat
            var formatRegistry = itemBuilder.GetItemFormat(data);
            data.ItemFormat = formatRegistry.GetItemFormat();

            if (formatRegistry.ShouldInitialize()) {
				OnInitialize(data.ItemFormat);
				AfterInitialized(data.ItemFormat,true);
			}
            //Item
            data.Item = itemBuilder.InitializeItem(data);
            OnInitialize(data.Item);
			AfterInitialized(data.Item, true);

			//Response
			if (itemBuilder.HasResponse(data)) {
                data.Response = itemBuilder.InitializeResponse(data, data.ItemFormat);
                OnInitialize(data.Response);
				AfterInitialized(data.Response, true);
			}

            return new SurveyItemContainer(data.Item, data.Response, data.ItemFormat);
        }
        protected class Data : ISurveyInitializerData, ISectionInitializerData, IItemInitializerData, IResponseInitializerCtx, IItemFormatInitializerCtx {
            protected SurveyBuilderEngine Engine;

            public Data(SurveyBuilderEngine engine) {
                Engine = engine;
            }
			
			public DateTime Now { get { return SurveyContainer.GetIssueDate(); } }
			public IInnerLookup Lookup { get; private set; }
            public ISurveyContainer SurveyContainer { get; set; }
            public ISurvey Survey { get; set; }
            public ISection Section { get; set; }
            public IItem Item { get; set; }
            public IResponse Response { get; set; }
            public IItemFormat ItemFormat { get; set; }

            public IForModel About { get; set; }
            public IForModel By { get; set; }

            public long OrgId { get { return Engine.OrgId; } }

            public void SetLookup(Type type) {
                Lookup = Engine.OuterLookup.GetInnerLookup(type);
            }

            public string ItemFormatKeyConstructor(string optionalKey = null) {
                return "__ItemFormat-" + optionalKey;
            }
            private class ItemFormatRegistry : IItemFormatRegistry {
                public IItemFormat ItemFormat { get; set; }
                public bool ShouldInitialize { get; set; }

                public IItemFormat GetItemFormat() {
                    return ItemFormat;
                }

                bool IItemFormatRegistry.ShouldInitialize() {
                    return ShouldInitialize;
                }
            }

            public IItemFormatRegistry RegistrationItemFormat(bool useRegistry, Func<IItemFormat> formatGenerator, string optionalKey = null) {

                bool shouldInitialize = false;
                var modifiedFormatGenerator = new Func<IItemFormat>(() => {
                    shouldInitialize = true;
                    return formatGenerator();
                });

                var format = !useRegistry ? modifiedFormatGenerator() : Lookup.GetOrAdd(ItemFormatKeyConstructor(optionalKey), x => modifiedFormatGenerator());

                return new ItemFormatRegistry() {
                    ItemFormat = format,
                    ShouldInitialize = shouldInitialize
                };
            }

            public bool FirstSeen(string key,string type="") {
                var dict = Lookup.GetOrAdd("~~FirstSeen~"+type+"~", _nil => new DefaultDictionary<string, bool>(x => true));
                var res =  dict[key];
                dict[key] = false;
                return res;
            }

            public bool FirstSeenByAbout() {
                var byAboutKey = By.ToKey() + "-" + About.ToKey();
                if (About.Is<SurveyUserNode>()) {
                    byAboutKey = By.ToKey() + "-" + ((SurveyUserNode)About).User.ToKey();
                }
                return FirstSeen(byAboutKey,"FirstSeenByAbout");
            }
        }

        #endregion
    }
}
