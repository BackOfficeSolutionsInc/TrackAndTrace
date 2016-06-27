using NHibernate;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Onboard;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
    public class OnboardingAccessor : BaseAccessor {

        public static OnboardingUser GetOrCreate(ISession s, BaseController ctrl, string page = null,bool overrideDisable=false)
        {
            var request = ctrl.Request;
            var response = ctrl.Response;

            var cookie = request.Cookies["Onboarding"];
            if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value)) {
                var found = s.QueryOver<OnboardingUser>().Where(x => x.Guid == cookie.Value && x.DeleteTime == null).SingleOrDefault();
                if (found != null && found.DeleteTime == null) {
                    if (found.DisableEdit && !overrideDisable)
                        throw new PermissionsException("Organization already exists. Please login and try again.");
                    if (found.UserId != null) {
                        found._UserOrg = s.Get<UserOrganizationModel>(found.UserId);
                        found._User = found._UserOrg.User;
                    }

                    if (page != null) {
                        found.CurrentPage = page;
                        s.Update(found);
                    }
                    return found;
                }
            }

            var f = Create(s, ctrl);
            return f;
        }
        public static OnboardingUser GetOrCreate(BaseController ctrl, string page = null)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var f = GetOrCreate(s, ctrl, page);


                    tx.Commit();
                    s.Flush();
                    return f;
                }
            }

        }

        public static OnboardingUser Update(BaseController ctrl, Action<OnboardingUser> update, bool overrideDisable = false)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var found = GetOrCreate(s, ctrl,overrideDisable:overrideDisable);
                    update(found);
                    s.Update(found);
                    tx.Commit();
                    s.Flush();
                    return found;
                }
            }
        }

        public static OnboardingUser Create(ISession s, BaseController ctrl)
        {
            HttpRequestBase request = ctrl.Request;
            HttpResponseBase response = ctrl.Response;
            var u = new OnboardingUser() {
                Guid = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                CurrentPage = "TheBasics",
                UserAgent = request.UserAgent,
                Languages = string.Join(",", (request.UserLanguages ?? new string[] { }))
            };

            s.Save(u);

            HttpCookie appCookie = new HttpCookie("Onboarding");
            appCookie.Value = u.Guid;
            appCookie.Expires = DateTime.Now.AddDays(100);
            response.Cookies.Add(appCookie);

            return u;
        }


        public static UserModel TryActivateOrganization(OnboardingUser o)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    if (o.OrganizationId == null)
                        throw new PermissionsException("Could not activate organization. Organization id does not exist.");
                    var found = s.Get<OrganizationModel>(o.OrganizationId);
                    if (found == null)
                        throw new PermissionsException("Could not activate organization. Organization does not exist.");

                    if (found.DeleteTime == null) {
                        return null; // already activated
                    } else if (found.DeleteTime == new DateTime(1, 1, 1)) {
                        found.DeleteTime = null;
                        o.DisableEdit = true;
                        o.DeleteTime = DateTime.UtcNow;

                        s.Update(o);
                        s.Update(found);

                        var user = s.Get<UserOrganizationModel>(o.UserId).User;

                        tx.Commit();
                        s.Flush();
                        return user;
                    } else {
                        throw new PermissionsException("Could not activate organization. Organization was deleted.");
                    }
                }
            }

        }

        public static void TryUpdateUser(OnboardingUser o)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    try {
                        if (o.UserId == null)
                            return;
                        if (o.DisableEdit)
                            throw new PermissionsException("Organization already exists. Please login and try again.");

                        var u = s.Get<UserOrganizationModel>(o.UserId);
                        if (u == null)
                            return;

                        if (u.User != null) {
                            u.User.FirstName = o.FirstName;
                            u.User.LastName = o.LastName;
                        }

                        var p = u.Positions.FirstOrDefault();
                        if (p == null && o.Position != null && o.OrganizationId != null) {
                            var orgPos = new OrganizationPositionModel() {
                                Organization = s.Load<OrganizationModel>(o.OrganizationId),
                                CreatedBy = u.Id,
                                CustomName = o.Position,
                            };
                            s.Save(orgPos);
                            var posDur = new PositionDurationModel() {
                                UserId = u.Id,
                                Position = orgPos,
                                PromotedBy = u.Id,
                                Start = DateTime.UtcNow
                            };
                            u.Positions.Add(posDur);
                            s.Update(u);
                        } else if (p != null && o.Position != null) {
                            p.Position.CustomName = o.Position;
                            p.DeleteTime = null;
                            s.Update(p);
                        } else if (p != null && o.Position == null) {
                            p.DeleteTime = DateTime.UtcNow;
                            s.Update(p);
                        }
                        tx.Commit();
                        s.Flush();

                    } catch (Exception e) {
                        log.Error("Error updating user in get stated.", e);
                    }
                }
            }
        }

        public static void TryUpdateOrganizatoin(OnboardingUser o)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    try {
                        if (o.OrganizationId == null)
                            return;
                        if (o.DisableEdit)
                            throw new PermissionsException("Organization already exists. Please login and try again.");

                        var org = s.Get<OrganizationModel>(o.OrganizationId);
                        if (org == null)
                            return;

                        var organizationName = o.CompanyName;
                        if (!String.IsNullOrWhiteSpace(organizationName) && org.Name.Standard != organizationName) {
                            org.Name.UpdateDefault(organizationName);
                            var managers = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.Managers).List().FirstOrDefault();
                            if (managers != null) {
                                managers.Name = "Managers at " + organizationName;
                                s.Update(managers);
                            }
                            var allTeam = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.AllMembers).List().FirstOrDefault();
                            if (allTeam != null) {
                                allTeam.Name = organizationName;
                                s.Update(allTeam);
                            }
                        }

                        tx.Commit();
                        s.Flush();
                    } catch (Exception e) {
                        log.Error("Error updating organization in get stated.", e);
                    }
                }
            }
        }

       
    }
}