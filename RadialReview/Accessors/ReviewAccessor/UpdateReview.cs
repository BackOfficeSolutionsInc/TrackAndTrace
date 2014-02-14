﻿using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors
{
    public partial class ReviewAccessor : BaseAccessor
    {
        #region Edit Review
        public static void AddAskablesToReview(AbstractUpdate s, PermissionsUtility perms, UserOrganizationModel caller, UserOrganizationModel forUser, ReviewModel reviewModel, List<AskableAbout> askables)
        {
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
        }

        public void AddToReview(UserOrganizationModel caller, long byUserId, long reviewId, long aboutUserId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    AddToReview(s.ToDataInteraction(true), perms, caller, byUserId, reviewId, aboutUserId, AboutType.NoRelationship);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        private static void AddToReview(DataInteraction s, PermissionsUtility perms, UserOrganizationModel caller, long byUserId, long reviewId, long aboutUserId, AboutType aboutType)
        {
            perms.ViewUserOrganization(byUserId, false).ViewReview(reviewId);

            var review = s.Get<ReviewModel>(reviewId);
            perms.ViewUserOrganization(review.ForUserId, false);

            var askable = new AskableUtility();

            var feedbackQuestion = ApplicationAccessor.GetApplicationQuestion(s.GetQueryProvider(), ApplicationAccessor.FEEDBACK);
            var userResponsibilities = ResponsibilitiesAccessor
                                                  .GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, aboutUserId)
                                                  .SelectMany(x => x.Responsibilities)
                                                  .ToListAlive();
            askable.AddUnique(userResponsibilities, aboutType, aboutUserId);
            askable.AddUnique(feedbackQuestion, aboutType, aboutUserId);

            var forUser = s.Get<UserOrganizationModel>(review.ForUserId);
            //var review=s.QueryOver<ReviewModel>().Where(x=>x.ForReviewsId == reviewContainerId && x.ForUserId==byUserId).SingleOrDefault();

            AddAskablesToReview(s.GetUpdateProvider(), perms, caller, forUser, review, askable.Askables);
        }

        public void RemoveQuestionFromReviewForUser(UserOrganizationModel caller, long reviewContainerId,long userId, long askableId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            { 
                using (var tx = s.BeginTransaction())
                {
                    var answers = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.Askable.Id == askableId && x.AboutUserId == userId).List();
                    var deleteTime = DateTime.UtcNow;

                    foreach (var answer in answers)
                    {
                        answer.DeleteTime = deleteTime;
                        s.Update(answer);
                    }
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void AddResponsibilityAboutUserToReview(UserOrganizationModel caller, long reviewContainerId, long userId, long askableId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var reviewContainer=s.Get<ReviewsModel>(reviewContainerId);
                    var orgId=reviewContainer.ForOrganization.Id;
                    var perms = PermissionsUtility.Create(s,caller).EditReviewContainer(reviewContainerId).ViewOrganization(orgId);                    
                    
                    var queryProvider = GetReviewQueryProvider(s,orgId,reviewContainerId);
                    queryProvider.AddData(reviewContainer.AsList());

                    var dataInteration = new DataInteraction(queryProvider,s.ToUpdateProvider());

                    var team = dataInteration.Get<OrganizationTeamModel>(reviewContainer.ForTeamId);
                    //I think we want ToList, not ToListAlive
                    var existingReviewUsers = dataInteration.Where<ReviewModel>(x => x.ForReviewsId == reviewContainerId).Select(x => x.ForUser).ToList();
                    var user = dataInteration.Get<UserOrganizationModel>(userId);

                    var relationships = GetUsersThatReviewUser(caller, perms, dataInteration, user, reviewContainer.GetParameters(), team, existingReviewUsers);

                    var askable = s.Get<Askable>(askableId);
                    
                    foreach (var r in relationships)
                    {
                        var existingReview = dataInteration.Where<ReviewModel>(x => x.ForUserId == r.Key.Id).Single();
                        var askableUtil=new AskableUtility();
                        foreach(var about in r.Value){
                            askableUtil.AddUnique(askable, about.Invert(), userId);
                        }
                        AddAskablesToReview(dataInteration.GetUpdateProvider(),perms,caller,r.Key,existingReview,askableUtil.Askables);
                    }

                    tx.Commit();
                    s.Flush();
                }
            }
        }
        #endregion

        public ResultObject RemoveUserFromReview(UserOrganizationModel caller, long reviewContainerId, long userOrganizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller).ManagesUserOrganization(userOrganizationId).EditReviewContainer(reviewContainerId);

                    var deleteTime = DateTime.UtcNow;
                    var user=s.Get<UserOrganizationModel>(userOrganizationId);

                    var review = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.ForUserId == userOrganizationId).SingleOrDefault();
                    review.DeleteTime = deleteTime;
                    s.Update(review);
                    review.DeleteTime = deleteTime;
                    var answers= s.QueryOver<AnswerModel>().Where(x => (x.AboutUserId == userOrganizationId || x.ByUserId==userOrganizationId) && x.ForReviewContainerId == reviewContainerId).List();
                    foreach (var a in answers)
                    {
                        a.DeleteTime = deleteTime;
                        s.Update(a);
                    }
                    tx.Commit();
                    s.Flush();
                    return ResultObject.Success("Removed " + user.GetNameAndTitle()+ " from the review.");
                }
            }

        }

        #region Update
        public ResultObject AddUserToReviewContainer(UserOrganizationModel caller, long reviewContainerId, long userOrganizationId)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller).ManagesUserOrganization(userOrganizationId).ViewReviews(reviewContainerId);
                        var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
                        var dueDate = reviewContainer.DueDate;
                        var sendEmails = false;
                        var reviewSelf = reviewContainer.ReviewSelf;
                        var reviewManagers = reviewContainer.ReviewManagers;
                        var reviewSubordinates = reviewContainer.ReviewSubordinates;
                        var reviewTeammates = reviewContainer.ReviewTeammates;
                        var reviewPeers = reviewContainer.ReviewPeers;
                        var organization = reviewContainer.ForOrganization;

                        OrganizationTeamModel team = TeamAccessor.GetTeam(s, perms, caller, reviewContainer.ForTeamId);


                        List<Exception> exceptions = new List<Exception>();
                        int sent = 0;
                        int errors = 0;

                        var beingReviewedUser = s.Get<UserOrganizationModel>(userOrganizationId);




                        var orgId = organization.Id;

                        var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).List();
                        var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).List();
                        var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).List();
                        var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).List();
                        var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId).List();
                        var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).List();
                        var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).List();
                        var reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId).List();

                        var queryProvider = new IEnumerableQuery();
                        queryProvider.AddData(allOrgTeams);
                        queryProvider.AddData(allTeamDurations);
                        queryProvider.AddData(allMembers);
                        queryProvider.AddData(allManagerSubordinates);
                        queryProvider.AddData(allPositions);
                        queryProvider.AddData(applicationQuestions);
                        queryProvider.AddData(application);
                        queryProvider.AddData(reviews);


                        var usersToReview = TeamAccessor.GetTeamMembers(s.ToQueryProvider(true), perms, caller, reviewContainer.ForTeamId)
                                                        .ToListAlive()
                                                        .Select(x => x.User).ToListAlive();

                        usersToReview.Add(beingReviewedUser);

                        //TODO Populate a queryprovider structure here..

                        AddUserToReview(caller, true, dueDate, sendEmails,
                            reviewContainer.GetParameters(),
                            new DataInteraction(queryProvider, s.ToUpdateProvider()), reviewContainer, perms, organization, team, exceptions,
                            ref sent, ref errors, beingReviewedUser, usersToReview);

                        if (errors > 0)
                        {
                            var message = String.Join("\n", exceptions.Select(x => x.Message));
                            return new ResultObject(new RedirectException(errors + " errors:\n" + message));
                        }

                        tx.Commit();
                        s.Flush();
                        return ResultObject.Create(false, "Successfully added " + beingReviewedUser.GetName() + " to the review.");
                    }
                }
            }
            catch (Exception e)
            {
                return new ResultObject(e);
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns>Complete</returns>
        public Boolean UpdateSliderAnswer(UserOrganizationModel caller, long id, decimal? value)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<SliderAnswer>(id);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

                    answer.Complete = value.HasValue;
                    answer.Percentage = value;
                    if (answer.Complete)
                    {
                        answer.CompleteTime = DateTime.UtcNow;
                    }
                    else
                    {
                        answer.CompleteTime = null;
                    }
                    s.Update(answer);

                    tx.Commit();
                    s.Flush();
                    return answer.Complete || !answer.Required;
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns>Complete</returns>
        public Boolean UpdateThumbsAnswer(UserOrganizationModel caller, long id, ThumbsType value)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<ThumbsAnswer>(id);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

                    answer.Complete = value != ThumbsType.None;
                    answer.Thumbs = value;
                    if (answer.Complete)
                    {
                        answer.CompleteTime = DateTime.UtcNow;
                    }
                    else
                    {
                        answer.CompleteTime = null;
                    }
                    s.Update(answer);
                    tx.Commit();
                    s.Flush();
                    return answer.Complete || !answer.Required;
                }
            }
        }
        public Boolean UpdateRelativeComparisonAnswer(UserOrganizationModel caller, long questionId, RelativeComparisonType choice)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<RelativeComparisonAnswer>(questionId);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

                    answer.Complete = (choice != RelativeComparisonType.None);
                    if (answer.Complete)
                    {
                        answer.CompleteTime = DateTime.UtcNow;
                    }
                    else
                    {
                        answer.CompleteTime = null;
                    }
                    answer.Choice = choice;
                    s.Update(answer);
                    tx.Commit();
                    s.Flush();
                    return answer.Complete || !answer.Required;
                }
            }
        }

        public Boolean UpdateFeedbackAnswer(UserOrganizationModel caller, long questionId, string feedback)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<FeedbackAnswer>(questionId);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);
                    answer.Complete = !String.IsNullOrWhiteSpace(feedback);
                    if (answer.Complete){
                        answer.CompleteTime = DateTime.UtcNow;
                    }else{
                        answer.CompleteTime = null;
                    }
                    answer.Feedback = feedback;
                    s.Update(answer);
                    tx.Commit();
                    s.Flush();
                    return answer.Complete || !answer.Required;
                }
            }
        }
        public void AddAnswerToReview(UserOrganizationModel caller, long reviewId, long answerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);
                    var feedback = s.Get<AnswerModel>(answerId);
                    var review = s.Get<ReviewModel>(reviewId);
                    if (review.ForUserId != feedback.AboutUserId)
                        throw new PermissionsException("Answer and Review do not match.");

                    review.ClientReview.FeedbackIds.Add(new LongModel() { Value = answerId });
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public void RemoveAnswerFromReview(UserOrganizationModel caller, long reviewId, long answerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);
                    var answer = s.Get<AnswerModel>(answerId);
                    var review = s.Get<ReviewModel>(reviewId);

                    if (review.ForUserId != answer.AboutUserId)
                        throw new PermissionsException("Answer and Review do not match.");

                    foreach (var id in review.ClientReview.FeedbackIds)
                    {
                        if (id.Value == answerId)
                            id.DeleteTime = DateTime.UtcNow;
                    }
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public long AddChartToReview(UserOrganizationModel caller, long reviewId, long xCategoryId, long yCategoryId, String groups, String filters,DateTime startTime,DateTime endTime)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId).ViewCategory(xCategoryId).ViewCategory(yCategoryId);
                    var xAxis = s.Get<QuestionCategoryModel>(xCategoryId);
                    var yAxis = s.Get<QuestionCategoryModel>(yCategoryId);

                    var review = s.Get<ReviewModel>(reviewId);

                    var tuple = new LongTuple() { 
                        Item1 = xCategoryId,
                        Item2 = yCategoryId,
                        Groups = groups,
                        Filters = filters,
                        StartDate=startTime,
                        EndDate=endTime
                    };
                    review.ClientReview.Charts.Add(tuple);
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                    return tuple.Id;
                }
            }
        }
        public void RemoveChartFromReview(UserOrganizationModel caller, long reviewId, long chartId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);

                    var review = s.Get<ReviewModel>(reviewId);
                    foreach (var id in review.ClientReview.Charts)
                    {
                        if (id.Id == chartId)
                            id.DeleteTime = DateTime.UtcNow;
                    }
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void SetIncludeQuestionTable(UserOrganizationModel caller, long reviewId, bool on)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);

                    var review = s.Get<ReviewModel>(reviewId);
                    review.ClientReview.IncludeQuestionTable = on;
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void SetIncludeManagerAnswers(UserOrganizationModel caller, long reviewId, bool on)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);

                    var review = s.Get<ReviewModel>(reviewId);
                    review.ClientReview.IncludeManagerFeedback = on;
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }


        public void SetIncludeSelfAnswers(UserOrganizationModel caller, long reviewId, bool on)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);

                    var review = s.Get<ReviewModel>(reviewId);
                    review.ClientReview.IncludeSelfFeedback = on;
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void Authorize(UserOrganizationModel caller, long reviewId, bool authorized)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);
                    var review = s.Get<ReviewModel>(reviewId);
                    review.ClientReview.Visible = authorized;
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public void UpdateNotes(UserOrganizationModel caller, long id, string notes)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(id);
                    var review = s.Get<ReviewModel>(id);

                    review.ClientReview.ManagerNotes = notes;
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        #endregion
    }
}