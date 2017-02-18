using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using Amazon.ElasticTranscoder.Model;
using Amazon.S3.Model;
using Amazon.SimpleDB.Model;
using Microsoft.AspNet.SignalR;
using NHibernate;
using NHibernate.Hql.Ast.ANTLR.Tree;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Periods;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using TrelloNet;
using Twilio;
using RadialReview.Models.L10;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Exceptions;
using Novacode;
using System.Globalization;
using System.Threading;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Synchronize;

namespace RadialReview.Accessors {
	public class VtoAccessor : BaseAccessor {

		public static void UpdateAllVTOs(ISession s, long organizationId, string connectionId, Action<dynamic> action) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			var vtoIds = s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>();
			foreach (var vtoId in vtoIds) {
				var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId),connectionId);
				action(group);
			}
		}

		public static void UpdateVTO(ISession s, long vtoId, string connectionId, Action<dynamic> action) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);
			action(group);
		}

		public static List<VtoModel> GetAllVTOForOrganization(UserOrganizationModel caller, long organizationId) { 
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

					return s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
				}
			}
		}

		public static VtoModel GetVTO(UserOrganizationModel caller, long vtoId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetVTO(s, perms, vtoId);
				}
			}
		}


		public static VtoModel GetVTO(ISession s, PermissionsUtility perms, long vtoId) {
			perms.ViewVTO(vtoId);
			var model = s.Get<VtoModel>(vtoId);
			model._Values = OrganizationAccessor.GetCompanyValues_Unsafe(s.ToQueryProvider(true), model.Organization.Id, null);
			var uniquesQ= s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Uniques && x.Vto.Id == vtoId && x.DeleteTime == null).Future();//.List().ToList();
			var looksLikeQ= s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_LookLike && x.Vto.Id == vtoId && x.DeleteTime == null).Future();//.List().ToList();
			var goalsQ = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_YearGoals && x.Vto.Id == vtoId && x.DeleteTime == null).Future();//.List().ToList();
																																							  //model.._GoalsForYear = s.QueryOver<VtoModel.VtoItem_String>().Where(x => x.Type == VtoItemType.List_Issues && x.Vto.Id == vtoId && x.DeleteTime == null).List().ToList();
			var rocksQ= s.QueryOver<Vto_Rocks>()
				.Where(x => x.Vto.Id == vtoId && x.DeleteTime == null).Future();
			

			model._Issues = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Issues && x.Vto.Id == vtoId && x.DeleteTime == null).List().Select(x=>new VtoIssue() {
				Id = x.Id,
				BaseId = x.BaseId,
				CopiedFrom = x.CopiedFrom,
				CreateTime = x.CreateTime,
				Data=x.Data,
				DeleteTime = x.DeleteTime,
				ForModel = x.ForModel,
				Ordering = x.Ordering,
				Type = x.Type,
				Vto = x.Vto,
			}).ToList();

			model.MarketingStrategy._Uniques = uniquesQ.ToList();
			model.ThreeYearPicture._LooksLike = looksLikeQ.ToList();
			model.OneYearPlan._GoalsForYear = goalsQ.ToList();
			model.QuarterlyRocks._Rocks  = rocksQ.ToList().Where(x => x.Rock.DeleteTime == null && x.Rock.CompanyRock).ToList();

			var issuesAttachedToRecur = model._Issues
				.Where(x => x.ForModel != null && x.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>())
				.Select(x=>x.ForModel.ModelId)
				.Distinct().ToArray();

			if (issuesAttachedToRecur.Any()) {
				var foundIssues = s.QueryOver<IssueModel.IssueModel_Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(issuesAttachedToRecur).List().ToList();
				foreach (var i in model._Issues) {
					if (i.ForModel!=null && i.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>()) {
						i.Issue = foundIssues.FirstOrDefault(x => x.Id == i.ForModel.ModelId);						
						i._Extras["Owner"] = i.Issue.NotNull(x => x.Owner.NotNull(y => y.GetName()));
						i._Extras["OwnerInitials"] = i.Issue.NotNull(x => x.Owner.NotNull(y => y.GetInitials()));
					}
				}
			}

			return model;
		}

		public static AngularVTO GetAngularVTO(UserOrganizationModel caller, long vtoId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var vto = GetVTO(s, perms, vtoId);

					var ang = AngularVTO.Create(vto);

					if (ang.L10Recurrence != null) {
						try {
							var recur = L10Accessor.GetL10Recurrence(s, perms, ang.L10Recurrence.Value, false);
							//var isLeadership = recur.TeamType == L10TeamType.LeadershipTeam;
							//if (isLeadership) {
							//   ang.QuarterlyRocks.Rocks = ang.QuarterlyRocks.Rocks.Where(x => x.Rock.CompanyRock ?? true).ToList();
							//}
							if (recur.TeamType != L10TeamType.LeadershipTeam)
								ang.IncludeVision = false;
						} catch (Exception) {

						}
					}
					return ang;
				}
			}
		}
		public static VtoModel CreateRecurrenceVTO(ISession s, PermissionsUtility perm, long recurrenceId) {
			perm.EditL10Recurrence(recurrenceId);//.CreateVTO(organizationId);
			var recurrence = s.Get<L10Recurrence>(recurrenceId);
			perm.ViewOrganization(recurrence.OrganizationId);

			var model = new VtoModel();
			model.Organization = s.Get<OrganizationModel>(recurrence.OrganizationId);

			s.SaveOrUpdate(model);
			//s.Save(model.MarketingStrategy);
			//s.Save(model.CoreFocus);
			//s.Save(model.ThreeYearPicture);
			//s.Save(model.OneYearPlan);
			//s.Save(model.QuarterlyRocks);

			model.CoreFocus.Vto = model.Id;
			model.MarketingStrategy.Vto = model.Id;
			model.OneYearPlan.Vto = model.Id;
			model.QuarterlyRocks.Vto = model.Id;
			model.ThreeYearPicture.Vto = model.Id;
			model.L10Recurrence = recurrenceId;

			model.Name = recurrence.Name;

			s.Update(model);

			recurrence.VtoId = model.Id;
			s.Update(recurrence);
			return model;
		}

		public static VtoModel CreateVTO(ISession s, PermissionsUtility perm, long organizationId) {
			perm.ViewOrganization(organizationId).CreateVTO(organizationId);

			var model = new VtoModel();
			model.Organization = s.Get<OrganizationModel>(organizationId);
			s.SaveOrUpdate(model);

			model.CoreFocus.Vto = model.Id;
			model.MarketingStrategy.Vto = model.Id;
			model.OneYearPlan.Vto = model.Id;
			model.QuarterlyRocks.Vto = model.Id;
			model.ThreeYearPicture.Vto = model.Id;

			s.Update(model);
			return model;
		}
		public static VtoModel CreateVTO(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);

					var model = CreateVTO(s, perm, organizationId);

					tx.Commit();
					s.Flush();

					return model;
				}
			}
		}

		public static void UpdateVtoString(UserOrganizationModel caller, long vtoStringId, String message, bool? deleted, string connectionId = null) {
			long? update_VtoId = null;
			VtoItem_String str = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					str = s.Get<VtoItem_String>(vtoStringId);
					var perm = PermissionsUtility.Create(s, caller).EditVTO(str.Vto.Id);
					
					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateVtoItem(vtoStringId));

					str.Data = message;
					if (str.BaseId == 0)
						str.BaseId = str.Id;

					if (deleted != null) {
						if (deleted == true && str.DeleteTime == null) {
							str.DeleteTime = DateTime.UtcNow;
							connectionId = null;
						} else if (deleted == false) {
							str.DeleteTime = null;
						}
					}

					s.Update(str);
					update_VtoId = str.Vto.Id;

					//Update IssueRecurrence
					if (str.ForModel != null) {
						if (str.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>()) {
							var issueRecur = s.Get<IssueModel.IssueModel_Recurrence>(str.ForModel.ModelId);
							if (perm.IsPermitted(x => x.EditL10Recurrence(issueRecur.Recurrence.Id))) {
								issueRecur.Issue.Message = message;
								s.Update(issueRecur.Issue);
							}
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			if (update_VtoId != null) {
				var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
				var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(update_VtoId.Value), connectionId);
				str.Vto = null;
				group.update(new AngularUpdate(){
					AngularVtoString.Create(str)
				});
			}
		}
		public static void UpdateVto(UserOrganizationModel caller, long vtoId, String name = null, String tenYearTarget = null, String tenYearTargetTitle = null, String coreValueTitle = null, String issuesListTitle = null, string connectionId = null) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditVTO(vtoId);
					var vto = s.Get<VtoModel>(vtoId);


					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateVto(vtoId));

					vto.Name = name;
					vto.TenYearTarget = tenYearTarget;
					vto.TenYearTargetTitle = tenYearTargetTitle;
					vto.CoreValueTitle = coreValueTitle;
					vto.IssuesListTitle = issuesListTitle;

					s.Update(vto);

					tx.Commit();
					s.Flush();
					group.update(new AngularVTO(vtoId) {
						Name = vto.Name,
						TenYearTarget = vto.TenYearTarget,
						TenYearTargetTitle = vto.TenYearTargetTitle,
						CoreValueTitle = vto.CoreValueTitle,
						IssuesListTitle = vto.IssuesListTitle
					});
				}
			}
		}

		public static void Update(UserOrganizationModel caller, BaseAngular model, string connectionId) {
			if (model.Type == typeof(AngularVtoString).Name) {
				var m = (AngularVtoString)model;
				UpdateVtoString(caller, m.Id, m.Data, null, connectionId);
			} else if (model.Type == typeof(AngularVTO).Name) {
				var m = (AngularVTO)model;
				UpdateVto(caller, m.Id, m.Name, m.TenYearTarget, m.TenYearTargetTitle, m.CoreValueTitle, m.IssuesListTitle, connectionId);
			} else if (model.Type == typeof(AngularCompanyValue).Name) {
				var m = (AngularCompanyValue)model;
				UpdateCompanyValue(caller, m.Id, m.CompanyValue, m.CompanyValueDetails, null, connectionId);
			} else if (model.Type == typeof(AngularCoreFocus).Name) {
				var m = (AngularCoreFocus)model;
				UpdateCoreFocus(caller, m.Id, m.Purpose, m.Niche, m.PurposeTitle, m.CoreFocusTitle, connectionId);
			} else if (model.Type == typeof(AngularStrategy).Name) {
				var m = (AngularStrategy)model;
				UpdateStrategy(caller, m.Id, m.TargetMarket, m.ProvenProcess, m.Guarantee, m.MarketingStrategyTitle, connectionId);
			} else if (model.Type == typeof(AngularVtoRock).Name) {
				var m = (AngularVtoRock)model;
				UpdateRock(caller, m.Id, m.Rock.Name, m.Rock.Owner.Id, null, connectionId);
			} else if (model.Type == typeof(AngularOneYearPlan).Name) {
				var m = (AngularOneYearPlan)model;
				UpdateOneYearPlan(caller, m.Id, m.FutureDate, m.Revenue, m.Profit, m.Measurables, m.OneYearPlanTitle, connectionId);
			} else if (model.Type == typeof(AngularQuarterlyRocks).Name) {
				var m = (AngularQuarterlyRocks)model;
				UpdateQuarterlyRocks(caller, m.Id, m.FutureDate, m.Revenue, m.Profit, m.Measurables, m.RocksTitle, connectionId);
			} else if (model.Type == typeof(AngularThreeYearPicture).Name) {
				var m = (AngularThreeYearPicture)model;
				UpdateThreeYearPicture(caller, m.Id, m.FutureDate, m.Revenue, m.Profit, m.Measurables, m.ThreeYearPictureTitle, connectionId);
			}


		}

		public static void UpdateThreeYearPicture(UserOrganizationModel caller, long id, DateTime? futuredate = null, string revenue = null, string profit = null, string measurables = null, string threeYearPictureTitle = null, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var threeYear = s.Get<ThreeYearPictureModel>(id);
					var vtoId = threeYear.Vto;

					var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
					var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);
					
					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateThreeYearPicture(id));

					PermissionsUtility.Create(s, caller).EditVTO(vtoId);

					threeYear.FutureDate = futuredate;
					threeYear.RevenueStr = revenue;
					threeYear.ProfitStr = profit;
					threeYear.Measurables = measurables;
					threeYear.ThreeYearPictureTitle = threeYearPictureTitle;
					s.Update(threeYear);

					tx.Commit();
					s.Flush();
					group.update(new AngularUpdate(){new AngularThreeYearPicture(id) {
						FutureDate = futuredate,
						Revenue = revenue,
						Profit = profit,
						Measurables = measurables,
						ThreeYearPictureTitle=threeYearPictureTitle
					}});
				}
			}
		}
		public static void UpdateQuarterlyRocks(UserOrganizationModel caller, long id, DateTime? futuredate = null, string revenue = null, string profit = null, string measurables = null, string rocksTitle = null, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var quarterlyRocks = s.Get<QuarterlyRocksModel>(id);
					var vtoId = quarterlyRocks.Vto;

					var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
					var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);

					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateQuarterlyRocks(id));

					PermissionsUtility.Create(s, caller).EditVTO(vtoId);

					quarterlyRocks.FutureDate = futuredate;
					quarterlyRocks.RevenueStr = revenue;
					quarterlyRocks.ProfitStr = profit;
					quarterlyRocks.Measurables = measurables;
					quarterlyRocks.RocksTitle = rocksTitle;
					s.Update(quarterlyRocks);

					tx.Commit();
					s.Flush();
#pragma warning disable CS0618 // Type or member is obsolete
					group.update(new AngularUpdate(){new AngularQuarterlyRocks(id) {
						FutureDate = futuredate,
						Revenue = revenue,
						Profit = profit,
						Measurables = measurables,
						RocksTitle=rocksTitle,
					}});
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}

		public static void UpdateOneYearPlan(UserOrganizationModel caller, long id, DateTime? futuredate = null, string revenue = null, string profit = null, string measurables = null, string oneYearPlanTitle = null, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var plan = s.Get<OneYearPlanModel>(id);
					var vtoId = plan.Vto;

					var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
					var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);

					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateOneYearPlan(id));

					PermissionsUtility.Create(s, caller).EditVTO(vtoId);

					plan.FutureDate = futuredate;
					plan.RevenueStr = revenue;
					plan.ProfitStr = profit;
					plan.Measurables = measurables;
					plan.OneYearPlanTitle = oneYearPlanTitle;
					s.Update(plan);

					tx.Commit();
					s.Flush();
#pragma warning disable CS0618 // Type or member is obsolete
					group.update(new AngularUpdate(){new AngularOneYearPlan(id) {
						FutureDate = futuredate,
						Revenue = revenue,
						Profit = profit,
						Measurables = measurables,
						OneYearPlanTitle=oneYearPlanTitle
					}});
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}

		public static void UpdateStrategy(UserOrganizationModel caller, long strategyId, String targetMarket = null, String provenProcess = null, String guarantee = null, String marketingStrategyTitle = null, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var strategy = s.Get<MarketingStrategyModel>(strategyId);
					var vtoId = strategy.Vto;

					var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
					var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);

					PermissionsUtility.Create(s, caller).EditVTO(vtoId);
					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateStrategy(strategyId));

					strategy.ProvenProcess = provenProcess;
					strategy.Guarantee = guarantee;
					strategy.TargetMarket = targetMarket;
					strategy.MarketingStrategyTitle = marketingStrategyTitle;
					s.Update(strategy);

					tx.Commit();
					s.Flush();
#pragma warning disable CS0618 // Type or member is obsolete
					group.update(new AngularUpdate(){new AngularStrategy(strategyId) {
						ProvenProcess = provenProcess,
						Guarantee = guarantee,
						TargetMarket = targetMarket,
						MarketingStrategyTitle=marketingStrategyTitle,
					}});
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}
		}

		public static void UpdateCoreFocus(UserOrganizationModel caller, long coreFocusId, string purpose, string niche, string purposeTitle, string coreFocusTitle, string connectionId) {
			//var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var coreFocus = s.Get<CoreFocusModel>(coreFocusId);
					PermissionsUtility.Create(s, caller).EditVTO(coreFocus.Vto);

					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateCoreFocus(coreFocusId));
					coreFocus.Purpose = purpose;
					coreFocus.Niche = niche;
					coreFocus.PurposeTitle = purposeTitle;
					coreFocus.CoreFocusTitle = coreFocusTitle;
					s.Update(coreFocus);

					var update = new AngularUpdate() { AngularCoreFocus.Create(coreFocus) };
					UpdateVTO(s, coreFocus.Vto, connectionId, x => x.update(update));
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void UpdateCompanyValue(UserOrganizationModel caller, long companyValueId, string message, string details, bool? deleted, string connectionId) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var companyValue = s.Get<CompanyValueModel>(companyValueId);
					PermissionsUtility.Create(s, caller).EditCompanyValues(companyValue.OrganizationId);

					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateCompanyValue(companyValueId));

					if (message != null) {
						companyValue.CompanyValue = message;
						s.Update(companyValue);
					}

					if (details != null) {
						companyValue.CompanyValueDetails = details;
						s.Update(companyValue);
					}

					if (deleted != null) {
						if (deleted == false)
							companyValue.DeleteTime = null;
						else if (companyValue.DeleteTime == null) {
							companyValue.DeleteTime = DateTime.UtcNow;
						}
						s.Update(companyValue);
					}
					var update = new AngularUpdate();
					update.Add(AngularCompanyValue.Create(companyValue));
					UpdateAllVTOs(s, companyValue.OrganizationId,connectionId, x => x.update(update));

					tx.Commit();
					s.Flush();

				}
			}
		}
		public static void UpdateRockAccountable(UserOrganizationModel caller, long rockId, long accountableUser) {
			//var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var rock = s.Get<Vto_Rocks>(rockId);
					PermissionsUtility.Create(s, caller).EditVTO(rock.Vto.Id).ViewUserOrganization(accountableUser, false);

					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateRockOwner(rockId));

					rock.Rock.AccountableUser = s.Get<UserOrganizationModel>(accountableUser);
					rock.Rock.ForUserId = accountableUser;

					var a = rock.Rock.AccountableUser.GetName();
					var b = rock.Rock.AccountableUser.GetImageUrl();

					s.Update(rock.Rock);
					tx.Commit();
					s.Flush();

					var update = new AngularUpdate() { AngularVtoRock.Create(rock), new AngularRock(rock.Rock), };
					UpdateVTO(s, rock.Vto.Id, null, x => x.update(update));
				}
			}
		}

		public static void UpdateRock(UserOrganizationModel caller, long rockId, string message, long? accountableUser, bool? deleted, string connectionId) {
			//var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var rock = s.Get<Vto_Rocks>(rockId);
					var perm = PermissionsUtility.Create(s, caller).EditVTO(rock.Vto.Id);

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					SyncUtil.EnsureStrictlyAfter(caller, s, SyncAction.UpdateRock(rockId));


					bool skipUpdate = false;

					if (deleted != null) {
						var vto = s.Get<VtoModel>(rock.Vto.Id);


						if (deleted == false) {
							rock.DeleteTime = null;
							//rock.Rock.DeleteTime = null;
							if (vto.L10Recurrence != null)
								L10Accessor.AddRock(s, perm, vto.L10Recurrence.Value, rock.Rock);

						} else if (rock.DeleteTime == null) {
							rock.DeleteTime = DateTime.UtcNow;
							//rock.Rock.DeleteTime = rock.DeleteTime;
							if (vto.L10Recurrence != null) {
								var recurRocks1 = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.ForRock.Id == rock.Rock.Id && x.L10Recurrence.Id == vto.L10Recurrence && x.DeleteTime == null).List().ToList();

								foreach (var r in recurRocks1) {
									r.DeleteTime = rock.DeleteTime;
									s.Update(r);
									var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id));
									group.removeRock(r.ForRock.Id);
								}

								//Delete this meetings rocks
								var m = L10Accessor._GetCurrentL10Meeting(s, perm, vto.L10Recurrence.Value, true, false, false);
								if (m != null) {
									var meetingRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>()
										.Where(x => x.ForRock.Id == rock.Rock.Id && x.L10Meeting.Id == m.Id && x.DeleteTime == null).List().ToList();

									foreach (var r in meetingRocks) {
										r.DeleteTime = rock.DeleteTime;
										s.Update(r);
									}
								}

								//var recurRocks = L10Accessor.GetRocksForRecurrence(s, perm, vto.L10Recurrence.Value);
								var arecur = new AngularRecurrence(vto.L10Recurrence.Value) {
									Rocks = AngularList.Create(AngularListType.Remove, new AngularRock(rock.Rock.Id).AsList()),
								};
								var group1 = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(vto.L10Recurrence.Value));
								group1.update(new AngularUpdate() { arecur });
							}
							var update = new AngularUpdate() {
								new AngularQuarterlyRocks(){
									Rocks = AngularList.Create(AngularListType.Remove,AngularVtoRock.Create(rock).AsList())
								}
							};
							UpdateVTO(s, rock.Vto.Id, null, x => x.update(update));
							skipUpdate = true; // Assumes all you do is delete the rock.
						}

					} else {
						if (rock.Rock.Rock != message) {
							rock.Rock.Rock = message;
							var recurRocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null && x.ForRock.Id == rock.Rock.Id).List().ToList();

							foreach (var r in recurRocks) {
								var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id));
								group.updateRockName(r.ForRock.Id, r.ForRock.Rock);
								group.update(new AngularUpdate() { new AngularRock(rock.Rock.Id) { Name = message } });
							}
						}
						if (accountableUser.HasValue) {
							perm.ViewUserOrganization(accountableUser.Value, false);
							rock.Rock.AccountableUser = s.Get<UserOrganizationModel>(accountableUser.Value);
						}
					}

					s.Update(rock);
					s.Update(rock.Rock);

					tx.Commit();
					s.Flush();

					if (!skipUpdate) {
						var update2 = new AngularUpdate() { AngularVtoRock.Create(rock) };
						UpdateVTO(s, rock.Vto.Id, connectionId /*should not be null*/, x => x.update(update2));
					}


				}
			}
		}


		public static void JoinVto(UserOrganizationModel caller, long vtoId, string connectionId) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewVTO(vtoId);
					hub.Groups.Add(connectionId, VtoHub.GenerateVtoGroupId(vtoId));
					Audit.VtoLog(s, caller, vtoId, "JoinVto");
				}
			}
		}

		public static VtoItem_String AddString(ISession s, PermissionsUtility perms, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoString>, IAngularItem> updateFunc, bool skipUpdate = false, ForModel forModel = null, string value = null) {
			perms.EditVTO(vtoId);
			var vto = s.Get<VtoModel>(vtoId);
			var organizationId = vto.Organization.Id;

			var items = s.QueryOver<VtoItem_String>().Where(x => x.Vto.Id == vtoId && x.Type == type && x.DeleteTime == null).List().ToList();
			var count = items.Count();

#pragma warning disable CS0618 // Type or member is obsolete
			var str = new VtoItem_String() {
				Type = type,
				Ordering = count,
				Vto = vto,
				ForModel = forModel,
				Data = value
			};
#pragma warning restore CS0618 // Type or member is obsolete

			s.Save(str);

			items.Add(str);
			var angularItems = AngularList.Create(AngularListType.ReplaceAll, AngularVtoString.Create(items));

			if (updateFunc != null) {
				if (skipUpdate)
					UpdateVTO(s, vtoId, null, x => x.update(updateFunc(vto, angularItems)));

				UpdateVTO(s, vtoId, null, x => x.update(new AngularUpdate() { updateFunc(vto, angularItems) }));
			}
			return str;
		}

		public static void AddString(UserOrganizationModel caller, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoString>, IAngularItem> updateFunc, bool skipUpdate = false, ForModel forModel = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					AddString(s, perms, vtoId, type, updateFunc, skipUpdate, forModel);
					tx.Commit();
					s.Flush();
				}
			}
		}


#pragma warning disable CS0618 // Type or member is obsolete
		public static void AddUniques(UserOrganizationModel caller, long vtoId) {
			AddString(caller, vtoId, VtoItemType.List_Uniques, (vto, items) => new AngularStrategy(vto.MarketingStrategy.Id) { Uniques = items });
		}
		public static void AddThreeYear(UserOrganizationModel caller, long vtoId) {
			AddString(caller, vtoId, VtoItemType.List_LookLike, (vto, list) => new AngularThreeYearPicture(vto.ThreeYearPicture.Id) { LooksLike = list });
		}
		public static void AddYearGoal(UserOrganizationModel caller, long vtoId) {
			AddString(caller, vtoId, VtoItemType.List_YearGoals, (vto, list) => new AngularOneYearPlan(vto.OneYearPlan.Id) { GoalsForYear = list });
		}
		public static void AddIssue(UserOrganizationModel caller, long vtoId) {
			AddString(caller, vtoId, VtoItemType.List_Issues, (vto, list) => new AngularVTO(vto.Id) { Issues = list }, true);
		}
#pragma warning restore CS0618 // Type or member is obsolete

		public static void AddCompanyValue(UserOrganizationModel caller, long vtoId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditVTO(vtoId);

					var vto = s.Get<VtoModel>(vtoId);

					var organizationId = vto.Organization.Id;
					var existing = OrganizationAccessor.GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, null);
					existing.Add(new CompanyValueModel() { OrganizationId = organizationId });
					OrganizationAccessor.EditCompanyValues(s, perms, organizationId, existing);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void AddRock(ISession s, PermissionsUtility perms, long vtoId, RockModel rock, DateTime? nowTime = null) {
			if (rock._AddedToVTO)
				throw new PermissionsException("Already added to vto");
			rock._AddedToVTO = true;

			perms.EditVTO(vtoId);

			var vto = s.Get<VtoModel>(vtoId);

			var organizationId = vto.Organization.Id;
			//var existing = OrganizationAccessor.GetCompanyRocks(s, perms, organizationId);

			// if (rock == null)
			//{

			//}
			//else
			//{
			//    rock.OrganizationId = organizationId;
			//    rock.Category = category;
			//    rock.PeriodId = 

			//}
			//existing.Add(rock);
			var now = nowTime ?? DateTime.UtcNow;

			var vtoRocks = s.QueryOver<Vto_Rocks>().Where(x => x.Vto.Id == vtoId).List().ToList();

			s.SaveOrUpdate(rock);
			var vtoRock = new Vto_Rocks {
				CreateTime = now,
				Rock = rock,
				Vto = vto,
				_Ordering = vtoRocks.Count(),

			};
			s.Save(vtoRock);

			if (vto.L10Recurrence != null && !rock._AddedToL10) {
				L10Accessor.AddRock(s, perms, vto.L10Recurrence.Value, rock, now);
			}

			vtoRocks.Add(vtoRock);


			var angularItems = AngularList.Create(AngularListType.Add, AngularVtoRock.Create(vtoRock).AsList());
#pragma warning disable CS0618 // Type or member is obsolete
			var update = new AngularUpdate() { new AngularQuarterlyRocks(vto.QuarterlyRocks.Id) { Rocks = angularItems } };
#pragma warning restore CS0618 // Type or member is obsolete
			UpdateVTO(s, vtoId, null, x => x.update(update));

			/*if (vto.L10Recurrence.HasValue)
            {
                var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();

                var recurRocks = L10Accessor.GetRocksForRecurrence(s, perms, vto.L10Recurrence.Value);
                var arecur = new AngularRecurrence(vto.L10Recurrence.Value)
                {
                    Rocks = recurRocks.Select(x => new AngularRock(x.ForRock)).ToList(),
                };
                var group1 = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(vto.L10Recurrence.Value));
                group1.update(new AngularUpdate() { arecur });
            }*/
		}

		public static void CreateNewRock(UserOrganizationModel caller, long vtoId, string message = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					CreateNewRock(s, perms, vtoId, caller.Id, message);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void CreateNewRock(ISession s, PermissionsUtility perms, long vtoId, long owner, string message = null) {
			var now = DateTime.UtcNow;
			var vto = s.Get<VtoModel>(vtoId);
			var organizationId = vto.Organization.Id;
			perms.ViewUserOrganization(owner, false);

			var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
			var rock = new RockModel() {
				CreateTime = now,
				OrganizationId = organizationId,
				CompanyRock = true,
				Category = category,
				//Period = s.Load<PeriodModel>(vto.PeriodId),
				PeriodId = vto.PeriodId,
				OnlyAsk = AboutType.Self,
				ForUserId = owner,
				AccountableUser = s.Load<UserOrganizationModel>(owner),
				Rock = message,
			};
			AddRock(s, perms, vtoId, rock, now);

		}

		private static string ParseVtoHeader(Novacode.Cell cell, string searchFor) {
			searchFor = searchFor.ToLower();
			var found = cell.Paragraphs.Where(x => x.StyleName != "ListParagraph").Where(x => x.Text.ToLower().Contains(searchFor)).FirstOrDefault().NotNull(x => x.Text);
			if (found != null) {
				var sp = found.Split(':');
				if (sp.Length > 1)
					found = string.Join(":", sp.Skip(1));
				else
					found = sp[0].SubstringAfter(searchFor);
			}
			return found;
		}

		public static VtoModel UploadVtoForRecurrence(UserOrganizationModel caller, DocX doc, long recurrenceId, List<Exception> exceptions) {

			exceptions = exceptions ?? new List<Exception>();


			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).AdminL10Recurrence(recurrenceId);
					var recur = s.Get<L10Recurrence>(recurrenceId);
					var vtoId = recur.VtoId;
					if (vtoId <= 0)
						throw new PermissionsException("V/TO does not exist.");
					perms.EditVTO(vtoId);
					var vto = s.Get<VtoModel>(vtoId);
					if (vto == null)
						throw new PermissionsException("V/TO does not exist.");

					//var organizationId = vto.Organization.Id;
					//var existing = OrganizationAccessor.GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, null);
					//existing.Add(new CompanyValueModel() { OrganizationId = organizationId });
					//OrganizationAccessor.EditCompanyValues(s, perms, organizationId, existing);
					#region Initialize defaults
					var corevaluesTitle = "CORE VALUES";
					var threeYearTitle = "3-YEAR PICTURE™";
					var coreFocusTitle = "CORE FOCUS™";
					var tenYearTargetTitle = "10-YEAR TARGET™";
					var marketingStrategyTitle = "MARKETING STRATEGY";
					var rocksTitle = "ROCKS";
					var issuesTitle = "ISSUES LIST";
					var oneYearTitle = "1-YEAR PLAN";

					List<string> corevaluesList = new List<string>();

					string threeYearFuture = "";
					string threeYearRevenue = "";
					string threeYearProfit = "";
					string threeYearMeasurables = "";
					var threeYearLooksList = new List<string>();

					string purpose = "<could not parse>";
					string niche = "<could not parse>";
					string purposeTitle = "Purpose/Cause/Passion";

					Cell tenYearCell = null;
					var marketingDict = new DefaultDictionary<string, string>(x => "<could not parse>");
					var uniques = new List<string>();

					string oneYearFuture = "";
					string oneYearRevenue = "";
					string oneYearProfit = "";
					string oneYearMeasurables = "";

					List<string> oneYearPlanGoals = new List<string>();
					string rocksFuture = "";
					string rocksRevenue = "";
					string rocksProfit = "";
					string rocksMeasurables = "";

					List<Row> rocksList = new List<Row>();
					List<string> issuesList = new List<string>();
					#endregion

					#region Page 1
					try {
						if (doc.Tables.Count < 2)
							throw new FormatException("Could not find the V/TO.");

						var page1 = doc.Tables[0];

						if (page1.Rows.Count != 5)
							throw new FormatException("Could not find Vision Page.");

						var corevaluesRow = page1.Rows[0];
						var threeYearPictureDetailsRow = page1.Rows[1];
						var coreFocusRow = page1.Rows[2];
						var tenYearRow = page1.Rows[3];
						var marketingStrategyRow = page1.Rows[4];

						try {
							//Core values
							if (corevaluesRow.Cells.Count != 3 || corevaluesRow.Cells[0].FillColor.Name != "bfbfbf")
								throw new FormatException("Could not find Core Values.");
							if (corevaluesRow.Cells[0].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(corevaluesRow.Cells[0].Paragraphs[0].Text))
								corevaluesTitle = corevaluesRow.Cells[0].Paragraphs[0].Text;
							var corevaluesCell = corevaluesRow.Cells[1];
							if (corevaluesCell.Lists.Count != 1 || !corevaluesCell.Lists[0].Items.Any())
								throw new FormatException("Could not find Core Values list.");
							corevaluesList = corevaluesCell.Lists[0].Items.Select(x => x.Text).ToList();
						} catch (Exception e) {
							exceptions.Add(e);
						}
						try {
							//3 year picture
							if (corevaluesRow.Cells.Count != 3 || corevaluesRow.Cells[2].FillColor.Name != "bfbfbf" || coreFocusRow.Cells.Count != 3)
								throw new FormatException("Could not find Three Year Picture.");
							if (corevaluesRow.Cells[2].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(corevaluesRow.Cells[2].Paragraphs[0].Text))
								threeYearTitle = corevaluesRow.Cells[2].Paragraphs[0].Text;
							if (threeYearPictureDetailsRow.Cells.Count != 3)
								throw new FormatException("Could not find Three Year Picture details.");
							var threeYearCell = threeYearPictureDetailsRow.Cells[2];

							//    throw new FormatException("Could not find Three Year Picture (What does it look like).");
							//var

							//var threeYearTop = threeYearCell.Paragraphs.Where(x => x.StyleName != "ListParagraph").ToList();

							//3 year picture - Headings
							try {
								threeYearFuture = ParseVtoHeader(threeYearCell, "Future Date");
								threeYearRevenue = ParseVtoHeader(threeYearCell, "Revenue");
								threeYearProfit = ParseVtoHeader(threeYearCell, "Profit");
								threeYearMeasurables = ParseVtoHeader(threeYearCell, "Measurables");
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add Three Year Picture heading", e));
							}
							try {
								if (threeYearCell.Lists.Count != 0) {
									threeYearLooksList = threeYearCell.Lists.Last().Items.Select(x => x.Text.Trim()).ToList();
								}
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add Three Year Picture heading", e));
							}
						} catch (Exception e) {
							exceptions.Add(e);
						}
						try {
							//Core Focus
							if (coreFocusRow.Cells.Count != 3 || coreFocusRow.Cells[0].FillColor.Name != "bfbfbf")
								throw new FormatException("Could not find Core Focus.");
							if (coreFocusRow.Cells[0].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(coreFocusRow.Cells[0].Paragraphs[0].Text))
								coreFocusTitle = coreFocusRow.Cells[0].Paragraphs[0].Text;
							var coreFocusCell = coreFocusRow.Cells[1];

							var nicheParagraphTuple = coreFocusCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("niche")).FirstOrDefault();

							if (nicheParagraphTuple == null && coreFocusCell.Paragraphs.Count == 2 && coreFocusCell.Paragraphs[0].MagicText.Count > 0) {
								purposeTitle = coreFocusCell.Paragraphs[0].MagicText[0].text;
								purpose = string.Join("", coreFocusCell.Paragraphs[0].MagicText.Skip(1).Select(x => x.text)).TrimStart(':').Trim();
								niche = string.Join("", coreFocusCell.Paragraphs[1].MagicText.Skip(1).Select(x => x.text)).TrimStart(':').Trim();
							} else {
								var purposeParagraphs = coreFocusCell.Paragraphs.Where((x, i) => i < nicheParagraphTuple.Item1).SelectMany(x => x.MagicText).ToList();
								var nicheParagraphs = coreFocusCell.Paragraphs.Where((x, i) => i >= nicheParagraphTuple.Item1).SelectMany(x => x.MagicText).ToList();

								purpose = string.Join("", purposeParagraphs.Skip(1).Select(x => x.text)).TrimStart(':').Trim();
								niche = string.Join("", nicheParagraphs.Skip(1).Select(x => x.text)).TrimStart(':').Trim();

								if (purposeParagraphs.Count > 0)
									purposeTitle = purposeParagraphs[0].text;
							}
						} catch (Exception e) {
							exceptions.Add(new FormatException("Could not add Core Focus.", e));
						}

						try {
							//10 year target
							if (tenYearRow.Cells.Count != 3 || tenYearRow.Cells[0].FillColor.Name != "bfbfbf")
								throw new FormatException("Could not find Ten Year Target.");
							if (tenYearRow.Cells[0].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(tenYearRow.Cells[0].Paragraphs[0].Text))
								tenYearTargetTitle = tenYearRow.Cells[0].Paragraphs[0].Text;
							tenYearCell = tenYearRow.Cells[1];
						} catch (Exception e) {
							exceptions.Add(new FormatException("Could not add Ten Year Target.", e));
						}

						try {
							//Marketing Strategy
							if (marketingStrategyRow.Cells.Count != 3 || marketingStrategyRow.Cells[0].FillColor.Name != "bfbfbf")
								throw new FormatException("Could not find Marketing Strategy.");
							if ((marketingStrategyRow.Cells[0].Paragraphs.Count == 1 || marketingStrategyRow.Cells[0].Paragraphs.Count == 2) && !string.IsNullOrWhiteSpace(string.Join(" ", marketingStrategyRow.Cells[0].Paragraphs.Select(x => x.Text))))
								marketingStrategyTitle = string.Join(" ", marketingStrategyRow.Cells[0].Paragraphs.Select(x => x.Text));
							var marketingStrategyCell = marketingStrategyRow.Cells[1];


							var targetTuple = Tuple.Create("target", marketingStrategyCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("target market") || x.Item2.Text.Contains("The List")).FirstOrDefault());
							var uniquesTuple = Tuple.Create("uniques", marketingStrategyCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("uniques")).FirstOrDefault());
							var provenTuple = Tuple.Create("proven", marketingStrategyCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("proven")).FirstOrDefault());
							var guaranteeTuple = Tuple.Create("guarantee", marketingStrategyCell.Paragraphs.Select((x, i) => Tuple.Create(i, x)).Where(x => x.Item2.Text.ToLower().Contains("guarantee")).FirstOrDefault());

							// <name, <location, paragraph>>
							var marketStratList = new List<Tuple<string, Tuple<int, Paragraph>>>() { targetTuple, uniquesTuple, provenTuple, guaranteeTuple };

							var ordering = marketStratList.Where(x => x.Item2 != null).OrderBy(x => x.Item2.Item1).ToList().Where(x => x.Item2.Item2 != null).ToList();


							if (ordering.Any()) {
								for (var i = 0; i < ordering.Count; i++) {
									var start = ordering[i].Item2.Item1;
									var end = 0;
									if (i != ordering.Count - 1)
										end = ordering[i + 1].Item2.Item1;
									else
										end = marketingStrategyCell.Paragraphs.Count;
									//Grab this section's paragraphs
									//merge the magic text together, skip the first one (usually the title)
									var sectionTitle = ordering[i].Item1;
									marketingDict[sectionTitle] = string.Join("", marketingStrategyCell.Paragraphs.Where((x, j) => start <= j && j < end).SelectMany(x => x.MagicText).Skip(1).Select(x => x.text));
								}
							}
							if (marketingStrategyCell.Lists.Count == 1)
								uniques = marketingStrategyCell.Lists[0].Items.Select(x => x.Text).ToList();
							else if (marketingStrategyCell.Lists.Count > 1) {
								var uniquesHeadingLoc = marketingStrategyCell.Xml.Value.IndexOf(uniquesTuple.Item2.Item2.Xml.Value);
								uniques = marketingStrategyCell.Lists.FirstOrDefault(x => marketingStrategyCell.Xml.Value.IndexOf(x.Xml.Value) > uniquesHeadingLoc).NotNull(y => y.Items.Select(x => x.Text).ToList()) ?? uniques;
							}
						} catch (Exception e) {
							exceptions.Add(new FormatException("Could not add Marketing Strategy.", e));
						}
					} catch (Exception e) {
						exceptions.Add(e);
					}


					#endregion
					#region Page 2
					try {
						var page2 = doc.Tables[1];

						if (page2.Rows.Count != 2)
							throw new FormatException("Could not find Traction Page.");

						var headingsRow = page2.Rows[0];
						var tractionRow = page2.Rows[1];

						if (headingsRow.Cells.Count != 3 || headingsRow.Cells.Any(x => x.FillColor.Name != "bfbfbf"))
							throw new FormatException("Could not find Traction Page headings.");
						if (tractionRow.Cells.Count != 3)
							throw new FormatException("Could not find Traction Page data.");

						if (headingsRow.Cells[0].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(headingsRow.Cells[0].Paragraphs[0].Text))
							oneYearTitle = headingsRow.Cells[0].Paragraphs[0].Text;
						if (headingsRow.Cells[1].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(headingsRow.Cells[1].Paragraphs[0].Text))
							rocksTitle = headingsRow.Cells[1].Paragraphs[0].Text;
						if (headingsRow.Cells[2].Paragraphs.Count == 1 && !string.IsNullOrWhiteSpace(headingsRow.Cells[2].Paragraphs[0].Text))
							issuesTitle = headingsRow.Cells[2].Paragraphs[0].Text;

						//One Year Plan
						try {
							var oneYearPlanCell = tractionRow.Cells[0];
							if (oneYearPlanCell.Tables.Count != 1 || oneYearPlanCell.Tables[0].ColumnCount > 2)
								throw new FormatException("Could not find One Year Plan goals.");
							//One year target - Headings
							try {
								oneYearFuture = ParseVtoHeader(oneYearPlanCell, "Future Date");
								oneYearRevenue = ParseVtoHeader(oneYearPlanCell, "Revenue");
								oneYearProfit = ParseVtoHeader(oneYearPlanCell, "Profit");
								oneYearMeasurables = ParseVtoHeader(oneYearPlanCell, "Measurables");
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add One Year Goals heading.", e));
							}

							try {
								oneYearPlanGoals = oneYearPlanCell.Tables[0].Rows
									.Select(x => string.Join("\n", x.Cells.Last().Paragraphs.Select(y => y.Text)))
									.Where(x => !string.IsNullOrWhiteSpace(x))
									.ToList();
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add One Year Goals.", e));
							}

						} catch (Exception e) {
							exceptions.Add(e);
						}

						//Rocks
						try {
							var rocksCell = tractionRow.Cells[1];

							try {
								//One year target - Headings
								rocksFuture = ParseVtoHeader(rocksCell, "Future Date");
								rocksRevenue = ParseVtoHeader(rocksCell, "Revenue");
								rocksProfit = ParseVtoHeader(rocksCell, "Profit");
								rocksMeasurables = ParseVtoHeader(rocksCell, "Measurables");
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add Rocks heading.", e));
							}
							try {
								var bestTable = rocksCell.Tables.OrderByDescending(x => x.ColumnCount).Where(x => x.ColumnCount <= 3).FirstOrDefault();

								if (bestTable == null)
									throw new FormatException("Could not find Rocks list.");
								//if (rocksCell.Tables.Count != 1 || rocksCell.Tables[0].ColumnCount > 3)
								rocksList = bestTable.Rows;
							} catch (Exception e) {
								exceptions.Add(new FormatException("Could not add Rocks.", e));
							}
						} catch (Exception e) {
							exceptions.Add(e);
						}
						//Issues List
						try {
							var issuesCell = tractionRow.Cells[2];
							if (issuesCell.Tables.Count != 1 || issuesCell.Tables[0].ColumnCount > 2)
								throw new FormatException("Could not find Issues List.");
							issuesList = issuesCell.Tables[0].Rows
								.Select(x => string.Join("\n", x.Cells.Last().Paragraphs.Select(y => y.Text.Trim())))
								.Where(x => !string.IsNullOrWhiteSpace(x))
								.ToList();
						} catch (Exception e) {
							exceptions.Add(new FormatException("Could not add Issues List.", e));
						}
						#endregion
					} catch (Exception e) {
						exceptions.Add(e);
					}
					#region Update VTO
					//Headings
					vto.CoreValueTitle = corevaluesTitle;
					vto.CoreFocus.CoreFocusTitle = coreFocusTitle;
					vto.TenYearTargetTitle = tenYearTargetTitle;
					vto.MarketingStrategy.MarketingStrategyTitle = marketingStrategyTitle;
					vto.ThreeYearPicture.ThreeYearPictureTitle = threeYearTitle;

					vto.OneYearPlan.OneYearPlanTitle = oneYearTitle;
					vto.QuarterlyRocks.RocksTitle = rocksTitle;
					vto.IssuesListTitle = issuesTitle;


					//Core Values
					var organizationId = vto.Organization.Id;
					var existing = OrganizationAccessor.GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, null);
					foreach (var cv in corevaluesList) {
						existing.Add(new CompanyValueModel() { OrganizationId = organizationId, CompanyValue = cv });
					}
					OrganizationAccessor.EditCompanyValues(s, perms, organizationId, existing);

#pragma warning disable CS0219 // Variable is assigned but its value is never used
					var currencyStyle = NumberStyles.AllowCurrencySymbol | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowParentheses | NumberStyles.AllowThousands | NumberStyles.AllowTrailingWhite | NumberStyles.Currency;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
					var currentCulture = Thread.CurrentThread.CurrentCulture;
					//Three Year Picture
					vto.ThreeYearPicture.FutureDate = threeYearFuture.TryParseDateTime();
					vto.ThreeYearPicture.RevenueStr = threeYearRevenue;//.TryParseDecimal(currencyStyle, currentCulture);
					vto.ThreeYearPicture.ProfitStr = threeYearProfit;//.TryParseDecimal(currencyStyle, currentCulture);
					vto.ThreeYearPicture.Measurables = threeYearMeasurables;

					foreach (var t in threeYearLooksList) {
						VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_LookLike, null, skipUpdate: true, value: t);
					}

					//Core Focus 
					vto.CoreFocus.Niche = niche;
					vto.CoreFocus.Purpose = purpose;
					vto.CoreFocus.PurposeTitle = purposeTitle;


					//Ten Year Target
					if (tenYearCell != null) {
						vto.TenYearTarget = string.Join("\n", tenYearCell.Paragraphs.Select(x => x.Text));
					}

					//Marketing Strategy 

					vto.MarketingStrategy.TargetMarket = marketingDict["target"];
					vto.MarketingStrategy.ProvenProcess = marketingDict["proven"];
					vto.MarketingStrategy.Guarantee = marketingDict["guarantee"];

					foreach (var t in uniques) {
						VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_Uniques, null, skipUpdate: true, value: t);
					}

					//One Year Plan
					vto.OneYearPlan.FutureDate = oneYearFuture.TryParseDateTime();
					vto.OneYearPlan.RevenueStr = oneYearRevenue;//.TryParseDecimal(currencyStyle, currentCulture);
					vto.OneYearPlan.ProfitStr = oneYearProfit;//.TryParseDecimal(currencyStyle, currentCulture);
					vto.OneYearPlan.Measurables = oneYearMeasurables;

					foreach (var t in oneYearPlanGoals) {
						VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_YearGoals, null, skipUpdate: true, value: t);
					}

					//Rocks
					vto.QuarterlyRocks.FutureDate = rocksFuture.TryParseDateTime();
					vto.QuarterlyRocks.RevenueStr = rocksRevenue;//.TryParseDecimal(currencyStyle, currentCulture);
					vto.QuarterlyRocks.ProfitStr = rocksProfit;//.TryParseDecimal(currencyStyle, currentCulture);
					vto.QuarterlyRocks.Measurables = rocksMeasurables;

					var allUsers = TinyUserAccessor.GetOrganizationMembers(s, perms, vto.Organization.Id);
					Dictionary<string, DiscreteDistribution<TinyUser>> rockUserLookup = null;
					if (rocksList.Any() && (rocksList[0].ColumnCount == 3 || rocksList[0].ColumnCount == 2)) {
						var rockUsers = rocksList.Select(x => string.Join("\n", x.Cells.Last().Paragraphs.Select(y => y.Text)));
						rockUserLookup = DistanceUtility.TryMatch(rockUsers, allUsers);
					}

					try {
						foreach (var r in rocksList) {
							var owner = caller.Id;
							if (r.ColumnCount == 2 || r.ColumnCount == 3) {
								var ownerTup = new TinyUser() {
									FirstName = "",
									LastName = "",
									UserOrgId = owner
								};
								rockUserLookup[string.Join("\n", r.Cells.Last().Paragraphs.Select(y => y.Text))].TryResolveOne(ref ownerTup);
							}

							var message = r.Cells.Reverse<Cell>().Skip(1).FirstOrDefault().NotNull(x => string.Join("\n", x.Paragraphs.Select(y => y.Text)));
							if (!string.IsNullOrWhiteSpace(message)) {
								CreateNewRock(s, perms, vtoId, owner, message);
							}
						}
					} catch (Exception e) {
						exceptions.Add(new FormatException("Could not upload Rocks.", e));
					}

					//Issues
					foreach (var i in issuesList) {
						VtoAccessor.AddString(s, perms, vtoId, VtoItemType.List_Issues, null, skipUpdate: true, value: i);
					}
					#endregion

					tx.Commit();
					s.Flush();

					return vto;
				}
			}


		}
	}
}