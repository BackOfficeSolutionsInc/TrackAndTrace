using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Prereview;
using RadialReview.Models.Tasks;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;

namespace RadialReview.Accessors
{
    public class TaskAccessor : BaseAccessor
    {
        public static long AddTask(AbstractUpdate update, ScheduledTask task)
        {
            update.Save(task);
            return task.Id;
        }

        public long AddTask(ScheduledTask task)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var output=AddTask(s.ToUpdateProvider(), task);
                    tx.Commit();
                    s.Flush();
                    return output;
                }
            }
        }

        public List<ScheduledTask> GetTasksToExecute(DateTime now)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var all = s.QueryOver<ScheduledTask>().List().ToList();
                    return s.QueryOver<ScheduledTask>().Where(x => x.Executed == null && now.AddMinutes(2) > x.Fire && x.DeleteTime == null && x.ExceptionCount <= 11).List().ToList();
                }
            }
        }

        private void DownloadComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var task = s.Get<ScheduledTask>((long)e.UserState);

                    if (e.Error != null)
                    {
                        log.Error("Scheduled task error. " + task.Id, e.Error);
                        task.ExceptionCount++;
                        task.Fire = task.Fire + TimeSpan.FromMinutes(Math.Pow(2, task.ExceptionCount + 1));
                    }
                    else if (e.Cancelled)
                    {
                        log.Debug("Scheduled task was canceled. " + task.Id);
                    }
                    else
                    {
                        log.Debug("Scheduled task was executed. " + task.Id);
                        task.Executed = DateTime.UtcNow;
                    }
                    s.Update(task);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void ExecuteTask(String server, long taskId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var task = s.Get<ScheduledTask>(taskId);
                    if (task != null)
                    {
                        try
                        {
                            var webClient = new WebClient();
                            webClient.DownloadStringCompleted += DownloadComplete;
                            webClient.DownloadStringAsync(new Uri((server.TrimEnd('/') + "/" + task.Url.TrimStart('/')), UriKind.Absolute), taskId);
                        }
                        catch (Exception e)
                        {
                            log.Error("Scheduled task failed. " + task.Id, e);
                            task.ExceptionCount += 1;
                        }
                        s.Update(task);
                        tx.Commit();
                        s.Flush();
                    }

                }
            }
        }

        public int GetUnstartedTaskCountForUser(UserOrganizationModel caller, long forUserId,DateTime now)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var reviewCount = s.QueryOver<ReviewModel>().Where(x => x.ForUserId == forUserId && x.DueDate > now && !x.Complete).RowCount();
                    var prereviewCount = s.QueryOver<PrereviewModel>().Where(x => x.ManagerId== forUserId && x.PrereviewDue > now && !x.Started).RowCount();
                    return reviewCount + prereviewCount;
                }
            }
        }

        public List<TaskModel> GetTasksForUser(UserOrganizationModel caller, long forUserId)
        {
            var tasks = new List<TaskModel>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);

                    //Reviews
                    var reviews = ReviewAccessor.GetReviewsForUser(s, perms, caller, forUserId, 0, int.MaxValue);
                    var reviewTasks = reviews.Select(x => new TaskModel() { Id=x.Id, Type=TaskType.Review, Completion = x.GetCompletion(), DueDate = x.DueDate, Name = x.Name });
                    tasks.AddRange(reviewTasks);

                    //Prereviews
                    var prereviews = PrereviewAccessor.GetPrereviewsForUser(s.ToQueryProvider(true), perms, forUserId);
                    var reviewContainers = new Dictionary<long, String>();
                    var prereviewCount = new Dictionary<long, int>();
                    foreach(var p in prereviews){
                        reviewContainers[p.ReviewContainerId] = ReviewAccessor.GetReviewContainer(s.ToQueryProvider(true),perms,p.ReviewContainerId).ReviewName;
                        prereviewCount[p.Id] = s.QueryOver<PrereviewMatchModel>().Where(x => x.PrereviewId == p.Id && x.DeleteTime == null).RowCount();
                    }
                    var prereviewTasks = prereviews.Select(x => new TaskModel() { Id = x.Id, Type = TaskType.Prereview, Count = prereviewCount[x.Id], DueDate = x.PrereviewDue, Name = reviewContainers[x.ReviewContainerId] });
                    tasks.AddRange(prereviewTasks);

                }
            }
            return tasks;
        }

    }
}