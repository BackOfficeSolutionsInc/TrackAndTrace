using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.ClientSuccess;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Accessors {

    public enum SupportStatus {
        Open = 0,
        Backlog = 1,
        WillNotFix = 2,
        Closed = 100,

        JavascriptError = 50,
    }
    public class SupportData {

        public virtual SupportStatus Status { get; set; }
        public virtual long Id { get; set; }
        public virtual string Lookup { get; set; }
        public virtual string Email { get; set; }
        public virtual string Notes { get; set; }
        [AllowHtml]
        public virtual string Subject { get; set; }
        [AllowHtml]
        public virtual string Body { get; set; }
        [AllowHtml]
        public virtual string Console { get; set; }
        public virtual long Org { get; set; }
        public virtual long User { get; set; }
        public virtual string Url { get; set; }
        public virtual string PageTitle { get; set; }
        public virtual string ImageData { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? EmailViewed { get; set; }
        public virtual DateTime? CloseTime { get; set; }
        public virtual DateTime? LastViewed { get; set; }
        public virtual List<Tracker> _Listing { get; set; }
        public virtual string UserAgent { get; set; }

        //public virtual string Type { get; set; }
        public SupportData() {
            CreateTime = DateTime.UtcNow;
            Lookup = Guid.NewGuid().ToString();
        }
        public class Map : ClassMap<SupportData> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.Lookup).Index("supportdata_lookup_idx");
                Map(x => x.Email);
                Map(x => x.Subject);
                Map(x => x.Body);
                Map(x => x.Console).Length(100000);
                Map(x => x.ImageData).Length(200000);
                Map(x => x.Org);
                Map(x => x.User);
                Map(x => x.Url);
                Map(x => x.PageTitle);
                //Map(x => x.Type);
                Map(x => x.CreateTime);
                Map(x => x.CloseTime);
                Map(x => x.LastViewed);
                Map(x => x.Status).CustomType<SupportStatus>();
                Map(x => x.EmailViewed);
                Map(x => x.UserAgent);
            }
        }

    }
    public class SupportAccessor {
        public static void Add(SupportData data) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    s.Save(data);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static void SetStatus(string guid, SupportStatus status) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var g = Get(s, guid);
                    if (g != null) {
                        g = s.Get<SupportData>(g.Id);
                        g.Status = status;

                        if (status == SupportStatus.Closed)
                            g.CloseTime = DateTime.UtcNow;
                        else
                            g.CloseTime = null;

                        s.Update(g);

                        tx.Commit();
                        s.Flush();
                    }
                }
            }

        }

        //public static SupportData MarkSeen(UserOrganizationModel user, string guid,bool inEmail)
        //{
        //    using (var s = HibernateSession.GetCurrentSession()) {
        //        using (var tx = s.BeginTransaction()) {
        //            var f = Get(s, guid);
        //            if (f != null) {
        //                s.Save(new SupportViewedBy() {
        //                    SupportDataId = f.Id,
        //                    ViewedBy = user.Id,
        //                    InEmail = inEmail
        //                });
        //                tx.Commit();
        //                s.Flush();
        //            }
        //            return f;
        //        }
        //    }
        //}

        public static SupportData Get(string guid) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return Get(s, guid);
                }
            }
        }
        public static List<SupportData> List(bool open, bool closed, bool backlog, bool noFix, bool jsException) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    Junction d = Restrictions.Disjunction();
                    if (open)
                        d = d.Add<SupportData>(x => x.Status == SupportStatus.Open);
                    if (closed)
                        d = d.Add<SupportData>(x => x.Status == SupportStatus.Closed);
                    if (backlog)
                        d = d.Add<SupportData>(x => x.Status == SupportStatus.Backlog);
                    if (noFix)
                        d = d.Add<SupportData>(x => x.Status == SupportStatus.WillNotFix);
                    if (jsException)
                        d = d.Add<SupportData>(x => x.Status == SupportStatus.JavascriptError);

                    return s.QueryOver<SupportData>().And(d).List().ToList();

                }
            }
        }

        public static SupportData Get(ISession s, string guid) {
            var found = s.QueryOver<SupportData>().Where(x => x.Lookup == guid).List().FirstOrDefault();
            if (found != null) {
                found._Listing = s.QueryOver<Tracker>().Where(x => x.ResGuid == guid).List().ToList();
            }
            return found;
        }

        public static void MarkTooltipSeen(UserOrganizationModel caller, long tooltipId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    if (caller.User == null || string.IsNullOrWhiteSpace(caller.User.Id))
                        return;

                    s.Save(new TooltipSeen() {
                        TipId = tooltipId,
                        UserId = caller.User.Id
                    });

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static List<TooltipViewModel> GetTooltips(string userId, string path) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var now = DateTime.UtcNow;
                    //s.QueryOver<TooltipTemplate>()
                    //    .Where(x => (x.DeleteTime == null || x.DeleteTime > now) && x.Enabled == true)
                    //    .WhereRestrictionOn(x => x.UrlSelector)
                    //    .IsInsensitiveLike();
                    //var disjunction = new Disjunction();
                    var user = s.Get<UserModel>(userId);
                    if (user == null || user.DisableTips)
                        return new List<TooltipViewModel>();

                    var seen = s.QueryOver<TooltipSeen>().Where(x => x.UserId == userId).Select(x => x.TipId).List<long>().ToList();

                    var tooltips = s.CreateQuery("from TooltipTemplate t where :path like t.UrlSelector")
                        .SetParameter("path",path)                        
                        .List<TooltipTemplate>();

                    tooltips = tooltips
                        .Where(x => (x.DeleteTime == null || x.DeleteTime > now) && x.IsEnabled == true)
                        .Where(x=>!seen.Contains(x.Id))
                        .ToList();

                    return tooltips.Select(x => new TooltipViewModel(x)).ToList();
                }
            }
        }
    }
}