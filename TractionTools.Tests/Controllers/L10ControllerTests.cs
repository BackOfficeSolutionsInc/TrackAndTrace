using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Controllers;
using TractionTools.Tests.TestUtils;
using TractionTools.Tests.Utilities;
using RadialReview.Models.L10.VM;
using System.Threading.Tasks;
using RadialReview.Models.Scorecard;
using RadialReview.Model.Enums;
using RadialReview.Models.L10;
using RadialReview.Utilities.DataTypes;
using RadialReview.Accessors;
using RadialReview.Models.Todo;
using System.Collections.Generic;

namespace TractionTools.Tests.Controllers {
	public class L10MeetingVMTester {
		private L10MeetingVM model;
		public bool ExpectedCanAdmin = true;
		public bool ExpectedCanEdit = true;

		public bool ExpectedEnableTranscript = false;
		public bool ExpectedCloseHeadlines = false;
		public bool ExpectedCloseTodos = false;
		public bool ExpectedSeenTodoFireworks = false;
		public bool ExpectedSendEmail = false;
		public bool ExpectedShowScorecardChart = false;
		public bool ExpectedShowAdmin = false;

		public bool ExpectedMeetingIsNull = true;
		public bool ExpectedMeetingStartIsNull = true;
		public bool ExpectedScoresIsNull = true;
		public bool ExpectedHeadlinesIdIsNull = true;
		public bool ExpectedMemberPicturesIsNull = false;

		public int ExpectedCurrentTranscriptCount = 0;
		public int ExpectedHeadlinesCount = 0;
		public int ExpectedConnectedCount = 0;
		public int ExpectedMilestonesCount = 0;
		public int ExpectedWeeksCount = 0;
		public int? ExpectedRocksCount = null;
		public int? ExpectedTodoCounts = null;
		public int? ExpectedIssuesCounts = null;

		public object ExpectedAttendees = null;
		//public object ExpectedMemberPictures = null;

		public long[] ExpectedAttendeeIds = new long[] { };

		public ScorecardPeriod ExpectedScorecardType = ScorecardPeriod.Weekly;
		public ConcludeSendEmail ExpectedSendEmailRich = ConcludeSendEmail.None;


		private void Test() {
			Assert.AreEqual(ExpectedCanAdmin, model.CanAdmin);//Attendees can admin by default
			Assert.AreEqual(ExpectedCanEdit, model.CanEdit);

			Assert.AreEqual(ExpectedEnableTranscript, model.EnableTranscript);
			Assert.AreEqual(ExpectedCloseHeadlines, model.CloseHeadlines);
			Assert.AreEqual(ExpectedCloseTodos, model.CloseTodos);
			Assert.AreEqual(ExpectedSeenTodoFireworks, model.SeenTodoFireworks);
			Assert.AreEqual(ExpectedSendEmail, model.SendEmail);
			Assert.AreEqual(ExpectedShowScorecardChart, model.ShowScorecardChart);
			Assert.AreEqual(ExpectedShowAdmin, model.ShowAdmin);

			Assert.AreEqual(ExpectedCurrentTranscriptCount, model.CurrentTranscript.Count());
			Assert.AreEqual(ExpectedHeadlinesCount, model.Headlines.Count());
			Assert.AreEqual(ExpectedConnectedCount, model.Connected.Count());
			Assert.AreEqual(ExpectedMilestonesCount, model.Milestones.Count());
			Assert.AreEqual(ExpectedWeeksCount, model.Weeks.Count());


			Assert.AreEqual(ExpectedAttendees, model.Attendees);

			Assert.AreEqual(ExpectedScorecardType, model.ScorecardType);
			Assert.AreEqual(ExpectedSendEmailRich, model.SendEmailRich);

			//Assert.AreEqual(ExpectedMeeting, model.Meeting);
			TestAutoGen();
			TestAttendeePictures();
			TestMeeting();
			TestScores();
			TestRocks();
			TestTodos();
			TestIssues();
			TestHeadlines();

		}

		public void TestAutoGen() {
			Assert.IsNotNull(model.VtoId);
			Assert.IsNotNull(model.EndDate);
			Assert.IsNotNull(model.Recurrence);
		}
		private void TestHeadlines() {
			if (ExpectedHeadlinesIdIsNull)
				Assert.IsNull(model.HeadlinesId);
			else
				Assert.IsNotNull(model.HeadlinesId);
		}
		private void TestIssues() {
			if (ExpectedIssuesCounts == null)
				Assert.IsNull(model.Issues);
			else
				Assert.AreEqual(ExpectedIssuesCounts, model.Issues.Count());
		}
		private void TestScores() {
			if (ExpectedScoresIsNull)
				Assert.IsNull(model.Scores);
			else
				Assert.IsNotNull(model.Scores);
		}
		private void TestTodos() {
			if (ExpectedTodoCounts == null)
				Assert.IsNull(model.Todos);
			else
				Assert.AreEqual(ExpectedTodoCounts, model.Todos.Count());
		}
		private void TestRocks() {
			if (ExpectedRocksCount == null)
				Assert.IsNull(model.Rocks);
			else
				Assert.AreEqual(ExpectedRocksCount, model.Rocks.Count());
		}
		private void TestMeeting() {
			if (ExpectedMeetingIsNull) {
				Assert.IsNull(model.Meeting);
			} else {
				Assert.IsNotNull(model.Meeting);
				//TODO test meeting contents
			}

			if (ExpectedMeetingStartIsNull)
				Assert.IsNull(model.MeetingStart);
			else
				Assert.IsNotNull(model.MeetingStart);
		}
		private void TestAttendeePictures() {
			if (ExpectedMemberPicturesIsNull == true) {
				Assert.IsNull(model.MemberPictures);
			} else {
				Assert.IsNotNull(model.MemberPictures);
				Assert.AreEqual(ExpectedAttendeeIds.Count(), model.MemberPictures.Count());
			}
			//Assert.IsNotNull(model.Attendees);
			//Assert.AreEqual(ExpectedAttendeeIds.Count(), model.Attendees.Count());

			foreach (var a in ExpectedAttendeeIds) {
				Assert.IsTrue(model.MemberPictures.Any(x => x.UserId == a));
				//Assert.IsTrue(model.Attendees.Any(x=>x==a));
			}

		}
		public static void Test(L10MeetingVM model, Action<L10MeetingVMTester> changes) {
			var tester = new L10MeetingVMTester() {
				model = model,
			};
			changes(tester);
			tester.Test();
		}
	}

	[TestClass]
	public class L10ControllerTests : BaseTest {
		[TestMethod]
		[TestCategory("Controller")]
		public async Task L10Index() {
			var org = await OrgUtil.CreateOrganization();

			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var json = ctrl.GetView(x => x.Index());
				var model = json.GetModel<L10ListingVM>();
				Assert.AreEqual(0, model.Recurrences.Count());
			}

			var l1 = await org.CreateL10("L1");
			var l2 = await org.CreateL10("L2");

			var otherOrg = await OrgUtil.CreateOrganization();
			var l3 = await otherOrg.CreateL10("L3-Other");

			await l1.AddAttendee(org.Manager);

			using (var ctrl = new ControllerCtx<L10Controller>(org.Manager)) {
				var json = ctrl.GetView(x => x.Index());
				var model = json.GetModel<L10ListingVM>();
				Assert.AreEqual(1, model.Recurrences.Count());
				Assert.IsTrue(model.Recurrences.Any(x => x.Recurrence.Name == "L1"));
				Assert.IsFalse(model.Recurrences.Any(x => x.Recurrence.Name == "L2"));
				Assert.IsFalse(model.Recurrences.Any(x => x.Recurrence.Name == "L3"));
				Assert.IsTrue(model.Recurrences.First(x => x.Recurrence.Name == "L1").IsAttendee == true);
			}

			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var json = ctrl.GetView(x => x.Index());
				var model = json.GetModel<L10ListingVM>();
				Assert.AreEqual(0, model.Recurrences.Count());
			}

			await l1.AddAttendee(org.Employee);

			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var json = ctrl.GetView(x => x.Index());
				var model = json.GetModel<L10ListingVM>();
				Assert.AreEqual(1, model.Recurrences.Count());
				Assert.IsTrue(model.Recurrences.Any(x => x.Recurrence.Name == "L1"));
				Assert.IsFalse(model.Recurrences.Any(x => x.Recurrence.Name == "L2"));
				Assert.IsFalse(model.Recurrences.Any(x => x.Recurrence.Name == "L3"));
				Assert.IsTrue(model.Recurrences.First(x => x.Recurrence.Name == "L1").IsAttendee == true);
			}

			await l2.AddAttendee(org.Employee);
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var json = ctrl.GetView(x => x.Index());
				var model = json.GetModel<L10ListingVM>();
				Assert.AreEqual(2, model.Recurrences.Count());
				Assert.IsTrue(model.Recurrences.Any(x => x.Recurrence.Name == "L1"));
				Assert.IsTrue(model.Recurrences.Any(x => x.Recurrence.Name == "L2"));
				Assert.IsFalse(model.Recurrences.Any(x => x.Recurrence.Name == "L3"));
				Assert.IsTrue(model.Recurrences.First(x => x.Recurrence.Name == "L1").IsAttendee == true);
				Assert.IsTrue(model.Recurrences.First(x => x.Recurrence.Name == "L2").IsAttendee == true);
			}

			//Should see employee's l10 also (subordinate)
			using (var ctrl = new ControllerCtx<L10Controller>(org.Manager)) {
				var json = ctrl.GetView(x => x.Index());
				var model = json.GetModel<L10ListingVM>();
				Assert.AreEqual(2, model.Recurrences.Count());
				Assert.IsTrue(model.Recurrences.Any(x => x.Recurrence.Name == "L1"));
				Assert.IsTrue(model.Recurrences.Any(x => x.Recurrence.Name == "L2"));
				Assert.IsFalse(model.Recurrences.Any(x => x.Recurrence.Name == "L3"));
				Assert.IsTrue(model.Recurrences.First(x => x.Recurrence.Name == "L1").IsAttendee == true);
				Assert.IsTrue(model.Recurrences.First(x => x.Recurrence.Name == "L2").IsAttendee == false);

			}
		}

		[TestMethod]
		[TestCategory("Controller")]
		public async Task L10Meeting() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10("Test Meeting");
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Employee);

			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");

			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var json = ctrl.GetView(x => x.Meeting(l10.Id));
				var model = json.GetModel<L10MeetingVM>();

				L10MeetingVMTester.Test(model, x => {
					x.ExpectedMemberPicturesIsNull = true;
				});

				//Test for the actual recurrence
				L10Tester.Test(org, model.Recurrence, tester => {
					tester.Expected_DefaultAttendeeIds = new[] { org.Employee.Id, org.Manager.Id };
					tester.Expected_DefaultMeasurableNames = new[] { "meas1" };
					tester.Expected_DefaultRockNames = new[] { "rock1" };
				});
			}
		}
		
		[TestMethod]
		[TestCategory("Controller")]
		public async Task StartPage() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10("Test Meeting");
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Employee);

			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");

			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null));
				var model = partial.GetModel<L10MeetingVM>();

				Assert.AreEqual("StartMeeting", partial.ViewName);

				L10MeetingVMTester.Test(model, x => {
					x.ExpectedAttendeeIds = new[] { org.Manager.Id, org.Employee.Id };
				});

				L10Tester.Test(org, model.Recurrence, x => {
					x.Expected_DefaultAttendeeIds = new[] { org.Employee.Id, org.Manager.Id };
					x.Expected_DefaultRockNames = new[] { "rock1" };
					x.Expected_DefaultMeasurableNames = new[] { "meas1" };
				});

			}
		}

		[TestMethod]
		[TestCategory("Controller")]
		public async Task SeguePage() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10("Test Meeting");
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Employee);

			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");

			var recur = L10Accessor.GetL10Recurrence(org.Manager, l10, true);
			var pageId = recur._Pages.First(x => x.PageType == L10Recurrence.L10PageType.Segue).Id;

			//Shouldn't go to the page without starting the meeting
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				Assert.AreEqual("StartMeeting", partial.ViewName);
			}

			//Actually start the meeting
			var meeting = await L10Accessor.StartMeeting(org.Manager, org.Manager, l10, new[] { org.Manager.Id, org.Employee.Id }.ToList());
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				var model = partial.GetModel<L10MeetingVM>();

				Assert.AreEqual("Segue", partial.ViewName);
				L10MeetingVMTester.Test(model, x => {
					x.ExpectedAttendeeIds = new[] { org.Manager.Id, org.Employee.Id };
					x.ExpectedMeetingIsNull = false;
					x.ExpectedMeetingStartIsNull = false;
				});

				L10Tester.Test(org, model.Recurrence, x => {
					x.Expected_DefaultAttendeeIds = new[] { org.Employee.Id, org.Manager.Id };
					x.Expected_DefaultRockNames = new[] { "rock1" };
					x.Expected_DefaultMeasurableNames = new[] { "meas1" };
					x.ExpectedMeetingInProgress = meeting.Id;
				});

			}
		}

		[TestMethod]
		[TestCategory("Controller")]
		public async Task ScorecardPage() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10("Test Meeting");
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Employee);

			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");

			var recur = L10Accessor.GetL10Recurrence(org.Manager, l10, true);
			var pageId = recur._Pages.First(x => x.PageType == L10Recurrence.L10PageType.Scorecard).Id;

			//Shouldn't go to the page without starting the meeting
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				Assert.AreEqual("StartMeeting", partial.ViewName);
			}

			//Actually start the meeting
			var meeting = await L10Accessor.StartMeeting(org.Manager, org.Manager, l10, new[] { org.Manager.Id, org.Employee.Id }.ToList());
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				var model = partial.GetModel<L10MeetingVM>();

				Assert.AreEqual("Scorecard", partial.ViewName);
				L10MeetingVMTester.Test(model, x => {
					x.ExpectedAttendeeIds = new[] { org.Manager.Id, org.Employee.Id };
					x.ExpectedMeetingIsNull = false;
					x.ExpectedMeetingStartIsNull = false;
					x.ExpectedScoresIsNull = false;
					x.ExpectedWeeksCount = 15;
				});

				L10Tester.Test(org, model.Recurrence, x => {
					x.Expected_DefaultAttendeeIds = new[] { org.Employee.Id, org.Manager.Id };
					x.Expected_DefaultRockNames = new[] { "rock1" };
					x.Expected_DefaultMeasurableNames = new[] { "meas1" };
					x.ExpectedMeetingInProgress = meeting.Id;
				});

			}
		}
		
		[TestMethod]
		[TestCategory("Controller")]
		public async Task RocksPage() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10("Test Meeting");
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Employee);

			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");

			var recur = L10Accessor.GetL10Recurrence(org.Manager, l10, true);
			var pageId = recur._Pages.First(x => x.PageType == L10Recurrence.L10PageType.Rocks).Id;

			//Shouldn't go to the page without starting the meeting
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				Assert.AreEqual("StartMeeting", partial.ViewName);
			}

			//Actually start the meeting
			var meeting = await L10Accessor.StartMeeting(org.Manager, org.Manager, l10, new[] { org.Manager.Id, org.Employee.Id }.ToList());
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				var model = partial.GetModel<L10MeetingVM>();

				Assert.AreEqual("Rocks", partial.ViewName);
				L10MeetingVMTester.Test(model, x => {
					x.ExpectedAttendeeIds = new[] { org.Manager.Id, org.Employee.Id };
					x.ExpectedMeetingIsNull = false;
					x.ExpectedMeetingStartIsNull = false;
					x.ExpectedRocksCount = 1;

					//x.ExpectedScoresIsNull = false;
					//x.ExpectedWeeksCount = 15;
				});

				L10Tester.Test(org, model.Recurrence, x => {
					x.Expected_DefaultAttendeeIds = new[] { org.Employee.Id, org.Manager.Id };
					x.Expected_DefaultRockNames = new[] { "rock1" };
					x.Expected_DefaultMeasurableNames = new[] { "meas1" };
					x.ExpectedMeetingInProgress = meeting.Id;
				});

			}
		}

		[TestMethod]
		[TestCategory("Controller")]
		public async Task HeadlinesPage() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10("Test Meeting");
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Employee);

			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");
			await l10.AddHeadline("headline");

			var recur = L10Accessor.GetL10Recurrence(org.Manager, l10, true);
			var pageId = recur._Pages.First(x => x.PageType == L10Recurrence.L10PageType.Headlines).Id;

			//Shouldn't go to the page without starting the meeting
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				Assert.AreEqual("StartMeeting", partial.ViewName);
			}

			//Actually start the meeting
			var meeting = await L10Accessor.StartMeeting(org.Manager, org.Manager, l10, new[] { org.Manager.Id, org.Employee.Id }.ToList());
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				var model = partial.GetModel<L10MeetingVM>();

				Assert.AreEqual("Headlines", partial.ViewName);
				L10MeetingVMTester.Test(model, x => {
					x.ExpectedAttendeeIds = new[] { org.Manager.Id, org.Employee.Id };
					x.ExpectedMeetingIsNull = false;
					x.ExpectedMeetingStartIsNull = false;

					x.ExpectedHeadlinesCount = 1;
					x.ExpectedHeadlinesIdIsNull = false;
				});

				L10Tester.Test(org, model.Recurrence, x => {
					x.Expected_DefaultAttendeeIds = new[] { org.Employee.Id, org.Manager.Id };
					x.Expected_DefaultRockNames = new[] { "rock1" };
					x.Expected_DefaultMeasurableNames = new[] { "meas1" };
					x.ExpectedMeetingInProgress = meeting.Id;
				});

			}
		}

		[TestMethod]
		[TestCategory("Controller")]
		public async Task TodosPage() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10("Test Meeting");
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Employee);

			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");
			await l10.AddHeadline("headline");

			var recur = L10Accessor.GetL10Recurrence(org.Manager, l10, true);
			var pageId = recur._Pages.First(x => x.PageType == L10Recurrence.L10PageType.Todo).Id;

			//Shouldn't go to the page without starting the meeting
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				Assert.AreEqual("StartMeeting", partial.ViewName);
			}

			//Actually start the meeting
			var meeting = await L10Accessor.StartMeeting(org.Manager, org.Manager, l10, new[] { org.Manager.Id, org.Employee.Id }.ToList());
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				var model = partial.GetModel<L10MeetingVM>();

				Assert.AreEqual("Todo", partial.ViewName);
				L10MeetingVMTester.Test(model, x => {
					x.ExpectedAttendeeIds = new[] { org.Manager.Id, org.Employee.Id };
					x.ExpectedMeetingIsNull = false;
					x.ExpectedMeetingStartIsNull = false;

					x.ExpectedTodoCounts = 1;
					//x.ExpectedHeadlinesCount = 1;
					//x.ExpectedHeadlinesIdIsNull = false;
				});

				L10Tester.Test(org, model.Recurrence, x => {
					x.Expected_DefaultAttendeeIds = new[] { org.Employee.Id, org.Manager.Id };
					x.Expected_DefaultRockNames = new[] { "rock1" };
					x.Expected_DefaultMeasurableNames = new[] { "meas1" };
					x.ExpectedMeetingInProgress = meeting.Id;
				});

			}
		}

		[TestMethod]
		[TestCategory("Controller")]
		public async Task IdsPage() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10("Test Meeting");
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Employee);

			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");
			await l10.AddHeadline("headline");

			var recur = L10Accessor.GetL10Recurrence(org.Manager, l10, true);
			var pageId = recur._Pages.First(x => x.PageType == L10Recurrence.L10PageType.IDS).Id;

			//Shouldn't go to the page without starting the meeting
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				Assert.AreEqual("StartMeeting", partial.ViewName);
			}

			//Actually start the meeting
			var meeting = await L10Accessor.StartMeeting(org.Manager, org.Manager, l10, new[] { org.Manager.Id, org.Employee.Id }.ToList());
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				var model = partial.GetModel<L10MeetingVM>();

				Assert.AreEqual("IDS", partial.ViewName);
				L10MeetingVMTester.Test(model, x => {
					x.ExpectedAttendeeIds = new[] { org.Manager.Id, org.Employee.Id };
					x.ExpectedMeetingIsNull = false;
					x.ExpectedMeetingStartIsNull = false;

					x.ExpectedIssuesCounts = 1;
					//x.ExpectedHeadlinesCount = 1;
					//x.ExpectedHeadlinesIdIsNull = false;
				});

				L10Tester.Test(org, model.Recurrence, x => {
					x.Expected_DefaultAttendeeIds = new[] { org.Employee.Id, org.Manager.Id };
					x.Expected_DefaultRockNames = new[] { "rock1" };
					x.Expected_DefaultMeasurableNames = new[] { "meas1" };
					x.ExpectedMeetingInProgress = meeting.Id;
				});

			}
		}

		[TestMethod]
		[TestCategory("Controller")]
		public async Task ConcludePage() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10("Test Meeting");
			await l10.AddAttendee(org.Manager);
			await l10.AddAttendee(org.Employee);

			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");
			await l10.AddHeadline("headline");

			var recur = L10Accessor.GetL10Recurrence(org.Manager, l10, true);
			var pageId = recur._Pages.First(x => x.PageType == L10Recurrence.L10PageType.Conclude).Id;

			//Shouldn't go to the page without starting the meeting
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				Assert.AreEqual("StartMeeting", partial.ViewName);
			}

			await l10.AddTodo("todo2");

			//Actually start the meeting
			var meeting = await L10Accessor.StartMeeting(org.Manager, org.Manager, l10, new[] { org.Manager.Id, org.Employee.Id }.ToList());
			using (var ctrl = new ControllerCtx<L10Controller>(org.Employee)) {
				var partial = await ctrl.GetPartial(x => x.Load(l10.Id, null, page: "page-" + pageId));
				var model = partial.GetModel<L10MeetingVM>();

				Assert.AreEqual("Conclusion", partial.ViewName);
				L10MeetingVMTester.Test(model, x => {
					x.ExpectedAttendeeIds = new[] { org.Manager.Id, org.Employee.Id };
					x.ExpectedMeetingIsNull = false;
					x.ExpectedMeetingStartIsNull = false;

					x.ExpectedSendEmail = true;
					x.ExpectedSendEmailRich = ConcludeSendEmail.AllAttendees;
					x.ExpectedCloseTodos = true;
					x.ExpectedCloseHeadlines = true;
				});

				Assert.IsNotNull(partial.ViewBag.TodosForNextWeek);
				Assert.AreEqual(2, ((List<TodoModel>)partial.ViewBag.TodosForNextWeek).Count());

				L10Tester.Test(org, model.Recurrence, x => {
					x.Expected_DefaultAttendeeIds = new[] { org.Employee.Id, org.Manager.Id };
					x.Expected_DefaultRockNames = new[] { "rock1" };
					x.Expected_DefaultMeasurableNames = new[] { "meas1" };
					x.ExpectedMeetingInProgress = meeting.Id;
				});

			}
		}
	}
}
