using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview;
using RadialReview.Models.Enums;

namespace RadialReview.Controllers
{
    public class GroupController : BaseController
    {
        public static GroupAccessor _GroupAccessor = new GroupAccessor();

        //
        // GET: /Group/
        public ActionResult Index(long? organizationId)
        {
            var userOrg = GetOneUserOrganization(organizationId).Hydrate().ManagingGroups().ManagingUsers().Execute();
            IList<GroupModel> groups = userOrg.ManagingGroups;
            return View(groups);
        }

        public ActionResult Details(long id, long? organizationId)
        {
            var group = _GroupAccessor.Get(GetOneUserOrganization(organizationId), id);
            return View(group);
        }

        public ActionResult Create(long? organizationId)
        {
            var orgUser = GetOneUserOrganization(organizationId).Hydrate().ManagingGroups().ManagingUsers().Execute();
            organizationId = orgUser.Organization.Id;

            if (!orgUser.IsManagerCanEditOrganization())
                throw new PermissionsException();

            var group = new GroupModel() { DeleteTime = DateTime.UtcNow };

            group = _GroupAccessor.Edit(orgUser, group);
            return RedirectToAction("Edit", new { id = group.Id });
        }

        public ActionResult Edit(long? id, long? organizationId)
        {
            if (id == null)
                return View("Create");

            var orgUser = GetOneUserOrganization(organizationId)
                .Hydrate()
                .ManagingGroups()
                .ManagingUsers(subordinates: true)
                .Execute();
            //var orgUser=orgUsers.Where(x=>x.ManagingGroups.Any(y=>y.Id==id)).SingleOrDefault();
            if (orgUser == null)
                throw new PermissionsException();

            var found = orgUser.ManagingGroups.FirstOrDefault(x => x.Id == id);
            if (found == null)
                throw new PermissionsException();

            organizationId = orgUser.Organization.Id;

            var group = _GroupAccessor.Get(orgUser, id.Value);
            var directManage = orgUser.ManagingUsers;
            var possibleUsers = orgUser.AllSubordinatesAndSelf();
            foreach (var p in possibleUsers)
            {
                if (directManage.Any(x => x.Id == p.Id))    p.Properties.Update("classes", new List<String>(), x => x.Add("directlyManaged"));
                else                                        p.Properties.Update("classes", new List<String>(), x => x.Add("subordinate"));
                if (p.Id == orgUser.Id)
                {                                  
                    p.Properties.Update("classes", new List<String>(), x => x.Add("self"));
                    p.Properties.Update("altText", new List<String>(), x =>x.Add(DisplayNameStrings.you));
                }
                if (!p.IsAttached())     
                    p.Properties.Update("altText", new List<String>(), x => x.Add(DisplayNameStrings.unattached));
            }

            var start = possibleUsers.Where(x => !group.GroupUsers.Any(y => x.Id == y.Id)).OrderBy(x => x.Properties.GetOrDefault("parents", new List<String>()).Count).ToDragDropList();
            var end   = possibleUsers.Where(x =>  group.GroupUsers.Any(y => x.Id == y.Id)).OrderBy(x => x.Properties.GetOrDefault("parents", new List<String>()).Count).ToDragDropList();

            var groupViewModel = new GroupViewModel()
            {
                OrganizationId = organizationId.Value,
                Group = group,
                DragDrop = new DragDropViewModel()
                {
                    Start = start,
                    End = end,
                    StartName = "Exclude",
                    EndName = "Include"
                },
                Questions = new QuestionsViewModel(organizationId.Value, OriginType.Group, group.Id, group.CustomQuestions)


            };
            return View(groupViewModel);
        }

        private GroupViewModel ReconstructModel(UserOrganizationModel orgUser, GroupViewModel model)
        {
            var g = _GroupAccessor.Get(orgUser, model.Group.Id);
            var drags = (model.DragDrop.DragItems ?? "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var drops = (model.DragDrop.DropItems ?? "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            model.DragDrop.Start = orgUser.GetManagingUsersAndSelf().Where(x => drags.Any(y => y == "" + x.Id)).ToDragDropList();
            model.DragDrop.End = orgUser.GetManagingUsersAndSelf().Where(x => drops.Any(y => y == "" + x.Id)).ToDragDropList();
            model.Questions = new QuestionsViewModel(orgUser.Organization.Id, OriginType.Group, model.Group.Id, g.CustomQuestions);
            return model;
        }


        [HttpPost]
        public ActionResult Edit(GroupViewModel model)
        {
            var userOrg = GetUserOrganization(model.OrganizationId).Hydrate().ManagingGroups().ManagingUsers(subordinates:true).Execute();

            if (!userOrg.IsManagerCanEditOrganization())
                throw new PermissionsException();

            List<long> userIds = (model.DragDrop.DropItems ?? "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => long.Parse(x)).ToList();

            if (String.IsNullOrWhiteSpace(model.Group.GroupName))
            {
                TempData["Message"] = MessageStrings.NameRequired;
                return View(ReconstructModel(userOrg, model));
            }

            if (userIds.Count == 0)
            {
                TempData["Message"] = MessageStrings.InsufficientNumberOfMembers;
                return View(ReconstructModel(userOrg, model));
            }


            var users = userOrg.AllSubordinates.Where(x => userIds.Contains(x.Id)).ToList();

            var group = new GroupModel()
            {
                GroupName = model.Group.GroupName,
                GroupUsers = users,
                Id = model.Group.Id,
                DeleteTime = null
            };
            group.Managers.Add(userOrg);

            _GroupAccessor.Edit(userOrg, group);

            return Redirect(Url.Action("Manage", "Organization") + "#groups");
        }

    }
}