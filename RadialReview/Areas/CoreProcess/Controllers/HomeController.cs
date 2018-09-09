using RadialReview.Controllers;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace RadialReview.Areas.CoreProcess.Controllers {
	public class HomeController : BaseController {
        [Access(AccessLevel.UserOrganization)]
        // GET: CoreProcess/Home
        public async Task<ActionResult> Index() {            
            return View();
        }
    }
}
