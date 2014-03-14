using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Prereview;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Tasks;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class PrereviewAccessor : BaseAccessor
    {
        public void ManagerCustomizePrereview(UserOrganizationModel caller, long prereviewId, List<Tuple<long, long>> whoReviewsWho)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var prereview = s.Get<PrereviewModel>(prereviewId);
                    if (!prereview.Started && whoReviewsWho.Any())
                    {
                        prereview.Started = true;
                        s.Update(prereview);
                    }

                    var managerId = prereview.ManagerId;

                    PermissionsUtility.Create(s, caller).ManagerAtOrganization(managerId, caller.Organization.Id).EditUserOrganization(managerId);

                    var existing = s.QueryOver<PrereviewMatchModel>().Where(x => x.PrereviewId == prereviewId).List().ToListAlive();

                    foreach (var mids in whoReviewsWho.Where(x => !existing.Any(y => y.FirstUserId == x.Item1 && y.SecondUserId == x.Item2)))
                    {
                        var match = new PrereviewMatchModel()
                        {
                            FirstUserId = mids.Item1,
                            SecondUserId = mids.Item2,
                            PrereviewId = prereviewId,
                        };
                        s.Save(match);
                    }
                    var deleteTime = DateTime.UtcNow;

                    foreach (var mids in existing.Where(x => !whoReviewsWho.Any(y => y.Item1 == x.FirstUserId && y.Item2 == x.SecondUserId)))
                    {
                        mids.DeleteTime = deleteTime;
                        s.Update(mids);
                    }
                    /*
                    var allPrereviews = s.QueryOver<PrereviewMatchModel>().Where(x=>x.PrereviewId==prereviewId).List().ToList();
                    foreach(var mids in whoReviewsWho)
                    {
                        foreach(var update in allPrereviews.Where(x=>x.FirstUserId==mids.Item1 && x.SecondUserId==mids.Item2)){
                            update.DeleteTime=DateTime.UtcNow;
                            s.Update(update);
                        }                        
                    }*/
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void CreatePrereview(UserOrganizationModel caller, long forTeamId, String reviewName, bool sendEmails, DateTime dueDate, DateTime preReviewDue)
        {
            if (preReviewDue >= dueDate)
                throw new PermissionsException("The pre-review due date must be before the review due date.");

            using (var s = HibernateSession.GetCurrentSession())
            {
                var createReviewGuid = Guid.NewGuid();
                var perms = PermissionsUtility.Create(s, caller);
                var reviewContainer = new ReviewsModel()
                {
                    DateCreated = DateTime.UtcNow,
                    DueDate = dueDate,
                    ReviewName = reviewName,
                    CreatedById = caller.Id,
                    /*ReviewManagers = reviewManagers,
                    ReviewPeers = reviewPeers,
                    ReviewSelf = reviewSelf,
                    ReviewSubordinates = reviewSubordinates,
                    ReviewTeammates = reviewTeammates,*/
                    ForTeamId = forTeamId,
                };
                ReviewAccessor.CreateReviewContainer(s, perms, caller, reviewContainer);
                using (var tx = s.BeginTransaction())
                {
                    var dataInteraction = ReviewAccessor.GetReviewDataInteraction(s, caller.Organization.Id);
                    var teammembers = TeamAccessor.GetTeamMembers(dataInteraction.GetQueryProvider(), perms, caller, forTeamId);

                    var team = dataInteraction.GetQueryProvider().Get<OrganizationTeamModel>(forTeamId);
                    var managerIds = teammembers.Where(x => x.User.ManagerAtOrganization || team.ManagedBy == x.UserId).Select(x => x.UserId).ToList();
                    var errors = 0;
                    var sent = 0;

                    var createReviewNexus = new NexusModel(createReviewGuid) { ActionCode = NexusActions.CreateReview };
                    createReviewNexus.SetArgs("" + reviewContainer.Id);
                    NexusAccessor.Put(s.ToUpdateProvider(), createReviewNexus);

                    var task = new ScheduledTask()
                    {
                        Fire = preReviewDue,
                        Url = "/n/" + createReviewGuid
                    };

                    TaskAccessor.AddTask(dataInteraction.GetUpdateProvider(), task);

                    reviewContainer.TaskId = task.Id;

                    foreach (var mid in managerIds)
                    {
                        var prereview = new PrereviewModel()
                        {
                            ManagerId = mid,
                            PrereviewDue = preReviewDue,
                            ReviewContainerId = reviewContainer.Id
                        };
                        dataInteraction.Save(prereview);

                        var guid = Guid.NewGuid();
                        var nexus = new NexusModel(guid)
                        {
                            ActionCode = NexusActions.Prereview,
                            ByUserId = caller.Id,
                            ForUserId = mid,
                        };

                        nexus.SetArgs("" + reviewContainer.Id, "" + prereview.Id);
                        var manager = dataInteraction.Get<UserOrganizationModel>(mid);
                        NexusAccessor.Put(dataInteraction.GetUpdateProvider(), nexus);

                        if (sendEmails)
                        {
                            try
                            {
                                //Send email
                                var subject = String.Format(RadialReview.Properties.EmailStrings.Prereview_Subject, caller.Organization.GetName());
                                var body = String.Format(EmailStrings.Prereview_Body, manager.GetName(), caller.GetName(), dueDate.ToShortDateString(), ProductStrings.BaseUrl + "n/" + guid, ProductStrings.BaseUrl + "n/" + guid, ProductStrings.ProductName);
                                Emailer.SendEmail(dataInteraction.GetUpdateProvider(), manager.GetEmail(), subject, body);
                                sent++;
                            }
                            catch (Exception e)
                            {
                                log.Error(e.Message, e);
                                errors++;
                            }
                        }
                    }
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public PrereviewModel GetPrereview(UserOrganizationModel caller, long prereviewId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewPrereview(prereviewId);
                    var prereview = s.Get<PrereviewModel>(prereviewId);
                    return prereview;
                }
            }
        }

        public List<Tuple<long, long>> GetCustomMatches(UserOrganizationModel caller, long prereviewId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewPrereview(prereviewId);
                    return s.QueryOver<PrereviewMatchModel>()
                        .Where(x => x.PrereviewId == prereviewId)
                        .List().ToListAlive()
                        .Select(x => Tuple.Create(x.FirstUserId, x.SecondUserId))
                        .ToList();
                }
            }
        }

        public List<Tuple<long, long>> GetAllMatchesForReview(UserOrganizationModel caller, long reviewContainerId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewReviews(reviewContainerId);

                    var prereviews = s.QueryOver<PrereviewModel>().Where(x => x.ReviewContainerId == reviewContainerId).List().ToList();

                    var all = new List<Tuple<long, long>>();

                    foreach (var prereview in prereviews)
                    {
                        all.AddRange(s.QueryOver<PrereviewMatchModel>().Where(x => x.PrereviewId == prereview.Id).List().ToListAlive().Select(x => Tuple.Create(x.FirstUserId, x.SecondUserId)).ToList());
                    }

                    return all.Distinct().ToList();
                }
            }
        }

        public static List<PrereviewModel> GetPrereviewsForUser(AbstractQuery s, PermissionsUtility perms, long userOrgId)
        {
            perms.ViewUserOrganization(userOrgId, false);
            return s.Where<PrereviewModel>(x => x.ManagerId == userOrgId).ToList();
        }

        public void UnsafeExecuteAllPrereviews(ISession s, long reviewContainerId, DateTime now)
        {
            var prereviews = s.QueryOver<PrereviewModel>().Where(x => x.ReviewContainerId == reviewContainerId).List().ToList();
            foreach (var prereview in prereviews)
            {
                prereview.Executed = now;
                s.Update(prereview);
            }
        }
    }
}