using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.Utilities;
using RadialReview.Accessors;
using RadialReview.Models.Issues;
using System.Threading.Tasks;
using RadialReview.Utilities;
using static RadialReview.Models.PermItem;
using RadialReview.Exceptions;
using RadialReview.Models.Todo;
using System.Collections.Generic;
using RadialReview.Models;
using NHibernate;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class UserPermissions : BasePermissionsTest {

		[TestMethod]
		[TestCategory("Permissions")]
		public void UserDeleted() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var user = new UserOrganizationModel();
					s.Save(user);
					PermissionsUtility.Create(s, user);

				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var user = new UserOrganizationModel();
					user.DeleteTime = DateTime.MinValue;
					s.Save(user);
					Throws<PermissionsException>(() => PermissionsUtility.Create(s, user));

				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var user = new UserOrganizationModel();
					user.DeleteTime = DateTime.MaxValue;
					s.Save(user);
					PermissionsUtility.Create(s, user);
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var user = new UserOrganizationModel();
					user.DeleteTime = DateTime.UtcNow.AddSeconds(2);
					s.Save(user);
					PermissionsUtility.Create(s, user);
				}
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var user = new UserOrganizationModel();
					user.DeleteTime = DateTime.UtcNow.AddSeconds(-2);
					s.Save(user);
					Throws<PermissionsException>(() => PermissionsUtility.Create(s, user));


				}
			}

		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditUserModel() {
			var c = await Ctx.Build();

			await c.Org.RegisterUser(c.Employee);
			c.AssertAll(p => p.EditUserModel(c.Employee.User.Id), c.Employee);

		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditUserOrganization() {
			var c = await Ctx.Build();

			c.AssertAll(p => p.EditUserOrganization(c.Manager.Id), c.Manager);
			c.AssertAll(p => p.EditUserOrganization(c.Employee.Id), c.Employee, c.Manager);
			c.AssertAll(p => p.EditUserOrganization(c.Middle.Id), c.Middle, c.Manager);
			c.AssertAll(p => p.EditUserOrganization(c.E1.Id), c.E1, c.Manager, c.Middle);
			c.AssertAll(p => p.EditUserOrganization(c.E2.Id), c.E2, c.Manager, c.Middle);
			c.AssertAll(p => p.EditUserOrganization(c.E3.Id), c.E3, c.Manager, c.Middle);
			c.AssertAll(p => p.EditUserOrganization(c.E4.Id), c.E4, c.Manager, c.E1);
			c.AssertAll(p => p.EditUserOrganization(c.E5.Id), c.E5, c.Manager, c.E1);
			c.AssertAll(p => p.EditUserOrganization(c.E6.Id), c.E6, c.Manager, c.Middle, c.E2);

			c.AssertAll(p => p.EditUserOrganization(c.E7.Id), c.E7, c.Manager);
			c.AssertAll(p => p.EditUserOrganization(c.Client.Id), c.Client, c.Manager);
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ViewUserOrganization() {
			var c = await Ctx.Build();
			foreach (var user in c.AllUsers) {
				c.AssertAll(p => p.ViewUserOrganization(user.Id, false), c.AllUsers);
			}

			c.AssertAll(p => p.ViewUserOrganization(c.Manager.Id, true), c.Manager);
			c.AssertAll(p => p.ViewUserOrganization(c.Middle.Id, true), c.Manager, c.Middle);
			c.AssertAll(p => p.ViewUserOrganization(c.E1.Id, true), c.E1, c.Manager, c.Middle);
			c.AssertAll(p => p.ViewUserOrganization(c.E2.Id, true), c.E2, c.Manager, c.Middle);
			c.AssertAll(p => p.ViewUserOrganization(c.E6.Id, true), c.E6,c.E2, c.Manager, c.Middle);

		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ManagesUserOrganization() {
			var c = await Ctx.Build();

			c.AssertAll(p => p.ManagesUserOrganization(c.Manager.Id, false), c.Manager);
			//Only admins manage themselves when self is disabled
			c.AssertAll(p => p.ManagesUserOrganization(c.Manager.Id, true), c.Manager);

			c.AssertAll(p => p.ManagesUserOrganization(c.Middle.Id, false), c.Middle, c.Manager);
			c.AssertAll(p => p.ManagesUserOrganization(c.Middle.Id, true),  c.Manager);

			c.AssertAll(p => p.ManagesUserOrganization(c.E2.Id, false), c.E2, c.Middle, c.Manager);
			c.AssertAll(p => p.ManagesUserOrganization(c.E2.Id, true),  c.Middle, c.Manager);

			c.AssertAll(p => p.ManagesUserOrganization(c.E6.Id, false), c.E2, c.Middle, c.Manager);
			c.AssertAll(p => p.ManagesUserOrganization(c.E6.Id, true),  c.E2, c.Middle, c.Manager);

			c.AssertAll(p => p.ManagesUserOrganization(c.E4.Id, false), c.E1, c.Manager);
			c.AssertAll(p => p.ManagesUserOrganization(c.E4.Id, true),  c.E1, c.Manager);

			c.AssertAll(p => p.ManagesUserOrganization(c.Employee.Id, false), c.Manager);
			c.AssertAll(p => p.ManagesUserOrganization(c.Employee.Id, true), c.Manager);

			c.AssertAll(p => p.ManagesUserOrganization(c.E7.Id, false), c.Manager);
			c.AssertAll(p => p.ManagesUserOrganization(c.E7.Id, true), c.Manager);

			c.AssertAll(p => p.ManagesUserOrganization(c.Client.Id, false), c.Manager);
			c.AssertAll(p => p.ManagesUserOrganization(c.Client.Id, true), c.Manager);
		}


		[TestMethod]
		[TestCategory("Permissions")]
		public async Task RemoveUser() {
			var c = await Ctx.Build();
			//Admins only by default
			foreach (var user in c.AllUsers) {
				c.AssertAll(p => p.RemoveUser(user.Id), c.AllAdmins);
			}

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanRemoveUsers = true;
				s.Update(org);
			});

			c.AssertAll(p => p.RemoveUser(c.Employee.Id), c.Manager);
			c.AssertAll(p => p.RemoveUser(c.Middle.Id), c.Manager);
			c.AssertAll(p => p.RemoveUser(c.E6.Id), c.Manager, c.E2, c.Middle);
			c.AssertAll(p => p.RemoveUser(c.E7.Id), c.Manager);
			c.AssertAll(p => p.RemoveUser(c.Manager.Id), c.Manager);
		}

		/*
		 
		[TestMethod]
		[TestCategory("Permissions")]
		public async Task XXX() {
			var c = await Ctx.Build();
			c.AssertAll(p => p.XXX(YYY), c.Manager);
		}

		 */
	}
}
