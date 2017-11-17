using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.UserTemplate;
using RadialReview.Utilities.DataTypes;
using System.Threading.Tasks;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Rocks;

namespace RadialReview.Controllers {
	public class RocksController : BaseController {
		public class RockVM {
			public long TemplateId { get; set; }
			public long UserId { get; set; }
			public List<RockModel> Rocks { get; set; }
			public List<Models.UserTemplate.UserTemplate.UT_Rock> TemplateRocks { get; set; }
			public DateTime CurrentTime { get; set; }
			public bool Locked { get; set; }
			public bool UpdateOutstandingReviews { get; set; }
			public bool UpdateAllL10s { get; set; }

			public RockVM() {
				CurrentTime = DateTime.UtcNow;
				UpdateAllL10s = false;//true;
			}
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Pad(long id, bool showControls=true) {
			try {
				var rock = RockAccessor.GetRock(GetUser(), id);
				var padId = rock.PadId;
				if (!_PermissionsAccessor.IsPermitted(GetUser(), x => x.EditRock(id))) {
					padId = await PadAccessor.GetReadonlyPad(rock.PadId);
				}
				return Redirect(Config.NotesUrl("p/" + padId + "?showControls=" + (showControls ? "true" : "false") + "&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName=" + Url.Encode(GetUser().GetName())));
			} catch (Exception ) {
				return RedirectToAction("Index", "Error");
			}
		}


		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> SetDueDate(long rockId, DateTime duedate) {
			await L10Accessor.UpdateRock(GetUser(), rockId, null, null, null, null, dueDate: duedate);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Archive(long id) {
			var userId = id;
			var archivedRocks = RockAccessor.GetArchivedRocks(GetUser(), userId);
			return View(archivedRocks);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult ModalSingle(long id, long userId, long? periodId) {
			_PermissionsAccessor.Permitted(GetUser(), x => x.EditQuestionForUser(userId));
			RockModel rock;
			if (id == 0)
				rock = new RockModel() {
					CreateTime = DateTime.UtcNow,
					OnlyAsk = AboutType.Self,
				};
			else {
				rock = RockAccessor.GetRock(GetUser(), id);
			}

			ViewBag.Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			//ViewBag.Periods = PeriodAccessor.GetPeriod(GetUser(), periodId.Value).AsList().ToSelectList(x => x.Name, x => x.Id);//PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);

			return PartialView(new RocksController.RockVM { Rocks = rock.AsList(), UserId = userId });
		}

		//[Access(AccessLevel.UserOrganization)]
		//public JsonResult Delete(long id) {
		//	RockAccessor.DeleteRock(GetUser(), id);
		//	return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
		//}

		[Access(AccessLevel.Radial)]
		public JsonResult Undelete(long id) {
			RockAccessor.UndeleteRock(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult BlankEditorRow(bool includeUsers = false, bool unusedCompanyRock = false, long? periodId = null, bool hideperiod = false, bool showCompany = false, bool excludeDelete = false, long? recurrenceId = null) {
			ViewBag.Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			if (includeUsers) {
				if (recurrenceId != null) {
					ViewBag.PossibleUsers = L10Accessor.GetAttendees(GetUser(), recurrenceId.Value);
				} else {
					ViewBag.PossibleUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
				}
			}

			ViewBag.HidePeriod = true;// hideperiod;
			ViewBag.ShowCompany = showCompany;
			ViewBag.HideDelete = excludeDelete;
			ViewBag.HideArchive = excludeDelete;

			return PartialView("_RockRow", new RockModel() {
				CreateTime = DateTime.UtcNow,
				//CompanyRock = companyRock,
				OnlyAsk = AboutType.Self,
				PeriodId = periodId
			});
		}

		//[Access(AccessLevel.Manager)]
		//[Untested("Vto_Rocks","Remove from UI")]
		//[Obsolete("Do not use",true)]
		//public PartialViewResult CompanyRockModal(long id) {
		//	var orgId = id;
		//	var rocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();

		//	//var rocks = RockAccessor.GetAllRocksAtOrganization(GetUser(), orgId, true);
		//	var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
		//	ViewBag.Periods = periods;
		//	ViewBag.PossibleUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
		//	return PartialView(new RocksController.RockVM { Rocks = rocks, UserId = id });
		//}

		//[HttpPost]
		//[Access(AccessLevel.Manager)]
		//[Untested("Vto_Rocks", "Remove from UI")]
		//[Obsolete("Do not use", true)]
		//public JsonResult CompanyRockModal(RocksController.RockVM model) {
		//	//var rocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();
		//	var oid = GetUser().Organization.Id;
		//	model.Rocks.ForEach(x => x.OrganizationId = oid);
		//	RockAccessor.EditCompanyRocks(GetUser(), GetUser().Organization.Id, model.Rocks);
		//	return Json(ResultObject.Create(model.Rocks.Select(x => new { Session = x.Period.Name, Rock = x.Rock, Id = x.Id }), status: StatusType.Success));
		//}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Modal(long id) {
			_PermissionsAccessor.Permitted(GetUser(), x => x.EditQuestionForUser(id));
			var userId = id;
			var rocks = RockAccessor.GetAllRocks(GetUser(), userId);
			var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).OrderByDescending(x => x.EndTime).ToSelectList(x => x.Name, x => x.Id);
			ViewBag.Periods = periods;
			return PartialView(new RocksController.RockVM { Rocks = rocks, UserId = id });
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Modal(RocksController.RockVM model) {
			foreach (var r in model.Rocks) {
				r.ForUserId = model.UserId;
			}
			await RockAccessor.EditRocks(GetUser(), model.UserId, model.Rocks, model.UpdateOutstandingReviews, model.UpdateAllL10s);
			return Json(ResultObject.Create(model.Rocks.Select(x => new { Session = x.Period.Name, Rock = x.Rock, Id = x.Id }), status: StatusType.Success));
		}

		[Access(AccessLevel.UserOrganization)]
		public FileContentResult Listing() {
			var csv = RockAccessor.Listing(GetUser(), GetUser().Organization.Id);
			return File(csv.ToBytes(), "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + csv.Title + ".csv");
		}

		public class RockTable {
			public List<RockModel> Rocks { get; set; }
			public List<long> Editables { get; set; }
			public bool Editable { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Table(long id, bool editor = false, bool current = true) {
			var forUserId = id;
			var rocks = RockAccessor.GetAllRocks(GetUser(), forUserId);
			var editables = new List<long>();

			if (current)
				rocks = rocks.Where(x => x.CompleteTime == null).ToList();


			if (editor && _PermissionsAccessor.IsPermitted(GetUser(), x => x.ManagesUserOrganization(forUserId, false))) {
				editables = rocks.Select(x => x.Id).ToList();
			}

			var model = new RockTable() {
				Editables = editables,
				Rocks = rocks,
				Editable = editor,
			};

			return PartialView(model);


		}

		public class RockAndMilestonesVM {
			public List<AngularMilestone> Milestones { get; set; }
			public AngularRock Rock { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		[Untested("Vto_Rocks","Make sure that Company rock is uneditable.")]
		public PartialViewResult EditModal(long id) {
			var rockAndMs = RockAccessor.GetRockAndMilestones(GetUser(), id);

			var model = new RockAndMilestonesVM() {
				Rock = new AngularRock(rockAndMs.Rock,false),
				Milestones = rockAndMs.Milestones.Select(x => new AngularMilestone(x)).ToList()
			};

			ViewBag.AnyL10sWithMilestones = rockAndMs.AnyMilestoneMeetings;

			return PartialView(model);

		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> EditModal(RocksController.RockAndMilestonesVM model) {
			var rock = model.Rock;
			await L10Accessor.Update(GetUser(), rock, null);
			return Json(ResultObject.SilentSuccess());
		}


		//public ActionResult Assessment()
		#region Template
		[Access(AccessLevel.Manager)]
		public PartialViewResult TemplateModal(long id) {
			var templateId = id;
			var template = UserTemplateAccessor.GetUserTemplate(GetUser(), templateId, loadRocks: true);
			var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			ViewBag.Periods = periods;
			return PartialView(new RocksController.RockVM { TemplateRocks = template._Rocks, TemplateId = templateId });
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public async Task<JsonResult> TemplateModal(RocksController.RockVM model) {
			foreach (var r in model.TemplateRocks) {
				if (r.Id == 0) {
					if (r.DeleteTime == null)
						await UserTemplateAccessor.AddRockToTemplate(GetUser(), model.TemplateId, r.Rock, r.PeriodId);
				} else
					UserTemplateAccessor.UpdateRockTemplate(GetUser(), r.Id, r.Rock, r.PeriodId, r.DeleteTime);
			}

			return Json(ResultObject.SilentSuccess()); //ResultObject.Create(model.TemplateRocks.Select(x => new { Session = x.Period.Name, Rock = x.Rock, Id = x.Id }), status: StatusType.SilentSuccess));
		}
		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankTemplateEditorRow(long id) {
			var templateId = id;
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewTemplate(templateId));
			ViewBag.Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			return PartialView("_TemplateRockRow", new UserTemplate.UT_Rock() {
				TemplateId = templateId
			});
		}
		#endregion
	}
}