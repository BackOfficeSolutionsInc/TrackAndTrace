using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using RadialReview.Models.Admin;
using RadialReview.Exceptions;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class AdminChangeRoles {


		private void CanChangeRoleTest(bool isAdmin, bool hasRole, bool isSwan, bool hasAudit, bool isTT, bool expected, string testName, bool adminSetRoleException = false) {
			var myUserOrganizationIds = new long[] { 1, 2, 3 };
			var ttOrgId = 5;

			var validAudit = new AdminAccessViewModel() {
				AccessId = 1,
				AccessLevel = AdminAccessLevel.View,
				RequestedDurationMinutes = 1,
				Reason = "asdf",
				SetAsEmail="-view-",
			};

			//isAdmin
			var requestedUserOrganizationId = hasRole ? 1 : 4;
			var accountType = isSwan ? AccountType.SwanServices : AccountType.Demo;
			var audit = hasAudit ? validAudit : null;
			var requestedOrgId = isTT ? ttOrgId : 6;

			try {
				var result = UserAccessor.CanChangeToRole(requestedUserOrganizationId, myUserOrganizationIds, requestedOrgId, accountType, new long[] { ttOrgId }, isAdmin, audit, null);
				if (adminSetRoleException)
					Assert.Fail("Expected exception: " + testName);
				Assert.AreEqual(expected, result.Allowed, "FAILED:" + testName);
			} catch (AdminSetRoleException e) {
				if (!adminSetRoleException)
					Assert.Fail("Unexpected exception: " + testName);
			}
		}


		[TestMethod]
		public void CanChangeRoles() {
			
			/*	1	Admin	hasRole	NonSwan	hasAudit	notTT	TRUE	*/
			CanChangeRoleTest(true, true, false, true, false, true, "Admin,hasRole,NonSwan,hasAudit,notTT");
			/*	2	NonAdmin	hasRole	NonSwan	hasAudit	notTT	TRUE	*/
			CanChangeRoleTest(false, true, false, true, false, true, "NonAdmin,hasRole,NonSwan,hasAudit,notTT");
			/*	3	Admin	doesntHaveRole	NonSwan	hasAudit	notTT	TRUE	*/
			CanChangeRoleTest(true, false, false, true, false, true, "Admin,doesntHaveRole,NonSwan,hasAudit,notTT");
			/*	4	NonAdmin	doesntHaveRole	NonSwan	hasAudit	notTT	FALSE	*/
			CanChangeRoleTest(false, false, false, true, false, false, "NonAdmin,doesntHaveRole,NonSwan,hasAudit,notTT");
			/*	5	Admin	hasRole	swan	hasAudit	notTT	TRUE	*/
			CanChangeRoleTest(true, true, true, true, false, true, "Admin,hasRole,swan,hasAudit,notTT");
			/*	6	NonAdmin	hasRole	swan	hasAudit	notTT	TRUE	*/
			CanChangeRoleTest(false, true, true, true, false, true, "NonAdmin,hasRole,swan,hasAudit,notTT");
			/*	7	Admin	doesntHaveRole	swan	hasAudit	notTT	TRUE	*/
			CanChangeRoleTest(true, false, true, true, false, true, "Admin,doesntHaveRole,swan,hasAudit,notTT");
			/*	8	NonAdmin	doesntHaveRole	swan	hasAudit	notTT	FALSE	*/
			CanChangeRoleTest(false, false, true, true, false, false, "NonAdmin,doesntHaveRole,swan,hasAudit,notTT");
			/*	9	Admin	hasRole	NonSwan	doesntHaveAudit	notTT	AdminSetRoleException	*/
			CanChangeRoleTest(true, true, false, false, false, false, "Admin,hasRole,NonSwan,doesntHaveAudit,notTT", true);
			/*	10	NonAdmin	hasRole	NonSwan	doesntHaveAudit	notTT	TRUE	*/
			CanChangeRoleTest(false, true, false, false, false, true, "NonAdmin,hasRole,NonSwan,doesntHaveAudit,notTT");
			/*	11	Admin	doesntHaveRole	NonSwan	doesntHaveAudit	notTT	AdminSetRoleException	*/
			CanChangeRoleTest(true, false, false, false, false, false, "Admin,doesntHaveRole,NonSwan,doesntHaveAudit,notTT", true);
			/*	12	NonAdmin	doesntHaveRole	NonSwan	doesntHaveAudit	notTT	FALSE	*/
			CanChangeRoleTest(false, false, false, false, false, false, "NonAdmin,doesntHaveRole,NonSwan,doesntHaveAudit,notTT");
			/*	13	Admin	hasRole	swan	doesntHaveAudit	notTT	TRUE	*/
			CanChangeRoleTest(true, true, true, false, false, true, "Admin,hasRole,swan,doesntHaveAudit,notTT");
			/*	14	NonAdmin	hasRole	swan	doesntHaveAudit	notTT	TRUE	*/
			CanChangeRoleTest(false, true, true, false, false, true, "NonAdmin,hasRole,swan,doesntHaveAudit,notTT");
			/*	15	Admin	doesntHaveRole	swan	doesntHaveAudit	notTT	TRUE	*/
			CanChangeRoleTest(true, false, true, false, false, true, "Admin,doesntHaveRole,swan,doesntHaveAudit,notTT");
			/*	16	NonAdmin	doesntHaveRole	swan	doesntHaveAudit	notTT	FALSE	*/
			CanChangeRoleTest(false, false, true, false, false, false, "NonAdmin,doesntHaveRole,swan,doesntHaveAudit,notTT");
			/*	17	Admin	hasRole	NonSwan	hasAudit	isTT	TRUE	*/
			CanChangeRoleTest(true, true, false, true, true, true, "Admin,hasRole,NonSwan,hasAudit,isTT");
			/*	18	NonAdmin	hasRole	NonSwan	hasAudit	isTT	TRUE	*/
			CanChangeRoleTest(false, true, false, true, true, true, "NonAdmin,hasRole,NonSwan,hasAudit,isTT");
			/*	19	Admin	doesntHaveRole	NonSwan	hasAudit	isTT	FALSE	*/
			CanChangeRoleTest(true, false, false, true, true, false, "Admin,doesntHaveRole,NonSwan,hasAudit,isTT");
			/*	20	NonAdmin	doesntHaveRole	NonSwan	hasAudit	isTT	FALSE	*/
			CanChangeRoleTest(false, false, false, true, true, false, "NonAdmin,doesntHaveRole,NonSwan,hasAudit,isTT");
			/*	21	Admin	hasRole	swan	hasAudit	isTT	TRUE	*/
			CanChangeRoleTest(true, true, true, true, true, true, "Admin,hasRole,swan,hasAudit,isTT");
			/*	22	NonAdmin	hasRole	swan	hasAudit	isTT	TRUE	*/
			CanChangeRoleTest(false, true, true, true, true, true, "NonAdmin,hasRole,swan,hasAudit,isTT");
			/*	23	Admin	doesntHaveRole	swan	hasAudit	isTT	TRUE	*/
			CanChangeRoleTest(true, false, true, true, true, true, "Admin,doesntHaveRole,swan,hasAudit,isTT");
			/*	24	NonAdmin	doesntHaveRole	swan	hasAudit	isTT	FALSE	*/
			CanChangeRoleTest(false, false, true, true, true, false, "NonAdmin,doesntHaveRole,swan,hasAudit,isTT");
			/*	25	Admin	hasRole	NonSwan	doesntHaveAudit	isTT	TRUE	*/
			CanChangeRoleTest(true, true, false, false, true, true, "Admin,hasRole,NonSwan,doesntHaveAudit,isTT");
			/*	26	NonAdmin	hasRole	NonSwan	doesntHaveAudit	isTT	TRUE	*/
			CanChangeRoleTest(false, true, false, false, true, true, "NonAdmin,hasRole,NonSwan,doesntHaveAudit,isTT");
			/*	27	Admin	doesntHaveRole	NonSwan	doesntHaveAudit	isTT	FALSE	(??)*/
			CanChangeRoleTest(true, false, false, false, true, false, "Admin,doesntHaveRole,NonSwan,doesntHaveAudit,isTT");
			/*	28	NonAdmin	doesntHaveRole	NonSwan	doesntHaveAudit	isTT	FALSE	*/
			CanChangeRoleTest(false, false, false, false, true, false, "NonAdmin,doesntHaveRole,NonSwan,doesntHaveAudit,isTT");
			/*	29	Admin	hasRole	swan	doesntHaveAudit	isTT	TRUE	*/
			CanChangeRoleTest(true, true, true, false, true, true, "Admin,hasRole,swan,doesntHaveAudit,isTT");
			/*	30	NonAdmin	hasRole	swan	doesntHaveAudit	isTT	TRUE	*/
			CanChangeRoleTest(false, true, true, false, true, true, "NonAdmin,hasRole,swan,doesntHaveAudit,isTT");
			/*	31	Admin	doesntHaveRole	swan	doesntHaveAudit	isTT	TRUE	*/
			CanChangeRoleTest(true, false, true, false, true, true, "Admin,doesntHaveRole,swan,doesntHaveAudit,isTT");
			/*	32	NonAdmin	doesntHaveRole	swan	doesntHaveAudit	isTT	FALSE	*/
			CanChangeRoleTest(false, false, true, false, true, false, "NonAdmin,doesntHaveRole,swan,doesntHaveAudit,isTT");












		}
	}
}
