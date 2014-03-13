﻿using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class IssueReviewModel
    {
        public String OrganizationName { get; set; }
        public List<SelectListItem> Teams { get; set; }
        public long SelectedTeam { get; set; }
    }

    public class CreateReviewModel
    {
        public long TeamId { get; set; }
        public String TeamName { get; set; }
        public bool EmailManagers { get; set; }
        public DateTime SelectedDate { get; set; }
        public String Name { get; set; }
        public bool ManagersCanCustomize { get; set; }
    }
    
    public class IssueController : BaseController
    {

        //
        // GET: /Issue/
        [Access(AccessLevel.Manager)]
        public ActionResult Index()
        {
            var model = new IssueReviewModel()
            {
                OrganizationName = GetUser().Organization.GetName(),
                Teams = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), GetUser().Id).Select(x => new SelectListItem() { Text = x.Name, Value = "" + x.Id }).ToList(),
            };
            ViewBag.Page = "Generate";
            return View(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public ActionResult Index(FormCollection form)
        {
            if (form["review"] == "issueReview")
            {
                var customized = form.AllKeys.Where(x => x.StartsWith("customize_")).Select(x =>
                {
                    var split = x.Split('_');
                    return Tuple.Create(long.Parse(split[1]), long.Parse(split[2]));
                });

                _ReviewAccessor.CreateReviewFromCustom(
                    GetUser(),
                    form["TeamId"].ToLong(),
                    form["DueDate"].ToDateTime("MM-dd-yyyy", form["TimeZoneOffset"].ToDouble() + 24),
                    form["ReviewName"],
                    form["SendEmails"].ToBoolean(),
                    customized.ToList()
                    );
            }
            else if (form["review"] == "issuePrereview")
            {
                _PrereviewAccessor.CreatePrereview(
                    GetUser(),
                    form["TeamId"].ToLong(),
                    form["ReviewName"],
                    true,//form["SendEmails"].ToBoolean(),
                    form["DueDate"].ToDateTime("MM-dd-yyyy", form["TimeZoneOffset"].ToDouble() + 24),
                    form["PrereviewDate"].ToDateTime("MM-dd-yyyy", form["TimeZoneOffset"].ToDouble() + 24)
                    );

            }
            else
            {
                throw new PermissionsException("Review type is not recognized");
            }

            return RedirectToAction("Index", "Home");
        }


        [Access(AccessLevel.Manager)]
        public ActionResult Customize(long id)
        {
            var teamId = id;

            var model = _ReviewEngine.GetCustomizeModel(GetUser(), teamId);
            var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
            model.AllUsers = allUsers;

            return PartialView(model);
            
        }


        [Access(AccessLevel.Manager)]
        public ActionResult IssueOrganization()
        {
            var orgTeam = _TeamAccessor.GetOrganizationTeams(GetUser(), GetUser().Organization.Id).FirstOrDefault(x => x.Type == TeamType.AllMembers);
            ViewBag.OrganizationId = orgTeam.Id;
            return PartialView();
        }

        [Access(AccessLevel.Manager)]
        public ActionResult IssueTeam()
        {
            var teams = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), GetUser().Id);
            return PartialView(teams.Select(x => new SelectListItem() { Text = x.GetName(), Value = "" + x.Id }).ToList());
        }

        [Access(AccessLevel.Manager)]
        public ActionResult ManagersCustomize()
        {
            return PartialView();
        }

        [Access(AccessLevel.Manager)]
        public ActionResult SelfCustomize()
        {
            return PartialView();
        }

        /*
        [HttpPost]
        [Access(AccessLevel.Manager)]
        public ActionResult IssueOrganization(CreateReviewModel model)
        {
            throw new Exception("Implement me");

            //return View();
        }*/
    }
}