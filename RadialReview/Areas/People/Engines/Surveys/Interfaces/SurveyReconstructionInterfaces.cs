using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.People.Engines.Surveys.Interfaces {

    public interface ISurveyReconstructionAggregator  {
        void Prelookup(IReconstructionData data);

        ISurveyContainer GetSurveysContainer(IReconstructionData data);
        IEnumerable<ISurvey> GetAllSurveys(IReconstructionData data);
        IEnumerable<ISection> GetAllSections(IReconstructionData data);
        IEnumerable<IItem> GetAllItems(IReconstructionData data);
        IEnumerable<IResponse> GetAllResponses(IReconstructionData data);
        IEnumerable<IItemFormat> GetAllItemFormats(IReconstructionData data);
    }

    public interface ISurveyTraverse {
        void AtSurveyContainer(ISurveyContainer container);
        void SurveyContainerToSurvey(ISurveyContainer parent, ISurvey child);
        void SurveyToSection(ISurvey parent, ISection child);
        void SectionToItem(ISection parent, IItemContainer child);
		void OnComplete(ISurveyContainer container);
    }

    public interface ISurveyContainerReconstructor : ISurveyContainerReconstructor<ISurveyContainer, ISurvey, ISection, IItem, IItemFormat, IResponse> {

    }

    public interface ISurveyContainerReconstructor<SURVEYCONTAINER, SURVEY, SECTION, ITEM, ITEMFORMAT, RESPONSE>
        where SURVEYCONTAINER : ISurveyContainer
        where SURVEY : ISurvey
        where SECTION : ISection
        where ITEM : IItem
        where ITEMFORMAT : IItemFormat
        where RESPONSE : IResponse {
        SURVEYCONTAINER ReconstructSurveyContainer(ISurveyContainer data);
        SURVEY ReconstructSurvey(ISurvey data);
        SECTION ReconstructSection(ISection data);
        ITEM ReconstructItem(IItem data);
        ITEMFORMAT ReconstructItemFormat(IItemFormat data);
        RESPONSE ReconstructItemResponse(IResponse data);
    }

    public interface IReconstructionData {
        IInnerLookup Lookup { get; }
        long SurveyContainerId { get; }
        long OrgId { get; }
    }    

    public interface ISurveyReconstructorEvents {
        void OnBegin(IOuterLookup outerLookup);
    }

}
