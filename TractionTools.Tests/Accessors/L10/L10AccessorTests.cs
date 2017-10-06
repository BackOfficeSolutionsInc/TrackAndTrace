using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Models.Todo;
using RadialReview.Utilities.DataTypes;
using System.Collections.Generic;
using RadialReview.Utilities;
using RadialReview.Models.Enums;
using TractionTools.Tests.TestUtils;
using RadialReview.Models.L10;
using RadialReview.Models;
using System.Linq;
using TractionTools.Tests.Utilities;
using System.Threading.Tasks;
using RadialReview.Model.Enums;

namespace TractionTools.Tests.Accessors
{
    [TestClass]
    public class L10AccessorTests : BaseTest
    {
        private DateTime twoWeeksAgo = new DateTime(2016, 2, 7);
        private DateTime lastWeek = new DateTime(2016, 2, 14);
        private DateTime lastWeekMeetingStart = new DateTime(2016, 2, 19);
        private DateTime thisWeek = new DateTime(2016, 2, 21);
        private DateTime meetingStart = new DateTime(2016, 2, 26);
        private DateTime nextWeek = new DateTime(2016, 2, 28);


        private List<TodoModel> todoList = new List<TodoModel>();

        private TodoModel TestTodoNoMeeting(DateTime dueDate, DateTime? completeTime, Ratio twoWeeksAgoRatio, Ratio lastWeekRatio, Ratio thisWeekRatio, long? completeDuringMeeting = null, Ratio endOfTime = null)
        {
            return TestTodo(dueDate, completeTime, twoWeeksAgoRatio, lastWeekRatio, thisWeekRatio, completeDuringMeeting, false, endOfTime: endOfTime);
        }
        private TodoModel TestTodo(DateTime dueDate, DateTime? completeTime, Ratio twoWeeksAgoRatio, Ratio lastWeekRatio, Ratio thisWeekRatio, long? completeDuringMeeting = null, bool meetingIsRunning = true, Ratio endOfTime = null)
        {
            var todo = new TodoModel()
            {
                DueDate = dueDate,
                CompleteTime = completeTime,
                CompleteDuringMeetingId = completeDuringMeeting,
            };
            DateTime? ms = meetingIsRunning ? (DateTime?)(meetingStart) : nextWeek.AddDays(1);

            Assert.AreEqual(new Ratio(0, 0), L10Accessor.GetTodoCompletion(todo, DateTime.MinValue, twoWeeksAgo, ms), "beginningOfTimeRatio");
            Assert.AreEqual(twoWeeksAgoRatio, L10Accessor.GetTodoCompletion(todo, twoWeeksAgo, lastWeek, ms), "twoWeeksAgoRatio");
            Assert.AreEqual(lastWeekRatio, L10Accessor.GetTodoCompletion(todo, lastWeek, thisWeek, ms), "lastWeekRatio");
            Assert.AreEqual(thisWeekRatio, L10Accessor.GetTodoCompletion(todo, thisWeek, nextWeek, ms), "thisWeekRatio");
            Assert.AreEqual(endOfTime ?? new Ratio(0, 0), L10Accessor.GetTodoCompletion(todo, nextWeek, DateTime.MaxValue, ms), "endOfTimeRatio");

            todoList.Add(todo);
            return todo;
        }

        private void AssignAndTest(Ratio expected, string[] arr)
        {
            var time = new DateTime(2016, 2, 1);
            DateTime s = DateTime.MinValue;
            DateTime e = DateTime.MinValue;
            DateTime m = DateTime.MinValue;
            DateTime d = DateTime.MinValue;
            DateTime c = DateTime.MinValue;


            foreach (var a in arr)
            {
                switch (a)
                {
                    case "s": s = time; break;
                    case "e": e = time; break;
                    case "m": m = time; break;
                    case "d": d = time; break;
                    case "c": c = time; break;
                    default: throw new Exception("Asdfadsf");
                }
                time = time.AddDays(1);
            }
            var todo = new TodoModel()
            {
                DueDate = d,
                CompleteTime = c,
            };
            var found = L10Accessor.GetTodoCompletion(todo, s, e, m);
            Assert.AreEqual(expected, found);//expected.Numerator==found.Numerator && found.Denominator==expected.Denominator);

        }

        [TestMethod]
        public void TestTodoCompletion_Excel()
        {
            var y = new Ratio(1, 1);
            var n = new Ratio(0, 1);
            var z = new Ratio(0, 0);
            var s = "s";
            var m = "m";
            var d = "d";
            var e = "e";
            var c = "c";
            AssignAndTest(z, new[] { m, d, c, s, e });
            AssignAndTest(z, new[] { m, d, s, c, e });
            AssignAndTest(n, new[] { m, d, s, e, c });
            AssignAndTest(z, new[] { m, c, d, s, e });
            AssignAndTest(z, new[] { m, c, s, e, d });
            AssignAndTest(y, new[] { m, c, s, d, e });
            AssignAndTest(n, new[] { m, s, d, c, e });
            AssignAndTest(n, new[] { m, s, d, e, c });
            AssignAndTest(y, new[] { m, s, c, d, e });
            AssignAndTest(z, new[] { m, s, c, e, d });
            AssignAndTest(z, new[] { m, s, e, d, c });
            AssignAndTest(z, new[] { m, s, e, c, d });
            AssignAndTest(z, new[] { d, m, c, s, e });
            AssignAndTest(n, new[] { d, m, s, e, c });
            AssignAndTest(z, new[] { d, m, s, c, e });
            AssignAndTest(z, new[] { d, c, m, s, e });
            AssignAndTest(z, new[] { d, c, s, m, e });
            AssignAndTest(z, new[] { d, c, s, e, m });
            AssignAndTest(n, new[] { d, s, e, m, c });
            AssignAndTest(n, new[] { d, s, e, c, m });
            AssignAndTest(n, new[] { d, s, m, e, c });
            AssignAndTest(z, new[] { d, s, m, c, e });
            AssignAndTest(z, new[] { d, s, c, e, m });
            AssignAndTest(z, new[] { d, s, c, m, e });
            AssignAndTest(z, new[] { c, m, s, e, d });
            AssignAndTest(y, new[] { c, m, s, d, e });
            AssignAndTest(z, new[] { c, m, d, s, e });
            AssignAndTest(z, new[] { c, d, s, m, e });
            AssignAndTest(z, new[] { c, d, s, e, m });
            AssignAndTest(z, new[] { c, d, m, s, e });
            AssignAndTest(y, new[] { c, s, d, e, m });
            AssignAndTest(y, new[] { c, s, d, m, e });
            AssignAndTest(z, new[] { c, s, e, d, m });
            AssignAndTest(z, new[] { c, s, e, m, d });
            AssignAndTest(y, new[] { c, s, m, d, e });
            AssignAndTest(z, new[] { c, s, m, e, d });
            AssignAndTest(n, new[] { s, m, d, c, e });
            AssignAndTest(n, new[] { s, m, d, e, c });
            AssignAndTest(y, new[] { s, m, c, d, e });
            AssignAndTest(z, new[] { s, m, c, e, d });
            AssignAndTest(z, new[] { s, m, e, d, c });
            AssignAndTest(z, new[] { s, m, e, c, d });
            AssignAndTest(n, new[] { s, d, m, e, c });
            AssignAndTest(n, new[] { s, d, m, c, e });
            AssignAndTest(n, new[] { s, d, c, e, m });
            AssignAndTest(n, new[] { s, d, c, m, e });
            AssignAndTest(n, new[] { s, d, e, c, m });
            AssignAndTest(n, new[] { s, d, e, m, c });
            AssignAndTest(y, new[] { s, c, m, d, e });
            AssignAndTest(z, new[] { s, c, m, e, d });
            AssignAndTest(y, new[] { s, c, d, m, e });
            AssignAndTest(y, new[] { s, c, d, e, m });
            AssignAndTest(z, new[] { s, c, e, m, d });
            AssignAndTest(z, new[] { s, c, e, d, m });
            AssignAndTest(z, new[] { s, e, m, c, d });
            AssignAndTest(z, new[] { s, e, m, d, c });
            AssignAndTest(z, new[] { s, e, d, c, m });
            AssignAndTest(z, new[] { s, e, d, m, c });
            AssignAndTest(z, new[] { s, e, c, d, m });
            AssignAndTest(z, new[] { s, e, c, m, d });


        }

        [TestMethod]
        public void TestTodoCompletion()
        {

            this.todoList = new List<TodoModel>();
            var z = new Ratio(0, 0);
            var n = new Ratio(0, 1);
            var y = new Ratio(1, 1);
            var Q = z;

            var thisMeetingId = 2;
            var lastMeetingId = 1;

            //For This Week meeting is running
            TestTodo(thisWeek.AddDays(2), thisWeek.AddDays(1), z, z, y);                                //This week, on time          
            TestTodo(thisWeek.AddDays(2), lastWeek.AddDays(1), z, z, y);                                //This week, early
            TestTodo(thisWeek.AddDays(2), thisWeek.AddDays(3), z, z, n);                                //This week, late
            TestTodo(thisWeek.AddDays(2), null, z, z, n, endOfTime: n);                                  //This week, incomplete
            TestTodo(meetingStart.AddDays(1), null, z, z, z);                                       //This week, incomplete, due after meeting start
            TestTodo(thisWeek.AddDays(2), meetingStart.AddMinutes(15), z, z, y, thisMeetingId);          //This week, complete during meeting
            TestTodo(thisWeek.AddDays(2), lastWeekMeetingStart.AddMinutes(15), z, z, y, lastMeetingId);  //This week, complete during last meeting

            //For last week
            TestTodo(lastWeek.AddDays(2), lastWeek.AddDays(1), z, y, z);                                 //Last week, early
            TestTodo(lastWeek.AddDays(2), twoWeeksAgo.AddDays(3), z, y, z);                              //Last week, very early
            TestTodo(lastWeek.AddDays(2), thisWeek.AddDays(3), z, n, Q);                                 //Last week, late
            TestTodo(lastWeek.AddDays(2), null, z, n, n, endOfTime: n);                               //Last week, incomplete
            TestTodo(lastWeek.AddDays(2), meetingStart.AddMinutes(15), z, n, Q, thisMeetingId);           //Last week, complete during this meeting
            TestTodo(lastWeek.AddDays(2), lastWeekMeetingStart.AddMinutes(15), z, y, z, lastMeetingId);   //Last week, complete during lastweek meeting

            //For two weeks ago
            TestTodo(twoWeeksAgo.AddDays(2), twoWeeksAgo.AddDays(1), y, z, z);                           //2 week ago, on time
            TestTodo(twoWeeksAgo.AddDays(2), twoWeeksAgo.AddDays(-7), y, z, z);                          //2 week ago, early
            TestTodo(twoWeeksAgo.AddDays(2), twoWeeksAgo.AddDays(3), n, z, z);                           //2 week ago, late
            TestTodo(twoWeeksAgo.AddDays(2), lastWeek.AddDays(3), n, Q, z);                              //2 week ago, very late
            TestTodo(twoWeeksAgo.AddDays(2), thisWeek.AddDays(3), n, n, Q);                              //2 week ago, very very late
            TestTodo(twoWeeksAgo.AddDays(2), null, n, n, n, endOfTime: n);                            //2 week ago, incomplete
            TestTodo(twoWeeksAgo.AddDays(2), lastWeekMeetingStart.AddMinutes(15), n, Q, z, lastMeetingId);//2 week ago, complete during last start
            TestTodo(twoWeeksAgo.AddDays(2), meetingStart.AddMinutes(15), n, n, Q, thisMeetingId);        //2 week ago, complete during this meeting 

            if (Q == n)
            {
                Assert.AreEqual(new Ratio(2, 8), L10Accessor.GetTodoCompletion(todoList, twoWeeksAgo, lastWeek, meetingStart));
                Assert.AreEqual(new Ratio(3, 11), L10Accessor.GetTodoCompletion(todoList, lastWeek, thisWeek, meetingStart));
                Assert.AreEqual(new Ratio(4, 12), L10Accessor.GetTodoCompletion(todoList, thisWeek, nextWeek, meetingStart));
            }
            else if (Q == z)
            {
                Assert.AreEqual(new Ratio(2, 8), L10Accessor.GetTodoCompletion(todoList, twoWeeksAgo, lastWeek, meetingStart));
                Assert.AreEqual(new Ratio(3, 9), L10Accessor.GetTodoCompletion(todoList, lastWeek, thisWeek, meetingStart));
                Assert.AreEqual(new Ratio(4, 8), L10Accessor.GetTodoCompletion(todoList, thisWeek, nextWeek, meetingStart));
            }
            else
            {
                Assert.Fail("?");
            }

            todoList = new List<TodoModel>();

            //For This Week (meeting is NOT running)
            TestTodoNoMeeting(thisWeek.AddDays(2), thisWeek.AddDays(1), z, z, y);                                //This week, on time          
            TestTodoNoMeeting(thisWeek.AddDays(2), lastWeek.AddDays(1), z, z, y);                                //This week, early
            TestTodoNoMeeting(thisWeek.AddDays(2), thisWeek.AddDays(3), z, z, n);                                //This week, late
            TestTodoNoMeeting(thisWeek.AddDays(2), null, z, z, n, endOfTime: n);                             //This week, incomplete
            TestTodoNoMeeting(meetingStart.AddDays(1), null, z, z, n, endOfTime: n);                         //This week, incomplete, due after meeting start
            TestTodoNoMeeting(thisWeek.AddDays(2), meetingStart.AddMinutes(15), z, z, y, thisMeetingId);          //This week, complete during meeting
            TestTodoNoMeeting(thisWeek.AddDays(2), lastWeekMeetingStart.AddMinutes(15), z, z, y, lastMeetingId);  //This week, complete during last meeting

            //For last week (meeting is NOT running)
            TestTodoNoMeeting(lastWeek.AddDays(2), lastWeek.AddDays(1), z, y, z);                                 //Last week, early
            TestTodoNoMeeting(lastWeek.AddDays(2), twoWeeksAgo.AddDays(3), z, y, z);                              //Last week, very early
            TestTodoNoMeeting(lastWeek.AddDays(2), thisWeek.AddDays(3), z, n, Q);                                 //Last week, late
            TestTodoNoMeeting(lastWeek.AddDays(2), null, z, n, n, endOfTime: n);                               //Last week, incomplete
            TestTodoNoMeeting(lastWeek.AddDays(2), meetingStart.AddMinutes(15), z, n, Q, thisMeetingId);           //Last week, complete during this meeting
            TestTodoNoMeeting(lastWeek.AddDays(2), lastWeekMeetingStart.AddMinutes(15), z, y, z, lastMeetingId);   //Last week, complete during lastweek meeting


            if (Q == n)
            {
                Assert.AreEqual(new Ratio(0, 0), L10Accessor.GetTodoCompletion(todoList, twoWeeksAgo, lastWeek, DateTime.MaxValue));
                Assert.AreEqual(new Ratio(3, 6), L10Accessor.GetTodoCompletion(todoList, lastWeek, thisWeek, DateTime.MaxValue));
                Assert.AreEqual(new Ratio(4, 10), L10Accessor.GetTodoCompletion(todoList, thisWeek, nextWeek, DateTime.MaxValue));
            }
            else if (Q == z)
            {
                Assert.AreEqual(new Ratio(0, 0), L10Accessor.GetTodoCompletion(todoList, twoWeeksAgo, lastWeek, DateTime.MaxValue));
                Assert.AreEqual(new Ratio(3, 6), L10Accessor.GetTodoCompletion(todoList, lastWeek, thisWeek, DateTime.MaxValue));
                Assert.AreEqual(new Ratio(4, 8), L10Accessor.GetTodoCompletion(todoList, thisWeek, nextWeek, DateTime.MaxValue));
            }
            else
            {
                Assert.Fail("?");
            }

            todoList = new List<TodoModel>();

            TestTodo(meetingStart.AddDays(1.9), null, z, z, z);
            TestTodo(meetingStart.AddDays(1.9), meetingStart.AddDays(1), z, z, y);

        }

        [TestMethod]
        public void TestTodoCompletion_EdgeCase()
        {
            var date = new DateTime(2016,2,19);
            var meetingTime = new DateTime(2016,2,19,8,0,0);

            //Meeting starts on day its due
            var todo = new TodoModel(){
                DueDate = date,
                CompleteTime = null,
            };
            Assert.AreEqual(new Ratio(0, 0), L10Accessor.GetTodoCompletion(todo, lastWeek, thisWeek, meetingTime));


            //Due today
            var duedate = new DateTime(2016, 2, 19);
            var now = new DateTime(2016, 2, 19, 8, 15, 0);
            var todo2 = new TodoModel()            {
                DueDate = duedate,
                CompleteTime = null,
            };
            Assert.AreEqual(new Ratio(0, 0), L10Accessor.GetTodoCompletion(todo2, lastWeek, thisWeek, now));
            todo2.CompleteTime = new DateTime(2016, 2, 19, 8, 7, 0);
            Assert.AreEqual(new Ratio(1, 1), L10Accessor.GetTodoCompletion(todo2, lastWeek, thisWeek, now));
            todo2.CompleteTime = new DateTime(2016, 2, 19, 12+6, 1, 0);
            Assert.AreEqual(new Ratio(1, 1), L10Accessor.GetTodoCompletion(todo2, lastWeek, thisWeek, now));
            todo2.Organization = new OrganizationModel();
            todo2.Organization.Settings.TimeZoneId = "Central Standard Time";
            Assert.AreEqual(new Ratio(0, 1), L10Accessor.GetTodoCompletion(todo2, lastWeek, thisWeek, now));
            todo2.Organization.Settings.TimeZoneId = "Greenwich Standard Time";
            Assert.AreEqual(new Ratio(1, 1), L10Accessor.GetTodoCompletion(todo2, lastWeek, thisWeek, now));
            
            todo2.CompleteTime = new DateTime(2016, 2, 20, 2, 1, 0);
            Assert.AreEqual(new Ratio(0, 1), L10Accessor.GetTodoCompletion(todo2, lastWeek, thisWeek, now));
            todo2.Organization.Settings.TimeZoneId = "Afghanistan Standard Time";
            Assert.AreEqual(new Ratio(1, 1), L10Accessor.GetTodoCompletion(todo2, lastWeek, thisWeek, now));

            
        }
		
        [TestMethod]
        public void TestScorecardCreation()
        {
            this.todoList = new List<TodoModel>();

            long thisMeetingId = -1;
            long lastMeetingId = -1;
            long recurrenceId = -1;
            UserOrganizationModel user = null;
            #region DB Setup
            DbCommit(s =>
            {
                //Org
                var o = new OrganizationModel() { };
                o.Settings.TimeZoneId = "GMT Standard Time";
                s.Save(o);

                //User
                var u = new UserOrganizationModel() { Organization = o, IsRadialAdmin = true };
                s.Save(u);
                user = u;
                //Recurrence
                var r = new L10Recurrence() { Organization = o, OrganizationId = o.Id, IncludeAggregateTodoCompletion = true, IncludeIndividualTodos = true };
                s.Save(r);
                recurrenceId = r.Id;
                var m1 = new L10Meeting() { L10Recurrence = r, L10RecurrenceId = recurrenceId, StartTime = lastWeekMeetingStart, CompleteTime = lastWeekMeetingStart.AddHours(1.5), Organization = o, OrganizationId = o.Id, MeetingLeader = u, MeetingLeaderId = u.Id };
                s.Save(m1);
                lastMeetingId = m1.Id;
                var m2 = new L10Meeting() { L10Recurrence = r, L10RecurrenceId = recurrenceId, StartTime = meetingStart, Organization = o, OrganizationId = o.Id, MeetingLeader = u, MeetingLeaderId = u.Id };
                s.Save(m2);
                thisMeetingId = m2.Id;


                var genTodo = new Func<DateTime, DateTime?, TodoModel>((d, c) => new TodoModel()
                {
                    CompleteTime = c,
                    DueDate = d,
                    ForRecurrence = r,
                    ForRecurrenceId = recurrenceId,
                    CreatedBy = u,
                    CreatedById = u.Id,
                    AccountableUser = u,
                    AccountableUserId = u.Id,
                    Organization = o,
                    OrganizationId = o.Id
                });
                //For this week
                s.Save(genTodo(thisWeek.AddDays(2), thisWeek.AddDays(1)));              //This week, on time          
                s.Save(genTodo(thisWeek.AddDays(2), lastWeek.AddDays(1)));              //This week, early
                s.Save(genTodo(thisWeek.AddDays(2), thisWeek.AddDays(3)));              //This week, late
                s.Save(genTodo(thisWeek.AddDays(2), null));                             //This week, incomplete
                s.Save(genTodo(meetingStart.AddDays(1), null));                         //This week, incomplete, due after meeting start
                var t = genTodo(thisWeek.AddDays(2), meetingStart.AddMinutes(15));
                t.CompleteDuringMeetingId = thisMeetingId;                              //This week, complete during meeting
                s.Save(t);  
                t = genTodo(thisWeek.AddDays(2), lastWeekMeetingStart.AddMinutes(15));  //This week, complete during last meeting
                t.CompleteDuringMeetingId = lastMeetingId;
                s.Save(t);

                //For last week
                s.Save(genTodo(lastWeek.AddDays(2), lastWeek.AddDays(1)));              //Last week, early
                s.Save(genTodo(lastWeek.AddDays(2), twoWeeksAgo.AddDays(3)));           //Last week, very early
                s.Save(genTodo(lastWeek.AddDays(2), thisWeek.AddDays(3)));              //Last week, late
                s.Save(genTodo(lastWeek.AddDays(2), null));                             //Last week, incomplete
                t = genTodo(lastWeek.AddDays(2), meetingStart.AddMinutes(15));          //Last week, complete during this meeting
                t.CompleteDuringMeetingId = thisMeetingId;
                s.Save(t);
                t = genTodo(lastWeek.AddDays(2), lastWeekMeetingStart.AddMinutes(15));
                t.CompleteDuringMeetingId = lastMeetingId;                              //Last week, complete during lastweek meeting
                s.Save(t);

                //For two weeks ago
                s.Save(genTodo(twoWeeksAgo.AddDays(2), twoWeeksAgo.AddDays(1)));        //2 week ago, on time
                s.Save(genTodo(twoWeeksAgo.AddDays(2), twoWeeksAgo.AddDays(-7)));       //2 week ago, early
                s.Save(genTodo(twoWeeksAgo.AddDays(2), twoWeeksAgo.AddDays(3)));        //2 week ago, late
                s.Save(genTodo(twoWeeksAgo.AddDays(2), lastWeek.AddDays(3)));           //2 week ago, very late
                s.Save(genTodo(twoWeeksAgo.AddDays(2), thisWeek.AddDays(3)));           //2 week ago, very very late
                s.Save(genTodo(twoWeeksAgo.AddDays(2), null));                          //2 week ago, incomplete
                t = genTodo(twoWeeksAgo.AddDays(2), lastWeekMeetingStart.AddMinutes(15)); //2 week ago, complete during last start
                t.CompleteDuringMeetingId = lastMeetingId;
                s.Save(t);
                t = genTodo(twoWeeksAgo.AddDays(2), meetingStart.AddMinutes(15));         //2 week ago, complete during this meeting 
                t.CompleteDuringMeetingId = thisMeetingId;
                s.Save(t);
            });
            #endregion
            DbQuery(s =>
            {
                var perms = PermissionsUtility.Create(s, user);
                var now = meetingStart;

                var scores = L10Accessor.GetScoresForRecurrence(s, perms, recurrenceId, true, now);
                var agg = scores.Where(x => x.Measurable.Id == -10001).ToList();
                var individual = scores.Where(x => x.Measurable.Id != -10001).ToList();

                var week0 = agg.First(x => x.ForWeek == twoWeeksAgo.AddDays(7));
                var week1 = agg.First(x => x.ForWeek == lastWeek.AddDays(7));
                var week2 = agg.First(x => x.ForWeek == thisWeek.AddDays(7));
                //var week3 = agg.First(x => x.ForWeek == nextWeek.AddDays(7));
                //var week4 = agg.First(x => x.ForWeek == nextWeek);

                Assert.AreEqual(25m, week0.Measured);
                Assert.AreEqual(33.3m, week1.Measured);
                Assert.AreEqual(50m, week2.Measured);
                //Assert.AreEqual(0m/4m, week3.Measured);



                //int i = 0;
            });

        }

		[TestMethod]
		public async Task GetL10Recurrence() {
			var org = await OrgUtil.CreateOrganization();
			var l10 = await org.CreateL10(org.Manager);
			
			await l10.AddRock("rock1");
			await l10.AddMeasurable("meas1");
			await l10.AddTodo("todo1");
			await l10.AddIssue("issue1");

			var recur = L10Accessor.GetL10Recurrence(org.Manager, l10, true);
			
			Assert.IsFalse(recur.AttendingOffByDefault);
			Assert.IsFalse(recur.CombineRocks);
			Assert.AreEqual(5, recur.ConclusionMinutes);
			Assert.IsTrue(recur.CountDown);
			Assert.AreEqual(org.Manager.Id, recur.CreatedById);
			Assert.IsNotNull(recur.CreateTime);
			Assert.AreEqual(0, recur.CurrentWeekHighlightShift);
			Assert.IsNull(recur.DefaultIssueOwner);
			Assert.AreEqual(0, recur.DefaultTodoOwner);
			Assert.IsNull(recur.DeleteTime);
			Assert.IsFalse(recur.EnableTranscription);
			Assert.IsNotNull(recur.HeadlinesId);
			Assert.AreEqual(5, recur.HeadlinesMinutes);
			Assert.AreEqual(PeopleHeadlineType.HeadlinesList, recur.HeadlineType);
			Assert.AreEqual(60, recur.IDSMinutes);
			Assert.IsFalse(recur.IncludeAggregateTodoCompletion);
			Assert.IsTrue(recur.IncludeAggregateTodoCompletionOnPrintout);
			Assert.IsFalse(recur.IncludeIndividualTodos);
			Assert.IsTrue(recur.IsLeadershipTeam);
			Assert.IsNull(recur.MeetingInProgress);
			Assert.AreEqual(MeetingType.L10, recur.MeetingType);
			Assert.IsNull(recur.Name);
			Assert.IsNull(recur.OrderIssueBy);
			Assert.IsNotNull(recur.Organization);
			Assert.AreEqual(org.Id, recur.OrganizationId);
			Assert.IsFalse(recur.PreventEditingUnownedMeasurables);
			Assert.AreEqual(PrioritizationType.Rank, recur.Prioritization);
			Assert.IsFalse(recur.Pristine);
			Assert.IsFalse(recur.ReverseScorecard);
			Assert.AreEqual(5, recur.RockReviewMinutes);
			Assert.AreEqual(L10RockType.Original, recur.RockType);
			Assert.AreEqual(5, recur.ScorecardMinutes);

			Assert.AreEqual(5, recur.SegueMinutes);
			Assert.IsNull(recur.SelectedVideoProvider);
			Assert.IsNull(recur.SelectedVideoProviderId);
			//Assert.AreEqual(recur.ShowHeadlinesBox); obsolete
			Assert.IsNull(recur.StartOfWeekOverride);
			Assert.AreEqual(L10TeamType.LeadershipTeam, recur.TeamType);
			Assert.AreEqual(5, recur.TodoListMinutes);
			Assert.IsNotNull(recur.VideoId);
			Assert.IsNotNull(recur.VtoId);
			Assert.IsNull(recur._WhoCanEdit);

			Assert.IsNotNull(recur._DefaultAttendees);
			Assert.IsTrue(recur._DefaultAttendees.Any(x => x.User.Id == org.Manager.Id));


			Assert.IsNotNull(recur._DefaultMeasurables);
			Assert.AreEqual(1, recur._DefaultMeasurables.Count());
			Assert.AreEqual("meas1", recur._DefaultMeasurables.First().Measurable.Title);

			Assert.IsNotNull(recur._DefaultRocks);
			Assert.AreEqual(1, recur._DefaultRocks.Count());
			Assert.AreEqual("rock1", recur._DefaultRocks.First().ForRock.Rock);

			Assert.IsNotNull(recur._MeetingNotes);
			Assert.AreEqual(0, recur._MeetingNotes.Count());

			Assert.IsNotNull(recur._Pages);
			Assert.AreEqual(7, recur._Pages.Count());
			Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Segue));
			Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Scorecard));
			Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Rocks));
			Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Headlines));
			Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Todo));
			Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.IDS));
			Assert.IsTrue(recur._Pages.Any(x => x.PageType == L10Recurrence.L10PageType.Conclude));

			Assert.IsNotNull(recur._VideoConferenceProviders);
			Assert.AreEqual(0, recur._VideoConferenceProviders.Count());


		}

    }
}
