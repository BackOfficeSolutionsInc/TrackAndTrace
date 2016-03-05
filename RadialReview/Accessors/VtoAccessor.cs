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

namespace RadialReview.Accessors
{
    public class VtoAccessor : BaseAccessor
    {

        public static void UpdateAllVTOs(ISession s, long organizationId, Action<dynamic> action)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
            var vtoIds = s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>();
            foreach (var vtoId in vtoIds)
            {
                var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId));
                action(group);
            }
        }

        public static void UpdateVTO(ISession s, long vtoId, Action<dynamic> action)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
            var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId));
            action(group);
        }

        public static List<VtoModel> GetAllVTOForOrganization(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

                    return s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
                }
            }
        }

        public static VtoModel GetVTO(UserOrganizationModel caller, long vtoId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetVTO(s, perms, vtoId);
                }
            }
        }


        public static VtoModel GetVTO(ISession s, PermissionsUtility perms, long vtoId)
        {
            perms.ViewVTO(vtoId);
            var model = s.Get<VtoModel>(vtoId);
            model._Values = OrganizationAccessor.GetCompanyValues(s.ToQueryProvider(true), perms, model.Organization.Id, null);
            model.MarketingStrategy._Uniques = s.QueryOver<VtoModel.VtoItem_String>().Where(x => x.Type == VtoItemType.List_Uniques && x.Vto.Id == vtoId && x.DeleteTime == null).List().ToList();
            model.ThreeYearPicture._LooksLike = s.QueryOver<VtoModel.VtoItem_String>().Where(x => x.Type == VtoItemType.List_LookLike && x.Vto.Id == vtoId && x.DeleteTime == null).List().ToList();
            model.OneYearPlan._GoalsForYear = s.QueryOver<VtoModel.VtoItem_String>().Where(x => x.Type == VtoItemType.List_YearGoals && x.Vto.Id == vtoId && x.DeleteTime == null).List().ToList();
            //model.._GoalsForYear = s.QueryOver<VtoModel.VtoItem_String>().Where(x => x.Type == VtoItemType.List_Issues && x.Vto.Id == vtoId && x.DeleteTime == null).List().ToList();
            model.QuarterlyRocks._Rocks = s.QueryOver<VtoModel.Vto_Rocks>().Where(x => x.Vto.Id == vtoId && x.DeleteTime == null).List().ToList();
            model._Issues = s.QueryOver<VtoModel.VtoItem_String>().Where(x => x.Type == VtoItemType.List_Issues && x.Vto.Id == vtoId && x.DeleteTime == null).List().ToList();
            return model;
        }

        public static AngularVTO GetAngularVTO(UserOrganizationModel caller, long vtoId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    var vto = GetVTO(s, perms, vtoId);

                    var ang = AngularVTO.Create(vto);
                    return ang;
                }
            }
        }
        public static VtoModel CreateRecurrenceVTO(ISession s, PermissionsUtility perm, long recurrenceId)
        {
            perm.EditL10Recurrence(recurrenceId);//.CreateVTO(organizationId);
            var recurrence = s.Get<L10Recurrence>(recurrenceId);
            perm.ViewOrganization(recurrence.OrganizationId);

            var model = new VtoModel();
            model.Organization = s.Get<OrganizationModel>(recurrence.OrganizationId);
            s.Save(model.MarketingStrategy);
            s.Save(model.CoreFocus);
            s.Save(model.ThreeYearPicture);
            s.Save(model.OneYearPlan);
            s.Save(model.QuarterlyRocks);
            s.SaveOrUpdate(model);

            model.CoreFocus.Vto = model;
            model.MarketingStrategy.Vto = model;
            model.OneYearPlan.Vto = model;
            model.QuarterlyRocks.Vto = model;
            model.ThreeYearPicture.Vto = model;
            model.L10Recurrence = recurrenceId;

            model.Name = recurrence.Name;

            s.Update(model);

            recurrence.VtoId = model.Id;
            s.Update(recurrence);
            return model;
        }

        public static VtoModel CreateVTO(ISession s, PermissionsUtility perm, long organizationId)
        {
            perm.ViewOrganization(organizationId).CreateVTO(organizationId);

            var model = new VtoModel();
            model.Organization = s.Get<OrganizationModel>(organizationId);
            s.Save(model.MarketingStrategy);
            //s.Save(model.OrganizationWide);
            s.Save(model.CoreFocus);
            s.Save(model.ThreeYearPicture);
            s.Save(model.OneYearPlan);
            s.Save(model.QuarterlyRocks);

            s.SaveOrUpdate(model);

            //model.Name.Vto = model;
            //s.Update(model.Name);

            //model.OrganizationWide.Vto = model;
            //s.Update(model.OrganizationWide);

            model.CoreFocus.Vto = model;
            //model.CoreFocus.Niche.Vto = model;
            //model.CoreFocus.Purpose.Vto = model;

            //s.Update(model.CoreFocus);

            model.MarketingStrategy.Vto = model;
            //model.MarketingStrategy.Guarantee.Vto = model;
            //model.MarketingStrategy.ProvenProcess.Vto = model;
            //model.MarketingStrategy.TargetMarket.Vto = model;
            //model.MarketingStrategy.TenYearTarget.Vto = model;


            model.OneYearPlan.Vto = model;
            //model.OneYearPlan.FutureDate.Vto = model;
            //model.OneYearPlan.Measurables.Vto = model;
            //model.OneYearPlan.Profit.Vto = model;
            //model.OneYearPlan.Revenue.Vto = model;

            model.QuarterlyRocks.Vto = model;
            //model.QuarterlyRocks.Measurables.Vto = model;
            //model.QuarterlyRocks.Profit.Vto = model;
            //model.QuarterlyRocks.Revenue.Vto = model;

            model.ThreeYearPicture.Vto = model;
            //model.ThreeYearPicture.FutureDate.Vto = model;
            //model.ThreeYearPicture.Measurables.Vto = model;
            //model.ThreeYearPicture.Profit.Vto = model;
            //model.ThreeYearPicture.Revenue.Vto = model;

            s.Update(model);
            return model;
        }
        public static VtoModel CreateVTO(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perm = PermissionsUtility.Create(s, caller);

                    var model = CreateVTO(s, perm, organizationId);

                    tx.Commit();
                    s.Flush();

                    return model;
                }
            }
        }

        public static void UpdateVtoString(UserOrganizationModel caller, long vtoStringId, String message, bool? deleted, string connectionId = null)
        {
            long? update_VtoId = null;
            VtoModel.VtoItem_String str = null;
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    str = s.Get<VtoModel.VtoItem_String>(vtoStringId);
                    var perm = PermissionsUtility.Create(s, caller).EditVTO(str.Vto.Id);
                    //if (str.Data != message)
                    //{
                    /*newstr = new VtoModel.VtoItem_String(){
                        BaseId = str.BaseId,
                        CopiedFrom = str.Id,
                        Data = message,
                        Ordering = str.Ordering,
                        Type = str.Type,
                        Vto = str.Vto,
                    };*/
                    str.Data = message;
                    if (str.BaseId == 0)
                        str.BaseId = str.Id;

                    if (deleted != null)
                    {
                        if (deleted == true && str.DeleteTime == null)
                        {
                            str.DeleteTime = DateTime.UtcNow;
                            connectionId = null;
                        }
                        else if (deleted == false)
                        {
                            str.DeleteTime = null;
                        }
                    }

                    s.Update(str);
                    update_VtoId = str.Vto.Id;

                    //Update IssueRecurrence
                    if (str.ForModel != null)
                    {
                        if (str.ForModel.ModelType == ForModel.GetModelType<IssueModel.IssueModel_Recurrence>())
                        {
                            var issueRecur = s.Get<IssueModel.IssueModel_Recurrence>(str.ForModel.ModelId);
                            if (perm.IsPermitted(x => x.EditL10Recurrence(issueRecur.Recurrence.Id)))
                            {
                                issueRecur.Issue.Message = message;
                                //s.Update(issueRecur);
                                s.Update(issueRecur.Issue);
                            }
                        }
                    }

                    //}

                    tx.Commit();
                    s.Flush();
                }
            }
            if (update_VtoId != null)
            {
                var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
                var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(update_VtoId.Value), connectionId);
                str.Vto = null;
                group.update(new AngularUpdate(){
					AngularVtoString.Create(str)
				});
            }
        }
        public static void UpdateVto(UserOrganizationModel caller, long vtoId, String name = null, String tenYearTarget = null, String tenYearTargetTitle = null, String coreValueTitle = null, String issuesListTitle = null, string connectionId = null)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
            var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);

            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditVTO(vtoId);
                    var vto = s.Get<VtoModel>(vtoId);

                    vto.Name = name;
                    vto.TenYearTarget = tenYearTarget;
                    vto.TenYearTargetTitle = tenYearTargetTitle;
                    vto.CoreValueTitle = coreValueTitle;
                    vto.IssuesListTitle = issuesListTitle;

                    s.Update(vto);

                    tx.Commit();
                    s.Flush();
                    group.update(new AngularVTO(vtoId)
                    {
                        Name = vto.Name,
                        TenYearTarget = vto.TenYearTarget,
                        TenYearTargetTitle = vto.TenYearTargetTitle,
                        CoreValueTitle = vto.CoreValueTitle,
                        IssuesListTitle = vto.IssuesListTitle
                    });
                }
            }
        }

        public static void Update(UserOrganizationModel caller, BaseAngular model, string connectionId)
        {
            if (model.Type == typeof(AngularVtoString).Name)
            {
                var m = (AngularVtoString)model;
                UpdateVtoString(caller, m.Id, m.Data, null, connectionId);
            }
            else if (model.Type == typeof(AngularVTO).Name)
            {
                var m = (AngularVTO)model;
                UpdateVto(caller, m.Id, m.Name, m.TenYearTarget, m.TenYearTargetTitle, m.CoreValueTitle, m.IssuesListTitle, connectionId);
            }
            else if (model.Type == typeof(AngularCompanyValue).Name)
            {
                var m = (AngularCompanyValue)model;
                UpdateCompanyValue(caller, m.Id, m.CompanyValue, m.CompanyValueDetails, null, connectionId);
            }
            else if (model.Type == typeof(AngularCoreFocus).Name)
            {
                var m = (AngularCoreFocus)model;
                UpdateCoreFocus(caller, m.Id, m.Purpose, m.Niche, m.PurposeTitle, m.CoreFocusTitle, connectionId);
            }
            else if (model.Type == typeof(AngularStrategy).Name)
            {
                var m = (AngularStrategy)model;
                UpdateStrategy(caller, m.Id, m.TargetMarket, m.ProvenProcess, m.Guarantee, m.MarketingStrategyTitle, connectionId);
            }
            else if (model.Type == typeof(AngularVtoRock).Name)
            {
                var m = (AngularVtoRock)model;
                UpdateRock(caller, m.Id, m.Rock.Name, m.Rock.Owner.Id, null, connectionId);
            }
            else if (model.Type == typeof(AngularOneYearPlan).Name)
            {
                var m = (AngularOneYearPlan)model;
                UpdateOneYearPlan(caller, m.Id, m.FutureDate, m.Revenue, m.Profit, m.Measurables, m.OneYearPlanTitle, connectionId);
            }
            else if (model.Type == typeof(AngularQuarterlyRocks).Name)
            {
                var m = (AngularQuarterlyRocks)model;
                UpdateQuarterlyRocks(caller, m.Id, m.FutureDate, m.Revenue, m.Profit, m.Measurables, m.RocksTitle, connectionId);
            }
            else if (model.Type == typeof(AngularThreeYearPicture).Name)
            {
                var m = (AngularThreeYearPicture)model;
                UpdateThreeYearPicture(caller, m.Id, m.FutureDate, m.Revenue, m.Profit, m.Measurables, m.ThreeYearPictureTitle, connectionId);
            }


        }

        public static void UpdateThreeYearPicture(UserOrganizationModel caller, long id, DateTime? futuredate = null, decimal? revenue = null, decimal? profit = null, string measurables = null, string threeYearPictureTitle = null, string connectionId = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    var threeYear = s.Get<VtoModel.ThreeYearPictureModel>(id);
                    var vtoId = threeYear.Vto.Id;

                    var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
                    var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);

                    PermissionsUtility.Create(s, caller).EditVTO(vtoId);

                    threeYear.FutureDate = futuredate;
                    threeYear.Revenue = revenue;
                    threeYear.Profit = profit;
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
        public static void UpdateQuarterlyRocks(UserOrganizationModel caller, long id, DateTime? futuredate = null, decimal? revenue = null, decimal? profit = null, string measurables = null, string rocksTitle = null, string connectionId = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    var quarterlyRocks = s.Get<VtoModel.QuarterlyRocksModel>(id);
                    var vtoId = quarterlyRocks.Vto.Id;

                    var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
                    var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);

                    PermissionsUtility.Create(s, caller).EditVTO(vtoId);

                    quarterlyRocks.FutureDate = futuredate;
                    quarterlyRocks.Revenue = revenue;
                    quarterlyRocks.Profit = profit;
                    quarterlyRocks.Measurables = measurables;
                    quarterlyRocks.RocksTitle = rocksTitle;
                    s.Update(quarterlyRocks);

                    tx.Commit();
                    s.Flush();
                    group.update(new AngularUpdate(){new AngularQuarterlyRocks(id) {
						FutureDate = futuredate,
						Revenue = revenue,
						Profit = profit,
						Measurables = measurables,
                        RocksTitle=rocksTitle,
					}});
                }
            }
        }

        public static void UpdateOneYearPlan(UserOrganizationModel caller, long id, DateTime? futuredate = null, decimal? revenue = null, decimal? profit = null, string measurables = null, string oneYearPlanTitle = null, string connectionId = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    var plan = s.Get<VtoModel.OneYearPlanModel>(id);
                    var vtoId = plan.Vto.Id;

                    var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
                    var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);

                    PermissionsUtility.Create(s, caller).EditVTO(vtoId);

                    plan.FutureDate = futuredate;
                    plan.Revenue = revenue;
                    plan.Profit = profit;
                    plan.Measurables = measurables;
                    plan.OneYearPlanTitle = oneYearPlanTitle;
                    s.Update(plan);

                    tx.Commit();
                    s.Flush();
                    group.update(new AngularUpdate(){new AngularOneYearPlan(id) {
						FutureDate = futuredate,
						Revenue = revenue,
						Profit = profit,
						Measurables = measurables,
                        OneYearPlanTitle=oneYearPlanTitle
					}});
                }
            }
        }

        public static void UpdateStrategy(UserOrganizationModel caller, long strategyId, String targetMarket = null, String provenProcess = null, String guarantee = null, String marketingStrategyTitle = null, string connectionId = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    var strategy = s.Get<VtoModel.MarketingStrategyModel>(strategyId);
                    var vtoId = strategy.Vto.Id;

                    var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
                    var group = hub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoId), connectionId);

                    PermissionsUtility.Create(s, caller).EditVTO(vtoId);

                    strategy.ProvenProcess = provenProcess;
                    strategy.Guarantee = guarantee;
                    strategy.TargetMarket = targetMarket;
                    strategy.MarketingStrategyTitle = marketingStrategyTitle;
                    s.Update(strategy);

                    tx.Commit();
                    s.Flush();
                    group.update(new AngularUpdate(){new AngularStrategy(strategyId) {
						ProvenProcess = provenProcess,
						Guarantee = guarantee,
						TargetMarket = targetMarket,
                        MarketingStrategyTitle=marketingStrategyTitle,
					}});
                }
            }
        }

        public static void UpdateCoreFocus(UserOrganizationModel caller, long coreFocusId, string purpose, string niche, string purposeTitle, string coreFocusTitle, string connectionId)
        {
            //var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var coreFocus = s.Get<VtoModel.CoreFocusModel>(coreFocusId);
                    PermissionsUtility.Create(s, caller).EditVTO(coreFocus.Vto.Id);

                    coreFocus.Purpose = purpose;
                    coreFocus.Niche = niche;
                    coreFocus.PurposeTitle = purposeTitle;
                    coreFocus.CoreFocusTitle = coreFocusTitle;
                    s.Update(coreFocus);

                    var update = new AngularUpdate() { AngularCoreFocus.Create(coreFocus) };
                    UpdateVTO(s, coreFocus.Vto.Id, x => x.update(update));
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static void UpdateCompanyValue(UserOrganizationModel caller, long companyValueId, string message, string details, bool? deleted, string connectionId)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var companyValue = s.Get<CompanyValueModel>(companyValueId);
                    PermissionsUtility.Create(s, caller).EditCompanyValues(companyValue.OrganizationId);

                    if (message != null)
                    {
                        companyValue.CompanyValue = message;
                        s.Update(companyValue);
                    }

                    if (details != null)
                    {
                        companyValue.CompanyValueDetails = details;
                        s.Update(companyValue);
                    }

                    if (deleted != null)
                    {
                        if (deleted == false)
                            companyValue.DeleteTime = null;
                        else if (companyValue.DeleteTime == null)
                        {
                            companyValue.DeleteTime = DateTime.UtcNow;
                        }
                        s.Update(companyValue);
                    }
                    var update = new AngularUpdate();
                    update.Add(AngularCompanyValue.Create(companyValue));
                    UpdateAllVTOs(s, companyValue.OrganizationId, x => x.update(update));

                    tx.Commit();
                    s.Flush();

                }
            }
        }
        public static void UpdateRockAccountable(UserOrganizationModel caller, long rockId, long accountableUser)
        {
            //var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var rock = s.Get<VtoModel.Vto_Rocks>(rockId);
                    PermissionsUtility.Create(s, caller).EditVTO(rock.Vto.Id).ViewUserOrganization(accountableUser, false);

                    rock.Rock.AccountableUser = s.Get<UserOrganizationModel>(accountableUser);
                    rock.Rock.ForUserId = accountableUser;

                    var a = rock.Rock.AccountableUser.GetName();
                    var b = rock.Rock.AccountableUser.GetImageUrl();

                    s.Update(rock.Rock);
                    tx.Commit();
                    s.Flush();

                    var update = new AngularUpdate() { AngularVtoRock.Create(rock), new AngularRock(rock.Rock), };
                    UpdateVTO(s, rock.Vto.Id, x => x.update(update));
                }
            }
        }

        public static void UpdateRock(UserOrganizationModel caller, long rockId, string message, long? accountableUser, bool? deleted, string connectionId)
        {
            //var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var rock = s.Get<VtoModel.Vto_Rocks>(rockId);
                    var perm = PermissionsUtility.Create(s, caller).EditVTO(rock.Vto.Id);

                    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();

                    if (deleted != null)
                    {
                        var vto = s.Get<VtoModel>(rock.Vto.Id);


                        if (deleted == false)
                        {
                            rock.DeleteTime = null;
                            //rock.Rock.DeleteTime = null;
                            if (vto.L10Recurrence != null)
                                L10Accessor.AddRock(s, perm, vto.L10Recurrence.Value, rock.Rock);

                        }
                        else if (rock.DeleteTime == null)
                        {
                            rock.DeleteTime = DateTime.UtcNow;
                            //rock.Rock.DeleteTime = rock.DeleteTime;
                            if (vto.L10Recurrence != null)
                            {
                                var recurRocks1 = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.ForRock.Id == rock.Rock.Id && x.L10Recurrence.Id == vto.L10Recurrence && x.DeleteTime == null).List().ToList();

                                foreach (var r in recurRocks1){
                                    r.DeleteTime = rock.DeleteTime;
                                    s.Update(r);
                                    var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id));
                                    group.removeRock(r.ForRock.Id);
                                }

                                //Delete this meetings rocks
                                var m = L10Accessor._GetCurrentL10Meeting(s, perm, vto.L10Recurrence.Value, true, false, false);
                                if (m != null)
                                {
                                    var meetingRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>()
                                        .Where(x => x.ForRock.Id == rock.Rock.Id && x.L10Meeting.Id == m.Id && x.DeleteTime == null).List().ToList();

                                    foreach (var r in meetingRocks)
                                    {
                                        r.DeleteTime = rock.DeleteTime;
                                        s.Update(r);
                                    }
                                }

                                var recurRocks = L10Accessor.GetRocksForRecurrence(s, perm, vto.L10Recurrence.Value);
                                var arecur = new AngularRecurrence(vto.L10Recurrence.Value)
                                {
                                    Rocks = recurRocks.Select(x => new AngularRock(x.ForRock)).ToList(),
                                };
                                var group1 = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(vto.L10Recurrence.Value));
                                group1.update(new AngularUpdate() { arecur });
                            }
                        }
                    }
                    else
                    {
                        if (rock.Rock.Rock != message)
                        {
                            rock.Rock.Rock = message;
                            var recurRocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null && x.ForRock.Id == rock.Rock.Id).List().ToList();

                            foreach (var r in recurRocks)
                            {
                                var group = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(r.L10Recurrence.Id));
                                group.updateRockName(r.ForRock.Id, r.ForRock.Rock);                            
                                group.update(new AngularUpdate() { new AngularRock(rock.Rock.Id){ Name=message }});
                            }
                        }
                        if (accountableUser.HasValue)
                        {
                            perm.ViewUserOrganization(accountableUser.Value, false);
                            rock.Rock.AccountableUser = s.Get<UserOrganizationModel>(accountableUser.Value);
                        }
                    }

                    s.Update(rock);
                    s.Update(rock.Rock);

                    tx.Commit();
                    s.Flush();

                    var update = new AngularUpdate() { AngularVtoRock.Create(rock) };
                    UpdateVTO(s, rock.Vto.Id, x => x.update(update));


                }
            }
        }


        public static void JoinVto(UserOrganizationModel caller, long vtoId, string connectionId)
        {
            var hub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewVTO(vtoId);
                    hub.Groups.Add(connectionId, VtoHub.GenerateVtoGroupId(vtoId));
                    Audit.VtoLog(s, caller, vtoId, "JoinVto");
                }
            }
        }

        public static VtoModel.VtoItem_String AddString(ISession s, PermissionsUtility perms, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoString>, IAngularItem> updateFunc, bool skipUpdate = false, ForModel forModel = null, string value = null)
        {
            perms.EditVTO(vtoId);
            var vto = s.Get<VtoModel>(vtoId);
            var organizationId = vto.Organization.Id;

            var items = s.QueryOver<VtoModel.VtoItem_String>().Where(x => x.Vto.Id == vtoId && x.Type == type && x.DeleteTime == null).List().ToList();
            var count = items.Count();

            var str = new VtoModel.VtoItem_String()
            {
                Type = type,
                Ordering = count,
                Vto = vto,
                ForModel = forModel,
                Data = value
            };

            s.Save(str);

            items.Add(str);
            var angularItems = AngularList.Create(AngularListType.ReplaceAll, AngularVtoString.Create(items));


            if (skipUpdate)
                UpdateVTO(s, vtoId, x => x.update(updateFunc(vto, angularItems)));

            UpdateVTO(s, vtoId, x => x.update(new AngularUpdate() { updateFunc(vto, angularItems) }));
            return str;
        }

        public static void AddString(UserOrganizationModel caller, long vtoId, VtoItemType type, Func<VtoModel, BaseAngularList<AngularVtoString>, IAngularItem> updateFunc, bool skipUpdate = false, ForModel forModel = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    AddString(s, perms, vtoId, type, updateFunc, skipUpdate, forModel);
                    tx.Commit();
                    s.Flush();
                }
            }
        }


        public static void AddUniques(UserOrganizationModel caller, long vtoId)
        {
            AddString(caller, vtoId, VtoItemType.List_Uniques, (vto, items) => new AngularStrategy(vto.MarketingStrategy.Id) { Uniques = items });
        }

        public static void AddThreeYear(UserOrganizationModel caller, long vtoId)
        {
            AddString(caller, vtoId, VtoItemType.List_LookLike, (vto, list) => new AngularThreeYearPicture(vto.ThreeYearPicture.Id) { LooksLike = list });
        }

        public static void AddYearGoal(UserOrganizationModel caller, long vtoId)
        {
            AddString(caller, vtoId, VtoItemType.List_YearGoals, (vto, list) => new AngularOneYearPlan(vto.OneYearPlan.Id) { GoalsForYear = list });
        }
        public static void AddIssue(UserOrganizationModel caller, long vtoId)
        {
            AddString(caller, vtoId, VtoItemType.List_Issues, (vto, list) => new AngularVTO(vto.Id) { Issues = list }, true);
        }

        public static void AddCompanyValue(UserOrganizationModel caller, long vtoId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
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

        public static void AddRock(ISession s, PermissionsUtility perms, long vtoId, RockModel rock, DateTime? nowTime = null)
        {
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

            var vtoRocks = s.QueryOver<VtoModel.Vto_Rocks>().Where(x => x.Vto.Id == vtoId).List().ToList();

            s.SaveOrUpdate(rock);
            var vtoRock = new VtoModel.Vto_Rocks
            {
                CreateTime = now,
                Rock = rock,
                Vto = vto,
                _Ordering = vtoRocks.Count(),

            };
            s.Save(vtoRock);

            if (vto.L10Recurrence != null && !rock._AddedToL10)
            {
                L10Accessor.AddRock(s, perms, vto.L10Recurrence.Value, rock, now);
            }

            vtoRocks.Add(vtoRock);


            var angularItems = AngularList.Create(AngularListType.ReplaceAll, AngularVtoRock.Create(vtoRocks));
            var update = new AngularUpdate() { new AngularQuarterlyRocks(vto.QuarterlyRocks.Id) { Rocks = angularItems } };
            UpdateVTO(s, vtoId, x => x.update(update));

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

        public static void CreateNewRock(UserOrganizationModel caller, long vtoId, string message = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    var now = DateTime.UtcNow;
                    var vto = s.Get<VtoModel>(vtoId);
                    var organizationId = vto.Organization.Id;
                    var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
                    var rock = new RockModel()
                    {
                        CreateTime = now,
                        OrganizationId = organizationId,
                        CompanyRock = false,
                        Category = category,
                        //Period = s.Load<PeriodModel>(vto.PeriodId),
                        PeriodId = vto.PeriodId,
                        OnlyAsk = AboutType.Self,
                        ForUserId = caller.Id,
                        AccountableUser = caller,
                        Rock = message,
                    };
                    AddRock(s, perms, vtoId, rock, now);

                    tx.Commit();
                    s.Flush();
                }
            }
        }
    }
}