using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Reconstructor {

	public class DatabaseAggregator : ISurveyReconstructionAggregator {
		public ISession Session { get; protected set; }
		public DateRange Range { get; protected set; }
		public IForModel By { get; protected set; }
		public IForModel About { get; protected set; }

		public DatabaseAggregator(ISession session, IForModel by = null, IForModel about = null, DateRange range = null) {
			Session = session;
			Range = range;
			By = by;
			About = about;
		}

		public void Prelookup(IReconstructionData data) {
			var surveys = PrequerySurvey(data);
			var sections = PrequerySection(data, surveys);
			var items = PrequeryItem(data, surveys);
			var itemFormats = PrequeryItemFormat(data, surveys);
			var responses = PrequeryResponse(data, surveys);

			data.Lookup.Add("SurveyContainer", Session.Get<SurveyContainer>(data.SurveyContainerId));
			data.Lookup.AddList(surveys);
			data.Lookup.AddList(sections);
			data.Lookup.AddList(items);
			data.Lookup.AddList(itemFormats);
			data.Lookup.AddList(responses);

		}

		#region Prelookup helpers
		private IEnumerable<Survey> PrequerySurvey(IReconstructionData data) {
			var q = Session.QueryOver<Survey>().Where(x => x.OrgId == data.OrgId && x.SurveyContainerId == data.SurveyContainerId).Where(Range.Filter<Survey>());
			if (By != null)
				q = q.Where(x => x.By.ModelId == By.ModelId && x.By.ModelType == By.ModelType);
			if (About != null)
				q = q.Where(x => x.About.ModelId == About.ModelId && x.About.ModelType == About.ModelType);
			return q.Future();
		}
		private IEnumerable<SurveySection> PrequerySection(IReconstructionData data, IEnumerable<Survey> surveys) {
			var q = Session.QueryOver<SurveySection>().Where(x => x.OrgId == data.OrgId && x.SurveyContainerId == data.SurveyContainerId).Where(Range.Filter<SurveySection>());
			if (By != null || About != null) {
				var surveyIds = surveys.Select(x => x.Id).ToLazyCollection();
				q = q.WhereRestrictionOn(x => x.SurveyId).IsIn(surveyIds);
			}
			return q.Future();//.Select(sections => {
			//	if (By != null) {
			//		sections x.Where(
			//	}
			//		return null;
			//});
		}
		private IEnumerable<SurveyItem> PrequeryItem(IReconstructionData data, IEnumerable<Survey> surveys) {
			var q = Session.QueryOver<SurveyItem>().Where(x => x.OrgId == data.OrgId && x.SurveyContainerId == data.SurveyContainerId).Where(Range.Filter<SurveyItem>());
			if (By != null || About != null) {
				var surveyIds = surveys.Select(x => x.Id).ToLazyCollection();
				q = q.WhereRestrictionOn(x => x.SurveyId).IsIn(surveyIds);
			}
			return q.Future();
		}
		private IEnumerable<SurveyItemFormat> PrequeryItemFormat(IReconstructionData data, IEnumerable<Survey> surveys) {
			var q = Session.QueryOver<SurveyItemFormat>().Where(x => x.OrgId == data.OrgId && x.SurveyContainerId == data.SurveyContainerId).Where(Range.Filter<SurveyItemFormat>());
			return q.Future();
		}
		private IEnumerable<SurveyResponse> PrequeryResponse(IReconstructionData data, IEnumerable<Survey> surveys) {
			var q = Session.QueryOver<SurveyResponse>().Where(x => x.OrgId == data.OrgId && x.SurveyContainerId == data.SurveyContainerId).Where(Range.Filter<SurveyResponse>());

			if (By != null)
				q = q.Where(x => x.By.ModelId == By.ModelId && x.By.ModelType == By.ModelType);
			if (About != null)
				q = q.Where(x => x.About.ModelId == About.ModelId && x.About.ModelType == About.ModelType);

			return q.Future();
		}


		#endregion

		#region Getters
		public ISurveyContainer GetSurveysContainer(IReconstructionData data) {
			return data.Lookup.Get<SurveyContainer>("SurveyContainer");
		}

		public IEnumerable<ISurvey> GetAllSurveys(IReconstructionData data) {
			return data.Lookup.GetList<Survey>();
		}

		public IEnumerable<ISection> GetAllSections(IReconstructionData data) {
			return data.Lookup.GetList<SurveySection>();
		}

		public IEnumerable<IItem> GetAllItems(IReconstructionData data) {
			return data.Lookup.GetList<SurveyItem>();
		}

		public IEnumerable<IResponse> GetAllResponses(IReconstructionData data) {
			return data.Lookup.GetList<SurveyResponse>();
		}

		public IEnumerable<IItemFormat> GetAllItemFormats(IReconstructionData data) {
			return data.Lookup.GetList<SurveyItemFormat>();
		}
		#endregion
		//public IItem ReconstructItem(IItem data) {
		//    throw new NotImplementedException();
		//}

		//public IItemFormat ReconstructItemFormat(IItemFormat data) {
		//    throw new NotImplementedException();
		//}

		//public IResponse ReconstructItemResponse(IResponse data) {
		//    throw new NotImplementedException();
		//}

		//public ISection ReconstructSection(ISection data) {
		//    throw new NotImplementedException();
		//}

		//public ISurvey ReconstructSurvey(ISurvey data) {
		//    throw new NotImplementedException();
		//}

		//public ISurveyContainer ReconstructSurveyContainer(ISurveyContainer data) {
		//    throw new NotImplementedException();
		//}
	}
}