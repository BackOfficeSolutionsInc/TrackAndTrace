using RadialReview.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Routing;

namespace RadialReview.Utilities
{
    public class ServerUtility
    {
        private static String DummyCacheKey = "DummyCacheKey";
        private static TaskAccessor _TaskAccessor = new TaskAccessor();


        public static bool RegisterCacheEntry()
        {

            if (null != HttpContext.Current.Cache[DummyCacheKey]) return false;

            HttpContext.Current.Cache.Add(DummyCacheKey, "Test", null,
                DateTime.MaxValue, TimeSpan.FromMinutes(1),
                CacheItemPriority.Normal,
                new CacheItemRemovedCallback(CacheItemRemovedCallback));

            return true;
        }

        public static async void CacheItemRemovedCallback(string key, object value, CacheItemRemovedReason reason)
        {
            //reschedule
            await new Task(()=>{
                var complete=false;
                while (!complete)
                {
                    try
                    {
                        WebClient client = new WebClient();
                        client.DownloadData(GetConfigValue("BaseUrl") + "/Scheduler/Reschedule");
                        complete = true;
                    }
                    catch (Exception e)
                    {
                        Thread.Sleep(6000);                        
                    }
                }
            });

            await ExecuteAllTasks();
        }

        public static async Task ExecuteAllTasks()
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(_TaskAccessor.GetTasksToExecute(DateTime.UtcNow), task =>
                {
                    //var server=new HttpServerUtility( );
                    _TaskAccessor.ExecuteTask(GetConfigValue("BaseUrl"), task.Id);
                });
            });
        }

        public static String GetConfigValue(string key)
        {
            return WebConfigurationManager.AppSettings[key].ToString();
        }
    }
}