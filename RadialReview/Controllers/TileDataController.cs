using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Todos;

namespace RadialReview.Controllers
{
    public class TileDataController : BaseController
    {
        // GET: TileData

		[Access(AccessLevel.Any)]
		public PartialViewResult UserTodo()
		{
			return PartialView();
		}

		[Access(AccessLevel.Any)]
		public PartialViewResult UserScorecard()
		{
			return PartialView();
		}

		[Access(AccessLevel.Any)]
		public PartialViewResult UserRock()
		{
			return PartialView();
		}
		[Access(AccessLevel.Any)]
		public PartialViewResult UserManage()
		{
			return PartialView();
		}
		[Access(AccessLevel.User)]
		public PartialViewResult UserProfile()
		{
			return PartialView(GetUserModel());
		}
    }
}