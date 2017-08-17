using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Areas.People.Engines.Surveys.Impl;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.People.Engines.Surveys.Interfaces {


	/// <summary>
	/// Provides a "static" lookup for all components in the survey.
	/// Used during prequerying and testing.
	/// </summary>
	public interface IOuterLookup {
		void AddList<U>(Type classType, IEnumerable<U> objects);
		void AddItem<U>(Type classType, string key, U item) where U : class;
		IDictionary<Type, ICollection<object>> GetLookups(Type classType);
		U GetItem<U>(Type classType, string key) where U : class;
		IInnerLookup GetInnerLookup(Type classType);
		IInnerLookup GetInnerLookup<T>();
	}
	/// <summary>
	/// Provides a "static" lookup for a particular component in the survey
	/// </summary>
	public interface IInnerLookup {
		void AddList<U>(IEnumerable<U> objects);
		IReadOnlyCollection<U> GetList<U>();
		void Add<U>(string key, U value) where U : class;
		U Get<U>(string key) where U : class;
		U GetOrAdd<U>(string key, Func<string, U> defltValue) where U : class;

	}

	public interface IInitializer {
		void Prelookup(IInitializerLookupData data);
	}

	public interface IComponent : ILongIdentifiable {
		string GetName();
		string GetHelp();
		int GetOrdering();
		string ToPrettyString();
	}

	#region Initializers
	public interface ISurveyInitializer : IInitializer {
		long OrgId { get; }
		ISurveyContainer BuildSurveyContainer();
		ISurvey InitializeSurvey(ISurveyInitializerData data);
		IEnumerable<ISectionInitializer> GetAllPossibleSectionBuilders(IEnumerable<IByAbout> byAbouts);
		IEnumerable<ISectionInitializer> GetSectionBuilders(ISectionInitializerData data);
		//ISurvey InitializeSurvey(IForModel by, IForModel about, ISurveyContainer parent);
		//IEnumerable<ISectionInitializer> GetSectionBuilders(IPrequeryLookup lookup, ISurveyContainer container, ISurvey survey);
	}

	public interface ISectionInitializer : IInitializer {
		ISection InitializeSection(ISectionInitializerData data);
		IEnumerable<IItemInitializer> GetAllPossibleItemBuilders(IEnumerable<IByAbout> byAbouts);
		IEnumerable<IItemInitializer> GetItemBuilders(IItemInitializerData data);
		//ISection InitializeSection(ISurveyContainer container, ISurvey parent);
		//IEnumerable<IItemInitializer> GetItemBuilders(IPrequeryLookup lookup, ISurveyContainer container, ISurvey survey, ISection section);
	}

	public interface IItemInitializer : IInitializer {
		IItem InitializeItem(IItemInitializerData data);
		IItemFormatRegistry GetItemFormat(IItemFormatInitializerCtx ctx);
		bool HasResponse(IResponseInitializerCtx ctx);
		IResponse InitializeResponse(IResponseInitializerCtx ctx, IItemFormat format);
		//IItem InitializeItem(IPrequeryLookup lookup, ISurveyContainer container, ISurvey survey, ISection section);
		//IItemFormat GetItemFormat(IPrequeryLookup lookup, ISurveyContainer container, ISurvey survey, ISection section, IItem item);
		//bool HasResponse(IPrequeryLookup lookup, ISurveyContainer container, ISurvey survey, ISection section, IItem item);
		//IResponse InitializeResponse(IPrequeryLookup lookup, ISurveyContainer container, ISurvey survey, ISection section, IItem item, IItemFormat format);
	}

	#region Initializer Data

	public interface IInitializerData {
		long OrgId { get; }
		DateTime Now { get; }
	}

	public interface IInitializerLookupData : IInitializerData {
		ISession Session { get; }
		IEnumerable<IByAbout> ByAbouts { get; }
		IInnerLookup Lookup { get; }
	}

	public interface ISurveyInitializerData : IInitializerData {
		IInnerLookup Lookup { get; }
		IForModel By { get; }
		IForModel About { get; }
		ISurveyContainer SurveyContainer { get; }
	}
	public interface ISectionInitializerData : IInitializerData {
		IInnerLookup Lookup { get; }
		ISurveyContainer SurveyContainer { get; }
		ISurvey Survey { get; }
	}
	public interface IItemInitializerData : IInitializerData {
		IInnerLookup Lookup { get; }
		ISurveyContainer SurveyContainer { get; }
		ISurvey Survey { get; }
		ISection Section { get; }
		IItemFormat ItemFormat { get; set; } //Added

		IForModel By { get; }
		IForModel About { get; }
	}
	public interface IResponseInitializerCtx : IInitializerData {
		IInnerLookup Lookup { get; }
		ISurveyContainer SurveyContainer { get; }
		ISurvey Survey { get; }
		ISection Section { get; }
		IItem Item { get; }
	}

	public interface IItemFormatInitializerCtx : IInitializerData {
		IInnerLookup Lookup { get; }
		ISurveyContainer SurveyContainer { get; }
		ISurvey Survey { get; }
		ISection Section { get; }
		/// <summary>
		/// Registers the ItemFormat. If in register, it will only initialize one copy of this format
		/// </summary>
		/// <param name="shouldRegister">if true, save a copy to the Lookup and reference for the rest of the survey.</param>
		/// <param name="format"></param>
		/// <param name="interClassIdentifier">If you need to distinguish multiple ItemFormats within the same class, use this optional key</param>
		/// <returns></returns>
		IItemFormatRegistry RegistrationItemFormat(bool useRegistry, Func<IItemFormat> formatGenerator, string interClassIdentifier = null);
	}

	#endregion

	#endregion

	#region Transformers
	public interface ITransformByAbout {
		IEnumerable<IByAbout> TransformForCreation(IEnumerable<IByAbout> byAbouts);
		//IEnumerable<IByAbout> ReconstructTransform(IEnumerable<IByAbout> byAbouts);
	}
	public interface IPostProcessor {
		void Process(ISurveyContainer surveyContainer);
	}
	#endregion

	#region Components
	public interface ISurveyContainer : IComponent {
		SurveyType GetSurveyType();
		IEnumerable<ISurvey> GetSurveys();
		void AppendSurvey(ISurvey survey);
		DateTime GetIssueDate();
		DateTime? GetDueDate();
		IForModel GetCreator();
	}

	public interface ISurvey : IComponent, IByAbout {
		long GetSurveyContainerId();
		IEnumerable<ISection> GetSections();
		void AppendSection(ISection section);
		DateTime GetIssueDate();
		DateTime? GetDueDate();
	}

	public interface ISection : IComponent {
		long GetSurveyId();
		string GetSectionType();
		IEnumerable<IItemContainer> GetItemContainers();
		IEnumerable<IItem> GetItems();
		void AppendItem(IItemContainer item);
		/// <summary>
		/// When merging survey's on the About field, use this field to merge sections across the SurveyContainer.
		/// </summary>
		/// <returns></returns>
		string GetSectionMergerKey();
	}

	public interface IItemContainer : IComponent {
		IItem GetItem();
		IResponse GetResponse();
		IItemFormat GetFormat();
		bool HasResponse();
		/// <summary>
		/// When merging survey's on the About field, use this field to merge items across the SurveyContainer.
		/// </summary>
		/// <returns></returns>
		string GetItemMergerKey();
	}

	public interface IItem : IComponent {
		long GetSectionId();
		long GetItemFormatId();
		IForModel GetSource();
		/// <summary>
		/// When merging survey's on the About field, use this field to merge items across the SurveyContainer.
		/// </summary>
		/// <returns></returns>
		string GetItemMergerKey();
	}

	public interface IResponse : IComponent {
		long GetItemId();
		long GetItemFormatId();
		string GetAnswer();
		IByAbout GetByAbout();
	}

	public interface IItemFormat : IComponent {
		SurveyItemType GetItemType();
		SurveyQuestionIdentifier GetQuestionIdentifier();
		IItemFormat AddSetting(string key, object value);
		T GetSetting<T>(string key);
		IDictionary<string, object> GetSettings();
	}

	public interface IItemFormatRegistry {
		/// <summary>
		/// Indicates that a key should be saved
		/// </summary>
		bool ShouldInitialize();
		IItemFormat GetItemFormat();
	}

	#region SurveyAbout

	public interface ISurveyAboutContainer : IComponent {
		SurveyType GetSurveyType();
		IEnumerable<ISurveyAbout> GetSurveys();
		void AppendSurvey(ISurveyAbout survey);
	}

	public interface ISurveyAbout : IComponent {
		long GetSurveyContainerId();
		IEnumerable<ISectionAbout> GetSections();
		void AppendSection(ISectionAbout section);
		//void MergeWith(ISurvey survey);
		IForModel GetAbout();
		DateTime GetIssueDate();
	}

	public interface ISectionAbout : IComponent {
		long GetSurveyId();
		string GetSectionType();
		IEnumerable<IItemContainerAbout> GetItemContainers();
		IEnumerable<IItem> GetItems();
		void AppendItem(IItemContainerAbout item);
		//void MergeWith(ISection section);
		/// <summary>
		/// When merging survey's on the About field, use this field to merge sections across the SurveyContainer.
		/// </summary>
		/// <returns></returns>
		string GetSectionMergerKey();
	}

	public interface IItemContainerAbout : IComponent {
		IItem GetItem();
		IItemFormat GetFormat();
		IEnumerable<IResponse> GetResponses();
		/// <summary>
		/// When merging survey's on the About field, use this field to merge items across the SurveyContainer.
		/// </summary>
		/// <returns></returns>
		string GetItemMergerKey();
	}


	#endregion


	#endregion



	#region Events
	public interface ISurveyBuilderEvents {
		void OnBegin(ISurveyInitializer builder, long orgId, IOuterLookup outerLookup, IEnumerable<IByAbout> byAbouts);
		void OnEnd(ISurveyContainer container);
		void OnInitialize(IComponent compontent);
		void AfterInitialized(IComponent compontent,bool hasElements);
	}
	#endregion
}


//public interface IComponent<PARENTCOMPONENT,COMPONENT> {
//}

//public interface ISurveyComponent<PARENTCOMPONENT, COMPONENT, SUBCOMPONENT> //: IComponent<PARENTCOMPONENT,COMPONENT>
//    where PARENTCOMPONENT : ISurveyComponent
//    where COMPONENT : ISurveyComponent
//    where SUBCOMPONENT : ISurveyComponent {
//    COMPONENT Build(PARENTCOMPONENT parent);
//    IEnumerable<ISubcomponentBuilder<COMPONENT, SUBCOMPONENT, SUBSUBCOMPONENT>> SubcomponentBuilders<SUBSUBCOMPONENT>(COMPONENT component) where SUBSUBCOMPONENT : ISurveyComponent;
//}
//public interface ISurveyComponent { 

//}

//public interface ISurveyBuilder<COMPONENT> {
//}