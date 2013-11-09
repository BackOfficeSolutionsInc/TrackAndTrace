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

namespace RadialReview.Controllers
{
    public class GroupController : BaseController
    {
        public static GroupAccessor _GroupAccessor = new GroupAccessor();

        //
        // GET: /Group/
        public ActionResult Index(long? organizationId)
        {
            var userOrg=GetOneUserOrganization(organizationId,true);
            IList<GroupModel> groups=userOrg.ManagingGroups;
            return View(groups);
        }

        public ActionResult Details(long id, long? organizationId)
        {
            var group = _GroupAccessor.Get(GetOneUserOrganization(organizationId), id);
            return View(group);
        }

        public ActionResult Create(long? organizationId)
        {
            var orgUser = GetOneUserOrganization(organizationId, true);
            organizationId = orgUser.Organization.Id;

            if (!orgUser.IsManagerCanEditOrganization)
                throw new PermissionsException();

            var start = orgUser.GetManagingUsersAndSelf().ToDragDropList();

            var end = new List<DragDropItem>();
            var groupViewModel = new GroupViewModel()
            {
                OrganizationId = organizationId.Value,
                Group = new GroupModel() { },
                DragDrop = new DragDropViewModel()
                {
                    Start = start,
                    End = end,
                    StartName = "Exclude",
                    EndName = "Include"
                }
            };
            return View("Edit",groupViewModel);

        }

        public ActionResult Edit(long? id)
        {
            if (id == null)
                return View("Create");
            
            var orgUsers=GetUserOrganization(true);
            var orgUser=orgUsers.Where(x=>x.ManagingGroups.Any(y=>y.Id==id)).SingleOrDefault();
            if (orgUser==null)
                throw new PermissionsException();

            var found = orgUser.ManagingGroups.FirstOrDefault(x => x.Id == id);
            if (found == null)
                throw new PermissionsException();

            var organizationId=orgUser.Organization.Id;

            var group=_GroupAccessor.Get(orgUser,id.Value);
            var possibleUsers = orgUser.GetManagingUsersAndSelf();

            var start = possibleUsers.Where(x => !group.GroupUsers.Any(y => x.Id == y.Id)).ToDragDropList();
            var end = group.GroupUsers.ToDragDropList();
            
            var groupViewModel=new GroupViewModel(){
                OrganizationId=organizationId,
                Group = group,
                DragDrop = new DragDropViewModel()
                {
                    Start=start,
                    End=end,
                    StartName="Exclude",
                    EndName="Include"
                }
                
            };
            return View(groupViewModel);
        }

        private GroupViewModel ReconstructModel(UserOrganizationModel orgUser, GroupViewModel model)
        {
            _GroupAccessor.Get(orgUser,model.Group.Id);
            var drags = (model.DragDrop.DragItems ?? "").Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries).ToList();
            var drops = (model.DragDrop.DropItems ?? "").Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries).ToList();
            
            model.DragDrop.Start = orgUser.GetManagingUsersAndSelf().Where(x=>drags.Any(y=>y==""+x.Id)).ToDragDropList();
            model.DragDrop.End = orgUser.GetManagingUsersAndSelf().Where(x => drops.Any(y => y == "" + x.Id)).ToDragDropList();
            return model;
        }


        [HttpPost]
        public ActionResult Edit(GroupViewModel model)
        {
            var userOrg=GetUserOrganization(model.OrganizationId,true);

            if (!userOrg.IsManagerCanEditOrganization)
                throw new PermissionsException();

            List<long> userIds=(model.DragDrop.DropItems??"").Split(new char[]{','},StringSplitOptions.RemoveEmptyEntries).Select(x=>long.Parse(x)).ToList();

            if (userIds.Count == 0) {
                TempData["Message"] = MessageStrings.InsufficientNumberOfMembers;
                return View(ReconstructModel(userOrg,model));
            }

            if (String.IsNullOrWhiteSpace(model.Group.GroupName))
            {
                TempData["Message"] = MessageStrings.NameRequired;
                return View(ReconstructModel(userOrg, model));
            }

            var users= userOrg.GetManagingUsersAndSelf().Where(x=>userIds.Contains(x.Id)).ToList();
            
            var group=new GroupModel(){
                GroupName=model.Group.GroupName,
                GroupUsers=users,   
                Id=model.Group.Id,
            };
            group.Managers.Add(userOrg);

            _GroupAccessor.Edit(userOrg, group);

            return RedirectToAction("Index");
        }

    }
}