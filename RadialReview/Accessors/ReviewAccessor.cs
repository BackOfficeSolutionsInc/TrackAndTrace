using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
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
    public class ReviewAccessor : BaseAccessor
    {
        #region Create

        private static void CreateReviewContainer(ISession s, PermissionsUtility perms, UserOrganizationModel caller, ReviewsModel reviewContainer)
        {
            using (var tx = s.BeginTransaction())
            {
                perms.ManagerAtOrganization(caller.Id, caller.Organization.Id);
                reviewContainer.CreatedById = caller.Id;
                reviewContainer.ForOrganization = caller.Organization;
                s.SaveOrUpdate(reviewContainer);
                tx.Commit();
            }
        }

        public ResultObject CreateCompleteReview(UserOrganizationModel caller, long forTeamId, DateTime dueDate, String reviewName,bool emails)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                ReviewsModel reviewContainer;
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    reviewContainer = new ReviewsModel()
                      {
                          DateCreated = DateTime.UtcNow,
                          DueDate = dueDate,
                          ReviewName = reviewName,
                          CreatedById = caller.Id,
                      };

                    ReviewAccessor.CreateReviewContainer(s, perms, caller, reviewContainer);

                }

                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ManagingTeam(forTeamId);

                    var organization = caller.Organization;
                    OrganizationTeamModel team;
                    /*if (forTeamId == -5) // Subordinates
                    {
                        team = OrganizationTeamModel.SubordinateTeam(caller);
                    }
                    else
                    {*/
                    team = TeamAccessor.GetTeam(s, perms, caller, forTeamId);
                    //}
                    var usersToReview = TeamAccessor.GetTeamMembers(s, perms, caller, forTeamId).ToListAlive();

                    List<Exception> exceptions = new List<Exception>();
                    int sent = 0;
                    int errors = 0;
                    foreach (var beingReviewed in usersToReview)
                    {
                        var beingReviewedUser = beingReviewed.User;
                        try
                        {
                            /*about self
                            var askable = new List<AskableAbout>();
                            askable.AddRange(responsibilities.Select(x => new AskableAbout() { Askable = x, AboutUserId = beingReviewed.Id, AboutType = AboutType.Self }));
                            askable.AddRange(questions.Select(x => new AskableAbout() { Askable = x, AboutUserId = beingReviewed.Id, AboutType = AboutType.Self }));
                            */

                            //Generate Askables
                            var askables = GetAskables(s, perms, caller, beingReviewedUser, team);
                            //Create the Review
                            var review = QuestionAccessor.GenerateReviewForUser(s, perms, caller, beingReviewedUser, reviewContainer, askables);
                            //Generate Review Nexus
                            Guid guid = Guid.NewGuid();
                            NexusModel nexus = new NexusModel(guid)
                            {
                                ForUserId = beingReviewed.Id,
                                ActionCode = NexusActions.TakeReview
                            };
                            NexusAccessor.Put(s, nexus);
                            if (emails)
                            {
                                //Send email
                                var subject = String.Format(RadialReview.Properties.EmailStrings.NewReview_Subject, organization.Name.Translate());
                                var body = String.Format(EmailStrings.NewReview_Body, beingReviewedUser.GetName(), caller.GetName(), dueDate.ToShortDateString(), ProductStrings.BaseUrl + "n/" + guid, ProductStrings.BaseUrl + "n/" + guid, ProductStrings.ProductName);
                                Emailer.SendEmail(s, beingReviewedUser.EmailAtOrganization, subject, body);
                            }
                            sent++;
                        }
                        catch (Exception e)
                        {
                            log.Error(e.Message,e);
                            errors++;
                            exceptions.Add(e);
                        }
                    }
                    tx.Commit();
                    s.Flush();

                    if (errors > 0)
                    {
                        var message = String.Join("\n", exceptions.Select(x => x.Message));
                        return new ResultObject(new RedirectException(errors + " errors:\n" + message));
                    }
                    return ResultObject.Create(new { due = dueDate, sent = sent, errors = errors });
                }
            }
        }
        /// <summary>
        /// Not secure
        /// </summary>
        /// <param name="caller"></param>
        /// <param name="review"></param>
        /*public void UpdateIndividualReview(UserOrganizationModel caller,ReviewModel review)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    //PermissionsUtility.Create(s, caller).EditReview();
                 
                    s.SaveOrUpdate(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }*/

        #endregion

        #region Get
        public List<ReviewModel> GetReviewsForUser(UserOrganizationModel caller, UserOrganizationModel forUser)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var forUserId = forUser.Id;
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(forUserId, true);

                    var reviews = s.QueryOver<ReviewModel>()
                        .Where(x => x.ForUserId == forUserId)
                        //.Fetch(x => x.Answers).Eager
                        //add reviewModel Id to answers, query for that
                        .List().ToList();

                    for (int i = 0; i < reviews.Count; i++)
                        PopulateAnswers(s, reviews[i]);
                    return reviews;
                }
            }
        }

        public ReviewModel GetReview(UserOrganizationModel caller, long reviewId)
        {
            var output = new ReviewModel();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var reviewPopulated = s.QueryOver<ReviewModel>()
                       .Where(x => x.Id == reviewId)
                       .Fetch(x => x.Answers).Eager
                       .SingleOrDefault();

                    PermissionsUtility.Create(s, caller).ViewUserOrganization(reviewPopulated.ForUserId, true).ViewReview(reviewId);


                    PopulateAnswers(s, reviewPopulated);
                    return reviewPopulated;
                }
            }
        }

        public List<AnswerModel> GetAnswersForUserReview(UserOrganizationModel caller, long userOrgId, long reviewContainerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewUserOrganization(userOrgId, true);

                    var answers = s.QueryOver<AnswerModel>()
                                        .Where(x => x.AboutUserId == userOrgId && x.ForReviewContainerId == reviewContainerId)
                                        .List()
                                        .ToList();
                    return answers;
                }
            }
        }

        public List<AnswerModel> GetReviewContainerAnswers(UserOrganizationModel caller,  long reviewContainerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var reviewContainer=s.Get<ReviewsModel>(reviewContainerId);

                    PermissionsUtility.Create(s, caller).ViewOrganization(reviewContainer.ForOrganization.Id);

                    var answers = s.QueryOver<AnswerModel>()
                                        .Where(x => x.ForReviewContainerId == reviewContainerId)
                                        .List()
                                        .ToList();
                    return answers;
                }
            }
        }

        public ClientReviewModel GetClientReview(UserOrganizationModel caller, long reviewId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewReview(reviewId);
                    return s.QueryOver<ClientReviewModel>().Where(x => x.ReviewId == reviewId).SingleOrDefault();
                }
            }
        }

        public List<ReviewsModel> GetReviewsCreatedByUser(UserOrganizationModel caller, long userOrganizationId, bool populate)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditReviews(userOrganizationId);
                    var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.CreatedById == userOrganizationId).List().ToList();
                    if (populate)
                    {
                        foreach (var rc in reviewContainers)
                        {
                            PopulateReviewContainer(s, rc);
                        }
                    }
                    return reviewContainers;
                }
            }
        }

        public List<ReviewsModel> GetReviewsForOrganization(UserOrganizationModel caller, long organizationId, bool populate)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);
                    var reviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.ForOrganization.Id == organizationId).List().ToList();
                    
                    //Completion should be much faster than getting all the reviews...

                    foreach (var rc in reviewContainers)
                    {
                        PopulateReviewContainerCompletion(s, rc);
                    }

                    
                    if (populate)
                    {
                        foreach (var rc in reviewContainers)
                        {
                            PopulateReviewContainer(s, rc);
                        }
                    }
                    return reviewContainers;
                }
            }
        }

        public ReviewsModel GetReviewContainer(UserOrganizationModel caller, long reviewContainerId, bool populate)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewReviews(reviewContainerId);
                    var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
                    if (populate)
                        PopulateReviewContainer(s, reviewContainer);

                    return reviewContainer;
                }
            }
        }

        #endregion

        #region Update
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
                    answer.Feedback = feedback;
                    s.Update(answer);
                    tx.Commit();
                    s.Flush();
                    return answer.Complete || !answer.Required;
                }
            }
        }
        public void AddFeedbackToReview(UserOrganizationModel caller, long reviewId, long feedbackId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);
                    var feedback = s.Get<FeedbackAnswer>(feedbackId);
                    var review = s.Get<ReviewModel>(reviewId);
                    if (review.ForUserId != feedback.AboutUserId)
                        throw new PermissionsException("Feedback and Review do not match.");

                    review.ClientReview.FeedbackIds.Add(new LongModel() { Value = feedbackId });
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public void RemoveFeedbackFromReview(UserOrganizationModel caller, long reviewId, long feedbackId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId);
                    var feedback = s.Get<FeedbackAnswer>(feedbackId);
                    var review = s.Get<ReviewModel>(reviewId);

                    if (review.ForUserId != feedback.AboutUserId)
                        throw new PermissionsException("Feedback and Review do not match.");

                    foreach (var id in review.ClientReview.FeedbackIds)
                    {
                        if (id.Value == feedbackId)
                            id.DeleteTime = DateTime.UtcNow;
                    }
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public long AddChartToReview(UserOrganizationModel caller, long reviewId, long xCategoryId, long yCategoryId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManageReview(reviewId).ViewCategory(xCategoryId).ViewCategory(yCategoryId);
                    var xAxis = s.Get<QuestionCategoryModel>(xCategoryId);
                    var yAxis = s.Get<QuestionCategoryModel>(yCategoryId);

                    var review = s.Get<ReviewModel>(reviewId);

                    var tuple=new LongTuple() { Item1 = xCategoryId, Item2 = yCategoryId };
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
                    var review=s.Get<ReviewModel>(id);

                    review.ClientReview.ManagerNotes = notes;                    
                    s.Update(review);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        #endregion

        #region Populate
        private void PopulateReviewContainerCompletion(ISession s, ReviewsModel reviewContainer)
        {
            var reviewContainerId = reviewContainer.Id;
            var reviewsQuery = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId).List();

            var completion = new CompletionModel(0,0);
            foreach (var answer in reviewsQuery)
            {
                completion += answer.GetCompletion();
            }
            reviewContainer.Completion = completion;
        }

        private void PopulateReviewContainer(ISession s, ReviewsModel reviewContainer)
        {
            var reviewContainerId = reviewContainer.Id;
            var reviewsQuery = s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == reviewContainerId);
            reviewsQuery.Fetch(x => x.ForUser).Eager.Future();
            var reviews = reviewsQuery.List().ToList();

            foreach (var r in reviews)
            {
                PopulateAnswers(s, r);
            }

            reviewContainer.Reviews = reviews;
        }

        private void PopulateAnswers(ISession session, ReviewModel review)
        {
            var answers = session.QueryOver<AnswerModel>().Where(x => x.ForReviewId == review.Id).List().ToList();
            review.Answers = answers;
        }
        #endregion

        #region GenerateAnswers


        private static List<AskableAbout> GetAskables(ISession s, PermissionsUtility perms, UserOrganizationModel caller, UserOrganizationModel beingReviewed, OrganizationTeamModel team)
        {
            #region comment
            /** Old questions way to do things.
            var review = _QuestionAccessor.GenerateReviewForUser(user, s, reviewContainer);
            //review.ForReviewsId = reviewContainer.Id;
            //review.DueDate = reviewContainer.DueDate;
            //review.Name = reviewContainer.ReviewName;
            //_ReviewAccessor.UpdateIndividualReview(user, review);
            */
            #endregion

            //var feedbackLSM = ApplicationAccessor.GetApplicationLocalizedStringModel(s, ApplicationAccessor.FEEDBACK);
            //var feedbackCategory = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK);

            var feedbackQuestion = ApplicationAccessor.GetApplicationQuestion(s, ApplicationAccessor.FEEDBACK);

            var responsibilityGroups = ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(s, perms, caller, beingReviewed.Id);

            var askable = new AskableUtility();

            // Personal Responsibilities 
            {
                var responsibilities = responsibilityGroups.SelectMany(x => x.Responsibilities).ToListAlive();
                var questions = QuestionAccessor.GetQuestionsForUser(s, perms, caller, beingReviewed.Id);

                askable.AddUnique(responsibilities, AboutType.Self, beingReviewed.Id);
                askable.AddUnique(questions, AboutType.Self, beingReviewed.Id);
                askable.AddUnique(feedbackQuestion, AboutType.Self, beingReviewed.Id);
            }
            // Team members 
            {
                List<OrganizationTeamModel> teams;

                if (team.Type != TeamType.Standard)
                    teams = responsibilityGroups.Where(x => x is OrganizationTeamModel).Cast<OrganizationTeamModel>().Where(x => x.InterReview).ToList();
                else
                    teams = team.AsList();

                foreach (var t in teams)
                {
                    var teamMembers = TeamAccessor.GetTeamMembers(s, perms, caller, t.Id).Where(x => x.User.Id != beingReviewed.Id).ToListAlive();
                    foreach (var teammember in teamMembers)
                    {
                        var teamMemberResponsibilities = ResponsibilitiesAccessor
                                                            .GetResponsibilityGroupsForUser(s, perms, caller, teammember.User.Id)
                                                            .SelectMany(x => x.Responsibilities)
                                                            .ToListAlive();
                        askable.AddUnique(teamMemberResponsibilities, AboutType.Teammate, teammember.User.Id);
                        askable.AddUnique(feedbackQuestion, AboutType.Teammate, teammember.User.Id);
                    }
                }
            }
            // Peers
            {
                if (team.Type != TeamType.Standard)
                {
                    List<UserOrganizationModel> peers = UserAccessor.GetPeers(s, perms, caller, beingReviewed.Id);
                    foreach (var peer in peers)
                    {
                        var peerResponsibilities = ResponsibilitiesAccessor
                                                            .GetResponsibilityGroupsForUser(s, perms, caller, peer.Id)
                                                            .SelectMany(x => x.Responsibilities)
                                                            .ToListAlive();
                        askable.AddUnique(peerResponsibilities, AboutType.Peer, peer.Id);
                        askable.AddUnique(feedbackQuestion, AboutType.Peer, peer.Id);
                    }
                }
            }
            // Managers
            {
                List<UserOrganizationModel> managers;
                if (team.Type != TeamType.Standard)
                    managers = UserAccessor.GetManagers(s, perms, caller, beingReviewed.Id);
                else
                    managers = UserAccessor.GetUserOrganization(s, perms, caller, team.ManagedBy, false, false)
                                            .AsList()
                                            .Where(x => x.Id != beingReviewed.Id)
                                            .ToList();

                foreach (var manager in managers)
                {
                    var managerResponsibilities = ResponsibilitiesAccessor
                                                        .GetResponsibilityGroupsForUser(s, perms, caller, manager.Id)
                                                        .SelectMany(x => x.Responsibilities)
                                                        .ToListAlive();
                    askable.AddUnique(managerResponsibilities, AboutType.Manager, manager.Id);
                    askable.AddUnique(feedbackQuestion, AboutType.Manager, manager.Id);
                }
            }
            // Subordinates
            {
                if (team.Type != TeamType.Standard)
                {
                    List<UserOrganizationModel> subordinates = UserAccessor.GetSubordinates(s, perms, caller, beingReviewed.Id);
                    foreach (var subordinate in subordinates)
                    {
                        var subordinateResponsibilities = ResponsibilitiesAccessor
                                                            .GetResponsibilityGroupsForUser(s, perms, caller, subordinate.Id)
                                                            .SelectMany(x => x.Responsibilities)
                                                            .ToListAlive();
                        askable.AddUnique(subordinateResponsibilities, AboutType.Subordinate, subordinate.Id);
                        askable.AddUnique(feedbackQuestion, AboutType.Subordinate, subordinate.Id);
                    }
                }
            }
            return askable.Askables;
        }

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
#endregion

        public static void AddAskablesToReview(ISession s, PermissionsUtility perms, UserOrganizationModel caller, UserOrganizationModel forUser, ReviewModel reviewModel, List<AskableAbout> askables)
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

        public void AddToReview(UserOrganizationModel caller,long byUserId, long reviewId,long aboutUserId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    var perms = PermissionsUtility.Create(s, caller)
                        .ViewUserOrganization(byUserId,false)
                        .ViewReview(reviewId);

                    var review=s.Get<ReviewModel>(reviewId);
                    perms.ViewUserOrganization(review.ForUserId, false);

                    var askable = new AskableUtility();

                    var feedbackQuestion = ApplicationAccessor.GetApplicationQuestion(s, ApplicationAccessor.FEEDBACK);
                    var userResponsibilities = ResponsibilitiesAccessor
                                                          .GetResponsibilityGroupsForUser(s, perms, caller, aboutUserId)
                                                          .SelectMany(x => x.Responsibilities)
                                                          .ToListAlive();
                    askable.AddUnique(userResponsibilities, AboutType.NoRelationship, aboutUserId);
                    askable.AddUnique(feedbackQuestion, AboutType.NoRelationship, aboutUserId);

                    var forUser = s.Get<UserOrganizationModel>(review.ForUserId);
                    //var review=s.QueryOver<ReviewModel>().Where(x=>x.ForReviewsId == reviewContainerId && x.ForUserId==byUserId).SingleOrDefault();

                    AddAskablesToReview(s, perms, caller, forUser, review, askable.Askables);

                    tx.Commit();
                    s.Flush();
                }
            }
        }
    }
}