﻿using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class SchedulerController : BaseController
    {
        //
        // GET: /Scheduler/
        [Access(AccessLevel.Any)]
        public bool Index()
        {
            return true;
        }

        [Access(AccessLevel.Any)]
        public async Task<bool> Reschedule()
        {
            var tasks =_TaskAccessor.GetTasksToExecute(DateTime.UtcNow);
            await Task.WhenAll(tasks.Select( task =>
            {
                try
                {
                   return _TaskAccessor.ExecuteTask(ServerUtility.GetConfigValue("BaseUrl"), task);
                }
                catch (Exception e)
                {
                    log.Error("Task execution exception.", e);
                    return null;
                }
            }).Where(x=>x!=null));
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    foreach (var task in tasks)
                    {
                        s.Update(task);
                    }
                    tx.Commit();
                    s.Flush();
                }
            }

            return ServerUtility.RegisterCacheEntry();
        }
	}
}