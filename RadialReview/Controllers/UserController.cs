using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Json;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{


    public class UserController : BaseController
    {
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        private static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
        private static PositionAccessor _PositionAccessor = new PositionAccessor();
        private static TeamAccessor _TeamAccessor =new TeamAccessor();

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
                    return Json(new JsonObject(true, ExceptionStrings.DefaultPermissionsException));
                if (save.toSave == null)
                    return Json(JsonObject.Success);

                var questionsToEdit = save.toSave.Where(x => x.Type == "questionEnabled").ToList();
                var enabledQuestions = questionsToEdit.Where(x => x.Value == "true").Select(x => x.Id).ToList();
                var disabledQuestions = questionsToEdit.Where(x => x.Value == "false").Select(x => x.Id).ToList();
                _QuestionAccessor.SetQuestionsEnabled(user, save.forUser, enabledQuestions, disabledQuestions);

                return Json(JsonObject.Success);
            }
            catch (Exception e)
            {
                return Json(new JsonObject(e));
            }
        }

        [Access(AccessLevel.Manager)]
        public ActionResult AddModal()
        {
            var caller = GetUser().Hydrate().Organization().Execute();
            //var positions = _OrganizationAccessor.GetOrganizationPositions(caller, caller.Organization.Id);
            


            var orgPos = _OrganizationAccessor
                            .GetOrganizationPositions(GetUser(), GetUser().Organization.Id)
                            .OrderBy(x=>x.CustomName)
                            .ToSelectList(x => x.CustomName, x => x.Id).ToList();
            orgPos.Add(new SelectListItem() { Value = "-1", Text = "<" + DisplayNameStrings.createNew + ">" });

            var positions = _PositionAccessor
                                .AllPositions()
                                .ToSelectList(x => x.Name.Translate(), x => x.Id)
                                .ToList();

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
                managers=_OrganizationAccessor.GetOrganizationManagers(GetUser(),GetUser().Organization.Id)
                                                .ToSelectList(x=>x.GetName()+x.GetTitles(3,caller.Id).Surround(" (",")"), x=>x.Id)
                                                .ToList();

            if (caller.ManagingOrganization)
            {
                managers.Add(new SelectListItem() { Selected = false, Text = "[Manage Organization]", Value = "-3" });
            }


            var model = new CreateUserOrganizationViewModel() {
                Position = posModel,
                OrgId = caller.Organization.Id,
                StrictlyHierarchical = strictHierarchy,
                ManagerId = caller.Id,
                PotentialManagers = managers,
            };
            return PartialView(model);
        }

        public ActionResult EditModal(long userId)
        {

            var found=_UserAccessor.GetUserOrganization(GetUser(),userId);
            
            var strictHierarchy = GetUser().Organization.StrictHierarchy;
            List<SelectListItem> managers = new List<SelectListItem>();
            if (!strictHierarchy)
                managers=_OrganizationAccessor.GetOrganizationManagers(GetUser(),GetUser().Organization.Id)
                                                .ToSelectList(x=>x.GetName()+x.GetTitles(3, GetUser().Id).Surround(" (",")"), x=>x.Id)
                                                .ToList();

            var model = new EditUserOrganizationViewModel(){
                IsManager=found.ManagerAtOrganization,
                ManagerId = found.ManagedBy.ToListAlive().FirstOrDefault().NotNull(x => x.Id),
                PotentialManagers=managers,
                StrictlyHierarchical = strictHierarchy
            };


            return View("AddModal", model);
        }

        [HttpPost]
        public JsonResult EditModal(EditUserOrganizationViewModel model)
        {
            _UserAccessor.EditUser(GetUser(), model.IsManager, model.ManagerId);

            return Json(JsonObject.Success);
        }

        #endregion

        #region Positions

        [Access(AccessLevel.Manager)]
        public ActionResult Positions(long id)
        {
            var userId = id;
            var user = _UserAccessor.GetUserOrganization(GetUser(), userId);

            return View(user);
        }

        [Access(AccessLevel.Manager)]
        public JsonResult RemovePosition(long id,long userId)
        {
            _PositionAccessor.RemovePositionFromUser(GetUser(), userId, id);
            return Json(JsonObject.Success,JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult PositionModal(long userId, long id = 0)
        {
            var user = _UserAccessor.GetUserOrganization(GetUser(), userId);
            var pos  = user.Positions.FirstOrDefault(x => x.Id == id);

            var orgPos=_OrganizationAccessor
                            .GetOrganizationPositions(GetUser(),GetUser().Organization.Id)
                            .OrderBy(x => x.CustomName)
                            .ToSelectList(x => x.CustomName, x => x.Id, id).ToList();
            orgPos.Add(new SelectListItem() { Value = "-1", Text = "<" + DisplayNameStrings.createNew + ">" });

            var positions = _PositionAccessor
                                .AllPositions()
                                .ToSelectList(x=>x.Name.Translate(),x=>x.Id)
                                .ToList();

            var model = new UserPositionViewModel() { 
                UserId=userId,
                PositionId = id,
                OrgPositions = orgPos,
                Positions = positions,
                CustomPosition=null 
            };
            
            return PartialView(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult PositionModal(UserPositionViewModel model)
        {
            if (model.CustomPosition!=null)
            {
                var orgPos=_OrganizationAccessor.AddOrganizationPosition(GetUser(), GetUser().Organization.Id, model.CustomPositionId, model.CustomPosition);
                model.PositionId=orgPos.Id;
            }

            _PositionAccessor.AddPositionToUser(GetUser(), model.UserId, model.PositionId);

            return Json(JsonObject.Success);
        }

        #endregion

        #region Teams
        [Access(AccessLevel.Manager)]
        public ActionResult Teams(long id)
        {
            var userId = id;
            var teams = _TeamAccessor.GetUsersTeams(GetUser(), userId);
            var user = _UserAccessor.GetUserOrganization(GetUser(), userId).Hydrate().SetTeams(teams).Execute();


            return View(user);
        }

        [Access(AccessLevel.Manager)]
        public JsonResult RemoveTeam(long id)
        {
            _TeamAccessor.RemoveMember(GetUser(), id);
            return Json(JsonObject.Success,JsonRequestBehavior.AllowGet);
        }

        public class UserTeamViewModel
        {
            public long TeamId { get; set; }
            public long UserId { get; set; }
            public List<SelectListItem> OrgTeams { get; set; }
            public String CustomTeam { get; set; }
            public bool CustomOnlyManagersEdit { get;set;}
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
            var user = _UserAccessor.GetUserOrganization(GetUser(), userId).Hydrate().SetTeams(teams).Execute();
            var team = user.Teams.FirstOrDefault(x => x.Id == id);
            var orgTeam = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), GetUser().Id)
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

            _TeamAccessor.AddMember(GetUser(),model.TeamId, model.UserId);

            return Json(JsonObject.Success);
        }
        #endregion
    }
}
