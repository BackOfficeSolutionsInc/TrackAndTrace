using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;

namespace TractionTools.Tests.Permissions {
	public class BasePermissionsTest : BaseTest {




		public class Ctx {
			public FullOrg Org { get; set; }
			public Org OtherOrg { get; set; }

			//Helpers to dive into Org
			public long Id { get { return Org.Id; } }
			public UserOrganizationModel Manager { get { return Org.Manager; } }
			public UserOrganizationModel Employee { get { return Org.Employee; } }
			public UserOrganizationModel Middle { get { return Org.Middle; } }
			public UserOrganizationModel E1 { get { return Org.E1; } }
			public UserOrganizationModel E2 { get { return Org.E2; } }
			public UserOrganizationModel E3 { get { return Org.E3; } }
			public UserOrganizationModel E4 { get { return Org.E4; } }
			public UserOrganizationModel E5 { get { return Org.E5; } }
			public UserOrganizationModel E6 { get { return Org.E6; } }
			public UserOrganizationModel E7 { get { return Org.E7; } }
			public UserOrganizationModel Client { get { return Org.Client; } }
			public List<UserOrganizationModel> AllManagers { get { return Org.AllManagers; } }
			public List<UserOrganizationModel> AllNonmanagers { get { return Org.AllNonmanagers; } }
			public List<UserOrganizationModel> AllUsers { get { return Org.AllUsers; } }
			public List<UserOrganizationModel> AllAdmins { get { return Org.AllAdmins; } }


			public async Task<L10> CreateL10(UserOrganizationModel caller, ISession s, params UserOrganizationModel[] attendees) {
				return await CreateL10(caller,s, "Meeting", attendees);
			}

			public async Task<L10> CreateL10(UserOrganizationModel caller, ISession s, string name, params UserOrganizationModel[] attendees) {
				var perms = PermissionsUtility.Create(s, caller);
				var orgId = caller.Organization.Id;
				var l10 = await L10Accessor.CreateBlankRecurrence(s,perms,orgId, false);

				foreach (var a in attendees ?? new UserOrganizationModel[0]) {
					await L10Accessor.AddAttendee(s, perms, null, l10.Id, a.Id);
				}
				return new L10 {
					Creator = caller,
					Employee = null,
					Org = caller.Organization,
					Recur = l10
				};
			}

			public async Task<L10> CreateL10(UserOrganizationModel caller, params UserOrganizationModel[] attendees) {
				return await CreateL10(caller, "Meeting", attendees);
			}
			public async Task<L10> CreateL10(UserOrganizationModel caller, string name, params UserOrganizationModel[] attendees) {
				var l10 = await L10Accessor.CreateBlankRecurrence(caller, caller.Organization.Id, false);

				foreach (var a in attendees ?? new UserOrganizationModel[0]) {
					await L10Accessor.AddAttendee(caller, l10.Id, a.Id);
				}
				return new L10 {
					Creator = caller,
					Employee = null,
					Org = caller.Organization,
					Recur = l10
				};
			}

			public PermissionsAccessor Perms { get; set; }
			private Ctx() { }

			public static async Task<Ctx> Build() {
				var ctx = new Ctx();
				RemoveIsTest();
				SessionTransaction stx= new SessionTransaction();
				stx.s = HibernateSession.GetCurrentSession();
				try {
					stx.tx = stx.s.BeginTransaction();
					try {
						//stx = new SessionTransaction() { s = s, tx = tx };
						ctx.Org = await OrgUtil.CreateFullOrganization(stx);
						stx.tx.Commit();
						stx.s.Flush();
					} finally {
						stx.tx.Dispose();
					}
				} finally {
					stx.s.Dispose();
				}
				RemoveIsTest();
				stx.s = HibernateSession.GetCurrentSession();
				try {
					stx.tx = stx.s.BeginTransaction();
					try {
						//stx = new SessionTransaction() { s = s, tx = tx };
						ctx.OtherOrg = await OrgUtil.CreateOrganization(stx);
						ctx.Perms = new PermissionsAccessor();
						stx.tx.Commit();
						stx.s.Flush();
					} finally {
						stx.tx.Dispose();
					}
				} finally {
					stx.s.Dispose();
				}
				ctx.Perms = new PermissionsAccessor();
				return ctx;
			}

			public void AssertAll(Action<PermissionsUtility> ensurePermitted, IEnumerable<UserOrganizationModel> trueFor) {
				AssertAll(ensurePermitted, trueFor.ToArray());
			}
			public void AssertAll(Action<PermissionsUtility> ensurePermitted, params UserOrganizationModel[] trueFor) {
				var myOrgUsers = Org.AllUsers.Where(x => trueFor.Any(y => y.Id == x.Id));
				var otherOrgUsers = OtherOrg.AllUsers.Where(x => trueFor.Any(y => y.Id == x.Id));

				Org.AssertAllUsers(user => Perms.IsPermitted(user, ensurePermitted), myOrgUsers);
				OtherOrg.AssertAllUsers(user => Perms.IsPermitted(user, ensurePermitted), otherOrgUsers);

			}

			public async Task RegisterUser(UserOrganizationModel user) {
				await Org.RegisterUser(user);
			}

		}

	}
}