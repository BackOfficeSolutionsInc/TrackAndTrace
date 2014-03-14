using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models;
using RadialReview.Utilities;
using System.Threading.Tasks;

namespace RadialReview.Controllers
{
    public class NexusController : BaseController
    {
        public static NexusAccessor NexusAccessor = new NexusAccessor();
        public static OrganizationAccessor OrganizationAccessor = new OrganizationAccessor();

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult AddManagedUserToOrganization(CreateUserOrganizationViewModel model)
        {
            try
            {
                var user = GetUser().Hydrate().Organization().Execute();
                var org = user.Organization;
                if (org == null)
                    throw new PermissionsException();
                if (org.Id != model.OrgId)
                    throw new PermissionsException();

                if (model.Position.CustomPosition != null)
                {
                    var newPosition = OrganizationAccessor.EditOrganizationPosition(user, 0, user.Organization.Id, model.Position.CustomPositionId, model.Position.CustomPosition);
                    model.Position.PositionId = newPosition.Id;
                }

                var nexusId = NexusAccessor.JoinOrganizationUnderManager(user, model.ManagerId, model.IsManager, model.Position.PositionId, model.Email, model.FirstName, model.LastName);

                var message = "Successfully added " + model.FirstName + " " + model.LastName + ".";
                if (GetUser().Organization.SendEmailImmediately)
                {
                    message += " An invitation has been sent to " + model.Email + ".";
                    return Json(ResultObject.CreateMessage(StatusType.Success, message));
                }
                else
                {
                    message += " The invitation has NOT been sent. To send, click \"Send Invites\" below.";
                    return Json(ResultObject.CreateMessage(StatusType.Warning, message));
                }
            }
            catch (RedirectException e)
            {
                return Json(new ResultObject(e));
            }
            catch (Exception)
            {
                return Json(new ResultObject(true, ExceptionStrings.AnErrorOccuredContactUs));
            }
        }

        [Access(AccessLevel.Manager)]
        public JsonResult SendAllEmails()
        {
            var count = NexusAccessor.SendAllJoinEmails(GetUser(), GetUser().Organization.Id);
            return Json(ResultObject.Create(true, "Sent " + count + " email".Pluralize(count) + "."), JsonRequestBehavior.AllowGet);
        }

        private ActionResult MatchingNexus(NexusModel nexus,Func<ActionResult> otherwise)
        {
            try
            {
                if (GetUser().Id != nexus.ForUserId)
                    throw new Exception();
                return otherwise();
            }
            catch (Exception)
            {
                var u = _UserAccessor.GetUserOrganizationUnsafe(nexus.ForUserId);
                var username = u.GetUsername();
                try
                {
                    SignOut();
                    if (u.IsAttached())
                        return RedirectToAction("Login", "Account", new { returnUrl = Request.Url.AbsolutePath, username = username });
                    else
                        return RedirectToAction("Register", "Account", new { returnUrl = Request.Url.AbsolutePath });
                }
                catch (Exception)
                {
                    return RedirectToAction("Login", "Account");
                }
            }
        }
        
        [Access(AccessLevel.Any)]
        public async Task<ActionResult> Index(String id)
        {
            try
            {
                if (id == null)
                    throw new PermissionsException();
                var nexus = NexusAccessor.Get(id);
                switch (nexus.ActionCode)
                {
                    case NexusActions.JoinOrganizationUnderManager:
                        {
                            return RedirectToAction("Join", "Organization", new { id = id });
                        }
                    case NexusActions.TakeReview:
                        {
                            return MatchingNexus(nexus, () =>
                            {
                                NexusAccessor.Execute(nexus);
                                return RedirectToAction("Outstanding", "Reviews");
                            });
                        };
                    case NexusActions.ResetPassword:
                        {
                            SignOut();
                            return RedirectToAction("ResetPasswordWithToken", "Account", new { Id = id });
                        };
                    case NexusActions.Prereview:
                        {
                            return MatchingNexus(nexus, () =>{
                                NexusAccessor.Execute(nexus);
                                return RedirectToAction("Customize", "Prereview", new { id = nexus.GetArgs()[1] });
                            });
                        };
                    case NexusActions.CreateReview:
                        {
                            await Task.Run(() =>
                            {
                                var now=DateTime.UtcNow;
                                var admin = new UserOrganizationModel()
                                {
                                    IsRadialAdmin = true,
                                    Id = UserOrganizationModel.ADMIN_ID,
                                };
                                var reviewContainerId = nexus.GetArgs()[0].ToLong();
                                var whoReviewsWho = _PrereviewAccessor.GetAllMatchesForReview(admin, reviewContainerId);
                                //var prereview = _PrereviewAccessor.GetPrereview(admin, prereviewId);
                                var reviewContainer = _ReviewAccessor.GetReviewContainer(admin, reviewContainerId, false, false);
                                var organization = _OrganizationAccessor.GetOrganization(admin, reviewContainer.ForOrganizationId);

                                int sent, errors;
                                List<Exception> exceptions = new List<Exception>();
                                using (var s = HibernateSession.GetCurrentSession())
                                {
                                    using (var tx = s.BeginTransaction())
                                    {
                                        var perm = PermissionsUtility.Create(s, admin);
                                        _ReviewAccessor.CreateReviewFromPrereview(s.ToDataInteraction(true), perm, admin, reviewContainer, organization.GetName(), true, whoReviewsWho, out sent, out errors, ref exceptions);
                                        _PrereviewAccessor.UnsafeExecuteAllPrereviews(s, reviewContainerId, now);
                                        //Keep these:
                                        tx.Commit();
                                        s.Flush();
                                    }
                                }
                            });
                            return RedirectToAction("Index", "Home");
                        };
                }
            }
            catch (Exception)
            {
                ViewBag.Message = "There was an error in your request.";
                return RedirectToAction("Index", "Home");
            }
            log.Fatal("Nexus fall-through");
            return View();
        }
    }
}