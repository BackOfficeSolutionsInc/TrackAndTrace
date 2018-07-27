using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Askables;
using RadialReview.Models.Charts;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static RadialReview.Models.Charts.Line;
using static RadialReview.Utilities.DatePointAnalyzerUtility;

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
		protected static MetricGraphic GenerateBurndown(string name, List<EventTimes> times, string legendTitle = null) {
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
			mg.AddTimeseries(new MetricGraphicTimeseries(points, legendTitle));
			return mg;
		}


		public static List<DatePoint> GenerateDateDataFromEvents(List<EventTimes> times) {
			var adder = new List<Tuple<DateTime,int>>();
			foreach (var evt in times) {
				adder.Add(Tuple.Create(evt.CreateTime, +1));
				if (evt.DeleteTime != null && evt.CompleteTime != null) {
					adder.Add(Tuple.Create(Math2.Min(evt.DeleteTime.Value, evt.CompleteTime.Value), -1));
				} else if (evt.DeleteTime != null) {
					adder.Add(Tuple.Create(evt.DeleteTime.Value, -1));
				} else if (evt.CompleteTime != null) {
					adder.Add(Tuple.Create(evt.CompleteTime.Value, -1));
				}
			}
			var count = 0;
			var result = new List<DatePoint>();
			foreach (var a in adder.OrderBy(x => x.Item1)) {
				count += a.Item2;
				result.Add(new DatePoint(a.Item1,count));
			}
			return result;
		}

		public class AdminOrgStats {
			public AccountType AccountType { get; set; }
			public long OrgId { get; set; }		
			public string OrgName { get; set; }	
			public WindowAnalysis Registrations { get; set; }
		}

		public static List<AdminOrgStats> GetSuperAdminStatistics_Unsafe(DateTime start,DateTime? end = null) {
			end = end ?? DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					UserModel userAlias = null;
					OrganizationModel orgAlias = null;
					LocalizedStringModel orgNameAlias = null;
					var data = s.QueryOver<UserOrganizationModel>()
									.Left.JoinAlias(x => x.User, () => userAlias)
									.Left.JoinAlias(x => x.Organization, () => orgAlias)
									.Left.JoinAlias(x => orgAlias.Name, () => orgNameAlias)
									.Where(x => orgAlias.DeleteTime == null)
									.Select(x => x.AttachTime, x => x.DeleteTime, x => x.CreateTime, x => userAlias.CreateTime, x => x.Id, x => x.Organization.Id, x=> orgNameAlias.Standard,x=> orgAlias.AccountType)
									.List<object[]>()
									.Select(x => new {
										attachTime = (DateTime)x[0],
										deleteTime = (DateTime?)x[1],
										createTime = (DateTime)x[2],
										registrationTime = (DateTime?)x[3],
										userId = (long)x[4],
										orgId = (long)x[5],
										orgName = (string)x[6],
										accType = (AccountType)x[7]
									}).ToList();
					


					var registrationByOrg = data.Where(x => x.registrationTime != null).GroupBy(x => x.orgId);
					var result = new List<AdminOrgStats>();
					foreach (var orgReg in registrationByOrg) {
						var eventTimes = orgReg.Select(x => new EventTimes(x.registrationTime, x.deleteTime, x.deleteTime)).ToList();
						var orgData = GenerateDateDataFromEvents(eventTimes);
						result.Add(new AdminOrgStats() {
							OrgId = orgReg.Key,
							OrgName = orgReg.First().orgName,
							AccountType = orgReg.First().accType,
							Registrations = DatePointAnalyzerUtility.AnalyzeWindow(orgData, start, end.Value)
						});						
					}

					return result;
				}
			}

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

			var userroleids = UserAccessor.GetUserRolesAtOrganization(s, perms, orgId).Where(x => x.RoleType == Models.UserModels.UserRoleType.PlaceholderOnly)
				.Select(x => x.UserId).ToList();			

			UserModel userAlias = null;
			var data = s.QueryOver<UserOrganizationModel>()
							.Left.JoinAlias(x => x.User, () => userAlias)
							.Where(x => x.Organization.Id == orgId)
							.Select(x => x.AttachTime, x => x.DeleteTime, x => x.DeleteTime, x => x.CreateTime, x => userAlias.CreateTime, x => x.Id)
							.List<object[]>().Where(x => !userroleids.Contains((long)x[5]))
							.ToList();

			var ac = s.QueryOver<AccountabilityNode>()
				.Where(x => x.OrganizationId == orgId)
				.Where(x => x.DeleteTime == null || x.DeleteTime > new DateTime(2016, 8, 20))
				.Select(x => x.CreateTime, x => x.DeleteTime, x => x.DeleteTime)
				.List<object[]>()
				.SelectNoException(x => new EventTimes(x[0], x[1], x[2]))
				.ToList();

			var attach = data.Where(x => x[4] != null).Select(x => new EventTimes(x[4], x[1], x[2])).ToList();
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