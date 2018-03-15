using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Askables;
using RadialReview.Models.Charts;
using RadialReview.Models.Issues;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static RadialReview.Models.Charts.Line;

namespace RadialReview.Accessors {
    public class StatsAccessor {
        public class EventTimes {
            public DateTime CreateTime { get; set; }
            public DateTime? DeleteTime { get; set; }
            public DateTime? CompleteTime { get; set; }

            public EventTimes(object createTime, object deleteTime, object completeTime) {
                CreateTime = (DateTime)createTime;
                DeleteTime = (DateTime?)deleteTime;
                CompleteTime = (DateTime?)completeTime;
            }
        }
        protected static MetricGraphic GenerateBurndown(string name, List<EventTimes> times,string legendTitle=null) {
            var allDates = times.SelectMany(x => new[] { x.CompleteTime, x.DeleteTime, x.CreateTime })
                                .Where(x => x != null && x.Value > DateTime.MinValue && x.Value < DateTime.MaxValue)
                                .Select(x => x.Value)
                                .ToList();
            allDates.Add(DateTime.UtcNow);
            var min = allDates.Min().AddDays(-7);
            var max = allDates.Max().AddDays(7);

            var points = new List<MetricGraphic.DateData>();

            var i = min.StartOfWeek(DayOfWeek.Sunday);
            while (i <= max) {
                var timeslice = times.Where(x => x.CreateTime <= i && (i < x.DeleteTime || x.DeleteTime == null));
                var count = timeslice.Count(x => x.CompleteTime == null || x.CompleteTime > i);
                points.Add(new MetricGraphic.DateData() {
                    date = i,
                    value = count,
                });
                i = i.AddDays(7);
            }

            {
                i = DateTime.UtcNow.Date;
                var timeslice = times.Where(x => x.CreateTime <= i && (i < x.DeleteTime || x.DeleteTime == null));
                var count = timeslice.Count(x => x.CompleteTime == null || x.CompleteTime > i);
                try {
                    var a = timeslice.Max(x => x.CompleteTime);
                } catch { }
                points.Add(new MetricGraphic.DateData() {
                    date = i,
                    value = count,
                });
            }

            var mg = new MetricGraphic(name, null);
            mg.AddTimeseries(new MetricGraphicTimeseries(points,legendTitle));
            return mg;
        }

        public static MetricGraphic GetOrganizationRockCompletionBurndown(UserOrganizationModel caller, long orgId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewOrganization(orgId);

                    var rockData = s.QueryOver<RockModel>()
                        .Where(x => x.OrganizationId == orgId)
                        .Select(x => x.CreateTime, x => x.DeleteTime, x => x.CompleteTime)
                        .List<object[]>()
                        .SelectNoException(x => new EventTimes(x[0], x[1], x[2]))
                        .ToList();
                    return GenerateBurndown("Outstanding Rock", rockData);
                }
            }
        }

        public static MetricGraphic GetOrganizationIssueBurndown(UserOrganizationModel caller, long orgId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewOrganization(orgId);

                    IssueModel alias = null;
                    var data = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                        .JoinAlias(x => x.Issue, () => alias)
                        .Where(x => alias.OrganizationId == orgId)
                        .Select(x => x.CreateTime, x => x.DeleteTime, x => x.CloseTime)
                        .List<object[]>()
                        .SelectNoException(x => new EventTimes(x[0], x[1], x[2]))
                        .ToList();

                    return GenerateBurndown("Outstanding Issues", data);
                }
            }
        }

        public static MetricGraphic GetOrganizationTodoBurndown(UserOrganizationModel caller, long orgId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewOrganization(orgId);

                    var data = s.QueryOver<TodoModel>()
                        .Where(x => x.OrganizationId == orgId)
                        .Select(x => x.CreateTime, x => x.DeleteTime, x => x.CompleteTime)
                        .List<object[]>()
                        .SelectNoException(x => new EventTimes(x[0], x[1], x[2]))
                        .ToList();

                    return GenerateBurndown("Outstanding To-dos", data);
                }
            }
        }
        public static MetricGraphic GetOrganizationMemberBurndown(UserOrganizationModel caller, long orgId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetOrganizationMemberBurndown(s, perms, orgId);
                }
            }
        }

        public static MetricGraphic GetOrganizationMemberBurndown(ISession s, PermissionsUtility perms, long orgId) {
            perms.ViewOrganization(orgId);

            var data = s.QueryOver<UserOrganizationModel>()
                .Where(x => x.Organization.Id == orgId)
                .Select(x => x.AttachTime, x => x.DeleteTime, x => x.DeleteTime, x => x.CreateTime)
                .List<object[]>()
                .ToList();

            var ac = s.QueryOver<AccountabilityNode>()
                .Where(x => x.OrganizationId == orgId)
                .Where(x=>x.DeleteTime==null || x.DeleteTime > new DateTime(2016,8,20))
                .Select(x => x.CreateTime, x => x.DeleteTime, x => x.DeleteTime)
                .List<object[]>()
                .SelectNoException(x => new EventTimes(x[0], x[1], x[2]))
                .ToList();

            var attach = data.Select(x => new EventTimes(x[0], x[1], x[2])).ToList();
            var create = data.Select(x => new EventTimes(x[3], x[1], x[2])).ToList();

            var b1 = GenerateBurndown("Employees", attach, "Registered");
            var b2 = GenerateBurndown("", create, "Accounts");
            var b3 = GenerateBurndown("", ac, "Seats");

            var bd = new MetricGraphic("Employees");
            foreach (var i in b1.GetTimeseries().Union(b2.GetTimeseries().Union(b3.GetTimeseries())))
                bd.AddTimeseries(i);

            bd.aggregate_rollover = true;
            return bd;
        }
    }
}