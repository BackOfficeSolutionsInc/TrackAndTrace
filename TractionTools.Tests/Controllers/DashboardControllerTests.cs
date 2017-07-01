using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Controllers;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;
using System.Threading.Tasks;
using RadialReview.Models.Json;
using System.Web.Mvc;
using static RadialReview.Controllers.DashboardController;
using RadialReview.Models.Dashboard;

namespace TractionTools.Tests.Controllers {
	[TestClass]
	public class DashboardControllerTests : BaseTest {
		[TestMethod]
		public async Task TestCreateDashboard() {
			var org = await OrgUtil.CreateOrganization();

			//Register 1 for manager
			using (var ctrl = new ControllerCtx<DashboardController>(org.Manager)) {
				var json = ctrl.GetJson(x => x.CreateDashboard("title1", false, true));
				Assert.AreEqual(1L, json.GetModel<long>());
				var dash = ctrl.GetView(x => x.Index(1L));
				dash.AssertModelType<DashboardVM>();
				Assert.AreEqual("title1", dash.ViewBag.WorkspaceName);

			}

			//Register 1 for employee
			await org.RegisterUser(org.Employee);
			using (var c = new ControllerCtx<DashboardController>(org.Employee)) {
				var redirectDash = c.GetRedirect(x => x.Index());
				Assert.AreEqual(2L, redirectDash.RouteValues["id"]);
				var view = c.GetView(x => x.Index());
				view.AssertModelType<DashboardVM>();
				Assert.AreEqual(null, view.ViewBag.WorkspaceName);
			}

			//Manager cannot access
			using (var c = new ControllerCtx<DashboardController>(org.Manager)) {
				Throws<Exception>(() => c.Get().Index(2L));
			}

			//Create another for manager
			using (var c = new ControllerCtx<DashboardController>(org.Manager)) {
				var json = c.GetJson(x => x.CreateDashboard("title2", false, true));
				Assert.AreEqual(3L, json.GetModel<long>());
				var view = c.GetView(x => x.Index(3L));
				view.AssertModelType<DashboardVM>();
				Assert.AreEqual("title2", view.ViewBag.WorkspaceName);
			}
		}

		[TestMethod]
		public async Task TestCreateTile() {
			var org = await OrgUtil.CreateOrganization();

			//Register 1 for manager
			using (var ctrl = new ControllerCtx<DashboardController>(org.Manager)) {
				var json = ctrl.GetJson(x => x.CreateDashboard("title1", false, true));
				var dashId = json.GetModel<long>();

				foreach (TileType e in Enum.GetValues(typeof(TileType))) {
					ctrl.GetJson(x => x.CreateTile(dashId, false, type: e, title: "" + e));
				}				
			}


		}
	}
}
