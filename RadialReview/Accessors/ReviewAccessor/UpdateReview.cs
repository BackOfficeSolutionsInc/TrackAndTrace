using Microsoft.AspNet.SignalR;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Application;
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

            var appQuestions = ApplicationAccessor.GetApplicationQuestions(s.GetQueryProvider()).ToList();//, ApplicationAccessor.FEEDBACK);
            var userResponsibilities = ResponsibilitiesAccessor
                                                  .GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, caller, aboutUserId)
                                                  .SelectMany(x => x.Responsibilities)
                                                  .ToListAlive();
            askable.AddUnique(userResponsibilities, aboutType, aboutUserId);
            foreach(var aq in appQuestions)
                askable.AddUnique(aq, aboutType, aboutUserId);

            var forUser = s.Get<UserOrganizationModel>(review.ForUserId);
            //var review=s.QueryOver<ReviewModel>().Where(x=>x.ForReviewsId == reviewContainerId && x.ForUserId==byUserId).SingleOrDefault();

            AddAskablesToReview(s.GetUpdateProvider(), perms, caller, forUser, review, askable.Askables);
        }

        public void RemoveQuestionFromReviewForUser(UserOrganizationModel caller, long reviewContainerId, long userId, long askableId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answers = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.Askable.Id == askableId && x.AboutUserId == userId && x.DeleteTime == null).List();
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
                    var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
                    var orgId = reviewContainer.ForOrganization.Id;
                    var perms = PermissionsUtility.Create(s, caller).EditReviewContainer(reviewContainerId).ViewOrganization(orgId);

                    var queryProvider = GetReviewQueryProvider(s, orgId, reviewContainerId);
                    queryProvider.AddData(reviewContainer.AsList());

                    var dataInteration = new DataInteraction(queryProvider, s.ToUpdateProvider());

                    var team = dataInteration.Get<OrganizationTeamModel>(reviewContainer.ForTeamId);
                    //I think we want ToList, not ToListAlive
                    var existingReviewUsers = dataInteration.Where<ReviewModel>(x => x.ForReviewsId == reviewContainerId).Select(x => x.ForUser).ToList();
                    var user = dataInteration.Get<UserOrganizationModel>(userId);

                    var relationships = GetUsersThatReviewUser(caller, perms, dataInteration, user, reviewContainer.GetParameters(), team, existingReviewUsers);

                    var askable = s.Get<Askable>(askableId);

                    foreach (var r in relationships)
                    {
                        var existingReview = dataInteration.Where<ReviewModel>(x => x.ForUserId == r.Key.Id).Single();
                        var askableUtil = new AskableUtility();
                        foreach (var about in r.Value)
                        {
                            askableUtil.AddUnique(askable, about.Invert(), userId);
                        }
                        AddAskablesToReview(dataInteration.GetUpdateProvider(), perms, caller, r.Key, existingReview, askableUtil.Askables);
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
                    var perms = PermissionsUtility.Create(s, caller)
                        .ManagesUserOrganization(userOrganizationId, false)
                        .EditReviewContainer(reviewContainerId);

                    var deleteTime = DateTime.UtcNow;
                    var user = s.Get<UserOrganizationModel>(userOrganizationId);

                    var review = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId && x.ForUserId == userOrganizationId).SingleOrDefault();
                    review.DeleteTime = deleteTime;
                    s.Update(review);
                    review.DeleteTime = deleteTime;
                    var answers = s.QueryOver<AnswerModel>().Where(x => (x.AboutUserId == userOrganizationId || x.ByUserId == userOrganizationId) && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).List();
                    foreach (var a in answers)
                    {
                        a.DeleteTime = deleteTime;
                        s.Update(a);
                    }
                    tx.Commit();
                    s.Flush();
                    return ResultObject.Success("Removed " + user.GetNameAndTitle() + " from the review.");
                }
            }

        }

        #region Update
        public async Task<ResultObject> AddUserToReviewContainer(UserOrganizationModel caller, long reviewContainerId, long userOrganizationId, bool sendEmails)
        {
            var unsent = new List<MailModel>();
            String userBeingReviewed = null;
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller)
                            .ManagesUserOrganization(userOrganizationId, false)
                            .ViewReviews(reviewContainerId);
                        var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
                        var dueDate = reviewContainer.DueDate;
                        //var sendEmails = false;
                        var reviewSelf = reviewContainer.ReviewSelf;
                        var reviewManagers = reviewContainer.ReviewManagers;
                        var reviewSubordinates = reviewContainer.ReviewSubordinates;
                        var reviewTeammates = reviewContainer.ReviewTeammates;
                        var reviewPeers = reviewContainer.ReviewPeers;
                        var organization = reviewContainer.ForOrganization;

                        OrganizationTeamModel team = TeamAccessor.GetTeam(s, perms, caller, reviewContainer.ForTeamId);


                        List<Exception> exceptions = new List<Exception>();
                        // int sent = 0;
                        //int errors = 0;

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

                        unsent.AddRange(AddUserToReview(caller, true, dueDate,
                            reviewContainer.GetParameters(),
                            new DataInteraction(queryProvider, s.ToUpdateProvider()), reviewContainer, perms, organization, team, ref exceptions,
                            beingReviewedUser, usersToReview));
                        userBeingReviewed = beingReviewedUser.GetName();
                        tx.Commit();
                        s.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                return new ResultObject(e);
            }
            var result = new EmailResult();

            if (sendEmails)
            {
                result = await Emailer.SendEmails(unsent);
            }

            return result.ToResults("Successfully added " + userBeingReviewed + " to the review.");
        }

        /*public void UpdateStarted(UserOrganizationModel userOrganizationModel, List<AnswerModel> answers, DateTime now)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    foreach (var p in answers)
                    {
                        if (p.CompleteTime == null)
                        {
                            p.StartTime = now;
                        }


                    }
                    tx.Commit();
                    s.Flush();
                }
            }

        }*/

        public ReviewContainerStats UpdateAllCompleted(UserOrganizationModel caller, long reviewId, bool started, decimal durationMinutes, int additionalAnswered, int optionalAnswered)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditReview(reviewId);
                    var uncompleted = s.QueryOver<AnswerModel>().Where(x => x.ForReviewId == reviewId && x.Required && !x.Complete && x.DeleteTime == null).RowCount();
                    var review = s.Get<ReviewModel>(reviewId);

                    var output = new ReviewContainerStats(review.ForReviewsId);

                    var updated = false;
                    if (durationMinutes != 0)
                    {
                        if (review.DurationMinutes == null)
                            review.DurationMinutes = 0;
                        review.DurationMinutes += (decimal)Math.Min(durationMinutes, (decimal)TimingUtility.ExcludeLongerThan.TotalMinutes);
                        updated = true;
                    }

                    if (uncompleted == 0 && !review.Complete)
                    {
                        review.Complete = true;
                        updated = true;
                        output.Stats.ReviewsCompleted += 1;
                        output.Completion.Started -= 1;
                        output.Completion.Finished += 1;
                    }

                    if (started && !review.Started)
                    {
                        review.Started = true;
                        updated = true;
                        output.Completion.Started += 1;
                        output.Completion.Unstarted -= 1;
                    }

                    if (uncompleted != 0 && review.Complete)
                    {
                        review.Complete = false;
                        review.DurationMinutes = null;
                        updated = true;
                        output.Stats.ReviewsCompleted -= 1;
                        output.Completion.Finished -= 1;
                        output.Completion.Started += 1;
                    }

                    output.Stats.QuestionsAnswered += additionalAnswered;
                    output.Stats.OptionalsAnswered += optionalAnswered;

                    if (updated || output.Stats.QuestionsAnswered!=0 || output.Stats.OptionalsAnswered!=0)
                    {
                        IHubContext hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
                        //hub.Clients.All.updateReviewStats(ResultObject.Create(output));
                        hub.Clients.Group("manager_" + caller.Organization.Id).updateReviewStats(ResultObject.Create(output));
                    }

                    if (updated)
                    {
                        s.Update(review);
                        tx.Commit();
                        s.Flush();
                    }

                    return output;
                    //return review.Complete;
                }
            }
        }

        private void UpdateCompletion(AnswerModel answer, DateTime now, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
        {
            if (answer.Complete)
            {
                if (answer.CompleteTime == null)
                {
                    if (!answer.Required)
                        optionalAnsweredDelta += 1;
                    questionsAnsweredDelta += 1;
                }
                answer.CompleteTime = now;
            }
            else
            {
                if (answer.CompleteTime != null)
                {
                    if (!answer.Required)
                        optionalAnsweredDelta -= 1;
                    questionsAnsweredDelta -= 1;
                }
                answer.CompleteTime = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="id"></param>
        /// <param name="value"></param>
        /// <returns>Complete</returns>
        public Boolean UpdateSliderAnswer(UserOrganizationModel caller, long id, decimal? value, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<SliderAnswer>(id);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);
                    edited = false;
                    if (answer.Percentage != value)
                    {
                        edited = true;
                        answer.Complete = value.HasValue;
                        answer.Percentage = value;
                        UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
                        s.Update(answer);

                        tx.Commit();
                        s.Flush();
                    }
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
        public Boolean UpdateThumbsAnswer(UserOrganizationModel caller, long id, ThumbsType value, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<ThumbsAnswer>(id);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

                    edited = false;
                    if (value != answer.Thumbs)
                    {
                        edited = true;
                        answer.Complete = value != ThumbsType.None;
                        answer.Thumbs = value;
                        UpdateCompletion(answer, now, ref  questionsAnsweredDelta, ref  optionalAnsweredDelta);

                        s.Update(answer);
                        tx.Commit();
                        s.Flush();
                    }
                    return answer.Complete || !answer.Required;
                }
            }
        }
        public Boolean UpdateRelativeComparisonAnswer(UserOrganizationModel caller, long questionId, RelativeComparisonType choice, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<RelativeComparisonAnswer>(questionId);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

                    edited = false;
                    if (choice != answer.Choice)
                    {
                        edited = true;
                        answer.Complete = (choice != RelativeComparisonType.None);
                        UpdateCompletion(answer, now, ref  questionsAnsweredDelta, ref  optionalAnsweredDelta);
                        answer.Choice = choice;
                        s.Update(answer);
                        tx.Commit();
                        s.Flush();
                    }

                    return answer.Complete || !answer.Required;
                }
            }
        }

        public Boolean UpdateFeedbackAnswer(UserOrganizationModel caller, long questionId, string feedback, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var answer = s.Get<FeedbackAnswer>(questionId);
                    PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);
                    edited = false;
                    if (answer.Feedback != feedback)
                    {
                        edited = true;
                        answer.Complete = !String.IsNullOrWhiteSpace(feedback);
                        UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
                        answer.Feedback = feedback;
                        s.Update(answer);
                        tx.Commit();
                        s.Flush();
                    }
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
        public long AddChartToReview(UserOrganizationModel caller, long reviewId, long xCategoryId, long yCategoryId, String groups, String filters, DateTime startTime, DateTime endTime)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId).ViewCategory(xCategoryId).ViewCategory(yCategoryId);
                    var xAxis = s.Get<QuestionCategoryModel>(xCategoryId);
                    var yAxis = s.Get<QuestionCategoryModel>(yCategoryId);

                    var review = s.Get<ReviewModel>(reviewId);

                    var tuple = new LongTuple()
                    {
                        Item1 = xCategoryId,
                        Item2 = yCategoryId,
                        Groups = groups,
                        Filters = filters,
                        StartDate = startTime,
                        EndDate = endTime
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

        private void SetAggregateBy(UserOrganizationModel caller, long reviewId, string aggregateBy)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);

                    var review = s.Get<ReviewModel>(reviewId);
                    review.ClientReview.ScatterChart.Groups = aggregateBy;
                    s.Update(review.ClientReview);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void UpdateScatterChart(
            UserOrganizationModel   caller, 
            long                    reviewId, 
            string                  aggregateBy, 
            string                  filterBy, 
            string                  title, 
            long?                   xAxis, 
            long?                   yAxis, 
            DateTime?               startTime,
            DateTime?               endTime,
            bool?                   included) 
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);

                    var review = s.Get<ReviewModel>(reviewId);
                    if (aggregateBy != null)
                        review.ClientReview.ScatterChart.Groups = aggregateBy;
                    if (filterBy != null)
                        review.ClientReview.ScatterChart.Filters = filterBy;
                    if (title != null)
                        review.ClientReview.ScatterChart.Title = title;

                    if (xAxis != null)
                        review.ClientReview.ScatterChart.Item1 = xAxis.Value;
                    if (yAxis != null)
                        review.ClientReview.ScatterChart.Item2 = yAxis.Value;
                    if (startTime != null)
                        review.ClientReview.ScatterChart.StartDate = startTime.Value;
                    if (endTime != null)
                        review.ClientReview.ScatterChart.EndDate = endTime.Value;
                    if (included != null)
                        review.ClientReview.IncludeScatterChart = included.Value;
                
                    s.Update(review.ClientReview);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
    
        public void SetIncludeScatter(UserOrganizationModel caller, long reviewId, bool on)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);

                    var review = s.Get<ReviewModel>(reviewId);
                    review.ClientReview.IncludeScatterChart = on;
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public void SetIncludeTimeline(UserOrganizationModel caller, long reviewId, bool on)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);

                    var review = s.Get<ReviewModel>(reviewId);
                    review.ClientReview.IncludeTimelineChart = on;
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
		public void SetIncludeNotes(UserOrganizationModel caller, long reviewId, bool on) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManageReview(reviewId);

					var review = s.Get<ReviewModel>(reviewId);
					review.ClientReview.IncludeNotes = on;
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

        public ResultObject Authorize(UserOrganizationModel caller, long reviewId, bool authorized)
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
                    if (authorized)
                        return ResultObject.Success(review.ForUser.GetFirstName() + " is authorized to view this report.");
                    else
                        return ResultObject.Success(review.ForUser.GetFirstName() + " is NOT authorized to view this report.");
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