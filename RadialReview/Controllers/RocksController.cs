using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public class RocksController : BaseController
    {
		public class RockVM
		{
			public long UserId { get; set; }
			public List<RockModel> Rocks { get; set; }
			public DateTime CurrentTime { get; set; }

			public RockVM()
			{
				CurrentTime= DateTime.UtcNow;
			}
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult Modal(long id)
		{
			var userId = id;
			var rocks = _RockAccessor.GetAllRocks(GetUser(), userId);
			return PartialView(new RocksController.RockVM { Rocks = rocks, UserId = id });
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
		public PartialViewResult BlankEditorRow()
		{

			ViewBag.Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.Name, x => x.Id);
			return PartialView("_RockRow", new RockModel(){CreateTime = DateTime.UtcNow});
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
    }
}