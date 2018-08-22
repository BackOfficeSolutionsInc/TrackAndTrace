using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TractionTools.UITests.Selenium;
using System.Threading.Tasks;
using RadialReview;
using RadialReview.Models.Todo;
using RadialReview.Accessors;
using TractionTools.Tests.Utilities;
using OpenQA.Selenium;
using RadialReview.Models.VTO;
using OpenQA.Selenium.Interactions;
using RadialReview.Models.Rocks;

namespace TractionTools.UITests.FAQ {
	[TestClass]
	public class DashboardTodoDateTests : BaseSelenium {



		[ClassInitialize]
		public static void Setup(TestContext ctx) {

			//MeetingName = "WizardMeeting";
			//Recur = await L10Utility.CreateRecurrence(MeetingName);
		}

		private long ShiftToMidnight(DateTime d) {
			var tzOffset = (long)TimeZoneInfo.Local.GetUtcOffset(d).TotalMinutes;
			return d.Date.ToJsMs() + ((24 * 60 - tzOffset) * 60 * 1000 - 1);
		}

		[TestMethod]
		[TestCategory("Visual")]
		public async Task Visual_SetRockFutureDate() {

			var testId = Guid.NewGuid();
			var AUC = await GetAdminCredentials(testId);
			
			var l10 = await L10Accessor.CreateBlankRecurrence(AUC.User, AUC.User.Organization.Id,false);
			await L10Accessor.AddAttendee(AUC.User, l10.Id, AUC.User.Id);

			TestView(AUC, "/L10", d => {


				d.DefaultTimeout(TimeSpan.FromSeconds(25));
				d.Find(".l10-row .manage-btn").Click();
				d.Find(".btn-vto").Click();

				d.Find(".rocks-section .future-date input").Click();
				d.Find(".rocks-section .future-date input").SendKeys(Keys.ArrowLeft);
				d.Find(".rocks-section .future-date input").SendKeys(Keys.ArrowLeft);
				d.Find(".rocks-section .future-date input").SendKeys("01022018");
				d.Find(".rocks-section .future-date input").SendKeys(Keys.Enter);
				d.Wait(800);

				//var tzOffset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2018, 01, 02, 0, 0, 0, DateTimeKind.Utc)).TotalMinutes;
				//var expectedAngularTime = new DateTime(2018,01,02,0,0,0,DateTimeKind.Utc).ToJsMs() + ((24 * 60 - tzOffset)* 60 * 1000 - 1 );
				var expectedAngularTime = ShiftToMidnight(new DateTime(2018, 01, 02, 0, 0, 0));

				//Display correctly?
				var expectedString = "2018-01-02";

				{
					var foundDueDate = (long)d.ExecuteScript("return +angular.element($(\".rocks-section\")[0]).scope().model.QuarterlyRocks.FutureDate");
					var foundStr = d.Find(".rocks-section .future-date input").Val();

					Assert.AreEqual(expectedAngularTime, foundDueDate);
					Assert.AreEqual(expectedString, foundStr);
				}
				d.Navigate().Refresh();
				{
					var foundDueDate = (long)d.ExecuteScript("return +angular.element($(\".rocks-section\")[0]).scope().model.QuarterlyRocks.FutureDate");
					//Database doesnt save at the sub-second level
					Assert.IsTrue(Math.Abs(expectedAngularTime - foundDueDate) <= 1000);
					var foundStr = d.Find(".rocks-section .future-date input").Val();
					Assert.AreEqual(expectedString, foundStr);
				}
				var qrId = (long)d.ExecuteScript("return +angular.element($(\".rocks-section\")[0]).scope().model.QuarterlyRocks.Id");

				DbQuery(s => {
					var qrModel = s.Get<QuarterlyRocksModel>(qrId);
					var foundDueDate = qrModel.FutureDate.Value.ToJsMs();
					Assert.IsTrue(Math.Abs(expectedAngularTime - foundDueDate) <= 1000);
				});
			});

		}

		[TestMethod]
		[TestCategory("Visual")]
		public async Task Visual_SetTodoDate() {

			var testId = Guid.NewGuid();
			var AUC = await GetAdminCredentials(testId);
			TestView(AUC, "/", d => {

				d.DefaultTimeout(TimeSpan.FromSeconds(25));

				d.Find(".todo-heading .clickable.new").Click();


				d.Find("#Message").SendKeys("TimeTodo");
				d.Find("#modalOk").Click();

				//Open the calendar
				d.Find("md-datepicker .md-datepicker-triangle-button").Click();

				//Find the first day of the month..
				var firstDayOfMonth = d.Find(".md-calendar-date.md-calendar-selected-date.md-focus").Parent().Parent().Find(".md-calendar-date .md-calendar-date-selection-indicator").Parent();
				var fdomTimeStamp = firstDayOfMonth.Data("timestamp").ToLong();
				firstDayOfMonth.Click();

				var expectedAngularTime = fdomTimeStamp + (24 * 60 * 60 * 1000 - 1);
				var expectedString = fdomTimeStamp.ToDateTime().ToString("MM-dd-yyyy");

				{
					var foundDueDate = (long)d.ExecuteScript("return +angular.element($(\"md-datepicker\")[0]).scope().todo.DueDate");
					var foundStr = d.Find("md-datepicker input").Val();

					Assert.AreEqual(expectedAngularTime, foundDueDate);
					Assert.AreEqual(expectedString, foundStr);
				}
				d.Navigate().Refresh();
				{
					d.Wait(3000);
					var foundDueDate = (long)d.ExecuteScript("return +angular.element($(\"md-datepicker\")[0]).scope().todo.DueDate");
					//Database doesnt save at the sub-second level
					Assert.IsTrue(Math.Abs(expectedAngularTime - foundDueDate) <= 1000);
					var foundStr = d.Find("md-datepicker input").Val();
					Assert.AreEqual(expectedString, foundStr);
				}
				var todoId = (long)d.ExecuteScript("return +angular.element($(\"md-datepicker\")[0]).scope().todo.Id");

				DbQuery(s => {
					var todo = s.Get<TodoModel>(todoId);
					var foundDueDate = todo.DueDate.ToJsMs();
					Assert.IsTrue(Math.Abs(expectedAngularTime - foundDueDate) <= 1000);
				});
			});

		}

		[TestMethod]
		[TestCategory("Visual")]
		public async Task Visual_SetMilestoneDate() {

			var testId = Guid.NewGuid();
			var AUC = await GetAdminCredentials(testId);

			var l10 = await L10Accessor.CreateBlankRecurrence(AUC.User, AUC.User.Organization.Id, false);
			await L10Accessor.AddAttendee(AUC.User, l10.Id, AUC.User.Id);
			var recur = L10Accessor.GetL10Recurrence(AUC.User, l10.Id, LoadMeeting.True());
			recur.RockType =RadialReview.Model.Enums.L10RockType.Milestones;
			await L10Accessor.EditL10Recurrence(AUC.User, recur);
			var rock = await RockAccessor.CreateRock(AUC.User, AUC.User.Id, "TimeRock");
			await L10Accessor.AttachRock(AUC.User, l10.Id, rock.Id, false);


			TestView(AUC, "/", d => {

				d.DefaultTimeout(TimeSpan.FromSeconds(25));

				d.Find(".rockModal").Click();
				d.Find(".add-milestone").Click();
				d.Find("#milestone").SendKeys("TimeMilestone");
				d.Wait(500);
				d.Find("#modalOk").Click();
				d.Wait(1500);
				d.Find("#modalOk").Click();

				d.ExecuteScript("createTile('/TileData/Milestones','Url')");

				d.Find("md-datepicker.due-date");
				{
					var expectedAngularTime = ShiftToMidnight(DateTime.Now);
					{
						var foundDueDate = (long)d.ExecuteScript("return +angular.element($(\"md-datepicker.due-date\")[0]).scope().model.Milestones[0].DueDate");
						Assert.AreEqual(expectedAngularTime, foundDueDate);
					}
					d.Navigate().Refresh();
					{
						d.Find("md-datepicker.due-date");
						var foundDueDate = (long)d.ExecuteScript("return +angular.element($(\"md-datepicker.due-date\")[0]).scope().model.Milestones[0].DueDate");
						Assert.IsTrue(Math.Abs(expectedAngularTime - foundDueDate) <= 1000);
					}
				}

				//d.Find("#Message").SendKeys("TimeTodo");
				//d.Find("#modalOk").Click();
				{
					////Open the calendar
					d.Find("md-datepicker .md-datepicker-triangle-button").Click();

					////Find the first day of the month..
					var firstDayOfMonth = d.Find(".md-calendar-date.md-calendar-selected-date.md-focus").Parent().Parent().Find(".md-calendar-date .md-calendar-date-selection-indicator").Parent();
					var fdomTimeStamp = firstDayOfMonth.Data("timestamp").ToLong();
					firstDayOfMonth.Click();

					var expectedAngularTime = fdomTimeStamp + (24 * 60 * 60 * 1000 - 1);
					var expectedString = fdomTimeStamp.ToDateTime().ToString("MM-dd-yyyy");

					{
						var foundDueDate = (long)d.ExecuteScript("return +angular.element($(\"md-datepicker\")[0]).scope().todo.DueDate");
						var foundStr = d.Find("md-datepicker input").Val();

						Assert.AreEqual(expectedAngularTime, foundDueDate);
						Assert.AreEqual(expectedString, foundStr);
					}
					d.Navigate().Refresh();
					{
						d.Find("md-datepicker.due-date");
						var foundDueDate = (long)d.ExecuteScript("return +angular.element($(\"md-datepicker\")[0]).scope().todo.DueDate");
						//Database doesnt save at the sub-second level
						Assert.IsTrue(Math.Abs(expectedAngularTime - foundDueDate) <= 1000);
						var foundStr = d.Find("md-datepicker input").Val();
						Assert.AreEqual(expectedString, foundStr);
					}
					var todoId = (long)d.ExecuteScript("return +angular.element($(\"md-datepicker\")[0]).scope().todo.Id");

					DbQuery(s => {
						var milestone = s.Get<Milestone>(-todoId);
						var foundDueDate = milestone.DueDate.ToJsMs();
						Assert.IsTrue(Math.Abs(expectedAngularTime - foundDueDate) <= 1000);
					});
				}
			});

		}
	}
}
