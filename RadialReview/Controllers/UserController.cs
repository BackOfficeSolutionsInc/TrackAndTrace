using RadialReview.Accessors;
using RadialReview.Engines;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{


    public class UserController : BaseController
    {

        #region User
        public class SaveUserModel
        {
            public class Tup
            {
                public long Id { get; set; }
                public string Value { get; set; }
                public string Type { get; set; }
            }

            public Tup[] toSave { get; set; }
            public long forUser { get; set; }
            public long OrganizationId { get; set; }
        }
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
            return View();
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Manage(long id)
        {
            var caller = GetUser()
                        .Hydrate()
                        .ManagingUsers(subordinates: true)
                        .Organization()
                        .Execute();

            var found = caller.AllSubordinates.FirstOrDefault(x => x.Id == id);
            if (found == null)
                throw new PermissionsException();
            return View(new ManagerUserViewModel()
            {
                MatchingQuestions = _QuestionAccessor.GetQuestionsForUser(caller, id).ToListAlive(),
                User = found,
                OrganizationId = caller.Organization.Id
            });
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Save(SaveUserModel save)
        {
            try
            {
                var user = GetUser();
                if (user == null)
                    return Json(new ResultObject(true, ExceptionStrings.DefaultPermissionsException));
                if (save.toSave == null)
                    return Json(ResultObject.Success("Nothing to update."));

                var questionsToEdit = save.toSave.Where(x => x.Type == "questionEnabled").ToList();
                var enabledQuestions = questionsToEdit.Where(x => x.Value == "true").Select(x => x.Id).ToList();
                var disabledQuestions = questionsToEdit.Where(x => x.Value == "false").Select(x => x.Id).ToList();
                _QuestionAccessor.SetQuestionsEnabled(user, save.forUser, enabledQuestions, disabledQuestions);

                return Json(ResultObject.Success("Updated user."));
            }
            catch (Exception e)
            {
                return Json(new ResultObject(e));
            }
        }

        [Access(AccessLevel.Manager)]
        public ActionResult AddModal()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var caller = GetUser().Hydrate().Organization().Execute();
            //var positions = _OrganizationAccessor.GetOrganizationPositions(caller, caller.Organization.Id);
            var e1 = sw.ElapsedMilliseconds;


            var orgPos = _OrganizationAccessor
                            .GetOrganizationPositions(GetUser(), GetUser().Organization.Id)
                            .OrderBy(x => x.CustomName)
                            .ToSelectList(x => x.CustomName, x => x.Id).ToList();
            if (caller.Organization.ManagersCanEditPositions || caller.ManagingOrganization)
            {
                orgPos.Add(new SelectListItem() { Value = "-1", Text = "<" + DisplayNameStrings.createNew + ">" });
            }

            var e2 = sw.ElapsedMilliseconds;
            var positions = _PositionAccessor
                                .AllPositions()
                                .ToSelectList(x => x.Name.Translate(), x => x.Id)
                                .ToList();

            var e3 = sw.ElapsedMilliseconds;
            var posModel = new UserPositionViewModel()
            {
                UserId = -1L,
                PositionId = -2L,
                OrgPositions = orgPos,
                Positions = positions,
                CustomPosition = null
            };

            List<SelectListItem> managers = new List<SelectListItem>();
            var strictHierarchy = caller.Organization.StrictHierarchy;
            if (!strictHierarchy)
                managers = _OrganizationAccessor.GetOrganizationManagers(GetUser(), GetUser().Organization.Id)
                                                .ToSelectList(x => x.GetName() + x.GetTitles(3, caller.Id).Surround(" (", ")"), x => x.Id)
                                                .ToList();

            var e4 = sw.ElapsedMilliseconds;
            if (caller.ManagingOrganization)
            {
                managers.Add(new SelectListItem() { Selected = false, Text = "[Manage Organization]", Value = "-3" });
            }

            var model = new CreateUserOrganizationViewModel()
            {
                Position = posModel,
                OrgId = caller.Organization.Id,
                StrictlyHierarchical = strictHierarchy,
                ManagerId = caller.Id,
                PotentialManagers = managers,
            };
            var e5 = sw.ElapsedMilliseconds;
            return PartialView(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult EditModal(long id)
        {
            var userId = id;
            var found = _UserAccessor.GetUserOrganization(GetUser(), userId, true, false);

            /*var strictHierarchy = GetUser().Organization.StrictHierarchy;
            List<SelectListItem> potentialManagers = new List<SelectListItem>();
            if (!strictHierarchy)
                potentialManagers=_OrganizationAccessor.GetOrganizationManagers(GetUser(),GetUser().Organization.Id)
                                                .ToSelectList(x=>x.GetName()+x.GetTitles(3, GetUser().Id).Surround(" (",")"), x=>x.Id)
                                                .ToList();
            if ()
            {
                potentialManagers.Add(new SelectListItem() { Selected = false, Text = "[Manage Organization]", Value = "-3" });
            }*/


            var managers = _UserAccessor.GetManagers(GetUser(), id);

            var model = new EditUserOrganizationViewModel()
            {
                IsManager = found.ManagerAtOrganization,
                //ManagerId = managers.FirstOrDefault().NotNull(x => x.Id),
                //PotentialManagers=potentialManagers,
                //StrictlyHierarchical = strictHierarchy,
                UserId = userId,
                CanSetManagingOrganization = GetUser().ManagingOrganization && userId != GetUser().Id,
                ManagingOrganization = found.ManagingOrganization
            };


            return PartialView("EditModal", model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult EditModal(EditUserOrganizationViewModel model)
        {
            _UserAccessor.EditUser(GetUser(), model.UserId, model.IsManager, model.ManagingOrganization);

            return Json(ResultObject.Success("User updated."));
        }

        #endregion

        #region Positions

        [Access(AccessLevel.Manager)]
        public ActionResult Positions(long id)
        {
            var userId = id;
            var user = _UserAccessor.GetUserOrganization(GetUser(), userId, false, false).Hydrate().PersonallyManaging(GetUser()).Execute();

            return View(user);
        }

        [Access(AccessLevel.Manager)]
        public JsonResult RemovePosition(long id)
        {
            _PositionAccessor.RemovePositionFromUser(GetUser(), id);
            return Json(ResultObject.Success("Removed position."), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult PositionModal(long userId, long id = 0)
        {
            var user = _UserAccessor.GetUserOrganization(GetUser(), userId, false, false);
            var pos = user.Positions.FirstOrDefault(x => x.Id == id);

            var orgPos = _OrganizationAccessor
                            .GetOrganizationPositions(GetUser(), GetUser().Organization.Id)
                            .OrderBy(x => x.CustomName)
                            .ToSelectList(x => x.CustomName, x => x.Id, id).ToList();
            if (GetUser().ManagingOrganization || GetUser().Organization.ManagersCanEditPositions)
            {
                orgPos.Add(new SelectListItem() { Value = "-1", Text = "<" + DisplayNameStrings.createNew + ">" });
            }

            var positions = _PositionAccessor
                                .AllPositions()
                                .ToSelectList(x => x.Name.Translate(), x => x.Id)
                                .ToList();

            var model = new UserPositionViewModel()
            {
                UserId = userId,
                PositionId = id,
                OrgPositions = orgPos,
                Positions = positions,
                CustomPosition = null
            };

            return PartialView(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult PositionModal(UserPositionViewModel model)
        {
            if (model.CustomPosition != null)
            {
                var orgPos = _OrganizationAccessor.EditOrganizationPosition(GetUser(), 0, GetUser().Organization.Id, model.CustomPositionId, model.CustomPosition);
                model.PositionId = orgPos.Id;
            }

            _PositionAccessor.AddPositionToUser(GetUser(), model.UserId, model.PositionId);

            return Json(ResultObject.Success("Added position."));
        }

        #endregion

        #region Teams
        [Access(AccessLevel.Manager)]
        public ActionResult Teams(long id)
        {
            var userId = id;
            var teams = _TeamAccessor.GetUsersTeams(GetUser(), userId);
            var user = _UserAccessor.GetUserOrganization(GetUser(), userId, false, false).Hydrate().SetTeams(teams).PersonallyManaging(GetUser()).Execute();


            return View(user);
        }

        [Access(AccessLevel.Manager)]
        public JsonResult RemoveTeam(long id)
        {
            _TeamAccessor.RemoveMember(GetUser(), id);
            return Json(ResultObject.Success("Removed team."), JsonRequestBehavior.AllowGet);
        }

        public class UserTeamViewModel
        {
            public long TeamId { get; set; }
            public long UserId { get; set; }
            public List<SelectListItem> OrgTeams { get; set; }
            public String CustomTeam { get; set; }
            public bool CustomOnlyManagersEdit { get; set; }
            public bool CustomSecret { get; set; }

            public UserTeamViewModel()
            {
                CustomOnlyManagersEdit = true;
                CustomTeam = null;
            }
        }

        [Access(AccessLevel.Manager)]
        public ActionResult TeamModal(long userId, long id = 0)
        {
            var teams = _TeamAccessor.GetUsersTeams(GetUser(), userId);
            var aliveTeams = teams.ToListAlive();
            var user = _UserAccessor.GetUserOrganization(GetUser(), userId, false, false).Hydrate().SetTeams(teams).Execute();
            var team = user.Teams.FirstOrDefault(x => x.Id == id);
            var orgTeam = _TeamAccessor.GetOrganizationTeams(GetUser(), GetUser().Organization.Id)
                            .Where(x => !aliveTeams.Any(y => y.Team.Id == x.Id))//_TeamAccessor.GetTeamsDirectlyManaged(GetUser(), GetUser().Id)
                            .Where(x => x.Type == TeamType.Standard)
                            .ToSelectList(x => x.Name, x => x.Id, id).ToList();
            orgTeam.Add(new SelectListItem() { Value = "-1", Text = "<" + DisplayNameStrings.createNew + ">" });

            var model = new UserTeamViewModel()
            {
                UserId = userId,
                TeamId = id,
                OrgTeams = orgTeam,
                CustomTeam = null
            };

            return PartialView(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult TeamModal(UserTeamViewModel model)
        {
            if (model.CustomTeam != null)
            {
                var orgTeam = _OrganizationAccessor.AddOrganizationTeam(GetUser(), GetUser().Organization.Id, model.CustomTeam, model.CustomOnlyManagersEdit, model.CustomSecret);
                model.TeamId = orgTeam.Id;
            }

            _TeamAccessor.AddMember(GetUser(), model.TeamId, model.UserId);

            return Json(ResultObject.Success("Added to team."));
        }
        #endregion

        #region Managers
        [Access(AccessLevel.Manager)]
        public ActionResult Managers(long id)
        {
            var user = _UserAccessor.GetUserOrganization(GetUser(), id, false, false).Hydrate().Managers().PersonallyManaging(GetUser()).Execute();

            return View(user);
        }

        public class AddManagerViewModel
        {
            public List<SelectListItem> PotentialManagers { get; set; }
            public long ManagerId { get; set; }
            public long UserId { get; set; }
        }

        [Access(AccessLevel.Manager)]
        public ActionResult ManagerModal(long id)
        {
            var potentialManagers = new List<UserOrganizationModel>();
            if (GetUser().Organization.StrictHierarchy || GetUser().ManagingOrganization)
                potentialManagers = _OrganizationAccessor.GetOrganizationManagers(GetUser(), GetUser().Organization.Id);
            else
                potentialManagers = _UserAccessor.GetSubordinates(GetUser(), GetUser().Id).Where(x => x.ManagerAtOrganization).ToListAlive();


            var selfId = GetUser().Id;
            var model = new AddManagerViewModel()
            {
                UserId = id,
                PotentialManagers = potentialManagers.ToSelectList(x => x.GetNameAndTitle(3, selfId), x => x.Id).ToList()
            };

            return PartialView(model);
        }

        [Access(AccessLevel.Manager)]
        public JsonResult DeleteManager(long id)
        {
            _UserAccessor.RemoveManager(GetUser(), id);
            return Json(ResultObject.Success("Removed manager."));
        }

        [Access(AccessLevel.Manager)]
        [HttpPost]
        public JsonResult AddManager(AddManagerViewModel model)
        {
            _UserAccessor.AddManager(GetUser(), model.UserId, model.ManagerId);
            return Json(ResultObject.Success("Added manager."));
        }
        #endregion

        [Access(AccessLevel.UserOrganization)]
        public ActionResult Details(long id)
        {
            var model = _UserEngine.GetUserDetails(GetUser(), id);

            return View(model);
        }

    }
}
