using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Survey;
using RadialReview.Properties;
using RadialReview.Utilities;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class SurveyAccessor : BaseAccessor {

		public static SurveyContainerModel GetSurveyContainer(UserOrganizationModel caller, long surveyId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
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

		public static SurveyResultVM GetResults(UserOrganizationModel caller, long surveyId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewSurveyContainer(surveyId);

					var survey = s.Get<SurveyContainerModel>(surveyId);
					var results = s.QueryOver<SurveyTakeAnswer>().Where(x => x.SurveyContainerId == surveyId && x.DeleteTime == null).List().ToList();
					var questions = s.QueryOver<SurveyQuestionModel>().Where(x => x.ForQuestionGroupId == survey.QuestionGroup.Id && x.DeleteTime == null).List().ToList();

					var respondentCount = s.QueryOver<SurveyRespondentModel>().Where(x => x.ForRespondentGroupId == survey.RespondentGroup.Id && x.DeleteTime == null).List().Count;


					var o = new List<SurveyResultItemVM>();

					foreach (var question in questions) {
						var answers = results.Where(x => x.QuestionId == question.Id).ToList();
						SurveyResultItemVM res = null;
						switch (question.QuestionType) {
							case SurveyQuestionType.Radio:
								res = new SurveyResultItem_RadioVM() {
									Answers = Enumerable.Range(1, 5).Select(y => answers.Count(x => x.Answer == y)).ToArray(),
									Question = question.Question,
									PartialView = question.QuestionType.GetPartialView()
								};
								break;
							case SurveyQuestionType.Feedback:
								res = new SurveyResultItem_FeedbackVM() {
									Answers = answers.Select(y => y.AnswerString).Where(x => !String.IsNullOrWhiteSpace(x)).Shuffle().ToArray(),
									Question = question.Question,
									PartialView = question.QuestionType.GetPartialView()
								};
								break;
							default:
								throw new ArgumentOutOfRangeException("SurveyQuestionType: " + question.QuestionType);
						}
						o.Add(res);
					}

					return new SurveyResultVM() {
						Container = survey,
						Questions = o,
						TotalStartedRespondents = results.GroupBy(x => x.SurveyTakeId).Count(x => x.Any(y => y.AnswerTime != null)),
						TotalRequestedRespondents = respondentCount
					};
				}
			}
		}

		public static List<SurveyContainerModel> GetAllSurveyContainersForOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
					var survey = s.QueryOver<SurveyContainerModel>().Where(x => x.OrgId == organizationId && x.DeleteTime == null).List().ToList();
					return survey;
				}
			}
		}

		public static void SetValue(string id, int? value, string str) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var answer = s.Get<SurveyTakeAnswer>(id);
					if (answer == null || answer.DeleteTime != null)
						throw new PermissionsException("Answer does not exist.");
					answer.Answer = value;
					answer.AnswerString = str;
					answer.AnswerTime = DateTime.UtcNow;
					s.Update(answer);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static SurveyTake LoadOpenEndedSurvey(string respondentGuid, string surveyLookupId, string userAgent, string IPAddress, string referer) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var container = s.QueryOver<SurveyContainerModel>().Where(x => x.OpenEnded && x.DeleteTime == null && x.LookupId == surveyLookupId).SingleOrDefault();


					if (container == null)
						throw new PermissionsException("Survey does not exist.");

					var respondent = s.QueryOver<SurveyRespondentModel>().Where(x => x.DeleteTime == null && x.LookupGuid == respondentGuid).SingleOrDefault();

					var now = DateTime.UtcNow;

					List<SurveyTakeAnswer> answers;
					SurveyTake st;
					if (respondent == null) {
						respondent = new SurveyRespondentModel() {
							CreateTime = now,
							ForRespondentGroup = container.RespondentGroup,
							ForRespondentGroupId = container.RespondentGroup.Id,
							LookupGuid = Guid.NewGuid().ToString().ToLower().Replace("-", ""),
						};
						s.Save(respondent);
						st = new SurveyTake() {
							Respondent = respondent,
							RespondentId = respondent.Id,
							CreateTime = now,
							Container = container,
							ContainerId = container.Id,
							UserAgent = userAgent,
							IPAddress = IPAddress,
							Referer = referer

						};
						s.Save(st);

						var questions = container.QuestionGroup._Questions = s.QueryOver<SurveyQuestionModel>()
							.Where(x => x.DeleteTime == null && x.ForQuestionGroup.Id == container.QuestionGroup.Id)
							.List().ToList();
						answers = new List<SurveyTakeAnswer>();
						foreach (var q in questions) {
							var sta = new SurveyTakeAnswer() {
								CreateTime = now,
								Question = s.Load<SurveyQuestionModel>(q.Id),
								QuestionId = q.Id,
								SurveyTake = s.Load<SurveyTake>(st.Id),
								SurveyTakeId = st.Id,
								SurveyContainerId = container.Id,
							};
							s.Save(sta);
							answers.Add(sta);
						}

					} else {
						st = s.QueryOver<SurveyTake>().Where(x => x.DeleteTime == null && x.Respondent.Id == respondent.Id).List().FirstOrDefault();
						if (st == null)
							throw new PermissionsException("Could not find your survey");
						answers = s.QueryOver<SurveyTakeAnswer>().Where(x => x.DeleteTime == null && x.SurveyTake.Id == st.Id).List().ToList();
					}

					st._Answers = answers.OrderBy(x => x._Order).ToList();
					var a = st.Container.Name;

					tx.Commit();
					s.Flush();

					return st;
				}
			}
		}

		public static SurveyTake LoadSurvey(string id, string userAgent, string IPAddress, string referer) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var respondent = s.QueryOver<SurveyRespondentModel>().Where(x => x.DeleteTime == null && x.LookupGuid == id.ToLower()).List().FirstOrDefault();
					if (respondent == null)
						throw new PermissionsException("Survey does not exist.");

					var st = s.QueryOver<SurveyTake>().Where(x => x.DeleteTime == null && x.Respondent.Id == respondent.Id).List().FirstOrDefault();
					List<SurveyTakeAnswer> answers;
					if (st == null) {
						var now = DateTime.UtcNow;

						var container = s.QueryOver<SurveyContainerModel>().Where(x => x.DeleteTime == null && x.RespondentGroup.Id == respondent.ForRespondentGroup.Id).List().FirstOrDefault();
						if (container == null)
							throw new PermissionsException("Survey no longer exists.");

						st = new SurveyTake() {
							Respondent = s.Load<SurveyRespondentModel>(respondent.Id),
							RespondentId = respondent.Id,
							CreateTime = now,
							Container = s.Load<SurveyContainerModel>(container.Id),
							ContainerId = container.Id,
							UserAgent = userAgent,
							IPAddress = IPAddress,
							Referer = referer,

						};
						s.Save(st);

						var questions = container.QuestionGroup._Questions = s.QueryOver<SurveyQuestionModel>()
							.Where(x => x.DeleteTime == null && x.ForQuestionGroup.Id == container.QuestionGroup.Id)
							.List().ToList();
						answers = new List<SurveyTakeAnswer>();
						foreach (var q in questions) {
							var sta = new SurveyTakeAnswer() {
								CreateTime = now,
								Question = s.Load<SurveyQuestionModel>(q.Id),
								QuestionId = q.Id,
								SurveyTake = s.Load<SurveyTake>(st.Id),
								SurveyTakeId = st.Id,
								SurveyContainerId = container.Id,
							};
							s.Save(sta);
							answers.Add(sta);
						}
					} else {
						answers = s.QueryOver<SurveyTakeAnswer>().Where(x => x.DeleteTime == null && x.SurveyTake.Id == st.Id).List().ToList();
					}

					st._Answers = answers.OrderBy(x => x._Order).ToList();
					var a = st.Container.Name;

					tx.Commit();
					s.Flush();

					return st;
				}
			}
		}


		public static SurveyContainerModel EditSurvey(UserOrganizationModel caller, SurveyContainerModel model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditSurvey(model.Id);

					model._Organization = s.Load<OrganizationModel>(model.OrgId);
					model._Creator = s.Load<UserOrganizationModel>(model.CreatorId);


					s.TemporalSaveOrUpdate(model.QuestionGroup);
					s.TemporalSaveOrUpdate(model.RespondentGroup);

					var i = 0;
					foreach (var q in model.QuestionGroup._Questions) {
						q.ForQuestionGroup = s.Load<SurveyQuestionGroupModel>(model.QuestionGroup.Id);
						q.ForQuestionGroupId = model.QuestionGroup.Id;
						q._Order = i;
						s.TemporalSaveOrUpdate(q);
						i++;
					}

					foreach (var r in model.RespondentGroup._Respondents.Select(x => { x.Email = x.Email.Trim().ToLower(); return x; }).Distinct(x => x.Email)) {
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


		public static async Task<bool> IssueSurvey(UserOrganizationModel caller, long surveyContainerModelId) {
			var emails = new List<Mail>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditSurvey(surveyContainerModelId);

					var surveyContainer = s.Get<SurveyContainerModel>(surveyContainerModelId);
					if (surveyContainer == null || surveyContainer.DeleteTime != null)
						throw new PermissionsException("Survey does not exist.");

					if (surveyContainer.IssueDate != null)
						throw new PermissionsException("Survey was already issued.");

					surveyContainer.IssueDate = DateTime.UtcNow;
					s.Update(surveyContainer);

					var sqs = s.QueryOver<SurveyQuestionModel>().Where(x => x.DeleteTime == null && x.ForQuestionGroupId == surveyContainer.QuestionGroup.Id).List().ToList();
					var srs = s.QueryOver<SurveyRespondentModel>().Where(x => x.DeleteTime == null && x.ForRespondentGroupId == surveyContainer.RespondentGroup.Id).List().ToList();


					foreach (var r in srs) {
						var survey = new SurveyTake() {
							Container = surveyContainer,
							ContainerId = surveyContainer.Id,
							Respondent = r,
							RespondentId = r.Id,
						};
						s.Save(survey);
						foreach (var q in sqs) {
							var answer = new SurveyTakeAnswer() {
								Question = q,
								SurveyTake = survey,
								SurveyTakeId = survey.Id,
								QuestionId = q.Id,
								SurveyContainerId = surveyContainerModelId,
								_Order = q._Order

							};
							s.Save(answer);
						}

						var url = Config.BaseUrl(caller.Organization) + "Survey/Take/" + r.LookupGuid;

						emails.Add(
							Mail.To(EmailTypes.SurveyIssued, r.Email)
								.SubjectPlainText(surveyContainer.EmailSubject)
								.BodyPlainText(surveyContainer.EmailBody + "<br/><a href=" + url + ">" + url + "</a>")
							);

					}

					tx.Commit();
					s.Flush();



				}
			}
			await Emailer.SendEmails(emails);
			return true;
		}

		public static List<SurveyQuestionGroupModel> GetAllSurveyQuestionGroupsForOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
					var survey = s.QueryOver<SurveyQuestionGroupModel>().Where(x => x.OrgId == organizationId && x.DeleteTime == null).List().ToList();
					return survey;
				}
			}
		}

		public static List<SurveyRespondentGroupModel> GetAllSurveyRespondentGroupsForOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
					var survey = s.QueryOver<SurveyRespondentGroupModel>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();
					return survey;
				}
			}
		}

	}
}