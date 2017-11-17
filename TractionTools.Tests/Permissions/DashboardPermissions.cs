using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Models.Dashboard;
using System.Threading.Tasks;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class DashboardPermissions : BasePermissionsTest {
		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ViewDashboardForUser() {
			var c = await Ctx.Build();
			await c.Org.RegisterAllUsers();
			await c.OtherOrg.RegisterAllUsers();

			foreach (var u in c.AllUsers) {
				c.AssertAll(p => p.ViewDashboardForUser(u.User.Id), u);
			}
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditDashboard() {
			var c = await Ctx.Build();

			await c.Org.RegisterAllUsers();
			await c.OtherOrg.RegisterAllUsers();

			foreach (var u in c.AllUsers) {
				var dash = DashboardAccessor.CreateDashboard(u, null, false, true);
				var did = dash.Id;
				c.AssertAll(p => p.EditDashboard(did), u);
				Assert.IsTrue(dash.PrimaryDashboard);
			}

			var another = DashboardAccessor.CreateDashboard(c.E1, "another", false, false);
			c.AssertAll(p => p.EditDashboard(another.Id), c.E1);			
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditTile() {
			var c = await Ctx.Build();

			await c.Org.RegisterAllUsers();
			await c.OtherOrg.RegisterAllUsers();

			var dash = DashboardAccessor.CreateDashboard(c.E1, null, false, true);

			var tile = DashboardAccessor.CreateTile(c.E1, dash.Id, 1, 1, 1, 1, "a", null, TileType.FAQGuide);

			c.AssertAll(p => p.EditTile(tile.Id), c.E1);
		}

		/*
		[TestMethod]
		[TestCategory("Permissions")]
		public void XXX() {
			var c = await Ctx.Build();
			c.AssertAll(p => p.XXX(YYY), c.Manager);
			//var perm = new Action<PermissionsUtility>(p=>p.XXX(YYY));
			//c.AssertAll(perm, c.Manager);
		}
		*/
	}
}
