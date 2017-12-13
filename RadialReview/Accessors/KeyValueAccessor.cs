﻿using RadialReview.Models.Application;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class KeyValueAccessor : BaseAccessor
    {

		[Obsolete("Use Variable instead")]
        public static List<KeyValueModel> Get(String key)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    return s.QueryOver<KeyValueModel>().Where(x => x.K == key).List().ToList();
                }
            }
        }
		[Obsolete("Use Variable instead")]
		public static long Put(String key, String value)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var kv = new KeyValueModel() { K = key, V = value };
                    s.Save(kv);
                    tx.Commit();
                    s.Flush();
                    return kv.Id;
                }
            }
        }
		[Obsolete("Use Variable instead")]
		public static void Remove(long id)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var obj = s.Get<KeyValueModel>(id);
                        s.Delete(obj);
                        tx.Commit();
                        s.Flush();
                    }
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }

    }
}