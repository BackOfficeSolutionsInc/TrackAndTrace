﻿using RadialReview.Accessors;
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

        public static async void Reschedule()
        {
            while (true)
            {
                try
                {
                    WebClient client = new WebClient();
					var output = await client.DownloadDataTaskAsync(Config.BaseUrl(null) + "/Scheduler/Reschedule");
                    break;
                }
                catch (Exception)
                {
                    
                }
                Thread.Sleep(15000);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async void CacheItemRemovedCallback(string key, object value, CacheItemRemovedReason reason)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            Reschedule();
        }
        /*
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
        }*/

       /* public static String GetConfigValue(string key)
        {
            return WebConfigurationManager.AppSettings[key].ToString();
        }*/
    }
}