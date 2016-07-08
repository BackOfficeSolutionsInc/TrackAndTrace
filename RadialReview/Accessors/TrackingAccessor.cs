using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
    public class TrackingAccessor {

        public static void MarkSeen(string guid,UserOrganizationModel user=null, Tracker.TrackerSource source = Tracker.TrackerSource.Email)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    MarkSeen(s, guid, user, source);
                    tx.Commit();
                    s.Flush();
                }
            }
        }
        public static void MarkSeen(ISession s, string guid, UserOrganizationModel user = null,Tracker.TrackerSource source=Tracker.TrackerSource.Email)
        {
            if (!string.IsNullOrWhiteSpace(guid)) {
                s.Save(new Tracker() {
                    ViewedBy = user.NotNull(x => x.Id),
                    ResGuid = guid,
                    Source=source
                });
            }
        }


        public static List<Tracker> GetTracked(ISession s, string guid)
        {
            return s.QueryOver<Tracker>().Where(x => x.ResGuid == guid).List().ToList();
        }
    }
}