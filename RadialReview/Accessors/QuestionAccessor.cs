using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
	public class QuestionAccessor : BaseAccessor {
		public void SetQuestionsEnabled(UserOrganizationModel caller, long forUserId, List<long> enabled, List<long> disabled) {
			//Yea this is pretty nasty.
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					caller = s.Get<UserOrganizationModel>(caller.Id);
					PermissionsUtility.Create(s, caller).OwnedBelowOrEqual(forUserId);

					//Enable
					var foundEnabled = s.GetByMultipleIds<QuestionModel>(enabled);
					foreach (var f in foundEnabled) {
						var found = f.DisabledFor.ToList().FirstOrDefault(x => x.Value == forUserId);
						if (found != null) {
							var newList = f.DisabledFor.ToList();
							newList.RemoveAll(x => x.Value == forUserId);
							f.DisabledFor = newList;
							s.Delete(s.Load<LongModel>(found.Id));
						}
					}
					tx.Commit();
					s.Flush();
				}
				using (var tx = s.BeginTransaction()) {
					//Disable
					var foundDisabled = s.GetByMultipleIds<QuestionModel>(disabled);
					foreach (var f in foundDisabled) {
						if (!f.DisabledFor.Any(x => x.Value == forUserId)) {
							f.DisabledFor.Add(new LongModel() { Value = forUserId });
							s.SaveOrUpdate(f);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static ReviewModel GenerateReviewForUser(HttpContext context, DataInteraction dataInteraction, PermissionsUtility perms, UserOrganizationModel reviewer, ReviewsModel reviewContainer, AskableCollection askables) {

			if (dataInteraction.Contains<ReviewModel>()) {
				var existing = dataInteraction.Where<ReviewModel>(x => x.ForReviewContainerId == reviewContainer.Id && x.ReviewerUserId == reviewer.Id && x.DeleteTime == null).ToList();
				if (existing.Any())
					return existing.First();
			}

			reviewer = dataInteraction.Get<UserOrganizationModel>(reviewer.Id);

			var reviewModel = new ReviewModel() {
				ReviewerUserId = reviewer.Id,
				ForReviewContainerId = reviewContainer.Id,
				DueDate = reviewContainer.DueDate,
				Name = reviewContainer.ReviewName,
			};
			if (context != null)
				new Cache(new HttpContextWrapper(context)).InvalidateForUser(reviewer, CacheKeys.UNSTARTED_TASKS);
			else {
				log.Info("Context was null, could not invalidate unstarted tasks");
			}

			dataInteraction.Save(reviewModel);
			reviewModel.ClientReview.ReviewId = reviewModel.Id;
			reviewModel.ClientReview.ReviewContainerId = reviewModel.ForReviewContainerId;
			dataInteraction.Update(reviewModel);

			ReviewAccessor.AddAskablesToReview(dataInteraction, perms, new Reviewer(reviewer.Id), reviewModel, reviewContainer.AnonymousByDefault, askables);
			return reviewModel;
		}

		[Obsolete("Use AskableAccessor.GetAskablesForUser", false)]
		public static List<QuestionModel> GetQuestionsForUser(AbstractQuery s, PermissionsUtility perms, long forUserId, DateRange range) {
			perms.ViewUserOrganization(forUserId, false);
			var forUser = s.Get<UserOrganizationModel>(forUserId);

			forUser = s.Get<UserOrganizationModel>(forUser.Id);
			var questions = new List<QuestionModel>();
			//Group Questions
			questions.AddRange(forUser.Groups.SelectMany(x => x.CustomQuestions));
			//Organization Questions
			var orgId = forUser.Organization.Id;
			var orgQuestions = s.Where<QuestionModel>(x => x.OriginId == orgId && x.OriginType == OriginType.Organization);
			questions.AddRange(orgQuestions);
			//Application Questions
			var applicationQuestions = s.All<ApplicationWideModel>().SelectMany(x => x.CustomQuestions).ToList();
			questions.AddRange(applicationQuestions);
			return questions.FilterRange(range).ToList();
		}

		public List<QuestionModel> GetQuestionsForUser(UserOrganizationModel caller, long forUserId, DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
#pragma warning disable CS0618 // Type or member is obsolete
					return GetQuestionsForUser(s.ToQueryProvider(true), perms, forUserId, range);
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}

		public Boolean AttachQuestionToOrganization(UserModel owner, OrganizationModel organization, QuestionModel question) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					throw new Exception("owner.Id != userOrg.id");
				}
			}
		}

		public Boolean AttachQuestionToGroup(UserModel owner, GroupModel group, QuestionModel question) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					var user = db.Get<UserOrganizationModel>(owner.Id);
					var asManager = group.Managers.Any(x => x.Id == group.Id);
					if (asManager) {
						group = db.Get<GroupModel>(group.Id);
						group.CustomQuestions.Add(question);
						db.SaveOrUpdate(group);
						return true;
					}
					throw new PermissionsException();
				}
			}
		}

		public QuestionCategoryModel GetCategory(UserOrganizationModel caller, long categoryId, Boolean overridePermissions) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var category = s.Get<QuestionCategoryModel>(categoryId);
					if (!overridePermissions) {
						PermissionsUtility.Create(s, caller).ViewOrigin(category.OriginType, category.OriginId);
					}
					return category;
				}
			}
		}

		public QuestionModel EditQuestion(UserOrganizationModel caller, long questionId, Origin origin = null, LocalizedStringModel question = null, long? categoryId = null, DateTime? deleteTime = null, QuestionType? questionType = null) {
			QuestionModel q;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					caller = s.Get<UserOrganizationModel>(caller.Id);

					var permissionUtility = PermissionsUtility.Create(s, caller);
					q = new QuestionModel();

					if (questionId == 0) //Creating
					{
						q.DateCreated = DateTime.UtcNow;
						q.CreatedById = caller.Id;
					} else {
						permissionUtility.EditQuestion(questionId);
						q = s.Get<QuestionModel>(questionId);
					}

					//Edit Origin
					if (origin != null) {
						permissionUtility.EditOrigin(origin, true);
						q.OriginId = origin.OriginId;
						q.OriginType = origin.OriginType;
					}
					//Edit Question
					if (question != null) {
						q.Question.UpdateDefault(question.Standard);
					}
					//Edit CategoryId
					if (categoryId != null) {
						if (categoryId == 0)
							throw new PermissionsException();
						permissionUtility.PairCategoryToQuestion(categoryId.Value, questionId);
						var category = s.Get<QuestionCategoryModel>(categoryId);
						q.Category = category;
					}
					//Edit DeleteTime
					if (deleteTime != null) {
						q.DeleteTime = deleteTime.Value;
					}
					//Edit questionType
					if (questionType != null) {
						q.QuestionType = questionType.Value;
					}

					s.SaveOrUpdate(q);
					tx.Commit();
					s.Flush();
				}
			}
			return q;
		}

		public QuestionModel GetQuestion(UserOrganizationModel caller, long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewQuestion(id);
					var question = s.Get<QuestionModel>(id);
					return question;
				}
			}
		}

	}
}