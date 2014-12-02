using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
			public DateTime CurrentTime = DateTime.UtcNow;
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult Modal(long id)
		{
			var rocks = _UserAccessor.GetRocks(GetUser(), id);
			return PartialView(new RocksController.RockVM { Rocks = rocks, UserId = id });
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult BlankEditorRow()
		{
			return PartialView("_RockRow", new RockModel());
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult Modal(RocksController.RockVM model)
		{
			foreach (var r in model.Rocks)
			{
				r.ForUserId = model.UserId;
			}
			_UserAccessor.EditRocks(GetUser(), model.UserId, model.Rocks);
			return Json(ResultObject.SilentSuccess());
		}

		//public ActionResult Assessment()
    }
}