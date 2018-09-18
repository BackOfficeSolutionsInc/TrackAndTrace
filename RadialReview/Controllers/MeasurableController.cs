using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Json;
using RadialReview.Models.Scorecard;
using RadialReview.Models.UserTemplate;
using System.Threading.Tasks;
using static RadialReview.Utilities.SelectExistingOrCreateUtility;

namespace RadialReview.Controllers
{
    public class MeasurableController : BaseController
    {

		public class MeasurableVM
		{
			public long UserId { get; set; }
			public List<MeasurableModel> Measurables { get; set; }
			public List<MeasurableModel> AdminMeasurables { get; set; }
			public DateTime CurrentTime = DateTime.UtcNow;


			public List<UserTemplate.UT_Measurable> TemplateMeasurables { get; set; }
			public long TemplateId { get; set; }
            public bool UpdateAllL10s { get; set; }
            public MeasurableVM()
            {
				UpdateAllL10s = false;//true;
            }
		}

		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Search(string q, int results = 4, string exclude = null) {
			long[] excludeLong = new long[] { };
			if (exclude != null) {
				try {
					excludeLong = exclude.Split(',').Select(x => x.ToLong()).ToArray();
				} catch (Exception) { }
			}

			var oo = await ScorecardAccessor.Search(GetUser(), GetUser().Organization.Id, q, excludeLong);
			//var oo = _SearchUsers(q, results, exclude);
			var o = oo.Select(x => {
				var desc = "Owner: " + x.AccountableUser.GetName();
				if (x.AccountableUserId != x.AdminUserId)
					desc += " (Admin: " + x.AdminUser.GetName() + ")";
				return new BaseSelectExistingOrCreateItem {					
					ItemValue = "" + x.Id,
					Name = x.Title,
					ImageUrl = x.AccountableUser.GetImageUrl(),
					Description = desc
				};
			}).ToList();
			return Json(ResultObject.SilentSuccess(o), JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult BlankEditorRow(bool accountable=false,long? admin=null)
		{
           // ViewBag.AllMembers = _DeepSubordianteAccessor.GetSubordinatesAndSelfModels(GetUser(), GetUser().Id);

			ViewBag.AllMembers = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, false).ToSelectList(x=>x.Name,x=>x.UserId);
			ViewBag.ShowAccountable = accountable;
			return PartialView("_MeasurableRow", new MeasurableModel(GetUser().Organization){
				AdminUserId = admin ?? 0,
			});
		}
		[Access(AccessLevel.Radial)]
		public JsonResult Undelete(long id) {
			ScorecardAccessor.UndeleteMeasurable(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Modal(long id)
		{
			PermissionsAccessor.Permitted(GetUser(), x => x.EditQuestionForUser(id));
			var measurables = ScorecardAccessor.GetUserMeasurables(GetUser(), id, includeAdmin:false);
			ViewBag.AllMembers = _OrganizationAccessor
				.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false)
				.ToSelectList(x => x.GetNameAndTitle(), x => x.Id);
			

			return PartialView(new MeasurableController.MeasurableVM {
				Measurables = measurables.Where(x=>x.AccountableUserId==id).ToList(),
				AdminMeasurables = measurables.Where(x=>x.AdminUserId == id && x.AccountableUserId!=id).ToList(),
				UserId = id
			});
		}


		[Access(AccessLevel.UserOrganization)]
		public JsonResult RemoveAdmin(long id) {

			ScorecardAccessor.RemoveAdmin(GetUser(), id);

			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Modal(MeasurableController.MeasurableVM model) {
			var avail = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, false).Select(x => x.UserId).ToList();

			if (!avail.Contains(model.UserId))
				throw new PermissionsException();


			foreach (var r in model.Measurables) {
				r.AccountableUserId = model.UserId;
				if (!avail.Contains(r.AdminUserId))
					throw new PermissionsException();
			}
			await ScorecardAccessor.EditMeasurables(GetUser(), model.UserId, model.Measurables, model.UpdateAllL10s);
			return Json(ResultObject.SilentSuccess());
		}

		#region Template
		[Access(AccessLevel.Manager)]
		public PartialViewResult TemplateModal(long id)
		{
			//var rocks = ScorecardAccessor.GetUserMeasurables(GetUser(), id);
			//ViewBag.AllMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false).ToSelectList(x => x.GetNameAndTitle(), x => x.Id);
			var template = UserTemplateAccessor.GetUserTemplate(GetUser(), id, loadMeasurables: true);
			return PartialView(new MeasurableController.MeasurableVM { TemplateMeasurables = template._Measurables, TemplateId = id });
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankTemplateEditorRow(long id)
		{
			//ViewBag.AllMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false).ToSelectList(x => x.GetNameAndTitle(), x => x.Id);
			return PartialView("_TemplateMeasurableRow", new UserTemplate.UT_Measurable(){
				TemplateId = id
			});
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult TemplateModal(MeasurableController.MeasurableVM model)
		{
			foreach (var r in model.TemplateMeasurables)
			{
				if (r.Id == 0)
				{
					if (r.DeleteTime == null)
						UserTemplateAccessor.AddMeasurableToTemplate(GetUser(), model.TemplateId, r.Measurable,r.GoalDirection,r.Goal);
				}
				else
					UserTemplateAccessor.UpdateMeasurableTemplate(GetUser(), r.Id, r.Measurable, r.GoalDirection, r.Goal, r.DeleteTime);
			}
			return Json(ResultObject.SilentSuccess());
		}
		#endregion
	}
}