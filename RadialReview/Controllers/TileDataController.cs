using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Todos;

namespace RadialReview.Controllers
{
    public class TileDataController : BaseController
    {
        // GET: TileData

		[Access(AccessLevel.Any)]
		//[OutputCache(Duration = 600, VaryByParam = "none",Location=OutputCacheLocation.Server)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public PartialViewResult UserTodo2()
		{
			return PartialView("UserTodo");
		}

		[Access(AccessLevel.Any)]
		//[OutputCache(Duration = 600, VaryByParam = "none", Location = OutputCacheLocation.Server)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public PartialViewResult UserScorecard2()
		{
			return PartialView("UserScorecard");
		}
		[Access(AccessLevel.Any)]
		//[OutputCache(Duration = 600, VaryByParam = "none", Location = OutputCacheLocation.Server)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public PartialViewResult UserRock2()
		{
			return PartialView("UserRock");
		}
		[Access(AccessLevel.Any)]
		//[OutputCache(Duration = 600, VaryByParam = "none", Location = OutputCacheLocation.Server)]
		//[OutputCache(NoStore = true, Duration = 0, VaryByParam = "*")]
		public PartialViewResult UserManage2()
		{
			return PartialView("UserManage");
		}

		[Access(AccessLevel.User)]
		public PartialViewResult UserProfile2()
		{
			return PartialView("UserProfile",GetUser().User);
		}
    }
}