using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class QuestionAccessor : BaseAccessor
    {


        public QuestionModel CreateQuestion(QuestionModel question)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    db.Save(question);
                    tx.Commit();
                    db.Flush();
                    //db.Questions.Add(question);
                    //db.SaveChanges();
                }
            }
            return question;
        }

        public void SetQuestionsEnabled(UserOrganizationModel caller, long forUserId, List<long> enabled, List<long> disabled)
        {
            //Yea this is pretty nasty.
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    caller = s.Get<UserOrganizationModel>(caller.Id);
                    PermissionsUtility.Create(s, caller).OwnedBelowOrEqual(x => x.Id == forUserId);

                    //Enable
                    var foundEnabled = s.GetByMultipleIds<QuestionModel>(enabled);
                    foreach (var f in foundEnabled)
                    {
                        var found = f.DisabledFor.ToList().FirstOrDefault(x => x.Long == forUserId);
                        if (found != null)
                        {
                            var newList = f.DisabledFor.ToList();
                            newList.RemoveAll(x => x.Long == forUserId);
                            f.DisabledFor = newList;
                            s.Delete(s.Load<LongModel>(found.Id));
                        }
                    }
                    tx.Commit();
                    s.Flush();
                }
                using (var tx = s.BeginTransaction())
                {
                    //Disable
                    var foundDisabled = s.GetByMultipleIds<QuestionModel>(disabled);
                    foreach (var f in foundDisabled)
                    {
                        if (!f.DisabledFor.Any(x => x.Long == forUserId))
                        {
                            f.DisabledFor.Add(new LongModel() { Long = forUserId });
                            s.SaveOrUpdate(f);
                        }
                    }
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="forUser"></param>
        /// <returns></returns>
        public ReviewModel GenerateReviewForUser(UserOrganizationModel caller, UserOrganizationModel forUser, ReviewsModel reviewContainer, List<AskableAbout> askables)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    var reviewModel=GenerateReviewForUser(s, perms, caller, forUser, reviewContainer, askables);
                    tx.Commit();
                    s.Flush();
                    return reviewModel;
                }
            }
        }

        public static ReviewModel GenerateReviewForUser(ISession s, PermissionsUtility perms, UserOrganizationModel caller, UserOrganizationModel forUser, ReviewsModel reviewContainer, List<AskableAbout> askables)
        {
            //var questions = GetQuestionsForUser(s, caller, forUser,caller.AllSubordinatesAndSelf());
            //var responsibilities =

            forUser = s.Get<UserOrganizationModel>(forUser.Id);

            var askable = new List<Askable>();

            var reviewModel = new ReviewModel()
            {
                ForUserId = forUser.Id,
                ForReviewsId = reviewContainer.Id,
                DueDate = reviewContainer.DueDate,
                Name = reviewContainer.ReviewName,
            };

            s.Save(reviewModel);

            reviewModel.ClientReview.ReviewId = reviewModel.Id;

            s.Update(reviewModel);

            foreach (var q in askables)
            {
                switch (q.Askable.GetQuestionType())
                {
                    case QuestionType.RelativeComparison: GenerateRelativeComparisonAnswers(s, caller, forUser, q, reviewModel); break;
                    case QuestionType.Slider: GenerateSliderAnswers(s, caller, forUser, q, reviewModel); break;
                    case QuestionType.Thumbs: GenerateThumbsAnswers(s, caller, forUser, q, reviewModel); break;
                    case QuestionType.Feedback: GenerateFeedbackAnswers(s, caller, forUser, q, reviewModel); break;
                    default: throw new ArgumentException("Unrecognized questionType(" + q.Askable.GetQuestionType() + ")");
                }
            }
            s.SaveOrUpdate(reviewModel);
            return reviewModel;
        }


        /*
        public ReviewModel GenerateResponsibilitiesReview(UserOrganizationModel caller, UserOrganizationModel forUser, ReviewsModel reviewContainer,List<ResponsibilityGroupModel> responsibilityGroup)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    forUser = s.Get<UserOrganizationModel>(forUser.Id);

                    var reviewModel = new ReviewModel()
                    {
                        ForUserId = forUser.Id,
                        ForReviewsId = reviewContainer.Id,
                        DueDate = reviewContainer.DueDate,
                        Name = reviewContainer.ReviewName,
                    };

                    s.Save(reviewModel);
                    foreach (var rg in responsibilityGroup)
                    {
                        foreach (var r in rg.Responsibilities)
                        {
                            GenerateSliderAnswers(s, caller, forUser, r, reviewModel);
                            /*switch (q.QuestionType)
                            {
                                case QuestionType.RelativeComparison: GenerateRelativeComparisonAnswers(s, caller, forUser, q, reviewModel); break;
                                case QuestionType.Slider: GenerateSliderAnswers(s, caller, forUser, q, reviewModel); break;
                                case QuestionType.Thumbs: GenerateThumbsAnswers(s, caller, forUser, q, reviewModel); break;
                                case QuestionType.Feedback: GenerateFeedbackAnswers(s, caller, forUser, q, reviewModel); break;
                                default: throw new ArgumentException("Unrecognized questionType(" + q.QuestionType + ")");
                            }
                        }
                    }
                    s.SaveOrUpdate(reviewModel);
                    tx.Commit();
                    s.Flush();

                    return reviewModel;
                }
            }
        }*/


        private static void GenerateSliderAnswers(ISession session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review)
        {

            var slider = new SliderAnswer()
            {
                Complete = false,
                Percentage = null,
                Askable = askable.Askable,
                Required = askable.Askable.Required,
                ForReviewId = review.Id,
                ByUserId = forUser.Id,
                AboutUserId = askable.AboutUserId,
                ForReviewContainerId = review.ForReviewsId,
                AboutType = askable.AboutType

            };
            session.Save(slider);

        }
        private static void GenerateFeedbackAnswers(ISession session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review)
        {
            var feedback = new FeedbackAnswer()
            {
                Complete = false,
                Feedback = null,
                Askable = askable.Askable,
                Required = askable.Askable.Required,
                ForReviewId = review.Id,
                ByUserId = forUser.Id,
                AboutUserId = askable.AboutUserId,
                ForReviewContainerId = review.ForReviewsId,
                AboutType = askable.AboutType
            };
            session.Save(feedback);

        }

        private static void GenerateThumbsAnswers(ISession session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review)
        {
            var thumbs = new ThumbsAnswer()
            {
                Complete = false,
                Thumbs = ThumbsType.None,
                Askable = askable.Askable,
                Required = askable.Askable.Required,
                ForReviewId = review.Id,
                ByUserId = forUser.Id,
                AboutUserId = askable.AboutUserId,
                ForReviewContainerId = review.ForReviewsId,
                AboutType = askable.AboutType
            };
            session.Save(thumbs);

        }

        private static void GenerateRelativeComparisonAnswers(ISession session, UserOrganizationModel caller, UserOrganizationModel forUser, AskableAbout askable, ReviewModel review)
        {
            var peers = forUser.ManagedBy.ToListAlive().Select(x => x.Manager).SelectMany(x => x.ManagingUsers.ToListAlive().Select(y => y.Subordinate));
            var managers = forUser.ManagedBy.ToListAlive().Select(x => x.Manager);
            var managing = forUser.ManagingUsers.ToListAlive().Select(x => x.Subordinate);

            var groupMembers = forUser.Groups.SelectMany(x => x.GroupUsers);

            var union = peers.UnionBy(x => x.Id, managers, managing, groupMembers).ToList();

            var len = union.Count();
            List<Tuple<UserOrganizationModel, UserOrganizationModel>> items = new List<Tuple<UserOrganizationModel, UserOrganizationModel>>();
            for (int i = 0; i < len - 1; i++)
            {
                for (int j = i + 1; j < len; j++)
                {
                    var relComp = new RelativeComparisonAnswer()
                    {
                        Required = askable.Askable.Required,
                        Askable = askable.Askable,
                        Complete = false,
                        First = union[i],
                        Second = union[j],
                        Choice = RelativeComparisonType.Skip,
                        ForReviewId = review.Id,
                        ByUserId = forUser.Id,
                        AboutUserId = askable.AboutUserId,
                        ForReviewContainerId = review.ForReviewsId,
                        AboutType = askable.AboutType
                    };
                    items.Add(Tuple.Create(union[i], union[j]));
                    session.Save(relComp);
                }
            }

        }

        public static List<QuestionModel> GetQuestionsForUser(ISession s,PermissionsUtility perms, UserOrganizationModel caller, long forUserId)
        {
            perms.ViewUserOrganization(forUserId, false);
            var forUser = s.Get<UserOrganizationModel>(forUserId);

            caller = s.Get<UserOrganizationModel>(caller.Id);
            forUser = s.Get<UserOrganizationModel>(forUser.Id);
            List<QuestionModel> questions = new List<QuestionModel>();
            //Self Questions
            questions.AddRange(forUser.CustomQuestions);
            //Group Questions
            questions.AddRange(forUser.Groups.SelectMany(x => x.CustomQuestions));
            //Organization Questions
            var orgId = forUser.Organization.Id;
            var orgQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == orgId && x.OriginType == OriginType.Organization).List().ToList();
            questions.AddRange(orgQuestions);
            //Application Questions
            var applicationQuestions = s.QueryOver<ApplicationWideModel>().List().SelectMany(x => x.CustomQuestions).ToList();
            questions.AddRange(applicationQuestions);
            return questions;
        }

        public List<QuestionModel> GetQuestionsForUser(UserOrganizationModel caller, long forUserId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetQuestionsForUser(s, perms, caller, forUserId);
                }
            }
        }

        public Boolean AttachQuestionToOrganization(UserModel owner, OrganizationModel organization, QuestionModel question)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    throw new Exception("owner.Id != userOrg.id");
                    /*
                    var user = db.Get<UserOrganizationModel>(owner.Id);//db.UserOrganizationModels.Find(owner.Id);
                    var asManager = organization.ManagersCanAddQuestions && user.ManagerAtOrganization.Any(x => x.Id == organization.Id);
                    var asOverseer = user.ManagingOrganizations.Any(x => x.Id == organization.Id);
                    if (asManager || asOverseer)
                    {
                        organization = db.Get<OrganizationModel>(organization.Id);//s db.Organizations.Attach(organization);
                        organization.CustomQuestions.Add(question);
                        db.SaveOrUpdate(organization);
                        tx.Commit();
                        db.Flush();
                        return true;
                    }
                    throw new PermissionsException();*/
                }
            }
        }

        public Boolean AttachQuestionToGroup(UserModel owner, GroupModel group, QuestionModel question)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    var user = db.Get<UserOrganizationModel>(owner.Id);//db.UserOrganizationModels.Find(owner.Id);
                    var asManager = group.Managers.Any(x => x.Id == group.Id);
                    if (asManager)
                    {
                        group = db.Get<GroupModel>(group.Id);// db.Groups.Attach(group);
                        group.CustomQuestions.Add(question);
                        db.SaveOrUpdate(group);
                        return true;
                    }
                    throw new PermissionsException();
                }
            }
        }

        public QuestionCategoryModel GetCategory(UserOrganizationModel caller, long categoryId, Boolean overridePermissions)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var category = s.Get<QuestionCategoryModel>(categoryId);
                    if (!overridePermissions)
                    {
                        PermissionsUtility.Create(s, caller).ViewOrigin(category.OriginType, category.OriginId);
                    }
                    return category;
                }
            }
        }

        public QuestionModel EditQuestion(UserOrganizationModel caller, long questionId, Origin origin = null,
                                                                                        LocalizedStringModel question = null,
                                                                                        long? categoryId = null,
                                                                                        DateTime? deleteTime = null,
                                                                                        QuestionType? questionType = null)
        {
            QuestionModel q;
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    caller = s.Get<UserOrganizationModel>(caller.Id);

                    var permissionUtility = PermissionsUtility.Create(s, caller);
                    q = new QuestionModel();

                    if (questionId == 0) //Creating
                    {
                        q.DateCreated = DateTime.UtcNow;
                        q.CreatedById = caller.Id;
                    }
                    else
                    {
                        q = s.Get<QuestionModel>(questionId);
                        permissionUtility.EditQuestion(q);
                    }

                    /*if (question.CreatedBy.Id == caller.Id)
                        question.CreatedBy = caller;*/
                    //Edit Origin
                    if (origin != null)
                    {
                        permissionUtility.EditOrigin(origin);
                        q.OriginId = origin.OriginId;
                        q.OriginType = origin.OriginType;
                    }
                    //Edit Question
                    if (question != null)
                    {
                        q.Question.UpdateDefault(question.Standard);
                        //q.Question = s.Get<LocalizedStringModel>(question.Id);
                    }
                    //Edit CategoryId
                    if (categoryId != null)
                    {
                        if (categoryId == 0)
                            throw new PermissionsException();
                        permissionUtility.PairCategoryToQuestion(categoryId.Value, questionId);
                        var category = s.Get<QuestionCategoryModel>(categoryId);
                        q.Category = category;
                    }
                    //Edit DeleteTime
                    if (deleteTime != null)
                    {
                        q.DeleteTime = deleteTime.Value;
                    }
                    //Edit questionType
                    if (questionType != null)
                    {
                        q.QuestionType = questionType.Value;
                    }

                    s.SaveOrUpdate(q);
                    tx.Commit();
                    s.Flush();
                }
            }
            return q;
        }

        public QuestionModel GetQuestion(UserOrganizationModel caller, long id)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var question = s.Get<QuestionModel>(id);
                    PermissionsUtility.Create(s, caller).ViewQuestion(question);
                    return question;
                }
            }
        }

    }
}