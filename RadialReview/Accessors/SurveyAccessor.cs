using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Survey;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class SurveyAccessor : BaseAccessor
	{

		public static SurveyContainerModel GetSurveyContainer(UserOrganizationModel caller, long surveyId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).ViewSurveyContainer(surveyId);
					var survey = s.Get<SurveyContainerModel>(surveyId);

					survey.QuestionGroup._Questions = s.QueryOver<SurveyQuestionModel>()
						.Where(x => x.DeleteTime == null && x.ForQuestionGroup.Id == survey.QuestionGroup.Id)
						.List().ToList();

					survey.RespondentGroup._Respondents = s.QueryOver<SurveyRespondentModel>()
						.Where(x => x.DeleteTime == null && x.ForRespondentGroup.Id == survey.RespondentGroup.Id)
						.List().ToList();

					return survey;
				}
			}
		}

		public static List<SurveyContainerModel> GetAllSurveyContainersForOrganization(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id,organizationId);
					var survey = s.QueryOver<SurveyContainerModel>().Where(x=>x.Organization.Id==organizationId && x.DeleteTime==null).List().ToList();
					return survey;
				}
			}
		}

		public static void SetValue(string id, int? value)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var answer = s.Get<SurveyTakeAnswer>(id);
					if (answer==null || answer.DeleteTime!=null)
						throw new PermissionsException("Answer does not exist.");
					answer.Answer = value;
					s.Update(answer);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static SurveyTake LoadSurvey(string id,string userAgent,string IPAddress)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var respondent = s.QueryOver<SurveyRespondentModel>().Where(x => x.DeleteTime == null && x.LookupGuid == id.ToLower()).List().FirstOrDefault();
					if (respondent==null)
						throw new PermissionsException("Survey does not exist.");

					var st = s.QueryOver<SurveyTake>().Where(x => x.DeleteTime == null && x.Respondent.Id == respondent.Id).List().FirstOrDefault();
					List<SurveyTakeAnswer> answers;
					if (st == null){
						var now = DateTime.UtcNow;
						
						var container = s.QueryOver<SurveyContainerModel>().Where(x => x.DeleteTime == null && x.RespondentGroup.Id == respondent.ForRespondentGroup.Id).List().FirstOrDefault();
						if (container == null)
							throw new PermissionsException("Survey does not exist.");

						st = new SurveyTake()
						{
							Respondent = s.Load<SurveyRespondentModel>(respondent.Id),
							RespondentId = respondent.Id, 
							CreateTime = now, 
							Container = s.Load<SurveyContainerModel>(container.Id),
							ContainerId = container.Id,
							UserAgent = userAgent,
							IPAddress = IPAddress,
							
						};
						s.Save(st);

						var questions = container.QuestionGroup._Questions = s.QueryOver<SurveyQuestionModel>()
							.Where(x => x.DeleteTime == null && x.ForQuestionGroup.Id == container.QuestionGroup.Id)
							.List().ToList();
						answers = new List<SurveyTakeAnswer>();
						foreach (var q in questions){
							var sta = new SurveyTakeAnswer(){
								CreateTime = now,
								Question = s.Load<SurveyQuestionModel>(q.Id),
								QuestionId = q.Id,
								SurveyTake = s.Load<SurveyTake>(st.Id),
								SurveyTakeId = st.Id,
							};
							s.Save(sta);
							answers.Add(sta);
						}
					}else{
						answers = s.QueryOver<SurveyTakeAnswer>().Where(x => x.DeleteTime == null && x.SurveyTake.Id == st.Id).List().ToList();
					}

					st._Answers = answers;
					var a = st.Container.Name;

					tx.Commit();
					s.Flush();

					return st;
				}
			}
		}


		public static SurveyContainerModel EditSurvey(UserOrganizationModel caller, SurveyContainerModel model)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).EditSurvey(model.Id);
						
					model.Organization = s.Load<OrganizationModel>(model.Organization.Id);
					model.Creator      = s.Load<UserOrganizationModel>(model.Creator.Id);

					s.TemporalSaveOrUpdate(model.QuestionGroup);
					s.TemporalSaveOrUpdate(model.RespondentGroup);

					foreach (var q in model.QuestionGroup._Questions){
						q.ForQuestionGroup = s.Load<SurveyQuestionGroupModel>(model.QuestionGroup.Id);
						q.ForQuestionGroupId = model.QuestionGroup.Id;
						s.TemporalSaveOrUpdate(q);
					}

					foreach (var r in model.RespondentGroup._Respondents)
					{
						r.ForRespondentGroup = s.Load<SurveyRespondentGroupModel>(model.RespondentGroup.Id);
						r.ForRespondentGroupId = model.RespondentGroup.Id;
						r.LookupGuid = r.LookupGuid ?? Guid.NewGuid().ToString().ToLower().Replace("-", "");
						s.TemporalSaveOrUpdate(r);
					}
					
					s.SaveOrUpdate(model);

				
					
					tx.Commit();
					s.Flush();

					return model;
				}
			}
		}

		public static List<SurveyQuestionGroupModel> GetAllSurveyQuestionGroupsForOrganization(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
					var survey = s.QueryOver<SurveyQuestionGroupModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
					return survey;
				}
			}
		}

		public static List<SurveyRespondentGroupModel> GetAllSurveyRespondentGroupsForOrganization(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
					var survey = s.QueryOver<SurveyRespondentGroupModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
					return survey;
				}
			}
		}
	}
}