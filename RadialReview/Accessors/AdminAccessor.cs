using Hangfire;
using RadialReview.Crosscutting.Flags;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Admin;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Payments;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static RadialReview.Models.OrganizationModel;

namespace RadialReview.Accessors {

	[Obsolete("Not safe and expensive")]
	public class AdminAccessor {

		public class AllUserEmail {
			public String UserName { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public String UserEmail { get; set; }
			public String OrgName { get; set; }
			public long UserId { get; set; }
			public long OrgId { get; set; }
			public DateTime UserCreateTime { get; set; }
			public DateTime? UserDeleteTime { get; set; }
			public string AccountType { get; set; }
			public DateTime? OrgCreateTime { get; set; }
			public DateTime? LastLogin { get; set; }
			public bool IsAdmin { get; set; }
			public bool IsManager { get; set; }
			public DateTime? OrgDeleteTime { get; set; }
			public DateTime? TrialExpire { get; internal set; }
			public bool EvalEnabled { get; internal set; }
			public bool PeopleEnabled { get; internal set; }
			public bool L10Enabled { get; internal set; }
			//public bool Blacklist { get; set; }
		}

		public class LatestExport {
			public long Id { get; set; }
			public DateTime GeneratedAt { get; set; }
			public TimeSpan GenerationDuration { get; set; }
			public Csv Data { get; set; }
		}

		private static long ExportId = 1;
		private static FixedSizedQueue<LatestExport> AllUserDataExports = new FixedSizedQueue<LatestExport>(4);


		[Obsolete("Not safe and expensive")]
		public static FixedSizedQueue<LatestExport> GetExportList() {
			return AllUserDataExports;
		}


		[Obsolete("Not safe and expensive")]
		public static void ClearExports() {
			AllUserDataExports.Empty();
		}

		[Obsolete("Not safe and expensive")]
		[Queue(HangfireQueues.Immediate.ALPHA)]/*Queues must be lowecase alphanumeric. You must add queues to BackgroundJobServerOptions in Startup.auth.cs*/
		[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
		public static Csv GenerateAllUserData_Admin_Unsafe(DateTime now) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					UserOrganizationModel userAlias = null;
					PaymentPlanModel paymentPlanAlias = null;
					OrganizationSettings settingsAlias = null;

					var allUsersF = s.QueryOver<UserLookup>().Where(x => x.HasJoined).Future();

					var allOrgsF = s.QueryOver<OrganizationModel>()
										.JoinAlias(x => x.PaymentPlan, () => paymentPlanAlias)
										//.JoinAlias(x => x._Settings, () => settingsAlias)
										.Select(x => x.Id, x => x.Name.Id, x => x.DeleteTime, x => x.CreationTime, x => x.AccountType, x => paymentPlanAlias.FreeUntil,
												x => x._Settings.EnableL10, x => x._Settings.EnablePeople, x => x._Settings.EnableReview)
										.Future<object[]>();
					var localizedStringF = s.QueryOver<LocalizedStringModel>().Select(x => x.Id, x => x.Standard).Future<object[]>();



					UserModel uAlias = null;
					TempUserModel tempUserAlias = null;
					OrganizationModel orgAlias = null;
					var allDeletedQ = s.QueryOver<UserOrganizationModel>()
						//.Left.JoinAlias(x => x.User, () => uAlias)
						//.Left.JoinAlias(x => x.TempUser, () => tempUserAlias)
						.Left.JoinAlias(x => x.Organization, () => orgAlias)
						.Where(x => x.DeleteTime != null || orgAlias.DeleteTime != null)
							.Select(x => x.Id, x => x.DeleteTime,/*x => uAlias.UserName, x => tempUserAlias.Email,*/ x => orgAlias.DeleteTime)
						.Future<object[]>()
						.Select(x => new {
							Id = (long)x[0],
							DeleteTime = ((DateTime?)x[1] ?? (DateTime?)x[2]),
							//Email = ((string)x[2]) ?? ((string)x[3]),
						});


					var meetingsByCompanyF = s.QueryOver<L10Meeting>()
						.Where(x => x.CompleteTime != null && x.Preview == false)
						.Select(x => x.OrganizationId, x => x.StartTime, x => x.CompleteTime)
						.Future<object[]>()
						.Select(x => new {
							OrgId = (long)x[0],
							StartTime = (DateTime?)x[1],
							CompleteTime = (DateTime?)x[2],
						});

					var paymentTokens = s.QueryOver<PaymentSpringsToken>()
						.Where(x => x.Active == true && x.DeleteTime == null)
						.Select(x => x.OrganizationId, x => x.MonthExpire, x => x.YearExpire, x => x.TokenType)
						.Future<object[]>()
						.Select(x => new {
							OrgId = (long)x[0],
							MonthExpire = (int)x[1],
							YearExpire = (int)x[2],
							TokenType = x[3] == null ? PaymentSpringTokenType.CreditCard : (PaymentSpringTokenType)x[3],
						});

					var allUserNames = s.QueryOver<UserModel>()
						.Select(x => x.UserName, x => x.FirstName, x => x.LastName, x => x.DeleteTime)
						.Future<object[]>()
						.Select(x => new {
							Email = (string)x[0],
							FN = (string)x[1],
							LN = (string)x[2],
							Deleted = ((DateTime?)x[3]) != null
						});


					var chartsF = s.QueryOver<AccountabilityChart>().Where(x => x.DeleteTime == null).Select(x => x.RootId).Future<long>();
					var nodesF = s.QueryOver<AccountabilityNode>().Where(x => x.DeleteTime == null).Select(
						x => x.Id,
						x => x.ParentNodeId,
						x => x.UserId
					).Future<object[]>();
					var orgflagsF = s.QueryOver<OrganizationFlag>().Where(x => x.DeleteTime == null).Future();
					var userFlagsF = s.QueryOver<UserRole>().Where(x => x.DeleteTime == null).Future();

					var allUsers = allUsersF.ToList();
					var allLocalizedStrings = localizedStringF.Select(x => new {
						Id = (long)x[0],
						Name = (string)x[1]
					}).ToDictionary(x => x.Id, x => x.Name);

					var allOrgs = allOrgsF.Select(x => new {
						Id = (long)x[0],
						NameId = (long)x[1],
						Name = (string)allLocalizedStrings.GetOrDefault((long)x[1], ""),
						DeleteTime = (DateTime?)x[2],
						CreateTime = (DateTime)x[3],
						AccountType = (AccountType)x[4],
						TrialExpire = (DateTime?)x[5],
						EnabledL10 = ((bool?)x[6] ?? false),
						EnablePeople = ((bool?)x[7] ?? false),
						EnableEval = ((bool?)x[8] ?? false)
					}).ToDictionary(x => x.Id, x => x);


					var items = allUsers.Select(x => {
						var org = allOrgs.GetOrDefault(x.OrganizationId, null);
						//if (org.DeleteTime != null)
						//	return null;
						return new AllUserEmail() {
							UserName = x.Name,
							UserEmail = x.Email,
							UserId = x.UserId,
							OrgId = x.OrganizationId,
							OrgName = org.NotNull(y => y.Name),
							AccountType = "" + org.NotNull(y => y.AccountType),
							OrgCreateTime = org.NotNull(y => y.CreateTime),
							OrgDeleteTime = org.NotNull(y => y.DeleteTime),
							UserCreateTime = x.CreateTime,
							UserDeleteTime = x.DeleteTime,
							LastLogin = x.LastLogin,
							IsAdmin = x.IsAdmin,
							IsManager = x.IsManager,
							TrialExpire = org.NotNull(y => y.TrialExpire),
							EvalEnabled = org.NotNull(y => y.EnableEval),
							PeopleEnabled = org.NotNull(y => y.EnablePeople),
							L10Enabled = org.NotNull(y => y.EnabledL10),


							//Deleted = x.DeleteTime!=null  || org.DeleteTime !=null || org.AccountType == AccountType.Cancelled
						};
					}).Where(x => x != null).ToList();

					var charts = chartsF.Select(x => new { RootId = x }).ToList();
					var nodes = nodesF.Select(x => new {
						Id = (long)x[0],
						ParentNodeId = (long?)x[1],
						UserId = (long?)x[2]
					}).ToList();

					var leadershipMembers = new DefaultDictionary<long, bool>(x => false);
					foreach (var c in charts.ToList()) {
						var roots = nodes.Where(x => x.Id == c.RootId).ToList();
						if (roots.Any()) {
							var visionaryRow = nodes.Where(x => roots.Any(y => x.ParentNodeId == y.Id)).ToList();

							foreach (var i in roots.Where(x => x.UserId != null).Select(x => x.UserId))
								leadershipMembers[i.Value] = true;
							foreach (var i in visionaryRow.Where(x => x.UserId != null).Select(x => x.UserId))
								leadershipMembers[i.Value] = true;

							if (visionaryRow.Count <= 3) {
								var integratorRow = nodes.Where(x => visionaryRow.Any(y => x.ParentNodeId == y.Id)).ToList();
								foreach (var i in integratorRow.Where(x => x.UserId != null).Select(x => x.UserId))
									leadershipMembers[i.Value] = true;


								if (integratorRow.Count == 1) {
									var leadershipTeamRow = nodes.Where(x => integratorRow.Any(y => x.ParentNodeId == y.Id)).ToList();
									foreach (var i in leadershipTeamRow.Where(x => x.UserId != null).Select(x => x.UserId))
										leadershipMembers[i.Value] = true;
								}
							}
						}
					}

					var nameLookup = allUserNames.ToList().Distinct(x => x.Email).ToDictionary(x => x.Email.ToLower(), x => x);
					var orgFlags = orgflagsF.GroupBy(x => x.OrganizationId).ToDictionary(x => x.Key, x => x.ToList());
					var userFlags = userFlagsF.GroupBy(x => x.UserId).ToDictionary(x => x.Key, x => x.ToList());

					var meetingsByCompany = meetingsByCompanyF.GroupBy(x => x.OrgId).ToDictionary(x => x.Key, x => x.ToList());
					var meetingsByCompanyInCriteria = meetingsByCompanyF.GroupBy(x => x.OrgId).ToDictionary(x => x.Key, x => x.Count(y => {
						var duration = (y.CompleteTime - y.StartTime).Value.TotalMinutes;
						return duration >= 30 && duration <= 60 * 3;
					}));
					var lastMeetingsDateByCompany = meetingsByCompanyF.GroupBy(x => x.OrgId).ToDefaultDictionary(x => x.Key, x => x.Max(y => y.StartTime).Value.ToShortDateString(), x => "");


					var hasPaymentLookupByCompany = paymentTokens.GroupBy(x => x.OrgId).ToDefaultDictionary(x => x.Key, x => true, x => false);
					var paymentTypeLookupByCompany = paymentTokens.GroupBy(x => x.OrgId).ToDefaultDictionary(x => x.Key, x => "" + x.First().TokenType, x => "None");
					var paymentExpireLookupByCompany = paymentTokens.GroupBy(x => x.OrgId).ToDefaultDictionary(x => x.Key, x => "" + x.First().MonthExpire + "/" + x.First().YearExpire, x => "");
					var allDeletedLookup = allDeletedQ.ToDefaultDictionary(x => x.Id, x => x.DeleteTime, x => null);


					var csv = new Csv();
					csv.Title = "UserId";
					foreach (var o in items) {
						if (o.UserEmail.ToLower().EndsWith("@mytractiontools.com")) {
							continue;
						}
						var fn = nameLookup.GetOrDefault(o.UserEmail, null).NotNull(x => x.FN) ?? o.UserName.NotNull(x => x.SubstringBefore(" ")) ?? o.UserName;
						var ln = nameLookup.GetOrDefault(o.UserEmail, null).NotNull(x => x.LN) ?? o.UserName.NotNull(x => x.SubstringAfter(" ")) ?? o.UserName;

						var of = orgFlags.GetOrAddDefault(o.OrgId, (x) => new List<OrganizationFlag>()).Select(x => x.FlagType).ToArray();
						var uf = userFlags.GetOrAddDefault(o.UserId, (x) => new List<UserRole>()).Select(x => x.RoleType).ToArray();

						var ofStrings = of.Select(x => "" + x).ToList();
						ofStrings.Add(o.AccountType);


						var deleteTime = o.UserDeleteTime ?? allDeletedLookup[o.UserId];


						//csv.Add("" + o.UserId, "UserName", o.UserName);
						csv.Add("" + o.UserId, "UserName", o.UserName);
						csv.Add("" + o.UserId, "FirstName", fn);
						csv.Add("" + o.UserId, "LastName", ln);
						csv.Add("" + o.UserId, "UserEmail", o.UserEmail);
						csv.Add("" + o.UserId, "OrgName", o.OrgName);
						csv.Add("" + o.UserId, "UserId", "" + o.UserId);
						csv.Add("" + o.UserId, "OrgId", "" + o.OrgId);
						csv.Add("" + o.UserId, "LastLogin", "" + o.LastLogin);
						csv.Add("" + o.UserId, "UserCreateTime", "" + o.UserCreateTime);
						csv.Add("" + o.UserId, "UserDeleteTime", "" + deleteTime);
						csv.Add("" + o.UserId, "AccountType", o.AccountType);
						csv.Add("" + o.UserId, "OrgCreateTime", "" + o.OrgCreateTime);
						csv.Add("" + o.UserId, "OrgDeleteTime", "" + o.OrgDeleteTime);
						csv.Add("" + o.UserId, "LeadershipTeam_Guess", "" + leadershipMembers[o.UserId]);
						csv.Add("" + o.UserId, "LeadershipTeam_ClientMarked", "" + uf.Any(x => x == UserRoleType.LeadershipTeamMember));
						csv.Add("" + o.UserId, "UserType_AccountContact", "" + uf.Any(x => x == UserRoleType.AccountContact));
						csv.Add("" + o.UserId, "UserType_Placeholder", "" + uf.Any(x => x == UserRoleType.PlaceholderOnly));
						csv.Add("" + o.UserId, "Delinquent", "" + of.Any(x => x == OrganizationFlagType.Delinquent));
						csv.Add("" + o.UserId, "OrgFlags", string.Join("|", ofStrings));
						csv.Add("" + o.UserId, "UserFlags", string.Join("|", uf));
						csv.Add("" + o.UserId, "TT_Blacklist", "" + uf.Any(x => x == UserRoleType.EmailBlackList));
						csv.Add("" + o.UserId, "IsAdmin", "" + o.IsAdmin);
						csv.Add("" + o.UserId, "IsManager", "" + o.IsManager);
						csv.Add("" + o.UserId, "NumMeetingsWithinCloseCriteria", "" + meetingsByCompanyInCriteria.GetOrDefault(o.OrgId, 0));
						csv.Add("" + o.UserId, "PaymentEntered", "" + hasPaymentLookupByCompany[o.OrgId]);
						csv.Add("" + o.UserId, "PaymentType", "" + paymentTypeLookupByCompany[o.OrgId]);
						csv.Add("" + o.UserId, "PaymentExpire", "" + paymentExpireLookupByCompany[o.OrgId]);
						csv.Add("" + o.UserId, "TrialExpire", (hasPaymentLookupByCompany[o.OrgId] ? "" : ("" + o.TrialExpire.NotNull(z => z.Value.ToShortDateString()))));
						csv.Add("" + o.UserId, "LastMeetingTime", "" + lastMeetingsDateByCompany[o.OrgId]);

						csv.Add("" + o.UserId, "EvalEnabled", "" + o.EvalEnabled);
						csv.Add("" + o.UserId, "PeopleEnabled", "" + o.PeopleEnabled);
						csv.Add("" + o.UserId, "L10Enabled", "" + o.L10Enabled);


					}

					var elapsed = DateTime.UtcNow - now;

					AdminAccessor.AllUserDataExports.Enqueue(new LatestExport() {
						Id = ExportId++,
						Data = csv,
						GeneratedAt = now,
						GenerationDuration = elapsed
					});
					return csv;
				}
			}
		}



		[Obsolete("Not safe")]
		public static List<AdminAccessModel> GetAdminAccessLogs_Unsafe(int days) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var logs = s.QueryOver<AdminAccessModel>().Where(x => x.DeleteTime > DateTime.UtcNow.AddDays(-days)).List().ToList();

					var adminsIds = logs.Select(x => x.AdminUserId).Distinct().ToArray();
					var accessIds = logs.Select(x => x.AccessId).Distinct().ToArray();
					var userEmails = logs.Select(x => x.SetAsEmail).Distinct().ToArray();

					var adminQ = s.QueryOver<UserModel>()
						.WhereRestrictionOn(x => x.Id).IsIn(adminsIds)
						.Select(x => x.FirstName, x => x.LastName, x => x.UserName, x => x.Id)
						.Future<object[]>()
						.Select(x => new { firstName = (string)x[0], lastName = (string)x[1], email = (string)x[2], Id = (string)x[3] });

					var userEmailQ = s.QueryOver<UserModel>().WhereRestrictionOn(x => x.UserName).IsIn(userEmails)
						.Select(x => x.FirstName, x => x.LastName, x => x.UserName)
						.Future<object[]>()
						.Select(x => new { firstName = (string)x[0], lastName = (string)x[1], email = (string)x[2]/*, Email = (string)x[3]*/ });

					UserModel userAlias = null;
					OrganizationModel orgAlias = null;
					LocalizedStringModel nameAlias = null;
					var accessQ = s.QueryOver<UserOrganizationModel>()
						.JoinAlias(x => x.User, () => userAlias)
						.JoinAlias(x => x.Organization, () => orgAlias)
						.JoinAlias(x => orgAlias.Name, () => nameAlias)
						.WhereRestrictionOn(x => x.Id).IsIn(accessIds)
						.Select(x => userAlias.FirstName, x => userAlias.LastName, x => userAlias.UserName, x => x.Id, x => nameAlias.Standard)
						.Future<object[]>()
						.Select(x => new { firstName = (string)x[0], lastName = (string)x[1], email = (string)x[2], Id = (long)x[3], OrgName = (string)x[4] });

					var adminLU = adminQ.ToDefaultDictionary(x => x.Id, x => x, x => null);
					var userEmailLU = userEmailQ.ToDefaultDictionary(x => x.email, x => x, x => null);
					var accessLU = accessQ.ToDefaultDictionary(x => x.Id, x => x, x => null);
					var orgLU = accessQ.ToDefaultDictionary(x => x.Id, x => x, x => null);

					foreach (var log in logs) {
						var ue = userEmailLU[log.SetAsEmail??""].NotNull(x => x.firstName + " " + x.lastName);
						var orgName = "";
						if (ue == null) {
							var access = accessLU[log.AccessId];
							if (access != null) {
								ue = access.NotNull(x => x.firstName + " " + x.lastName);
								orgName = access.OrgName;
							}
						}
						log._AccessName = ue;
						log._AdminName = adminLU[log.AdminUserId??""].NotNull(x => x.firstName + " " + x.lastName);
						log._AccessOrganization = orgName;
					}
					return logs;
				}
			}
		}
	}
}