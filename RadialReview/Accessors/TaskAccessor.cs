using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Prereview;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Tasks;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
                    var output = AddTask(s.ToUpdateProvider(), task);
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
                    return s.QueryOver<ScheduledTask>().Where(x => x.Executed == null && x.Started==null && now.AddMinutes(2) > x.Fire && x.DeleteTime == null && x.ExceptionCount <= 11).List().ToList();
                }
            }
        }

        public void MarkStarted(List<ScheduledTask> tasks, DateTime? date)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    foreach (var t in tasks)
                    {
                        t.Started = date;
                        s.Update(t);
                    }
                    tx.Commit();
                    s.Flush();
                }
            }

        }

        /*
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
        }*/

        public async Task ExecuteTask(String server, ScheduledTask task)
        {
            if (task != null)
            {
	            try{
		            if (task.Url != null){
			            var webClient = new WebClient();
			            var str = await webClient.DownloadStringTaskAsync(new Uri((server.TrimEnd('/') + "/" + task.Url.TrimStart('/')), UriKind.Absolute));
		            }
		            log.Debug("Scheduled task was executed. " + task.Id);
                    task.Executed = DateTime.UtcNow;
                }
                catch (Exception e)
                {
                    log.Error("Scheduled task error. " + task.Id, e);
                    task.ExceptionCount++;
                    task.Fire = DateTime.UtcNow + TimeSpan.FromMinutes(Math.Pow(2, task.ExceptionCount + 1));
                }
            }
        }

        public int GetUnstartedTaskCountForUser(UserOrganizationModel caller, long forUserId, DateTime now)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction()){
	                var profileImage = 0;
	                try{
		                profileImage = String.IsNullOrEmpty(s.Get<UserOrganizationModel>(forUserId).User.ImageGuid) ? 1 : 0;
	                }catch{
		                
	                }


					var reviewCount = s.QueryOver<ReviewModel>().Where(x => x.ForUserId == forUserId && x.DueDate > now && !x.Complete && x.DeleteTime == null).Select(Projections.RowCount()).FutureValue<int>();
					var prereviewCount = s.QueryOver<PrereviewModel>().Where(x => x.ManagerId == forUserId && x.PrereviewDue > now && !x.Started && x.DeleteTime == null).Select(Projections.RowCount()).FutureValue<int>();
	                var nowPlus = now.Add(TimeSpan.FromDays(1));

					var scorecardCount = s.QueryOver<ScoreModel>().Where(x => x.AccountableUserId == forUserId && x.DateDue < nowPlus && x.DateEntered == null).Select(Projections.RowCount()).FutureValue<int>();
					return reviewCount.Value + prereviewCount.Value + scorecardCount.Value + profileImage;
                }
            }
        }

		
		//public List<ScoreModel> GetScoreTasks

        public List<TaskModel> GetTasksForUser(UserOrganizationModel caller, long forUserId,DateTime now)
        {
            var tasks = new List<TaskModel>();
            using (var s = HibernateSession.GetCurrentSession()) 
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    
                    //Reviews
                    var reviews = ReviewAccessor
						.GetReviewsForUser(s, perms, caller, forUserId, 0, int.MaxValue,now)
						.ToListAlive()
						.GroupBy(x=>x.ForReviewsId);

                    var reviewTasks = reviews.Select(x => new TaskModel(){
	                    Id = x.First().ForReviewsId,
						Type = TaskType.Review,
						Completion = CompletionModel.FromList(x.Select(y=>y.GetCompletion())),
						DueDate = x.Max(y=>y.DueDate),
						Name = x.First().Name
                    });
                    tasks.AddRange(reviewTasks);

                    //Prereviews
                    var prereviews = PrereviewAccessor.GetPrereviewsForUser(s.ToQueryProvider(true), perms, forUserId,now)
						.Where(x => x.Executed == null).ToListAlive();
                    var reviewContainers = new Dictionary<long, String>();
                    var prereviewCount = new Dictionary<long, int>();
                    foreach (var p in prereviews)
                    {
                        reviewContainers[p.ReviewContainerId] = ReviewAccessor.GetReviewContainer(s.ToQueryProvider(true), perms, p.ReviewContainerId).ReviewName;
                        prereviewCount[p.Id] = s.QueryOver<PrereviewMatchModel>()
							.Where(x => x.PrereviewId == p.Id && x.DeleteTime == null)
							.RowCount();
                    }
                    var prereviewTasks = prereviews.Select(x => new TaskModel(){
	                    Id = x.Id,
						Type = TaskType.Prereview,
						Count = prereviewCount[x.Id],
						DueDate = x.PrereviewDue,
						Name = reviewContainers[x.ReviewContainerId]
                    });
                    tasks.AddRange(prereviewTasks);

					//Scorecard
					var scores = s.QueryOver<ScoreModel>()
						.Where(x => x.AccountableUserId == forUserId && x.DateDue < now.AddDays(1) && x.DateEntered == null)
						.List().ToList();

	                var scoreTasks=scores.GroupBy(x=>x.DateDue.Date).Select(x=>new TaskModel(){
		                Count = x.Count(),
						DueDate = x.First().DateDue,
						Name = "Enter Scorecard Metrics",
						Type = TaskType.Scorecard
	                });
					tasks.AddRange(scoreTasks);

	                try{
		                if (String.IsNullOrEmpty(s.Get<UserOrganizationModel>(forUserId).User.ImageGuid)){
			                tasks.Add(new TaskModel(){
				                Type = TaskType.Profile,
				                Name = "Update Profile (Picture)",
				                DueDate = DateTime.MaxValue,
			                });
		                }
	                }
	                catch{
		                
	                }


	                /*

					  .Where(x => x.Executed == null).ToListAlive();

					foreach (var p in prereviews)
					{
						reviewContainers[p.ReviewContainerId] = ReviewAccessor.GetReviewContainer(s.ToQueryProvider(true), perms, p.ReviewContainerId).ReviewName;
						prereviewCount[p.Id] = s.QueryOver<PrereviewMatchModel>()
							.Where(x => x.PrereviewId == p.Id && x.DeleteTime == null)
							.RowCount();
					}
					var prereviewTasks = prereviews.Select(x => new TaskModel()
					{
						Id = x.Id,
						Type = TaskType.Prereview,
						Count = prereviewCount[x.Id],
						DueDate = x.PrereviewDue,
						Name = reviewContainers[x.ReviewContainerId]
					});*/

                }
            }
            return tasks;
        }


		public void UpdateScorecard(DateTime now)
		{
			using(var s = HibernateSession.GetCurrentSession())
			{
				using(var tx=s.BeginTransaction()){
					var measurables = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null && x.NextGeneration <= now).List().ToList();

					//var weekLookup = new Dictionary<long, DayOfWeek>();

					//Next Thursday
					foreach (var m in measurables){

						//var startOfWeek =weekLookup.GetOrAddDefault(m.OrganizationId, x => m.Organization.Settings.WeekStart);

						var nextDue = m.NextGeneration.StartOfWeek(DayOfWeek.Sunday).AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);

						var score = new ScoreModel(){
							AccountableUserId = m.AccountableUserId,
							DateDue = nextDue,
							MeasurableId = m.Id,
							OrganizationId = m.OrganizationId,
							ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday)
						};
						s.Save(score);
						m.NextGeneration = nextDue;
						s.Update(m);
					}
					tx.Commit();
					s.Flush(); 
				}
			}
		}
	}
}