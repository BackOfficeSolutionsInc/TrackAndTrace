using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.Tests.TestUtils;
using RadialReview.Utilities;
using static RadialReview.Models.PermItem;
using static TractionTools.Tests.Permissions.BasePermissionsTest;
using System.Threading.Tasks;
using System.Linq;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using NHibernate;
using RadialReview.Utilities.DataTypes;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class PermissionsUtilityTests : BaseTest {
		[TestMethod]
		[TestCategory("Permissions")]
		public async Task GetIdsForResourceThatUserIsMemberOfTest() {
			var c = await Ctx.Build();
			var nodes = QuarterlyConversationAccessor.AvailableByAboutsForMe(c.E1, true);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					Assert.AreEqual(10, Enum.GetValues(typeof(ResourceType)).GetLength(0));

					var perms = PermissionsUtility.Create(s, c.E1);
					//AccountabilityHierarchy
					{
						var accIds = perms.GetIdsForResourceThatUserIsMemberOf(ResourceType.AccountabilityHierarchy, c.E1.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(c.Org.Organization.AccountabilityChartId, accIds.First());
					}
					//CoreProcess - Members not implemented.
					{
						Throws<NotImplementedException>(() => perms.GetIdsForResourceThatUserIsMemberOf(ResourceType.CoreProcess, c.E1.Id));
						//var accIds = perms.GetIdsForResourceThatUserIsMemberOf(ResourceType.CoreProcess, c.E1.Id);
						//Assert.AreEqual(0, accIds.Count());
						//var pda = new ProcessDefAccessor();
						//var def = await pda.CreateProcessDef(s, PermissionsUtility.Create(s, c.E1), "cp");
						//accIds = perms.GetIdsForResourceThatUserIsMemberOf(ResourceType.CoreProcess, c.E1.Id);
						//Assert.AreEqual(1, accIds.Count());
						//Assert.AreEqual(def, accIds.First());
					}
					//L10Recurrence
					{
						var accIds = perms.GetIdsForResourceThatUserIsMemberOf(ResourceType.L10Recurrence, c.E1.Id);
						Assert.AreEqual(0, accIds.Count());

						var l10 = await c.CreateL10(c.E1, s);
						accIds = perms.GetIdsForResourceThatUserIsMemberOf(ResourceType.L10Recurrence, c.E1.Id);
						Assert.AreEqual(0, accIds.Count());

						await l10.AddAttendee(s, c.E1);

						accIds = perms.GetIdsForResourceThatUserIsMemberOf(ResourceType.L10Recurrence, c.E1.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(l10.Id, accIds.First());
					}
					//SurveyContainer
					{
						var accIds = perms.GetIdsForResourceThatUserIsMemberOf(ResourceType.SurveyContainer, c.E1.Id);
						Assert.AreEqual(0, accIds.Count());

						var sc = QuarterlyConversationAccessor.GenerateQuarterlyConversation_Unsafe(s, PermissionsUtility.Create(s, c.E1), "sc", nodes, new DateRange(), DateTime.UtcNow.AddDays(1), false);

						accIds = perms.GetIdsForResourceThatUserIsMemberOf(ResourceType.SurveyContainer, c.E1.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(sc.SurveyContainerId, accIds.First());
					}
				}
			}
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task GetIdsForResourcesCreatedByUserTest() {
			var c = await Ctx.Build();
			var nodes = QuarterlyConversationAccessor.AvailableByAboutsForMe(c.E1, true);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					Assert.AreEqual(10, Enum.GetValues(typeof(ResourceType)).GetLength(0));
					var perms = PermissionsUtility.Create(s, c.E1);
					//AccountabilityHierarchy
					{
						var accIds = perms.GetIdsForResourcesCreatedByUser(ResourceType.AccountabilityHierarchy, c.E1.Id);
						Assert.AreEqual(0, accIds.Count());
					}
					//L10Recurrence
					{
						var accIds = perms.GetIdsForResourcesCreatedByUser(ResourceType.L10Recurrence, c.E1.Id);
						Assert.AreEqual(0, accIds.Count());

						var l10 = await c.CreateL10(c.E1, s, c.E2);
						accIds = perms.GetIdsForResourcesCreatedByUser(ResourceType.L10Recurrence, c.E2.Id);
						Assert.AreEqual(0, accIds.Count());

						accIds = perms.GetIdsForResourcesCreatedByUser(ResourceType.L10Recurrence, c.E1.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(l10.Id, accIds.First());
					}
					//CoreProcess
					{
						var accIds = perms.GetIdsForResourcesCreatedByUser(ResourceType.CoreProcess, c.E1.Id);
						Assert.AreEqual(0, accIds.Count());

						var pda = new ProcessDefAccessor();
						var def = await pda.CreateProcessDef(s, PermissionsUtility.Create(s, c.E1), "cp");
						accIds = perms.GetIdsForResourcesCreatedByUser(ResourceType.CoreProcess, c.E1.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(def, accIds.First());
					}
					//SurveyContainer
					{
						var accIds = perms.GetIdsForResourcesCreatedByUser(ResourceType.SurveyContainer, c.E1.Id);
						Assert.AreEqual(0, accIds.Count());

						var sc = QuarterlyConversationAccessor.GenerateQuarterlyConversation_Unsafe(s, PermissionsUtility.Create(s, c.E1), "sc", nodes, new DateRange(), DateTime.UtcNow.AddDays(1), false);

						accIds = perms.GetIdsForResourcesCreatedByUser(ResourceType.SurveyContainer, c.E1.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(sc.SurveyContainerId, accIds.First());
					}
				}
			}
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task GetIdsForResourceForOrganizationTest() {
			var c = await Ctx.Build();
			var scNodes = QuarterlyConversationAccessor.AvailableByAboutsForMe(c.E1, true);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					Assert.AreEqual(10, Enum.GetValues(typeof(ResourceType)).GetLength(0));
					var perms = PermissionsUtility.Create(s, c.E1);
					//AccountabilityHierarchy
					{
						var accIds = perms.GetIdsForResourceForOrganization(ResourceType.AccountabilityHierarchy, c.Org.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(c.Org.Organization.AccountabilityChartId, accIds.First());
					}
					//L10Recurrence
					{
						var accIds = perms.GetIdsForResourceForOrganization(ResourceType.L10Recurrence, c.Org.Id);
						Assert.AreEqual(0, accIds.Count());

						var l10 = await c.CreateL10(c.E1, s, c.E2);
						accIds = perms.GetIdsForResourceForOrganization(ResourceType.L10Recurrence, c.Org.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(l10.Id, accIds.First());
					}
					//CoreProcess
					{
						var accIds = perms.GetIdsForResourceForOrganization(ResourceType.CoreProcess, c.Org.Id);
						Assert.AreEqual(0, accIds.Count());

						var pda = new ProcessDefAccessor();
						var def = await pda.CreateProcessDef(s, PermissionsUtility.Create(s, c.E1), "cp");
						accIds = perms.GetIdsForResourceForOrganization(ResourceType.CoreProcess, c.Org.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(def, accIds.First());
					}
					//SurveyContainer
					{
						var accIds = perms.GetIdsForResourceForOrganization(ResourceType.SurveyContainer, c.Org.Id);
						Assert.AreEqual(0, accIds.Count());

						var sc = QuarterlyConversationAccessor.GenerateQuarterlyConversation_Unsafe(s, PermissionsUtility.Create(s, c.E1), "sc", scNodes, new DateRange(), DateTime.UtcNow.AddDays(1), false);

						accIds = perms.GetIdsForResourceForOrganization(ResourceType.SurveyContainer, c.Org.Id);
						Assert.AreEqual(1, accIds.Count());
						Assert.AreEqual(sc.SurveyContainerId, accIds.First());
					}
				}
			}
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task GetAllPermItemsForUserTest() {
			var c = await Ctx.Build();
			var nodes = QuarterlyConversationAccessor.AvailableByAboutsForMe(c.E1, true);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var perms = PermissionsUtility.Create(s, c.E1);
					//AccountabilityHierarchy
					{
						var accIds = perms.GetAllPermItemsForUser(ResourceType.AccountabilityHierarchy, c.E1.Id).ToList();
						Assert.AreEqual(1, accIds.Count());
						var permItem = accIds.First();

						Assert.IsTrue(permItem.CanView);
						Assert.IsFalse(permItem.CanEdit);
						Assert.IsFalse(permItem.CanAdmin);


						accIds = perms.GetAllPermItemsForUser(ResourceType.AccountabilityHierarchy, c.Manager.Id).ToList();
						Assert.AreEqual(2, accIds.Count());

						var adminPermItem = accIds.Single(x => x.AccessorType == AccessType.Admins);
						var memberPermItem = accIds.Single(x => x.AccessorType == AccessType.Members);

						Assert.IsTrue(adminPermItem.CanView);
						Assert.IsTrue(adminPermItem.CanEdit);
						Assert.IsTrue(adminPermItem.CanAdmin);

						Assert.IsTrue(memberPermItem.CanView);
						Assert.IsFalse(memberPermItem.CanEdit);
						Assert.IsFalse(memberPermItem.CanAdmin);


					}
					//CoreProcess
					{
						var accIds = perms.GetAllPermItemsForUser(ResourceType.CoreProcess, c.E1.Id).ToList();
						Assert.AreEqual(0, accIds.Count());

						var pda = new ProcessDefAccessor();
						var def = await pda.CreateProcessDef(s, PermissionsUtility.Create(s, c.E1), "cp");
						accIds = perms.GetAllPermItemsForUser(ResourceType.CoreProcess, c.E1.Id).ToList();
						Assert.AreEqual(1, accIds.Count());
						var permItem = accIds.First();
						Assert.AreEqual(def, permItem.ResId);
						Assert.IsTrue(permItem.CanView);
						Assert.IsTrue(permItem.CanEdit);
						Assert.IsTrue(permItem.CanAdmin);
					}
					//L10Recurrence
					{
						var accIds = perms.GetAllPermItemsForUser(ResourceType.L10Recurrence, c.E1.Id).ToList();
						Assert.AreEqual(0, accIds.Count());

						var l10 = await c.CreateL10(c.Middle, s);
						{
							//Try with E1
							accIds = perms.GetAllPermItemsForUser(ResourceType.L10Recurrence, c.E1.Id).ToList();
							Assert.AreEqual(0, accIds.Count());
						}
						{
							//Try with E3
							accIds = perms.GetAllPermItemsForUser(ResourceType.L10Recurrence, c.Middle.Id).ToList();
							Assert.AreEqual(1, accIds.Count());
							var permItem = accIds.First();
							Assert.AreEqual(l10.Id, permItem.ResId);

							Assert.IsTrue(permItem.CanView);
							Assert.IsTrue(permItem.CanEdit);
							Assert.IsTrue(permItem.CanAdmin);
						}
						{
							//Try with E2
							await l10.AddAttendee(s, c.E2);

							accIds = perms.GetAllPermItemsForUser(ResourceType.L10Recurrence, c.E2.Id).ToList();
							Assert.AreEqual(1, accIds.Count());
							var permItem = accIds.First();
							Assert.AreEqual(l10.Id, permItem.ResId);

							Assert.IsTrue(permItem.CanView);
							Assert.IsTrue(permItem.CanEdit);
							Assert.IsTrue(permItem.CanAdmin);
						}

						{
							//Remove some permissions
							l10.RemovePermissions(s, AccessType.Members);
							accIds = perms.GetAllPermItemsForUser(ResourceType.L10Recurrence, c.E2.Id).ToList();
							Assert.AreEqual(0, accIds.Count());

						}
					}
					//SurveyContainer
					{
						var accIds = perms.GetAllPermItemsForUser(ResourceType.SurveyContainer, c.E1.Id);
						Assert.AreEqual(0, accIds.Count());

						var sc = QuarterlyConversationAccessor.GenerateQuarterlyConversation_Unsafe(s, PermissionsUtility.Create(s, c.E1), "sc", nodes, new DateRange(), DateTime.UtcNow.AddDays(1), false);

						accIds = perms.GetAllPermItemsForUser(ResourceType.SurveyContainer, c.E1.Id);
						Assert.AreEqual(2, accIds.Count());
						var creatorPermItem = accIds.Single(x => x.AccessorType == AccessType.Creator);
						var memberPermItem = accIds.Single(x => x.AccessorType == AccessType.Members);


						Assert.IsTrue(creatorPermItem.CanView);
						Assert.IsTrue(creatorPermItem.CanEdit);
						Assert.IsTrue(creatorPermItem.CanAdmin);

						Assert.IsTrue(memberPermItem.CanView);
						Assert.IsTrue(memberPermItem.CanEdit);
						Assert.IsFalse(memberPermItem.CanAdmin);
					}
				}
			}
		}
	}
}
