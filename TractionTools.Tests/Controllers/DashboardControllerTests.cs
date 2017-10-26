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
using NHibernate;
using RadialReview.Utilities;
using System.Collections.Generic;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Accessors;
using static RadialReview.Controllers.L10Controller;
using RadialReview.Models.Askables;
using System.Linq;
using RadialReview.Exceptions;

namespace TractionTools.Tests.Controllers {
	[TestClass]
	public class DashboardControllerTests : BaseTest {
		[TestMethod]
		[TestCategory("Controller")]
		public async Task TestCreateDashboard() {
			var org = await OrgUtil.CreateOrganization();
			long dashNum;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					dashNum = s.QueryOver<Dashboard>().RowCountInt64();
				}
			}

			//Register 1 for manager
			using (var ctrl = new ControllerCtx<DashboardController>(org.Manager)) {
				var json = ctrl.GetJson(x => x.CreateDashboard("title1", false, true));
				Assert.AreEqual(dashNum + 1, json.GetModel<long>());
				var dash = ctrl.GetView(x => x.Index(dashNum + 1));
				dash.AssertModelType<DashboardVM>();
				Assert.AreEqual("title1", dash.ViewBag.WorkspaceName);
			}

			//Register 1 for employee
			await org.RegisterUser(org.Employee);
			using (var c = new ControllerCtx<DashboardController>(org.Employee)) {
				var redirectDash = c.GetRedirect(x => x.Index());
				Assert.AreEqual(dashNum + 2, redirectDash.RouteValues["id"]);
				var view = c.GetView(x => x.Index());
				view.AssertModelType<DashboardVM>();
				Assert.AreEqual(null, view.ViewBag.WorkspaceName);
			}

			//Manager cannot access
			using (var c = new ControllerCtx<DashboardController>(org.Manager)) {
				Throws<Exception>(() => c.Get(x=>x.Index(dashNum + 2)));
			}

			//Create another for manager
			using (var c = new ControllerCtx<DashboardController>(org.Manager)) {
				var json = c.GetJson(x => x.CreateDashboard("title2", false, true));
				Assert.AreEqual(dashNum + 3, json.GetModel<long>());
				var view = c.GetView(x => x.Index(dashNum + 3L));
				view.AssertModelType<DashboardVM>();
				Assert.AreEqual("title2", view.ViewBag.WorkspaceName);
			}
		}

		[TestMethod]
		[TestCategory("Controller")]
		public async Task TestCreateTile() {
			var org = await OrgUtil.CreateOrganization();
			//Register 1 for manager
			using (var ctrl = new ControllerCtx<DashboardController>(org.Manager)) {
				var json = ctrl.GetJson(x => x.CreateDashboard("title1", false, true));
				var dashId = json.GetModel<long>();

				var tilesJson = ctrl.GetJson(x => x.Tiles(dashId));
				var tiles = tilesJson.GetModel<List<TileModel>>();
				Assert.AreEqual(6, tiles.Count);
				var tileCount = 6;
				foreach (TileType e in Enum.GetValues(typeof(TileType))) {
					if (e != TileType.Invalid) {
						ctrl.GetJson(x => x.CreateTile(dashId, false, type: e, title: "" + e, dataurl: "/url"));
						tileCount += 1;
					}
				}
				tilesJson = ctrl.GetJson(x => x.Tiles(dashId));
				tiles = tilesJson.GetModel<List<TileModel>>();
				Assert.AreEqual(tileCount, tiles.Count);
			}
		}

		[TestMethod]
		[TestCategory("Controller")]
		public async Task TestDashboardData() {
			MockHttpContext();
			var org = await OrgUtil.CreateOrganization();
			await org.RegisterUser(org.Employee);
			long dashId;
			//Register 1 for manager
			using (var ctrl = new ControllerCtx<DashboardController>(org.Employee)) {
				var json = ctrl.GetJson(x => x.CreateDashboard("title1", true, true));
				dashId = json.GetModel<long>();
			}

			var l10 = await org.CreateL10();
			await l10.AddAttendee(org.Employee);

			var l10Other = await org.CreateL10();

			await l10.AddRock("Rock1");
			await l10.AddRock("Rock2");
			await l10.AddRock("Rock3-not mine", org.Manager);
			await l10Other.AddRock("Rock4-other", org.Manager);

			await l10.AddMeasurable("Meas1");
			await l10.AddMeasurable("Meas2-not mine", org.Manager);
			await l10Other.AddMeasurable("Meas3-other", org.Manager);

			await l10.AddTodo("Todo1");
			try {
				await l10.AddTodo("Todo2-not mine", org.Manager);
				Assert.Fail();
			} catch (PermissionsException) {
			}
			await L10Accessor.AddAttendee(org.Manager, l10.Id, org.Manager.Id);
			await l10.AddTodo("Todo2-not mine", org.Manager);
			await L10Accessor.AddAttendee(org.Manager, l10Other.Id, org.Manager.Id);
			await l10Other.AddTodo("Todo3-other", org.Manager);

			await l10.AddIssue("Issue1");
			await l10Other.AddIssue("Issue2-other", org.Manager);

			using (var ctrl = new ControllerCtx<DashboardDataController>(org.Employee)) {
				var json = await ctrl.GetJson(x => x.Data2(org.Employee.Id));
				var model = json.GetModel<ListDataVM>();

				Assert.AreEqual(1, model.Todos.Count());
				Assert.AreEqual("Todo1", model.Todos.First().Name);
				Assert.AreEqual(2, model.Rocks.Count());
				Assert.AreEqual("Rock1", model.Rocks.First().Name);
				Assert.AreEqual("Rock2", model.Rocks.Last().Name);
				Assert.IsNull(model.Scorecard); // This is requested with a Load Url

				Assert.AreEqual(0, model.L10Todos.Count());
				Assert.AreEqual(0, model.L10SolvedIssues.Count());
				Assert.AreEqual(0, model.L10Scorecards.Count());
				Assert.AreEqual(0, model.L10Rocks.Count());
				Assert.AreEqual(0, model.L10Issues.Count());
				Assert.IsNull(model.CoreValues);
				Assert.IsNull(model.Members);
				Assert.IsNull(model.Notifications);

				Assert.AreEqual(1, model.LoadUrls.Count());
				Assert.IsTrue(model.LoadUrls.First().Data.StartsWith("/DashboardData/UserScorecardData/" + org.Employee.Id + "?userId=" + org.Employee.Id + "&completed=False&fullScorecard=False"));

				json =await ctrl.GetJson(x => x.UserScorecardData(org.Employee.Id, org.Employee.Id, false, false));
				model = json.GetModel<ListDataVM>();

				Assert.IsNull(model.Todos);
				Assert.IsNull(model.Rocks);
				Assert.IsNull(model.CoreValues);
				Assert.IsNull(model.Members);
				Assert.IsNull(model.Notifications);
				Assert.AreEqual(0, model.L10Todos.Count());
				Assert.AreEqual(0, model.L10SolvedIssues.Count());
				Assert.AreEqual(0, model.L10Scorecards.Count());
				Assert.AreEqual(0, model.L10Rocks.Count());
				Assert.AreEqual(0, model.L10Issues.Count());

				Assert.IsNotNull(model.Scorecard); // This is requested with a Load Url
				Assert.AreEqual(1, model.Scorecard.Measurables.Count());
				Assert.AreEqual("Meas1", model.Scorecard.Measurables.First().Name);
			}
			long tileId;
			using (var ctrl = new ControllerCtx<DashboardController>(org.Employee)) {
				ctrl.GetJson(x => x.CreateTile(dashId, false, type: TileType.L10Issues, title: "" + TileType.L10Issues, dataurl: "/url", keyId: "" + l10.Id));
				ctrl.GetJson(x => x.CreateTile(dashId, false, type: TileType.L10Todos, title: "" + TileType.L10Todos, dataurl: "/url", keyId: "" + l10.Id));
				tileId = ctrl.GetJson(x => x.CreateTile(dashId, false, type: TileType.L10Scorecard, title: "" + TileType.L10Scorecard, dataurl: "/url", keyId: "" + l10.Id)).GetModel<TileModel>().Id;
				ctrl.GetJson(x => x.CreateTile(dashId, false, type: TileType.L10Rocks, title: "" + TileType.L10Rocks, dataurl: "/url", keyId: "" + l10.Id));
			}

			using (var ctrl = new ControllerCtx<DashboardDataController>(org.Employee)) {
				var json = await ctrl.GetJson(x => x.Data2(org.Employee.Id,dashboardId : dashId));
				var model = json.GetModel<ListDataVM>();


				Assert.AreEqual(1, model.Todos.Count());
				Assert.AreEqual("Todo1", model.Todos.First().Name);
				Assert.AreEqual(2, model.Rocks.Count());
				Assert.AreEqual("Rock1", model.Rocks.First().Name);
				Assert.AreEqual("Rock2", model.Rocks.Last().Name);
				Assert.IsNull(model.Scorecard); // This is requested with a Load Url

				//Number of tiles
				Assert.AreEqual(1, model.L10Todos.Count());
				Assert.AreEqual(1, model.L10Rocks.Count());
				Assert.AreEqual(1, model.L10Issues.Count());
				Assert.AreEqual(0, model.L10SolvedIssues.Count());
				Assert.AreEqual(0, model.L10Scorecards.Count());// This is requested with a Load Url			
				Assert.AreEqual(2, model.LoadUrls.Count());
				Assert.IsTrue(model.LoadUrls.Last().Data.StartsWith("/DashboardData/L10ScorecardData/"+org.Employee.Id+"?name=&scorecardTileId="+tileId+"&l10Id="+l10.Id+"&completed=False&fullScorecard=False"));


				//Data within tile
				Assert.AreEqual(2, model.L10Todos.First().Contents.Count());
				Assert.AreEqual(3, model.L10Rocks.First().Contents.Count());
				Assert.AreEqual(1, model.L10Issues.First().Contents.Issues.Count());
				Assert.AreEqual(0, model.L10SolvedIssues.Count());
				
				Assert.IsNull(model.CoreValues);
				Assert.IsNull(model.Members);
				Assert.IsNull(model.Notifications);

				//Get L10Scorecard
				json = await ctrl.GetJson(x => x.L10ScorecardData(org.Employee.Id, "", tileId, l10.Id, false, false));
				model = json.GetModel<ListDataVM>();
				Assert.AreEqual(2, model.L10Scorecards.First().Contents.Measurables.Count());



			}
		}
	}
}
