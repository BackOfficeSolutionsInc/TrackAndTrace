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
using RadialReview.Controllers;
using RadialReview.Models.Askables;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class SettledPermissions : BasePermissionsTest {

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewDashboardForUser() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditOrganization(c.Id));

			//Only managers by default
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditDashboard() {
			var c = new Ctx();
			c.AssertAll(p => p.ViewOrganization(c.Id), c.AllUsers);

		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditTile() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditCompanyValues(c.Id));
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditCompanyPayment() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditCompanyValues(c.Id));
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditGroup() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditCompanyValues(c.Id));
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewGroup() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditApplication() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var perm = new Action<PermissionsUtility>(p => p.ViewL10Recurrence(l10.Id));
			c.AssertAll(perm, c.Manager, c.Middle);

			L10Accessor.AddAttendee(c.Manager, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);
			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, null, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, null, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, null, null);
				c.AssertAll(perm);
			}

		}
		[TestMethod]
		[TestCategory("Unset")]
		public void ViewApplication() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var perm = new Action<PermissionsUtility>(p => p.EditL10Recurrence(l10.Id));

			c.AssertAll(perm, c.Middle, c.Manager);

			L10Accessor.AddAttendee(c.Manager, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, null, false, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, false, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, false, null);
				c.AssertAll(perm);
			}

		}
		[TestMethod]
		[TestCategory("Unset")]
		public void EditIndustry() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var perm = new Action<PermissionsUtility>(p => p.AdminL10Recurrence(l10.Id));
			//L10Accessor.AddAttendee(c.Manager, l10.Id, c.Manager.Id);
			c.AssertAll(perm, c.Middle, c.Manager);

			L10Accessor.AddAttendee(c.Manager, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			//Revoke some permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, null, null, false);
				c.AssertAll(perm, c.Manager, c.Employee);
			}

			//Remove Members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, null, false);
				c.AssertAll(perm, c.Manager);
			}

			//Remove Admin
			{
				try {
					var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
					PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, null, false);
				} catch (PermissionsException e) {
					Assert.AreEqual("You must have an admin. Reverting setting change.", e.Message);
				}
				c.AssertAll(perm, c.Manager);
			}

			//Add member, remove admin
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, null, true);
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, null, false);
				c.AssertAll(perm, c.Employee);
			}

		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task ViewIndustry() {
			var c = new Ctx();

			var l101 = L10Accessor.CreateBlankRecurrence(c.Manager, c.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Employee.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Org.E5.Id);

			var issue = new IssueModel() { };
			var issue2 = new IssueModel() { };
			var perm1 = new Action<PermissionsUtility>(p => p.ViewIssue(issue.Id));
			var perm2 = new Action<PermissionsUtility>(p => p.ViewIssue(issue2.Id));

			await IssuesAccessor.CreateIssue(c.Manager, l101.Id, c.Manager.Id, issue);
			c.AssertAll(perm1, c.Manager, c.Employee, c.Org.E5);

			var l102 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			await IssuesAccessor.CreateIssue(c.Middle, l102.Id, c.Middle.Id, issue2);
			c.AssertAll(perm2, c.Middle, c.Manager);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task EditQuestion() {
			var c = new Ctx();
			var l101 = L10Accessor.CreateBlankRecurrence(c.Manager, c.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Employee.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Org.E5.Id);

			var todo = new TodoModel() { ForRecurrenceId = l101.Id };
			var perm1 = new Action<PermissionsUtility>(p => p.ViewTodo(todo.Id));
			await TodoAccessor.CreateTodo(c.Manager, l101.Id, todo);
			c.AssertAll(perm1, c.Manager, c.Employee, c.Org.E5);

			var l102 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var todo2 = new TodoModel() { ForRecurrenceId = l102.Id };
			var perm2 = new Action<PermissionsUtility>(p => p.ViewTodo(todo2.Id));
			await TodoAccessor.CreateTodo(c.Middle, l102.Id, todo2);
			c.AssertAll(perm2, c.Middle, c.Manager);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewQuestion() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var meeting = L10Accessor.StartMeeting(c.Middle, c.Middle, l10.Id, new List<long>());
			var perm = new Action<PermissionsUtility>(p => p.ViewL10Meeting(meeting.Id));

			c.AssertAll(perm, c.Middle, c.Manager);
			L10Accessor.AddAttendee(c.Middle, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Middle, c.Employee, c.Manager);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, null, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, null, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, null, null);
				c.AssertAll(perm);
			}
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditUserDetails() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);

			L10Accessor.CreateNote(c.Middle, l10.Id, "note");
			var note = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { l10.Id }).First();
			var perm = new Action<PermissionsUtility>(p => p.ViewL10Note(note.Id));

			c.AssertAll(perm, c.Manager, c.Middle);

			L10Accessor.AddAttendee(c.Middle, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, null, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, null, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, null, null);
				c.AssertAll(perm);
			}

		}
		[TestMethod]
		[TestCategory("Unset")]
		public void EditQuestionForUser() {
			var c = new Ctx();
			//Everyone can see by default
			c.AssertAll(p => p.ViewHierarchy(c.Org.Organization.AccountabilityChartId), c.AllUsers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditOrganizationQuestions() {
			var c = new Ctx();

			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.ManagerNode.Id), c.Manager);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.MiddleNode.Id), c.Manager, c.Middle);
			//We can manage the user if we manage them elsewhere
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E1MiddleNode.Id), c.Manager, c.Middle, c.E1);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E1BottomNode.Id), c.Manager, c.Middle, c.E1);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E2Node.Id), c.Manager, c.Middle, c.E2);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E3Node.Id), c.Manager, c.Middle, c.E3);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E4Node.Id), c.Manager, c.E1, c.E4);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E5Node.Id), c.Manager, c.E1, c.E5);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E6Node.Id), c.Manager, c.Middle, c.E2, c.E6);

		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditOrigin() {
			var c = new Ctx();
			c.AssertAll(p => p.EditHierarchy(c.Org.Organization.AccountabilityChartId), c.AllAdmins);
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void EditOrigin_TypeId() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditOrganization(c.Id));

			//Only managers by default
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewOrigin() {
			var c = new Ctx();
			c.AssertAll(p => p.ViewOrganization(c.Id), c.AllUsers);

		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewCategory() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditCompanyValues(c.Id));
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void PairCategoryToQuestion() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);

		}


		[TestMethod]
		[TestCategory("Unset")]
		public void ViewTeam() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditTeam() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void IssueForTeam() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var perm = new Action<PermissionsUtility>(p => p.EditL10Recurrence(l10.Id));

			c.AssertAll(perm, c.Middle, c.Manager);

			L10Accessor.AddAttendee(c.Manager, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, null, false, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, false, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, false, null);
				c.AssertAll(perm);
			}

		}
		[TestMethod]
		[TestCategory("Unset")]
		public void ManagingTeam() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var perm = new Action<PermissionsUtility>(p => p.EditL10Recurrence(l10.Id));

			c.AssertAll(perm, c.Middle, c.Manager);

			L10Accessor.AddAttendee(c.Manager, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, null, false, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, false, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, false, null);
				c.AssertAll(perm);
			}

		}
		[TestMethod]
		[TestCategory("Unset")]
		public void AdminReviewContainer() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var perm = new Action<PermissionsUtility>(p => p.AdminL10Recurrence(l10.Id));
			//L10Accessor.AddAttendee(c.Manager, l10.Id, c.Manager.Id);
			c.AssertAll(perm, c.Middle, c.Manager);

			L10Accessor.AddAttendee(c.Manager, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			//Revoke some permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, null, null, false);
				c.AssertAll(perm, c.Manager, c.Employee);
			}

			//Remove Members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, null, false);
				c.AssertAll(perm, c.Manager);
			}

			//Remove Admin
			{
				try {
					var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
					PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, null, false);
				} catch (PermissionsException e) {
					Assert.AreEqual("You must have an admin. Reverting setting change.", e.Message);
				}
				c.AssertAll(perm, c.Manager);
			}

			//Add member, remove admin
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, null, true);
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, null, false);
				c.AssertAll(perm, c.Employee);
			}

		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task EditReview() {
			var c = new Ctx();

			var l101 = L10Accessor.CreateBlankRecurrence(c.Manager, c.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Employee.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Org.E5.Id);

			var issue = new IssueModel() { };
			var issue2 = new IssueModel() { };
			var perm1 = new Action<PermissionsUtility>(p => p.ViewIssue(issue.Id));
			var perm2 = new Action<PermissionsUtility>(p => p.ViewIssue(issue2.Id));

			await IssuesAccessor.CreateIssue(c.Manager, l101.Id, c.Manager.Id, issue);
			c.AssertAll(perm1, c.Manager, c.Employee, c.Org.E5);

			var l102 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			await IssuesAccessor.CreateIssue(c.Middle, l102.Id, c.Middle.Id, issue2);
			c.AssertAll(perm2, c.Middle, c.Manager);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task ViewRGM() {
			var c = new Ctx();
			var l101 = L10Accessor.CreateBlankRecurrence(c.Manager, c.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Employee.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Org.E5.Id);

			var todo = new TodoModel() { ForRecurrenceId = l101.Id };
			var perm1 = new Action<PermissionsUtility>(p => p.ViewTodo(todo.Id));
			await TodoAccessor.CreateTodo(c.Manager, l101.Id, todo);
			c.AssertAll(perm1, c.Manager, c.Employee, c.Org.E5);

			var l102 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var todo2 = new TodoModel() { ForRecurrenceId = l102.Id };
			var perm2 = new Action<PermissionsUtility>(p => p.ViewTodo(todo2.Id));
			await TodoAccessor.CreateTodo(c.Middle, l102.Id, todo2);
			c.AssertAll(perm2, c.Middle, c.Manager);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewReviews() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var meeting = L10Accessor.StartMeeting(c.Middle, c.Middle, l10.Id, new List<long>());
			var perm = new Action<PermissionsUtility>(p => p.ViewL10Meeting(meeting.Id));

			c.AssertAll(perm, c.Middle, c.Manager);
			L10Accessor.AddAttendee(c.Middle, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Middle, c.Employee, c.Manager);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, null, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, null, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, null, null);
				c.AssertAll(perm);
			}
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewReview() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);

			L10Accessor.CreateNote(c.Middle, l10.Id, "note");
			var note = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { l10.Id }).First();
			var perm = new Action<PermissionsUtility>(p => p.ViewL10Note(note.Id));

			c.AssertAll(perm, c.Manager, c.Middle);

			L10Accessor.AddAttendee(c.Middle, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, null, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, null, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, null, null);
				c.AssertAll(perm);
			}

		}
		[TestMethod]
		[TestCategory("Unset")]
		public void ManageUserReview() {
			var c = new Ctx();
			//Everyone can see by default
			c.AssertAll(p => p.ViewHierarchy(c.Org.Organization.AccountabilityChartId), c.AllUsers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ManageUserReview_Answer() {
			var c = new Ctx();

			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.ManagerNode.Id), c.Manager);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.MiddleNode.Id), c.Manager, c.Middle);
			//We can manage the user if we manage them elsewhere
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E1MiddleNode.Id), c.Manager, c.Middle, c.E1);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E1BottomNode.Id), c.Manager, c.Middle, c.E1);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E2Node.Id), c.Manager, c.Middle, c.E2);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E3Node.Id), c.Manager, c.Middle, c.E3);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E4Node.Id), c.Manager, c.E1, c.E4);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E5Node.Id), c.Manager, c.E1, c.E5);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E6Node.Id), c.Manager, c.Middle, c.E2, c.E6);

		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditResponsibility() {
			var c = new Ctx();
			c.AssertAll(p => p.EditHierarchy(c.Org.Organization.AccountabilityChartId), c.AllAdmins);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void CreateTemplates() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditOrganization(c.Id));

			//Only managers by default
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewTemplate() {
			var c = new Ctx();
			c.AssertAll(p => p.ViewOrganization(c.Id), c.AllUsers);

		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditTemplate() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditCompanyValues(c.Id));
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void ViewPrereview() {

			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditUserScorecard() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);			
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewOrganizationScorecard() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditAttach() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);
		}
		[TestMethod]
		[TestCategory("Unset")]
		public void ViewMeasurable() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var perm = new Action<PermissionsUtility>(p => p.EditL10Recurrence(l10.Id));

			c.AssertAll(perm, c.Middle, c.Manager);

			L10Accessor.AddAttendee(c.Manager, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, null, false, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, false, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, false, null);
				c.AssertAll(perm);
			}

		}
		[TestMethod]
		[TestCategory("Unset")]
		public void EditMeasurable() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var perm = new Action<PermissionsUtility>(p => p.AdminL10Recurrence(l10.Id));
			//L10Accessor.AddAttendee(c.Manager, l10.Id, c.Manager.Id);
			c.AssertAll(perm, c.Middle, c.Manager);

			L10Accessor.AddAttendee(c.Manager, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			//Revoke some permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, null, null, false);
				c.AssertAll(perm, c.Manager, c.Employee);
			}

			//Remove Members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, null, false);
				c.AssertAll(perm, c.Manager);
			}

			//Remove Admin
			{
				try {
					var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
					PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, null, false);
				} catch (PermissionsException e) {
					Assert.AreEqual("You must have an admin. Reverting setting change.", e.Message);
				}
				c.AssertAll(perm, c.Manager);
			}

			//Add member, remove admin
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, null, null, true);
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, null, null, false);
				c.AssertAll(perm, c.Employee);
			}

		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task EditScore() {
			var c = new Ctx();

			var l101 = L10Accessor.CreateBlankRecurrence(c.Manager, c.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Employee.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Org.E5.Id);

			var issue = new IssueModel() { };
			var issue2 = new IssueModel() { };
			var perm1 = new Action<PermissionsUtility>(p => p.ViewIssue(issue.Id));
			var perm2 = new Action<PermissionsUtility>(p => p.ViewIssue(issue2.Id));

			await IssuesAccessor.CreateIssue(c.Manager, l101.Id, c.Manager.Id, issue);
			c.AssertAll(perm1, c.Manager, c.Employee, c.Org.E5);

			var l102 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			await IssuesAccessor.CreateIssue(c.Middle, l102.Id, c.Middle.Id, issue2);
			c.AssertAll(perm2, c.Middle, c.Manager);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public async Task CanViewUserMeasurables() {
			var c = new Ctx();
			var l101 = L10Accessor.CreateBlankRecurrence(c.Manager, c.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Employee.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Org.E5.Id);

			var todo = new TodoModel() { ForRecurrenceId = l101.Id };
			var perm1 = new Action<PermissionsUtility>(p => p.ViewTodo(todo.Id));
			await TodoAccessor.CreateTodo(c.Manager, l101.Id, todo);
			c.AssertAll(perm1, c.Manager, c.Employee, c.Org.E5);

			var l102 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var todo2 = new TodoModel() { ForRecurrenceId = l102.Id };
			var perm2 = new Action<PermissionsUtility>(p => p.ViewTodo(todo2.Id));
			await TodoAccessor.CreateTodo(c.Middle, l102.Id, todo2);
			c.AssertAll(perm2, c.Middle, c.Manager);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void CreateVTO() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var meeting = L10Accessor.StartMeeting(c.Middle, c.Middle, l10.Id, new List<long>());
			var perm = new Action<PermissionsUtility>(p => p.ViewL10Meeting(meeting.Id));

			c.AssertAll(perm, c.Middle, c.Manager);
			L10Accessor.AddAttendee(c.Middle, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Middle, c.Employee, c.Manager);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, null, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, null, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, null, null);
				c.AssertAll(perm);
			}
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewVTO() {
			var c = new Ctx();
			var l10 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);

			L10Accessor.CreateNote(c.Middle, l10.Id, "note");
			var note = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { l10.Id }).First();
			var perm = new Action<PermissionsUtility>(p => p.ViewL10Note(note.Id));

			c.AssertAll(perm, c.Manager, c.Middle);

			L10Accessor.AddAttendee(c.Middle, l10.Id, c.Employee.Id);
			c.AssertAll(perm, c.Manager, c.Middle, c.Employee);

			///Revoke permissions
			var allPerms = PermissionsAccessor.GetPermItems(c.Manager, l10.Id, ResourceType.L10Recurrence);
			//Remove Creator
			{
				var creator = allPerms.Items.First(x => x.AccessorType == AccessType.Creator);
				PermissionsAccessor.EditPermItem(c.Manager, creator.Id, false, null, null);
				c.AssertAll(perm, c.Manager, c.Employee);
			}
			//Remove Admin
			{
				var admin = allPerms.Items.First(x => x.AccessorType == AccessType.Admins);
				PermissionsAccessor.EditPermItem(c.Manager, admin.Id, false, null, null);
				c.AssertAll(perm, c.Employee);
			}

			//Remove members
			{
				var member = allPerms.Items.First(x => x.AccessorType == AccessType.Members);
				PermissionsAccessor.EditPermItem(c.Manager, member.Id, false, null, null);
				c.AssertAll(perm);
			}

		}
		[TestMethod]
		[TestCategory("Unset")]
		public void EditVTO() {
			var c = new Ctx();
			//Everyone can see by default
			c.AssertAll(p => p.ViewHierarchy(c.Org.Organization.AccountabilityChartId), c.AllUsers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewHeadline() {
			var c = new Ctx();

			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.ManagerNode.Id), c.Manager);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.MiddleNode.Id), c.Manager, c.Middle);
			//We can manage the user if we manage them elsewhere
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E1MiddleNode.Id), c.Manager, c.Middle, c.E1);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E1BottomNode.Id), c.Manager, c.Middle, c.E1);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E2Node.Id), c.Manager, c.Middle, c.E2);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E3Node.Id), c.Manager, c.Middle, c.E3);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E4Node.Id), c.Manager, c.E1, c.E4);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E5Node.Id), c.Manager, c.E1, c.E5);
			c.AssertAll(p => p.ManagesAccountabilityNodeOrSelf(c.Org.E6Node.Id), c.Manager, c.Middle, c.E2, c.E6);

		}

		[TestMethod]
		[TestCategory("Unset")]
		public void CanViewUserRocks() {
			var c = new Ctx();
			c.AssertAll(p => p.EditHierarchy(c.Org.Organization.AccountabilityChartId), c.AllAdmins);
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void ViewSurveyContainer() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditOrganization(c.Id));

			//Only managers by default
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void CreateSurvey() {
			var c = new Ctx();
			c.AssertAll(p => p.ViewOrganization(c.Id), c.AllUsers);

		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditSurvey() {
			var c = new Ctx();
			var perm = new Action<PermissionsUtility>(p => p.EditCompanyValues(c.Id));
			c.AssertAll(perm, c.AllAdmins);

			DbCommit(s => {
				var org = s.Get<OrganizationModel>(c.Org.Id);
				org.ManagersCanEdit = true;
				s.Update(org);
			});

			c.AssertAll(perm, c.AllManagers);
		}


		[TestMethod]
		[TestCategory("Unset")]
		public void EditPermissionOverride() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void EditForModel() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Unset")]
		public void ViewForModel() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id), c.AllManagers);
		}

	}
}
