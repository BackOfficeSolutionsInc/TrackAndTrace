using System.Net;
using System.Net.Sockets;
using System.Reflection;
using FluentNHibernate.Utils;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.SignalR;
using NHibernate.Criterion;
using NHibernate.Hql.Ast.ANTLR;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Log;
using RadialReview.Models.Payments;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Models.Tasks;
using RadialReview.Models.Application;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using RadialReview.Models.UserModels;
using WebGrease.Css.Extensions;
using RadialReview.Utilities.Productivity;
using RadialReview.Models.Todo;
using RadialReview.Models.Issues;
using RadialReview.Models.Scorecard;
using RadialReview.Models.L10;
using OxyPlot;
using RadialReview.Models.Onboard;

namespace RadialReview.Controllers {

	public class AdminController : BaseController {

		//[Access(AccessLevel.Radial)]
		//public async Task<ActionResult> Plot()
		//{
		//    //var stream = new MemoryStream();
		//    //var pngExporter = new PdfExporter { Width = 400, Height = 400, Background = OxyColors.White };
		//    //var s=_ChartsEngine.ReviewScatter2(GetUser(), 797, 198, "about-*", true, false);
		//    //var chart = OxyplotAccessor.ScatterPlot(s);
		//    //pngExporter.Export(chart, stream);

		//    return Pdf(PdfAccessor.GenerateReviewPrintout(GetUser(),))

		//    return new FileStreamResult(new MemoryStream(stream.ToArray()), "application/pdf");
		//}

		[Access(AccessLevel.Radial)]
		public ActionResult Signups(int days = 14) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var notRecent = DateTime.UtcNow.AddDays(-days);
					//var  = DateTime.UtcNow.AddDays(-1);
					var users = s.QueryOver<OnboardingUser>().OrderBy(x => x.StartTime).Desc.Where(x => x.StartTime > notRecent).List().ToList();
					return View(users);
				}
			}
		}



		[Access(AccessLevel.Radial)]
		public ActionResult Meetings(int minutes = 120) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var recent = DateTime.UtcNow.AddMinutes(-minutes);
					var notRecent = DateTime.UtcNow.AddDays(-1);
					var measurables = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && (x.CompleteTime == null || x.CompleteTime >= recent) && x.CreateTime > notRecent).List().ToList();
					return View(measurables);
				}
			}
		}
		public class MergeAcc {
			public UserOrganizationModel Main { get; set; }
			public UserOrganizationModel ToMerge { get; set; }
		}
		[Access(AccessLevel.Radial)]
		public ActionResult MergeAccounts(long? id = null) {
			var model = new MergeAcc {
				Main = GetUser()
			};
			if (id != null) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {

						var other = s.Get<UserOrganizationModel>(id);
						model.ToMerge = other;
					}
				}
			}
			return View(model);
		}
		[Access(AccessLevel.Radial)]
		public ActionResult PerformMergeAccounts(long mainId, long mergeId) {
			UserOrganizationModel main;
			UserOrganizationModel merge;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					main = s.Get<UserOrganizationModel>(mainId);
					merge = s.Get<UserOrganizationModel>(mergeId);

					if (main.TempUser != null)
						throw new PermissionsException("Cannot combine users: Main user has not registered");

					if (main.DeleteTime != null)
						throw new PermissionsException("Cannot combine users: Main user was deleted");
					if (merge.DeleteTime != null)
						throw new PermissionsException("Cannot combine users: Merge user was deleted");

					if (main.Organization.DeleteTime != null)
						throw new PermissionsException("Cannot combine users: Main Organization was deleted");
					if (merge.Organization.DeleteTime != null)
						throw new PermissionsException("Cannot combine users: Merge Organization was deleted");

					var email = main.User.UserName;
					var newIds = main.User.UserOrganizationIds.ToList();
					newIds.Add(mergeId);
					newIds = newIds.Distinct().ToList();


					main.User.UserOrganizationIds = newIds.ToArray();
					main.User.UserOrganizationCount = newIds.Count();
					main.User.UserOrganization.Add(merge);

					s.Update(main.User);




					merge.EmailAtOrganization = email;

					if (merge.TempUser != null) {
						merge.AttachTime = DateTime.UtcNow;
						merge.User = main.User;
						//merge.Organization = ;
						//merge.CurrentRole = userOrgPlaceholder;
						s.Delete(merge.TempUser);
						merge.TempUser = null;
					} else {
						if (merge.User.Id != main.User.Id) {
							merge.User.UserOrganizationIds = merge.User.UserOrganizationIds.Where(x => x != mainId).Distinct().ToArray();
							merge.User.UserOrganizationCount = merge.User.UserOrganizationIds.Count();
							merge.User.CurrentRole = merge.User.UserOrganizationIds.FirstOrDefault();
							merge.User.UserOrganization = merge.User.UserOrganization.Where(x => x.Id != mainId).ToArray();
							merge.User = main.User;
						}

						s.Update(merge.User);
					}

					s.Update(merge);


					main.UpdateCache(s);
					merge.UpdateCache(s);

					tx.Commit();
					s.Flush();

				}
			}
			return Content(
				@"Merged accounts.<br/>==================================<br/><br/>
				Hi " + main.GetFirstName() + @",<br/><br/>
				I've merged your accounts, please use '" + main.User.UserName + @"' to log on from now on to Traction Tools. To switch between accounts, click 'Change Organization' from the dropdown in the top-right.<br/><br/>
				Thank you,<br/><br/>
				" + GetUser().GetName());
		}



		[Access(AccessLevel.Radial)]
		public ActionResult MeetingsTable(int weeks = 3) {
			ViewBag.Weeks = weeks;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var recent = DateTime.UtcNow.AddDays(-weeks * 7);
					var notRecent = DateTime.UtcNow.AddDays(-weeks * 7 - 1);
					var measurables = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && (x.CompleteTime == null || x.CompleteTime >= recent) && x.CreateTime > notRecent).List().ToList();
					return View(measurables);
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult ShiftScorecard(long recurrence = 0, int weeks = 0) {
			if (recurrence == 0 || weeks == 0)
				return Content("Requires a recurrence and weeks parameter ?recurrence=&weeks= <br/>Warning: this command will shift the measurable regardless of whether it has been shifted for another meeting.");
			var messages = new List<string>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var measurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrence)
						.Fetch(x => x.Measurable).Eager
						.Select(x => x.Measurable).List<MeasurableModel>().ToList();

					foreach (var measurable in measurables) {
						if (measurable != null) {
							var message = "Measurable [" + string.Format("{0,-18}", measurable.Id) + "] shifted (" + string.Format("{0,-5}", weeks) + ") weeks.";
							messages.Add(message);
							log.Info(message);
							var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.Measurable.Id == measurable.Id).List().ToList();
							foreach (var score in scores) {
								score.DateDue = score.DateDue.AddDays(7 * weeks);
								score.ForWeek = score.ForWeek.AddDays(7 * weeks);
								s.Update(score);
							}
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			return Content(string.Join("<br/>", messages));
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> ResetDemo(long recurId = 1) {

			if (GetUser().Id == 600 || GetUser().IsRadialAdmin || GetUser().User.IsRadialAdmin) {
				//fall through
			} else {
				throw new PermissionsException();
			}

			var issues = new[] { "Board meeting location?", "Bonus allocation", "Equipment leases (current?)", "Sales department working remotely", "sales department morale" };
			var todos = new[] { "Call Vendors re: outstanding issues", "'Turnover' was not entered.", "call that speech writer back -- 'AthleticBusiness keynote speech'",
				"Send HR review documents for my team", "call back that canidant -- 'Shipping Errors' goal was missed by 8",
				"Send Lindsey data in prep for Board meeting",
				"Prepare meeting agenda for upcoming Board meeting", "Provide job description to HR for new EA", "Pass fitness room lead to sales for follow up",
				"Call StorEdge re: SEO & loop in Sales Team", "Send project update to Sales Team re: new software", "schedule time ......'AthleticBusiness keynote speech'",
				 };


			//var recurId = 1;
			var recur = L10Accessor.GetAngularRecurrence(GetUser(), recurId);
			var possibleUsers = recur.Attendees.Select(x => x.Id).ToList();
			possibleUsers.Add(600);

			var addedTodos = 0;
			var addedIssues = 0;
			var addedScores = 0;
			var deletedTodos = 0;
			var deletedIssues = 0;
			var deletedScores = 0;

			DateTime start = DateTime.UtcNow;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var recur1 = s.Get<L10Recurrence>(recurId);

					if (recur1.OrganizationId != 592)
						throw new Exception("Cannot edit meetings outside of Gart Sports");

					var caller = s.Get<UserOrganizationModel>(600L);
					var perms = PermissionsUtility.Create(s, caller);

					var r = new Random(22586113);

					foreach (var at in recur.Todos.Where(x => !x.Complete.Value)) {
						var todo = s.Load<TodoModel>(at.Id);
						todo.CompleteTime = DateTime.MinValue;
						s.Update(todo);
						deletedTodos += 1;
					}
					var createTime = DateTime.UtcNow.AddDays(-5);
					foreach (var todo in todos) {
						var complete = r.NextDouble() > .9 ? DateTime.UtcNow.AddDays(r.Next(-5, -1)) : (DateTime?)null;
						await TodoAccessor.CreateTodo(s, perms, recurId, new Models.Todo.TodoModel {
							AccountableUserId = possibleUsers[r.Next(possibleUsers.Count - 1)],
							Message = todo,
							ForRecurrenceId = recurId,
							DueDate = DateTime.UtcNow.AddDays(r.Next(1, 2)),
							CompleteTime = complete,
							CreateTime = createTime,
							OrganizationId = caller.Organization.Id,
						});
						createTime = createTime.AddMinutes(r.Next(3, 8));
						addedTodos += 1;
					}


					foreach (var at in recur.IssuesList.Issues.Where(x => !x.Complete.Value)) {
						var issue = s.Load<IssueModel.IssueModel_Recurrence>(at.Id);
						issue.CloseTime = DateTime.MinValue;
						s.Update(issue);
						deletedIssues += 1;
					}

					createTime = DateTime.UtcNow.AddDays(-5);
					foreach (var issue in issues) {
						//var complete = r.NextDouble() > .9 ? DateTime.UtcNow.AddDays(r.Next(-5, -1)) : (DateTime?)null;
						var owner = possibleUsers[r.Next(possibleUsers.Count - 1)];
						await IssuesAccessor.CreateIssue(s, perms, recurId, owner, new IssueModel {
							Message = issue,
							OrganizationId = caller.Organization.Id,
							CreateTime = createTime,
						});
						createTime = createTime.AddMinutes(r.Next(5, 15));
						addedIssues += 1;
					}
					var current = recur.Scorecard.Weeks.FirstOrDefault(x => x.IsCurrentWeek).ForWeekNumber;
					var emptyMeasurable = recur.Scorecard.Measurables.ElementAtOrDefault(r.Next(recur.Scorecard.Measurables.Count() - 1)).NotNull(x => x.Id);

					foreach (var angScore in recur.Scorecard.Scores.Where(x => x.ForWeek > current - 13)) {
						if (angScore.Id > 0) {
							if (angScore.ForWeek == current && angScore.Measurable.Id == emptyMeasurable) {
								var score = s.Load<ScoreModel>(angScore.Id);
								score.Measured = null;
								s.Update(score);
								deletedScores += 0;

							} else if (angScore.Measured == null) {
								var score = s.Load<ScoreModel>(angScore.Id);
								double stdDev = (double)(angScore.Measurable.Target.Value * (angScore.Measurable.Id.GetHashCode() % 5 * 2 + 1) / 100.0m);
								double mean = (double)angScore.Measurable.Target.Value;
								score.Measured = (decimal)Math.Floor(100 * r.NextNormal(mean, stdDev)) / 100m;
								s.Update(score);
								addedScores += 1;
							}
						}

					}

					tx.Commit();
					s.Flush();
				}
			}
			var duration = (DateTime.UtcNow - start).TotalSeconds;

			return Content("Todos: +" + addedTodos + "/-" + deletedTodos + " <br/>Issues: +" + addedIssues + "/-" + deletedIssues + " <br/>Scores: +" + addedScores + "/-" + deletedScores + " <br/>Duration: " + duration + "s");
		}


	}
	public partial class AccountController : UserManagementController {


		[Access(Controllers.AccessLevel.Radial)]
		public async Task<string> SetRadialAdmin(bool admin = true, string user = null) {
			user = user ?? GetUser().User.Id;
			var u = _UserAccessor.GetUserById(user);
			if (admin) {
				await UserManager.AddToRoleAsync(user, "RadialAdmin");
				return "Added " + u.UserName + ". (" + string.Join(", ", await UserManager.GetRolesAsync(u.Id)) + ")";
			} else {
				await UserManager.RemoveFromRoleAsync(user, "RadialAdmin");
				return "Removed " + u.UserName + ". (" + string.Join(", ", await UserManager.GetRolesAsync(u.Id)) + ")";
			}
		}

		[Access(Controllers.AccessLevel.Radial)]
		public ActionResult ListRadialAdmin() {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var res = s.CreateSQLQuery("select user.UserName,role.UserModel_id from UserRoleModel as role inner join UserModel as user where role.Role='RadialAdmin' and user.Id=role.UserModel_id").List<object[]>();
					var builder = "<table>";
					foreach (var o in res) {
						builder += "<tr><td>" + o[0] + "</td><td>" + o[1] + "</td></tr>";
					}
					builder += "</table>";
					return Content(builder);
				}			
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Headers() {
			return View();
		}

		[Access(AccessLevel.Radial)]

		public JsonResult GetRedis() {
			return Json(Config.Redis("CHANNEL"), JsonRequestBehavior.AllowGet);
		}
		[Access(AccessLevel.Radial)]
		public string Chrome(string id) {
			ChromeExtensionComms.SendCommand(id);
			return "ok: \"" + id + "\"";
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Version() {

			var version = Assembly.GetExecutingAssembly().GetName().Version;
			var date = new DateTime(2000, 1, 1)
				.AddDays(version.Build)
				.AddSeconds(version.Revision * 2);

			//var server = NetworkAccessor.GetPublicIP();//Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
			var server = Amazon.EC2.Util.EC2Metadata.InstanceId.ToString();
			return Content(version.ToString() + " <br/> " + date.ToString("U") + " <br/><br/> " + server);
		}


		[Access(AccessLevel.Radial)]
		[OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]

		public ActionResult FixEmail() {
			return View();
		}

		[Access(AccessLevel.Radial)]
		[HttpPost]
		public ActionResult FixEmail(FormCollection form) {
			var user = GetUser();

			if (user.GetEmail() != form["oldEmail"] || user.Id != form["userId"].ToLong())
				throw new PermissionsException("Incorrect User.");

			if (!IsValidEmail(form["newEmail"]))
				throw new PermissionsException("Email invalid.");

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					s.Evict(user);
					user = s.Get<UserOrganizationModel>(user.Id);
					user.EmailAtOrganization = form["newEmail"];

					if (user.User != null) {
						//s.Evict(user.User);
						user.User.UserName = form["newEmail"];
						//s.Update(user.User);
					}

					if (user.TempUser != null) {
						//s.Evict(user.TempUser);
						user.TempUser.Email = form["newEmail"];
						//s.Update(user.TempUser);
					}
					user.UpdateCache(s);
					var c = new Cache();
					c.InvalidateForUser(user, CacheKeys.USERORGANIZATION);
					c.InvalidateForUser(user, CacheKeys.USER);
					s.Update(user);
					tx.Commit();
					s.Flush();
				}
			}
			ViewBag.InfoAlert = "Make sure to email this person with their new login.";
			return RedirectToAction("FixEmail");
		}
		private bool IsValidEmail(string email) {
			try {
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == email;
			} catch {
				return false;
			}
		}

		[Access(AccessLevel.Radial)]
		public JsonResult Stats() {
			return Json(ApplicationAccessor.Stats(), JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.Radial)]
		public String TempDeep(long id) {
			var now = DateTime.UtcNow;
			var count = _UserAccessor.CreateDeepSubordinateTree(GetUser(), id, now);

			var o = "TempDeep - " + now.Ticks + " - " + count;
			log.Info(o);
			return o;
		}

		/*[Access(AccessLevel.Radial)]
        public String FixManagerGroups()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    tx.Commit();
                    s.Flush();
                }
            }
        }*/


		[Access(AccessLevel.Radial)]
		public int FixTeams() {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var teams = s.QueryOver<OrganizationTeamModel>().List();
					foreach (var t in teams) {
						if (t.Type == TeamType.Subordinates && t.DeleteTime == null) {
							var mid = t.ManagedBy;
							var m = s.Get<UserOrganizationModel>(mid);
							if (m.DeleteTime != null) {
								t.DeleteTime = m.DeleteTime;
								s.Update(t);
								count++;
							}
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count;

		}

		[Access(AccessLevel.Radial)]
		public string UndoRandomReview(long id) {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var values = s.QueryOver<CompanyValueAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();
					var roles = s.QueryOver<GetWantCapacityAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();
					var rocks = s.QueryOver<RockAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();
					var feedbacks = s.QueryOver<FeedbackAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();

					foreach (var v in values) {
						v.Exhibits = PositiveNegativeNeutral.Indeterminate;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count++;
					}
					foreach (var v in roles) {
						v.GetIt = FiveState.Indeterminate;
						v.WantIt = FiveState.Indeterminate;
						v.HasCapacity = FiveState.Indeterminate;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count += 3;
					}
					foreach (var v in rocks) {
						v.Finished = Tristate.Indeterminate;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count++;
					}
					foreach (var v in feedbacks) {
						v.Feedback = null;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count++;
					}
					tx.Commit();
					s.Flush();

				}
			}
			return "Undo Random Review. Update: " + count;

		}


		[Access(AccessLevel.Radial)]
		public string RandomReview(long id) {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var desenterPercent = .1;
					var standardPercent = .8;
					var standardPercentDeviation = .2;

					var unstartedPercent = .05;
					var incompletePercent = .1;

					var rockPercent = .88;

					var values = s.QueryOver<CompanyValueAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var roles = s.QueryOver<GetWantCapacityAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var rocks = s.QueryOver<RockAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var feebacks = s.QueryOver<FeedbackAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();

					var about2 = new HashSet<long>(values.Select(x => x.AboutUserId));
					roles.Select(x => x.AboutUserId).ForEach(x => about2.Add(x));
					rocks.Select(x => x.AboutUserId).ForEach(x => about2.Add(x));

					var reviewIds = new HashSet<long>();

					var about = about2.ToList();

					var r = new Random();
					var unstartedList = new List<long>();
					var incompleteList = new List<long>();

					for (var i = 0; i <= unstartedPercent * about.Count; i++)
						unstartedList.Add(about[r.Next(about.Count)]);
					for (var i = 0; i <= incompletePercent * about.Count; i++)
						incompleteList.Add(about[r.Next(about.Count)]);


					var lookup = about.ToDictionary(x => x, x => {
						//BadEgg
						var luckA = 0.3;
						var luckB = 0.3;

						if (r.NextDouble() > desenterPercent) {//Standard
							luckA = Math.Max(Math.Min((r.NextDouble() - .5) * standardPercentDeviation + standardPercent, 1), 0);
						}
						if (r.NextDouble() > desenterPercent) {//Standard
							luckB = Math.Max(Math.Min((r.NextDouble() - .5) * standardPercentDeviation + standardPercent, 1), 0);
						}

						return new { luckA, luckB };
					});

					foreach (var v in values) {
						var a = lookup[v.AboutUserId];
						if (!v.Complete) {
							if (unstartedList.Contains(v.ByUserId))
								continue;
							if (incompleteList.Contains(v.ByUserId) && r.NextDouble() > .5)
								continue;
							count++;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;
							if (r.NextDouble() < a.luckA) {
								v.Exhibits = PositiveNegativeNeutral.Positive;
							} else {
								if (r.NextDouble() < a.luckA / 2) {
									v.Exhibits = PositiveNegativeNeutral.Negative;
								} else {
									v.Exhibits = PositiveNegativeNeutral.Neutral;
								}
							}
							s.Update(v);
						}
					}

					foreach (var v in roles) {
						var a = lookup[v.AboutUserId];
						if (!v.Complete) {
							if (unstartedList.Contains(v.ByUserId))
								continue;
							if (incompleteList.Contains(v.ByUserId) && r.NextDouble() > .5)
								continue;

							count += 3;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;
							if (r.NextDouble() < a.luckB) {
								v.GetIt = (r.NextDouble() > .1) ? FiveState.Always : FiveState.Mostly;
								v.WantIt = (r.NextDouble() > .1) ? FiveState.Always : FiveState.Mostly;
								v.HasCapacity = (r.NextDouble() > .1) ? FiveState.Always : FiveState.Mostly;
							} else {
								v.GetIt = (r.NextDouble() > .25) ? FiveState.Rarely : FiveState.Never;
								v.WantIt = (r.NextDouble() > 25) ? FiveState.Rarely : FiveState.Never;
								v.HasCapacity = (r.NextDouble() > 25) ? FiveState.Rarely : FiveState.Never;
							}
							s.Update(v);
						}
					}

					foreach (var v in rocks) {
						var a = lookup[v.AboutUserId];
						if (!v.Complete) {
							if (unstartedList.Contains(v.ByUserId))
								continue;
							if (incompleteList.Contains(v.ByUserId) && r.NextDouble() > .5)
								continue;

							count++;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;
							v.Finished = (r.NextDouble() < rockPercent) ? Tristate.True : Tristate.False;
							s.Update(v);
						}
					}

					var allFeedbacks = new[] { "No comment.", "Good progress.", "Could use some work", "Excellent", "A pleasure to work with" };

					foreach (var v in feebacks) {
						var a = lookup[v.AboutUserId];
						if (!v.Complete) {
							if (unstartedList.Contains(v.ByUserId))
								continue;
							if (incompleteList.Contains(v.ByUserId) && r.NextDouble() > .5)
								continue;
							if (!v.Required)
								continue;
							count++;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;

							v.Feedback = (r.NextDouble() < .05) ? allFeedbacks[r.Next(allFeedbacks.Length)] : "";
							s.Update(v);
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			return "Completed Randomize. Updated:" + count;
		}




		[Access(AccessLevel.Radial)]
		public JsonResult AdminAllUserLookups(string search) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var users = s.QueryOver<UserLookup>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(c => c.Email).IsLike("%" + search + "%")
						.Select(x => x.Email, x => x.UserId, x => x.Name, x => x.OrganizationId)
						.List<object[]>().ToList();
					var orgs = s.QueryOver<OrganizationModel>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.Id).IsIn(users.Select(x => (long)x[3]).ToList())
						.List().ToDictionary(x => x.Id, x => x.GetName());

					return Json(new {
						results = users.Select(x => new {
							text = "" + x[0],
							value = "" + x[1],
							name = "" + x[2],
							organization = "" + orgs.GetOrDefault((long)x[3], "")
						}).ToArray()
					}, JsonRequestBehavior.AllowGet);
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public String FixScatterChart(bool delete = false) {
			var i = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var scatters = s.QueryOver<ClientReviewModel>().List();
					foreach (var sc in scatters) {
						if (sc.ScatterChart == null || delete) {
							i++;
							sc.ScatterChart = new LongTuple();
							if (sc.Charts.Any()) {
								sc.ScatterChart.Filters = sc.Charts.First().Filters;
								sc.ScatterChart.Groups = sc.Charts.First().Groups;
								sc.ScatterChart.Title = sc.Charts.First().Title;
							}
							s.Update(sc);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}

			return "" + i;
		}

		[Access(AccessLevel.Radial)]
		public String FixAnswers(long id) {
			var reviewContainerId = id;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var reviewContainer = s.Get<ReviewsModel>(id);
					var orgId = reviewContainer.ForOrganizationId;


					var answers = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var perms = PermissionsUtility.Create(s, GetUser());

					int i = 0;

					var dataInteraction = ReviewAccessor.GetReviewDataInteraction(s, orgId);
					var qp = dataInteraction.GetQueryProvider();

					foreach (var a in answers) {
						var relationship = RelationshipAccessor.GetRelationships(perms, qp, a.ByUserId, a.AboutUserId).First();
						if (relationship == Models.Enums.AboutType.NoRelationship) {
							//int b = 0;
						}


						if (relationship != a.AboutType) {
							a.AboutType = relationship;
							s.Update(a);
							i++;
						}
					}


					tx.Commit();
					s.Flush();
					return "" + i;
				}
			}
		}


		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> Emails(int id) {
			var emails = Enumerable.Range(0, id).Select(x => Mail.To(EmailTypes.Test, "clay.upton@gmail.com").Subject("TestBulk").Body("Email #{0}", "" + x));
			var result = (await Emailer.SendEmails(emails));
			result.Errors = null;

			return Json(result, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public JsonResult FixReviewData() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var reviews = s.QueryOver<ReviewModel>().List().ToList();
					var allAnswers = s.QueryOver<AnswerModel>().List().ToList();

					foreach (var r in reviews) {
						var update = false;
						if (r.DurationMinutes == null && r.Complete) {
							var ans = allAnswers.Where(x => x.ForReviewId == r.Id).ToList();
							r.DurationMinutes = (decimal?)TimingUtility.ReviewDurationMinutes(ans, TimingUtility.ExcludeLongerThan);
							update = true;
						}

						if (r.Started == false) {
							var started = allAnswers.Any(x => x.ForReviewId == r.Id && x.Complete);
							r.Started = started;
							update = true;
						}
						if (update) {
							s.Update(r);
						}
					}

					tx.Commit();
					s.Flush();
				}
			}

			return Json(true, JsonRequestBehavior.AllowGet);
		}

		/*
        [Access(AccessLevel.Radial)]
        public async Task<String> TempCreate()
        {
            var lines = Regex.Replace(Data.people,"\r","").Split('\n');// = System.IO.File.ReadAllLines(file);
            //var accountController = new AccountController();
            //Column Ids
            var FIRST_NAME      =0;
            var LAST_NAME       =1;
            var POSITION        =2;
            var EMAIL           =3;
            var MANAGER_EMAIL   =4;
            var IS_MANAGER      =5;
            //Other variables
            var ORGANIZATION    ="Cornerstone";

            String firstEmail = null;

            var members = lines.Skip(1).Select(x=>{ 
                var split= x.Split(',');
                return new {
                        Email       = split[EMAIL],
                        FirstName   = split[FIRST_NAME],
                        LastName    = split[LAST_NAME],
                        Password    = "`123qwer",
                        Position    = split[POSITION],
                        ManagerEmail= split[MANAGER_EMAIL],
                        IsManager   = split[IS_MANAGER],
                        Created     = false
                    };
            });

            //Create org
            var orgCreatorData=members.First(x=>String.IsNullOrWhiteSpace(x.ManagerEmail));
            /*await accountController.Register(new RegisterViewModel(){
                Email=orgCreatorData.Email,
                fname=orgCreatorData.FirstName,
                lname=orgCreatorData.LastName,
                Password=orgCreatorData.Password,
            });*


            foreach(var m in members)
            {
                await Register(new RegisterViewModel(){
                    Email=m.Email,
                    fname=m.FirstName,
                    lname=m.LastName,
                    Password=m.Password,
                });

            }


            var orgCreator = _UserAccessor.GetUserByEmail(orgCreatorData.Email);
            var basicPlan=_PaymentAccessor.BasicPaymentPlan();
            var org=_OrganizationAccessor.CreateOrganization(orgCreator,new LocalizedStringModel(ORGANIZATION),true,basicPlan);


            var orgCreatorUO = _UserAccessor.GetUserByEmail(orgCreatorData.Email).UserOrganization.First();

            var posDict = new Dictionary<String,long>();

            foreach(var position in members.UnionBy(x=>x.Position).Select(x=>x.Position))
            {                
                posDict[position]=_OrganizationAccessor.EditOrganizationPosition(orgCreatorUO,0,org.Id,_PositoinAccessor.AllPositions().FirstOrDefault().Id,position).Id;
            }

            var errors =true;
            var notCreated = members.Where(x=>!x.Created && !String.IsNullOrWhiteSpace(x.ManagerEmail)).ToList();
            while(errors)
            {
                errors=false;
                foreach (var m in notCreated.ToList())
                {
                    try
                    {
                        var manager = _UserAccessor.GetUserByEmail(m.ManagerEmail).UserOrganization.First();
                        var tempUser=_NexusAccessor.CreateUserUnderManager(orgCreatorUO, manager.Id, bool.Parse(m.IsManager), posDict[m.Position], "clay.upton@gmail.com", m.FirstName, m.LastName);
                        var nexus = _NexusAccessor.Get(tempUser.Guid);
                        //[organizationId,EmailAddress,userOrgId,Firstname,Lastname]
                        _OrganizationAccessor.JoinOrganization(_UserAccessor.GetUserByEmail(m.Email), manager.Id, long.Parse(nexus.GetArgs()[2]));
                        notCreated.Remove(m);
                    }catch{
                        errors=true;
                    }
                }
            }

            return "Success";
        }*/



		private RadialReview.Controllers.ReviewController.ReviewDetailsViewModel GetReviewDetails(ReviewModel review) {
			var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);
			var answers = _ReviewAccessor.GetAnswersForUserReview(GetUser(), review.ForUserId, review.ForReviewsId);
			var model = new RadialReview.Controllers.ReviewController.ReviewDetailsViewModel() {
				Review = review,
				Axis = categories.ToSelectList(x => x.Category.Translate(), x => x.Id),
				xAxis = categories.FirstOrDefault().NotNull(x => x.Id),
				yAxis = categories.Skip(1).FirstOrDefault().NotNull(x => x.Id),
				AnswersAbout = answers,
				Categories = categories.ToDictionary(x => x.Id, x => x.Category.Translate()),
				NumberOfWeeks = TimingUtility.NumberOfWeeks(GetUser())
			};
			return model;
		}

		[Access(AccessLevel.Any)]
		public bool TestTask(long id) {
			var fire = DateTime.UtcNow.AddSeconds(id);
			_TaskAccessor.AddTask(new ScheduledTask() { Fire = fire, Url = "/Account/TestTaskRecieve" });
			log.Debug("TestTaskRecieve scheduled for: " + fire.ToString());
			return true;
		}

		[AllowAnonymous]
		[Access(AccessLevel.Any)]
		public bool TestTaskRecieve() {
			log.Debug("TestTaskRecieve hit: " + DateTime.UtcNow.ToString());
			return true;
		}

		[Access(AccessLevel.Any)]
		public ActionResult TestChart(long id, long reviewsId) {
			var review = _ReviewAccessor.GetReview(GetUser(), id);

			var model = GetReviewDetails(review);
			return View(model);
		}

		[Access(AccessLevel.Radial)]
		public String SendMessage(long id, string message) {
			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(id)).status(message);
			return "Sent: " + message;
		}

		[Access(AccessLevel.Radial)]
		public String UpdateCache(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<UserOrganizationModel>(id).UpdateCache(s);
					tx.Commit();
					s.Flush();
					return "Updated: " + org.GetName();
				}
			}
		}


		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> ChargeToken(long id, decimal amt) {
#pragma warning disable CS0618 // Type or member is obsolete
			return Json(await PaymentAccessor.ChargeOrganizationAmount(id, amt, true), JsonRequestBehavior.AllowGet);
#pragma warning restore CS0618 // Type or member is obsolete
		}

		[Access(AccessLevel.Radial)]
		public JsonResult ClearCache() {
			var urlToRemove = Url.Action("UserScorecard", "TileData");
			HttpResponse.RemoveOutputCacheItem(urlToRemove);
			return Json("cleared", JsonRequestBehavior.AllowGet);
		}


	}
}