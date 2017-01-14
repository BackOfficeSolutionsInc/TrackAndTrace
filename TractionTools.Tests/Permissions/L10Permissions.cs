﻿using System;
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

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class L10Permissions : BasePermissionsTest {



		[TestMethod]
		[TestCategory("Permissions")]
		public void CreateL10Recurrence() {
			var c = new Ctx();
			c.AssertAll(p => p.CreateL10Recurrence(c.Id),c.AllManagers);
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public void ViewL10Recurrence() {
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
		[TestCategory("Permissions")]
		public void EditL10Recurrence() {
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
		[TestCategory("Permissions")]
		public void AdminL10Recurrence() {
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
					Assert.Fail();
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
		[TestCategory("Permissions")]
		public async Task ViewIssue() {
			var c = new Ctx();
			
			var l101 = L10Accessor.CreateBlankRecurrence(c.Manager, c.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Employee.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Org.E5.Id);
			
			var issue = new IssueModel() { };
			var issue2 = new IssueModel() { };
			var perm1 = new Action<PermissionsUtility>(p => p.ViewIssue(issue.Id));
			var perm2 = new Action<PermissionsUtility>(p => p.ViewIssue(issue2.Id));
						
			await IssuesAccessor.CreateIssue(c.Manager, l101.Id, c.Manager.Id, issue);
			c.AssertAll(perm1, c.Manager,c.Employee, c.Org.E5);
			
			var l102 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			await IssuesAccessor.CreateIssue(c.Middle, l102.Id, c.Middle.Id, issue2);
			c.AssertAll(perm2, c.Middle, c.Manager);			
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task ViewTodo() {
			var c = new Ctx();
			var l101 = L10Accessor.CreateBlankRecurrence(c.Manager, c.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Employee.Id);
			L10Accessor.AddAttendee(c.Manager, l101.Id, c.Org.E5.Id);

			var todo = new TodoModel() {ForRecurrenceId = l101.Id};
			var perm1 = new Action<PermissionsUtility>(p => p.ViewTodo(todo.Id));
			await TodoAccessor.CreateTodo(c.Manager, l101.Id, todo);
			c.AssertAll(perm1, c.Manager, c.Employee, c.Org.E5);

			var l102 = L10Accessor.CreateBlankRecurrence(c.Middle, c.Id);
			var todo2 = new TodoModel() {ForRecurrenceId = l102.Id};
			var perm2 = new Action<PermissionsUtility>(p => p.ViewTodo(todo2.Id));
			await TodoAccessor.CreateTodo(c.Middle, l102.Id, todo2);
			c.AssertAll(perm2, c.Middle, c.Manager);
		}

		[TestMethod]
		[TestCategory("Permissions")]
		public void ViewL10Meeting() {
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
				c.AssertAll( perm, c.Manager, c.Employee);
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
		[TestCategory("Permissions")]
		public void ViewL10Note() {
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

		/*
		 
		[TestMethod]
		[TestCategory("Permissions")]
		public void XXX() {
			var c = new Ctx();
			c.AssertAll(p => p.XXX(YYY), c.Manager);
		}

		 */
	}
}