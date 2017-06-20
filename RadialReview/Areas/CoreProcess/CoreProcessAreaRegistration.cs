using System.Web.Mvc;

namespace RadialReview.Areas.CoreProcess {
	public class CoreProcessAreaRegistration : AreaRegistration {
		public override string AreaName {
			get {
				return "CoreProcess";
			}
		}

		public override void RegisterArea(AreaRegistrationContext context) {
			context.MapRoute(
				"CoreProcess_default",
				"CoreProcess/{controller}/{action}/{id}",
				new { action = "Index", id = UrlParameter.Optional },
				namespaces: new[] { "RadialReview.Areas.CoreProcess.Controllers" }
			);
		}
	}
}