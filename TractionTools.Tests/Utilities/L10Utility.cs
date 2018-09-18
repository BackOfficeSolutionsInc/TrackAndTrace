using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using RadialReview;
using RadialReview.Accessors;
using RadialReview.Model.Enums;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.TestUtils;
using static RadialReview.Controllers.L10Controller;

namespace TractionTools.Tests.Utilities {
	public class L10 {
		public long Id { get { return Recur.Id; } }
		public L10Recurrence Recur { get; set; }
		public OrganizationModel Org { get; set; }
		public UserOrganizationModel Creator { get; set; }
		public UserOrganizationModel Employee { get; set; }


		public static implicit operator long(L10 d) {
			return d.Id;
		}

		public async Task<RockModel> AddRock(string name = "rock", UserOrganizationModel owner = null) {
			BaseTest.MockHttpContext();
			using (var s = HibernateSession.GetCurrentSession(false)) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, Creator);
					//var rock = new RockModel() {
					//	Rock = name,
					//	OrganizationId = Org.Id,
					//	AccountableUser = owner ?? Employee,
					//	ForUserId = (owner??Employee).Id
					//};
					//var addRock = AddRockVm.CreateRock(Id,,name);
					var rock = await RockAccessor.CreateRock(s, perms, (owner ?? Employee).Id, name);
					await L10Accessor.AttachRock(s, perms, Id, rock.Id, false);
					tx.Commit();
					s.Flush();
					return rock;
				}
			}
		}
		public async Task<MeasurableModel> AddMeasurable(string name = "measurable", UserOrganizationModel owner = null) {
			BaseTest.MockHttpContext();
			using (var s = HibernateSession.GetCurrentSession(false)) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, Creator);
					var forUser = owner ?? Employee;
					//var mm = new MeasurableModel() {
					//	AdminUser = forUser,
					//	AdminUserId = forUser.Id,
					//	AccountableUser = forUser,
					//	AccountableUserId = forUser.Id,
					//	OrganizationId = Org.Id,
					//	Title = name
					//};
					//var m = AddMeasurableVm.CreateMeasurableViewModel(Id, mm);
					var creator = MeasurableBuilder.Build(name,forUser.Id);
					var mm = await ScorecardAccessor.CreateMeasurable(s, perms, creator);
					await L10Accessor.AttachMeasurable(s, perms, Id, mm.Id);
					tx.Commit();
					s.Flush();
					return mm;
				}
			}
		}
		public async Task<TodoModel> AddTodo(string name = "todo", UserOrganizationModel owner = null) {
			using (var s = HibernateSession.GetCurrentSession(false)) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, Creator);
					var todo = new TodoModel() {
						AccountableUser = owner ?? Employee,
						Message = name,
						OrganizationId = Org.Id,
						ForRecurrenceId = Id
					};
					var todoC = TodoCreation.GenerateL10Todo(Id, name, null, (owner ?? Employee).Id, null);
					await TodoAccessor.CreateTodo(s, perms, todoC);
					tx.Commit();
					s.Flush();
					return todo;
				}
			}
		}
		public async Task<IssueModel> AddIssue(string name = "issue", UserOrganizationModel owner = null) {
			using (var s = HibernateSession.GetCurrentSession(false)) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, Creator);
					//var issue = new IssueModel() {
					//	Message = name,
					//	OrganizationId = Org.Id,
					//};

					var creation = IssueCreation.CreateL10Issue(name, null, (owner ?? Employee).Id, Id);
					var result = await IssuesAccessor.CreateIssue(s, perms, creation);// Id, (owner ?? Employee).Id, issue);
					tx.Commit();
					s.Flush();
					return result.IssueModel;
				}
			}
		}

		public async Task AddAttendee(UserOrganizationModel employee) {
			await L10Accessor.AddAttendee(Creator, Id, employee.Id);
		}
		public async Task AddAttendee(ISession s, UserOrganizationModel employee) {
			await L10Accessor.AddAttendee(s, PermissionsUtility.Create(s, Creator), null, Id, employee.Id);
		}

		public async Task<PeopleHeadline> AddHeadline(string name = "headline", UserOrganizationModel owner = null, UserOrganizationModel about = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, Creator);
					//(s, perms, null, Id,);
					var headline = new PeopleHeadline() {
						Message = name,
						OwnerId = (owner ?? Employee).Id,
						Owner = (owner ?? Employee),
						AboutId = (about ?? Employee).Id,
						About = (about ?? Employee),
						AboutName = (about ?? Employee).GetName(),
						RecurrenceId = Id,
						OrganizationId = Org.Id,
					};
					await HeadlineAccessor.CreateHeadline(s, perms, headline);

					tx.Commit();
					s.Flush();
					return headline;
				}
			}
		}

		public void AddAdmin(UserOrganizationModel user) {
			PermissionsAccessor.CreatePermItems(Creator, PermItem.ResourceType.L10Recurrence, Recur.Id, PermTiny.RGM(user.Id, false, false, true));
		}


		public void RemovePermissions(PermItem.AccessType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					RemovePermissions(s, type);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public void RemovePermissions(ISession s, PermItem.AccessType type) {
			var itemIds = new List<long>();

			itemIds = s.QueryOver<PermItem>()
				.Where(x =>
						x.DeleteTime == null &&
						x.AccessorType == type &&
						x.ResId == Recur.Id &&
						x.ResType == PermItem.ResourceType.L10Recurrence
				).Select(x => x.Id)
				.List<long>().ToList();

			if (itemIds.Any())
				Console.WriteLine("WARN: No perm items to delete");
			foreach (var i in itemIds) {
				PermissionsAccessor.DeletePermItem(s, PermissionsUtility.Create(s, Creator), i);
			}
		}
	}

	public class L10Tester {

		private Org org;
		private L10Recurrence recur;

		private L10Tester() {
		}

		[Obsolete("You must specify the L10Recurrence that you received FROM A METHOD CALL, not the L10 creation helper.", true)]
		public static void Test(Org org, L10 recur) {
			Assert.Fail("You must specify the L10Recurrence that you received FROM A METHOD CALL, not the L10 creation helper.");
		}

		public static void Test(Org org, L10Recurrence recur, Action<L10Tester> changes) {
			var tester = new L10Tester() {
				org = org,
				recur = recur,
			};
			changes(tester);
			tester.Test();
		}

		public decimal ExpectedSegueMinutes = 5;
		public decimal ExpectedScorecardMinutes = 5;
		public decimal ExpectedRockReviewMinutes = 5;
		public decimal ExpectedHeadlinesMinutes = 5;
		public decimal ExpectedTodoListMinutes = 5;
		public decimal ExpectedIDSMinutes = 60;
		public decimal ExpectedConclusionMinutes = 5;

		public bool ExpectedAttendingOffByDefault = false;
		public bool ExpectedCombineRocks = false;
		public bool ExpectedCountDown = true;
		public long ExpectedCreatedById { get { return org.Manager.Id; } }
		public int ExpectedCurrentWeekHighlightShift = 0;
		public object ExpectedDefaultIssueOwner = null;
		public long ExpectedDefaultTodoOwner = 0;
		public object ExpectedDeleteTime = null;
		public bool ExpectedEnableTranscription = false;
		public object ExpectedHeadlineType = PeopleHeadlineType.HeadlinesList;
		public bool ExpectedIncludeAggregateTodoCompletion = false;
		public bool ExpectedIncludeAggregateTodoCompletionOnPrintout = true;
		public bool ExpectedIncludeIndividualTodos = false;
		public bool ExpectedIsLeadershipTeam = true;
		public object ExpectedMeetingInProgress = null;
		public MeetingType ExpectedMeetingType = MeetingType.L10;
		public string ExpectedName = "Test Meeting";
		public object ExpectedOrderIssueBy = null;
		public long ExpectedOrganizationId { get { return org.Id; } }
		public bool ExpectedPreventEditingUnownedMeasurables = false;
		public PrioritizationType ExpectedPrioritization = PrioritizationType.Rank;
		public bool ExpectedPristine = false;
		public bool ExpectedReverseScorecard = false;
		public L10RockType ExpectedRockType = L10RockType.Original;
		public object ExpectedSelectedVideoProvider = null;
		public object ExpectedSelectedVideoProviderId = null;
		public object ExpectedStartOfWeekOverride = null;
		public object ExpectedTeamType = L10TeamType.LeadershipTeam;
		public object Expected_WhoCanEdit = null;
		//Assert.AreEqual(ShowHeadlinesBox); obsolete

		public long[] Expected_DefaultAttendeeIds = null;
		private int Expected_DefaultAttendeesCount { get { return Expected_DefaultAttendeeIds.Count(); } }
		private void TestDefaultAttendees() {
			if (Expected_DefaultAttendeeIds == null) {
				Assert.IsNull(recur._DefaultAttendees);
			} else {
				Assert.IsNotNull(recur._DefaultAttendees);
				Assert.AreEqual(Expected_DefaultAttendeesCount, recur._DefaultAttendees.Count());
				foreach (var id in Expected_DefaultAttendeeIds) {
					Assert.IsTrue(recur._DefaultAttendees.Any(x => x.User.Id == id));
				}
			}
		}

		public string[] Expected_DefaultMeasurableNames = null;
		private int Expected_DefaultMeasurablesCount { get { return Expected_DefaultMeasurableNames.Count(); } }
		private void TestDefaultMeasurables() {

			if (Expected_DefaultMeasurableNames == null) {
				Assert.IsNull(recur._DefaultMeasurables);
			} else {
				Assert.IsNotNull(recur._DefaultMeasurables);
				Assert.AreEqual(Expected_DefaultMeasurablesCount, recur._DefaultMeasurables.Count());
				foreach (var name in Expected_DefaultMeasurableNames) {
					Assert.IsTrue(recur._DefaultMeasurables.Any(x => x.Measurable.Title == name));
				}
			}
		}

		public string[] Expected_DefaultRockNames = null;
		private int Expected_DefaultRocksCount { get { return Expected_DefaultRockNames.Count(); } }
		private void TestDefaultRocks() {
			if (Expected_DefaultRockNames == null) {
				Assert.IsNull(recur._DefaultRocks);
			} else {
				Assert.IsNotNull(recur._DefaultRocks);
				Assert.AreEqual(Expected_DefaultRocksCount, recur._DefaultRocks.Count());
				foreach (var name in Expected_DefaultRockNames) {
					Assert.IsTrue(recur._DefaultRocks.Any(x => x.ForRock.Rock == name));
				}
			}
		}

		public string[] Expected_MeetingNotes = null;
		private int Expected_MeetingNotesCount { get { return Expected_MeetingNotes.Count(); } }

		public bool ShouldHavePages { get; set; }
		public bool ShouldHaveVideoConferenceProviders { get; set; }

		private void TestMeetingNotes() {
			if (Expected_MeetingNotes == null) {
				Assert.IsNull(recur._MeetingNotes);
			} else {
				Assert.IsNotNull(recur._MeetingNotes);
				Assert.AreEqual(Expected_MeetingNotesCount, recur._MeetingNotes.Count());
				foreach (var name in Expected_MeetingNotes) {
					Assert.IsTrue(recur._MeetingNotes.Any(x => x.Name == name));
				}
			}
		}

		private void TestAutogen() {
			Assert.IsNotNull(recur.CreateTime);
			Assert.IsNotNull(recur.HeadlinesId);
			Assert.IsNotNull(recur.Organization);
			Assert.IsNotNull(recur.VideoId);
			Assert.IsNotNull(recur.VtoId);

			if (ShouldHaveVideoConferenceProviders) {
				Assert.IsNotNull(recur._VideoConferenceProviders);
				Assert.AreEqual(0, recur._VideoConferenceProviders.Count());
			} else {
				Assert.IsNull(recur._VideoConferenceProviders);
			}
		}

		private void TestPages() {

			if (ShouldHavePages) {
				Assert.IsNotNull(recur._Pages);
				Assert.AreEqual(7, recur._Pages.Count());
				Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Segue));
				Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Scorecard));
				Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Rocks));
				Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Headlines));
				Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Todo));
				Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.IDS));
				Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Conclude));
			} else {
				Assert.IsNull(recur._Pages);
			}
		}


		private void Test() {
			Assert.AreEqual(ExpectedAttendingOffByDefault, recur.AttendingOffByDefault);
			Assert.AreEqual(ExpectedCombineRocks, recur.CombineRocks);
			Assert.AreEqual(ExpectedConclusionMinutes, recur.ConclusionMinutes);
			Assert.AreEqual(ExpectedCountDown, recur.CountDown);
			Assert.AreEqual(ExpectedCreatedById, recur.CreatedById);
			Assert.AreEqual(ExpectedCurrentWeekHighlightShift, recur.CurrentWeekHighlightShift);
			Assert.AreEqual(ExpectedDefaultIssueOwner, recur.DefaultIssueOwner);
			Assert.AreEqual(ExpectedDefaultTodoOwner, recur.DefaultTodoOwner);
			Assert.AreEqual(ExpectedDeleteTime, recur.DeleteTime);
			Assert.AreEqual(ExpectedEnableTranscription, recur.EnableTranscription);
			Assert.AreEqual(ExpectedHeadlinesMinutes, recur.HeadlinesMinutes);
			Assert.AreEqual(ExpectedHeadlineType, recur.HeadlineType);
			Assert.AreEqual(ExpectedIDSMinutes, recur.IDSMinutes);
			Assert.AreEqual(ExpectedIncludeAggregateTodoCompletion, recur.IncludeAggregateTodoCompletion);
			Assert.AreEqual(ExpectedIncludeAggregateTodoCompletionOnPrintout, recur.IncludeAggregateTodoCompletionOnPrintout);
			Assert.AreEqual(ExpectedIncludeIndividualTodos, recur.IncludeIndividualTodos);
			Assert.AreEqual(ExpectedIsLeadershipTeam, recur.IsLeadershipTeam);
			Assert.AreEqual(ExpectedMeetingInProgress, recur.MeetingInProgress);
			Assert.AreEqual(ExpectedMeetingType, recur.MeetingType);
			Assert.AreEqual(ExpectedName, recur.Name);
			Assert.AreEqual(ExpectedOrderIssueBy, recur.OrderIssueBy);
			Assert.AreEqual(ExpectedOrganizationId, recur.OrganizationId);
			Assert.AreEqual(ExpectedPreventEditingUnownedMeasurables, recur.PreventEditingUnownedMeasurables);
			Assert.AreEqual(ExpectedPrioritization, recur.Prioritization);
			Assert.AreEqual(ExpectedPristine, recur.Pristine);
			Assert.AreEqual(ExpectedReverseScorecard, recur.ReverseScorecard);
			Assert.AreEqual(ExpectedRockReviewMinutes, recur.RockReviewMinutes);
			Assert.AreEqual(ExpectedRockType, recur.RockType);
			Assert.AreEqual(ExpectedScorecardMinutes, recur.ScorecardMinutes);
			Assert.AreEqual(ExpectedSegueMinutes, recur.SegueMinutes);
			Assert.AreEqual(ExpectedSelectedVideoProvider, recur.SelectedVideoProvider);
			Assert.AreEqual(ExpectedSelectedVideoProviderId, recur.SelectedVideoProviderId);
			Assert.AreEqual(ExpectedStartOfWeekOverride, recur.StartOfWeekOverride);
			Assert.AreEqual(ExpectedTeamType, recur.TeamType);
			Assert.AreEqual(ExpectedTodoListMinutes, recur.TodoListMinutes);
			Assert.AreEqual(Expected_WhoCanEdit, recur._WhoCanEdit);

			TestDefaultAttendees();
			TestDefaultMeasurables();
			TestDefaultRocks();
			TestMeetingNotes();

			TestPages();

			TestAutogen();

			#region Hardcoded
			//Assert.IsFalse(recur.CombineRocks);
			//Assert.AreEqual(5, recur.ConclusionMinutes);
			//Assert.IsTrue(recur.CountDown);
			//Assert.AreEqual(org.Manager.Id, recur.CreatedById);
			//Assert.IsNotNull(recur.CreateTime);
			//Assert.AreEqual(0, recur.CurrentWeekHighlightShift);
			//Assert.IsNull(recur.DefaultIssueOwner);
			//Assert.AreEqual(0, recur.DefaultTodoOwner);
			//Assert.IsNull(recur.DeleteTime);
			//Assert.IsFalse(recur.EnableTranscription);
			//Assert.IsNotNull(recur.HeadlinesId);
			//Assert.AreEqual(5, recur.HeadlinesMinutes);
			//Assert.AreEqual(PeopleHeadlineType.HeadlinesList, recur.HeadlineType);
			//Assert.AreEqual(60, recur.IDSMinutes);
			//Assert.IsFalse(recur.IncludeAggregateTodoCompletion);
			//Assert.IsTrue(recur.IncludeAggregateTodoCompletionOnPrintout);
			//Assert.IsFalse(recur.IncludeIndividualTodos);
			//Assert.IsTrue(recur.IsLeadershipTeam);
			//Assert.IsNull(recur.MeetingInProgress);
			//Assert.AreEqual(MeetingType.L10, recur.MeetingType);
			//Assert.AreEqual("Test Meeting", recur.Name);
			//Assert.IsNull(recur.OrderIssueBy);
			//Assert.IsNotNull(recur.Organization);
			//Assert.AreEqual(org.Id, recur.OrganizationId);
			//Assert.IsFalse(recur.PreventEditingUnownedMeasurables);
			//Assert.AreEqual(PrioritizationType.Rank, recur.Prioritization);
			//Assert.IsFalse(recur.Pristine);
			//Assert.IsFalse(recur.ReverseScorecard);
			//Assert.AreEqual(5, recur.RockReviewMinutes);
			//Assert.AreEqual(L10RockType.Original, recur.RockType);
			//Assert.AreEqual(5, recur.ScorecardMinutes);
			//Assert.AreEqual(5, recur.SegueMinutes);
			//Assert.IsNull(recur.SelectedVideoProvider);
			//Assert.IsNull(recur.SelectedVideoProviderId);
			////Assert.AreEqual(recur.ShowHeadlinesBox); obsolete
			//Assert.AreEqual(recur.ShowHeadlinesBox); obsolete
			//Assert.IsNull(recur.StartOfWeekOverride);
			//Assert.AreEqual(L10TeamType.LeadershipTeam, recur.TeamType);
			//Assert.AreEqual(5, recur.TodoListMinutes);
			#endregion
		}
	}

	public class L10Utility {

		public static async Task<L10> CreateRecurrence(string name) {
			return await CreateRecurrence(existing: null, name: name);
		}

		public static async Task<L10> CreateRecurrence(L10 existing = null, string name = null, Org org = null) {
			UserOrganizationModel employee = org.NotNull(x => x.Employee);
			UserOrganizationModel manager = org.NotNull(x => x.Manager);
			OrganizationModel o = org.NotNull(x => x.Organization);
			if (existing == null) {

				BaseTest.DbCommit(s => {
					//Org
					if (o == null) {
						o = new OrganizationModel() { };
						o.Settings.TimeZoneId = "GMT Standard Time";
						s.Save(o);
					}

#pragma warning disable CS0618 // Type or member is obsolete
					var plan = PaymentAccessor.GeneratePlan(PaymentPlanType.Professional_Monthly_March2016, new DateTime(2016, 5, 14));
#pragma warning restore CS0618 // Type or member is obsolete
					PaymentAccessor.AttachPlan(s, o, plan);
					//User
					if (employee == null) {
						var u = new UserOrganizationModel() { Organization = o };
						s.Save(u);
						employee = u;
					}
					if (manager == null) {
						manager = new UserOrganizationModel() {
							Organization = o,
							ManagerAtOrganization = true,
						};
						s.Save(manager);
					}
				});
			} else {
				o = existing.Org;
				manager = existing.Creator;
			}

			var recur = await L10Accessor.CreateBlankRecurrence(manager, o.Id, false);
			if (name != null) {
				BaseTest.DbCommit(s => {
					recur = s.Get<L10Recurrence>(recur.Id);
					recur.Name = name;
					s.Update(recur);
				});
			}

			return new L10 {
				Employee = employee,
				Creator = manager,
				Org = o,
				Recur = recur
			};
		}
	}
}
