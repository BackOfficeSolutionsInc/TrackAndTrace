﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Json;
using RadialReview.Models.UserTemplate;

namespace RadialReview.Controllers
{
    public class RocksController : BaseController
    {
		public class RockVM
		{
			public long TemplateId { get; set; }
			public long UserId { get; set; }
			public List<RockModel> Rocks { get; set; }
			public List<Models.UserTemplate.UserTemplate.UT_Rock> TemplateRocks { get; set; }
			public DateTime CurrentTime { get; set; }
			public bool Locked { get; set; }

			public RockVM()
			{
				CurrentTime= DateTime.UtcNow;
			}

		}
		[Access(AccessLevel.Manager)]
		public PartialViewResult ModalSingle(long id,long userId,long periodId)
		{
			RockModel rock;
			if (id == 0)
				rock = new RockModel(){CreateTime = DateTime.UtcNow};
			else{
				rock = _RockAccessor.GetRock(GetUser(), id);
			}

			ViewBag.Periods = PeriodAccessor.GetPeriod(GetUser(), periodId).AsList().ToSelectList(x => x.Name, x => x.Id);//PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);

			return PartialView(new RocksController.RockVM { Rocks = rock.AsList(), UserId = userId });
		}
	    [Access(AccessLevel.Manager)]
	    public JsonResult Delete(long id)
	    {
		    RockAccessor.DeleteRock(GetUser(), id);
		    return Json(ResultObject.SilentSuccess());
	    }
		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankEditorRow(bool includeUsers=false,bool companyRock=false){
			ViewBag.Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			if (includeUsers)
				ViewBag.PossibleUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
			return PartialView("_RockRow", new RockModel(){
				CreateTime = DateTime.UtcNow,
				CompanyRock = companyRock,
			});
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult CompanyRockModal(long id)
		{
			var orgId = id;
			var rocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();

			//var rocks = RockAccessor.GetAllRocksAtOrganization(GetUser(), orgId, true);
			var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			ViewBag.Periods = periods;
			ViewBag.PossibleUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id,false,false);
			return PartialView(new RocksController.RockVM { Rocks = rocks, UserId = id });
		}
		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult CompanyRockModal(RocksController.RockVM model)
		{
			//var rocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();
			var oid = GetUser().Organization.Id;
			model.Rocks.ForEach(x=>x.OrganizationId=oid);
			_RockAccessor.EditCompanyRocks(GetUser(), GetUser().Organization.Id, model.Rocks);
			return Json(ResultObject.Create(model.Rocks.Select(x => new { Session = x.Period.Name, Rock = x.Rock, Id = x.Id }), status: StatusType.SilentSuccess));
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult Modal(long id)
		{
			var userId = id;
			var rocks = _RockAccessor.GetAllRocks(GetUser(), userId);
			var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x=>x.Name,x=>x.Id);
			ViewBag.Periods = periods;
			return PartialView(new RocksController.RockVM { Rocks = rocks, UserId = id });
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult Modal(RocksController.RockVM model)
		{
			foreach (var r in model.Rocks)
			{
				r.ForUserId = model.UserId;
			}
			_RockAccessor.EditRocks(GetUser(), model.UserId, model.Rocks);
			return Json(ResultObject.Create(model.Rocks.Select(x=>new { Session = x.Period.Name, Rock = x.Rock, Id =x.Id }),status:StatusType.SilentSuccess));
		}

	    public class RockTable
	    {
			public List<RockModel> Rocks { get; set; }
			public List<long> Editables { get; set; }
			public bool Editable { get; set; }
	    }

	    [Access(AccessLevel.UserOrganization)]
	    public ActionResult Table(long id,bool editor=false, bool current=true)
	    {
		    var forUserId = id;
			var rocks = _RockAccessor.GetAllRocks(GetUser(), forUserId);
		    var editables = new List<long>();

		    if (current)
			    rocks = rocks.Where(x => x.CompleteTime == null).ToList();


		    if (editor && _PermissionsAccessor.IsPermitted(GetUser(), x => x.ManagesUserOrganization(forUserId, false))){
			    editables = rocks.Select(x => x.Id).ToList();
		    }
			
		    var model= new RockTable(){
				Editables = editables,
				Rocks = rocks,
				Editable = editor,
		    };

			return PartialView(model);


	    }

		//public ActionResult Assessment()
		#region Template
		[Access(AccessLevel.Manager)]
		public PartialViewResult TemplateModal(long id)
		{
			var templateId = id;
			var template = UserTemplateAccessor.GetUserTemplate(GetUser(), templateId, loadRocks: true);
			var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			ViewBag.Periods = periods;
			return PartialView(new RocksController.RockVM { TemplateRocks = template._Rocks, TemplateId = templateId });
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult TemplateModal(RocksController.RockVM model)
		{
			foreach (var r in model.TemplateRocks){
				if (r.Id == 0){
					if (r.DeleteTime == null)
						UserTemplateAccessor.AddRockToTemplate(GetUser(), model.TemplateId, r.Rock, r.PeriodId);
				}
				else
					UserTemplateAccessor.UpdateRockTemplate(GetUser(), r.Id, r.Rock, r.PeriodId, r.DeleteTime);
			}

			return Json(ResultObject.SilentSuccess()); //ResultObject.Create(model.TemplateRocks.Select(x => new { Session = x.Period.Name, Rock = x.Rock, Id = x.Id }), status: StatusType.SilentSuccess));
		}
		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankTemplateEditorRow(long id)
		{
			var templateId = id;
			_PermissionsAccessor.Permitted(GetUser(),x=>x.ViewTemplate(templateId));
			ViewBag.Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			return PartialView("_TemplateRockRow", new UserTemplate.UT_Rock(){
				TemplateId = templateId
			});
		}
		#endregion
	}
}