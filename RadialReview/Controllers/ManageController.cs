using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ManageController : BaseController
    {
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static TeamAccessor _TeamAccessor = new TeamAccessor();
        protected static PositionAccessor _PositionAccessor = new PositionAccessor();
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
        //
        // GET: /Manage/
        [Access(AccessLevel.Manager)]
        public ActionResult Index()
        {
            //Main page
            var page = (string)Session["Manage"] ?? "Members";
            return RedirectToAction(page);
            /*
            switch (page)
            {
                case "Positions":  Positions();
                case "Teams": return Teams();
                case "Members": return Members();
                case "Reviews": return Reviews();
                default: return Positions();
            }*/

        }

        [Access(AccessLevel.Manager)]
        public async Task<ActionResult> Positions()
        {
            Session["Manage"] = "Positions";
            var orgPos = _OrganizationAccessor.GetOrganizationPositions(GetUser(), GetUser().Organization.Id);

            var positions = orgPos.Select(x =>
                new OrgPosViewModel(x, _PositionAccessor.GetUsersWithPosition(GetUser(), x.Id).Count())
            ).ToList();

            var caller = GetUser().Hydrate().EditPositions().Execute();

            var model = new OrgPositionsViewModel() { Positions = positions, CanEdit = caller.GetEditPosition() };
            return View(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Teams()
        {
            Session["Manage"] = "Teams";
            var orgTeams = _TeamAccessor.GetOrganizationTeams(GetUser(), GetUser().Organization.Id);
            var teams = orgTeams.Select(x => new OrganizationTeamViewModel { Team = x, Members = -1 }).ToList();

            for (int i = 0; i < orgTeams.Count(); i++)
            {
                try
                {
                    teams[i].Team = teams[i].Team.HydrateResponsibilityGroup().PersonallyManaging(GetUser()).Execute();
                    teams[i].Members = _TeamAccessor.GetTeamMembers(GetUser(), teams[i].Team.Id).ToListAlive().Count();
                }
                catch (Exception e)
                {
                    log.Error(e);
                }

            }
            var model = new OrganizationTeamsViewModel() { Teams = teams };
            return View(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Members()
        {
            Session["Manage"] = "Members";
            var user = GetUser().Hydrate().ManagingUsers(true).Execute();

            var members = _OrganizationAccessor.GetOrganizationMembers(user, user.Organization.Id, true, true);

            for (int i = 0; i < members.Count(); i++)
            {
                var u = members[i];
                u.PopulatePersonallyManaging(user, user.AllSubordinates);
                //var teams = _TeamAccessor.GetUsersTeams(GetUser(), u.Id);
                //members[i] = members[i].Hydrate().SetTeams(teams).PersonallyManaging(GetUser()).Managers().Execute();
            }
            var model = new OrgMembersViewModel(members);
            return View(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Organization()
        {
            var user = GetUser().Hydrate().Organization().Execute();
            if (!user.ManagingOrganization)
                throw new PermissionsException();

            var model = new OrganizationViewModel()
            {
                Id = user.Organization.Id,
                ManagersCanEdit = user.Organization.ManagersCanEdit,
                OrganizationName = user.Organization.Name.Standard,
                StrictHierarchy = user.Organization.StrictHierarchy,
                ManagersCanEditPositions = user.Organization.ManagersCanEditPositions,
            };

            return View(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public ActionResult Organization(OrganizationViewModel model)
        {
            _OrganizationAccessor.Edit(GetUser(), model.Id, model.OrganizationName, model.ManagersCanEdit, model.StrictHierarchy, model.ManagersCanEditPositions, model.SendEmailImmediately);
            ViewBag.Success = "Successfully Saved.";
            return View(model);
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Reviews()
        {
            Session["Manage"] = "Reviews";
            var reviews = _ReviewAccessor.GetReviewsForOrganization(GetUser(), GetUser().Organization.Id, false);
            var model = new OrgReviewsViewModel()
            {
                Reviews = reviews.Select(x => new ReviewsViewModel(x)).ToList()
            };

            return View(model);
        }
    }
}