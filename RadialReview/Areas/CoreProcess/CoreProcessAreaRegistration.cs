using System.Web.Mvc;

namespace RadialReview.Areas.CoreProcess {
	public class CoreProcessAreaRegistration : AreaRegistration {
		public override string AreaName {
			get {
				return "CoreProcess";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context) {

			//context.MapRoute(
			//"Tasks",
			//"CoreProcess/{action}/{id}",
			//new { controller = "Process", action = "Tasks", id = UrlParameter.Optional },
			//namespaces: new[] { "RadialReview.Areas.CoreProcess.Controllers" }
			//);

			context.MapRoute(
				"CoreProcess_default",
				"CoreProcess/{controller}/{action}/{id}",
				new { controller = "Process", action = "Index", id = UrlParameter.Optional },
				namespaces: new[] { "RadialReview.Areas.CoreProcess.Controllers" }
			);
		}
	}
}