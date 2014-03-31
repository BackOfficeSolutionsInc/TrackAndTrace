using NHibernate;
using RadialReview.Engines;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Prereview;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Models.Tasks;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task CreatePrereview(UserOrganizationModel caller, long forTeamId, String reviewName, bool sendEmails, DateTime dueDate, DateTime preReviewDue,bool ensureDefault)
        {
            if (preReviewDue >= dueDate)
                throw new PermissionsException("The pre-review due date must be before the review due date.");

            var unsentEmails = new List<MailModel>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                var createReviewGuid = Guid.NewGuid();
                var perms = PermissionsUtility.Create(s, caller);
                bool reviewManagers = true,
                     reviewPeers = true,
                     reviewSelf = true,
                     reviewSubordinates = true,
                     reviewTeammates = true;

                var reviewContainer = new ReviewsModel()
                {
                    DateCreated = DateTime.UtcNow,
                    DueDate = dueDate,
                    ReviewName = reviewName,
                    CreatedById = caller.Id,
                    PrereviewDueDate = preReviewDue,
                    HasPrereview =true,

                    ReviewManagers = reviewManagers,
                    ReviewPeers = reviewPeers,
                    ReviewSelf = reviewSelf,
                    ReviewSubordinates = reviewSubordinates,
                    ReviewTeammates = reviewTeammates,

                    ForTeamId = forTeamId,
                    EnsureDefault=ensureDefault,
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

                    var task = new ScheduledTask(){
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
                                unsentEmails.Add(
                                    MailModel.To(manager.GetEmail())
                                    .Subject(EmailStrings.Prereview_Subject,caller.Organization.GetName())
                                    .Body(EmailStrings.Prereview_Body, manager.GetName(), dueDate.ToShortDateString(), ProductStrings.BaseUrl + "n/" + guid, ProductStrings.BaseUrl + "n/" + guid, ProductStrings.ProductName)
                                    );
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
            await Emailer.SendEmails(unsentEmails);

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
                        .Where(x => x.PrereviewId == prereviewId && x.DeleteTime == null)
                        .List()
                        .Select(x => Tuple.Create(x.FirstUserId, x.SecondUserId))
                        .ToList();
                }
            }
        }

        public List<Tuple<long, long>> GetAllMatchesForReview(UserOrganizationModel caller, long reviewContainerId,List<Tuple<long, long>> defaultModel)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms=PermissionsUtility.Create(s, caller).ViewReviews(reviewContainerId);
                    var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
                    
                    var prereviews = s.QueryOver<PrereviewModel>().Where(x => x.ReviewContainerId == reviewContainerId && x.DeleteTime==null).List().ToList();
                    
                    var all = new List<Tuple<long, long>>();

                    foreach (var prereview in prereviews)
                    {
                        if (prereview.Started)
                        {
                            all.AddRange(s.QueryOver<PrereviewMatchModel>().Where(x => x.PrereviewId == prereview.Id && x.DeleteTime == null).List().Select(x => Tuple.Create(x.FirstUserId, x.SecondUserId)).ToList());
                        }
                        else
                        {
                            if (reviewContainer.EnsureDefault)
                            {
                                var subordinates = UserAccessor.GetDirectSubordinates(s.ToQueryProvider(true),perms,caller,prereview.ManagerId).Select(x=>x.Id).Union(prereview.ManagerId.AsList());
                                var managerCustomizeDefault=subordinates.SelectMany(sub => defaultModel.Where(x=>x.Item1==sub)).ToList();
                                all.AddRange(managerCustomizeDefault);
                            }
                        }
                    }
                    return all.Distinct().ToList();
                }
            }
        }

        public static List<PrereviewModel> GetPrereviewsForUser(AbstractQuery s, PermissionsUtility perms, long userOrgId,DateTime dueAfter)
        {
            perms.ViewUserOrganization(userOrgId, false);
            return s.Where<PrereviewModel>(x => x.ManagerId == userOrgId && x.DeleteTime == null && x.PrereviewDue > dueAfter).ToList();
        }

        public void UnsafeExecuteAllPrereviews(ISession s, long reviewContainerId, DateTime now)
        {
            var prereviews = s.QueryOver<PrereviewModel>().Where(x => x.ReviewContainerId == reviewContainerId && x.DeleteTime == null).List().ToList();
            foreach (var prereview in prereviews)
            {
                prereview.Executed = now;
                s.Update(prereview);
            }
        }
    }
}