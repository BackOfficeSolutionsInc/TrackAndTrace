using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CsvHelper;
using RadialReview.Utilities;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using System.Net;
using System.Globalization;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.RealTime;
using System.Text.RegularExpressions;

namespace RadialReview.Controllers {
	public partial class UploadController : BaseController {

		//private static Regex thousandRegex = new Regex("[\\d]\\s*k(\\s+|$)");
		private static decimal? ParceScore(string score) {
			string s = score.ToLower();
			var mult = 1.0m;
			if (s.Contains("mm"))
				mult = 1000000;
			else if (s.Contains("k") && Regex.IsMatch(s, "[\\d]\\s*k(\\s+|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
				mult = 1000;

			//s = Regex.Replace(s, "[^0-9\\.-\\s]", "");
			var parsed = Regex.Replace(score, "[^0-9\\s\\.-]", "").Trim().Split(new char[] { ' ', '\t' }).Select(x => x.TryParseDecimal()).FirstOrDefault(x => x != null);
			//s = s.Trim();

			//var parsed = s.TryParseDecimal();
			if (parsed != null)
				return parsed.Value * mult;
			return null;


		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<PartialViewResult> ProcessScorecardSelection(IEnumerable<int> users, IEnumerable<int> dates, IEnumerable<int> measurables, IEnumerable<int> goals, long recurrenceId, string path) {
			try {
				var ui = await UploadAccessor.DownloadAndParse(GetUser(), path);
				var csvData = ui.Csv;
				var userRect = new Rect(users);
				var measurableRect = new Rect(measurables);
				var goalsRect = new Rect(goals);

				userRect.EnsureRowOrColumn();
				userRect.EnsureSameRangeAs(measurableRect);
				userRect.EnsureSameRangeAs(goalsRect);

				Rect dateRect = null;
				if (dates != null)
					dateRect = new Rect(dates);

				if (dateRect != null && userRect.GetType() != dateRect.GetType())
					throw new ArgumentOutOfRangeException("rect", "Date selection and owner selection must be of different selection types (either row or column)");

				var userStrings = userRect.GetArray1D(csvData);
				var measurableStrings = measurableRect.GetArray1D(csvData);
				var goals1 = goalsRect.GetArray1D(csvData, x => ParceScore(x) ?? 0m);

				List<DateTime> dates1 = new List<DateTime>();
				if (dateRect != null) {
					var dateStrings = dateRect.GetArray1D(csvData);
					dates1 = TimingUtility.FixOrderedDates(dateStrings, new CultureInfo("en-US"));
				}


				var orgId = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, false).OrganizationId;
				var allUsers = TinyUserAccessor.GetOrganizationMembers(GetUser(), orgId);
				// var allUsers = OrganizationAccessor.GetMembers_Tiny(GetUser(), GetUser().Organization.Id);
				var userLookups = DistanceUtility.TryMatch(userStrings, allUsers);

				Rect scoreRect = null;
				List<List<Decimal?>> scores;
				if (dateRect != null) {
					if (dateRect.GetRectType() == RectType.Row || (dateRect.IsCell() && userRect.GetRectType() == RectType.Column)) {
						scoreRect = new Rect(dateRect.MinX, userRect.MinY, dateRect.MaxX, userRect.MaxY);
					} else {
						scoreRect = new Rect(userRect.MinX, dateRect.MinY, userRect.MaxX, dateRect.MaxY);
					}

					scores = scoreRect.GetArray(csvData, x => ParceScore(x));
				} else {
					scores = goals1.Select(x => new List<Decimal?>()).ToList();
				}
				var direction = goalsRect.GetArray1D(csvData, x => {
					if (x.Contains("<="))
						return LessGreater.LessThanOrEqual;
					if (x.Contains(">="))
						return LessGreater.GreaterThan;
					if (x.Contains("<"))
						return LessGreater.LessThan;
					if (x.Contains(">"))
						return LessGreater.GreaterThan;
					if (x.Contains("="))
						return LessGreater.EqualTo;
					return LessGreater.GreaterThan;
				});
				var units = goalsRect.GetArray1D(csvData, x => {
					if (x.Contains("$") || x.ToLower().Contains("usd") || x.ToLower().Contains("dollar"))
						return UnitType.Dollar;
					if (x.Contains("%"))
						return UnitType.Percent;
					if (x.Contains("£") || x.Contains("₤") || x.ToLower().Contains("gbp") || x.ToLower().Contains("pound"))
						return UnitType.Pound;
					if (x.Contains("€") || x.Contains("€") || x.ToLower().Contains("euro") || x.ToLower().Contains("eur"))
						return UnitType.Euros;
					return UnitType.None;
				});

				var m = new UploadScorecardSelectedDataVM() {
					Rows = csvData,
					Users = userStrings,
					UserLookup = userLookups,
					Measurables = measurableStrings,
					Dates = dates1,
					Goals = goals1,
					Scores = scores,
					RecurrenceId = recurrenceId,
					Path = path,
					//UseAWS = useAWS,
					ScoreRange = scoreRect.NotNull(x => string.Join(",", x.ToString())),
					MeasurableRectType = "" + dateRect.NotNull(x => x.GetRectType()),
					DateRange = dateRect.NotNull(x => string.Join(",", x.ToString())),
					AllUsers = allUsers.Select(x => new SelectListItem() { Text = x.FirstName + " " + x.LastName, Value = x.UserOrgId + "" }).ToList(),
					Direction = direction,
					Units = units
				};
				return PartialView("UploadScorecardSelected", m);
			} catch (Exception e) {
				throw new Exception(e.Message + "[" + path + "]", e);
			}
		}


		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> SubmitScorecard(FormCollection model) {
			var path = model["Path"].ToString();
			try {
				//var useAws = model["UseAWS"].ToBoolean();
				var recurrence = model["recurrenceId"].ToLong();
				var measurableRectType = model["MeasurableRectType"].ToString();
				Rect scoreRect = null;
				if (!string.IsNullOrWhiteSpace(model["ScoreRange"]))
					scoreRect = new Rect(model["ScoreRange"].ToString().Split(',').Select(x => x.ToInt()).ToList());
				Rect dateRect = null;
				if (!string.IsNullOrWhiteSpace(model["DateRange"]))
					dateRect = new Rect(model["DateRange"].ToString().Split(',').Select(x => x.ToInt()).ToList());

				_PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(recurrence));
				var ui = await UploadAccessor.DownloadAndParse(GetUser(), path);
				var csvData = ui.Csv;

				var keys = model.Keys.OfType<string>();
				var measurables = keys.Where(x => x.StartsWith("m_measurable_"))
					.ToDictionary(x => x.SubstringAfter("m_measurable_").ToInt(), x => (string)model[x]);
				var goals = keys.Where(x => x.StartsWith("m_goal_"))
					.ToDictionary(x => x.SubstringAfter("m_goal_").ToInt(), x => ParceScore(model[x]) ?? 0);
				var users = keys.Where(x => x.StartsWith("m_user_"))
					.ToDictionary(x => x.SubstringAfter("m_user_").ToInt(), x => model[x].ToLong());
				var goalDirs = keys.Where(x => x.StartsWith("m_goaldir_"))
					.ToDictionary(x => x.SubstringAfter("m_goaldir_").ToInt(), x => (LessGreater)Enum.Parse(typeof(LessGreater), model[x]));
				var goalUnits = keys.Where(x => x.StartsWith("m_goalunits_"))
					.ToDictionary(x => x.SubstringAfter("m_goalunits_").ToInt(), x => (UnitType)Enum.Parse(typeof(UnitType), model[x]));

				List<List<Decimal?>> scores = null;
				if (scoreRect != null)
					scores = scoreRect.GetArray(csvData, (x, c) => ParceScore(x));

				List<DateTime> dates = new List<DateTime>();
				if (dateRect != null) {
					var dateStrings = dateRect.GetArray1D(csvData);
					dates = TimingUtility.FixOrderedDates(dateStrings, new CultureInfo("en-US"));
				}
				var weekShift = 0;
				if (dates.Any()) {
					var weekNum = TimingUtility.GetWeekSinceEpoch(dates[0].AddDays(7).AddDays(6).StartOfWeek(DayOfWeek.Sunday));
					var weekStart = GetUser().GetOrganizationSettings().WeekStart;
					var genDate = TimingUtility.GetDateSinceEpoch(weekNum).AddDays(-7).AddDays(6).StartOfWeek(weekStart);
					while (genDate.AddDays(7)<=dates[0]) {
						weekShift += 1;
						genDate = TimingUtility.GetDateSinceEpoch(weekNum + weekShift).AddDays(-7).AddDays(6).StartOfWeek(weekStart);
					}
					genDate = TimingUtility.GetDateSinceEpoch(weekNum + weekShift).AddDays(-7).AddDays(6).StartOfWeek(weekStart);
					while (dates[0] < genDate) {
						weekShift -= 1;
						genDate = TimingUtility.GetDateSinceEpoch(weekNum + weekShift).AddDays(-7).AddDays(6).StartOfWeek(weekStart);
					}
				}

				var caller = GetUser();
				var now = DateTime.UtcNow;
				var measurableLookup = new Dictionary<int, MeasurableModel>();
				using (var s = HibernateSession.GetCurrentSession(singleSession:false)) {
					using (var tx = s.BeginTransaction()) {
						using (var rt = RealTimeUtility.Create(false)) {
							var org = s.Get<L10Recurrence>(recurrence).Organization;
							var perms = PermissionsUtility.Create(s, caller).ViewOrganization(org.Id);
							var ii = 1;
							foreach (var m in measurables) {
								var ident = m.Key;
								var owner = users[ident];
								var goal = goals[ident];
								var goaldir = goalDirs[ident];
								var measurable = new MeasurableModel() {
									Title = m.Value,
									OrganizationId = org.Id,
									Goal = goal,
									GoalDirection = goaldir,
									AccountableUserId = owner,
									AdminUserId = owner,
									CreateTime = now,
									_Ordering = ii
								};

								//Empty row?
								if (string.IsNullOrWhiteSpace(measurable.Title)) {
									if (scoreRect == null) {
										continue;
									} else {
										var scoreRow = measurableRectType != "Column"
									  ? new Rect(scoreRect.MinX, scoreRect.MinY + ident, scoreRect.MaxX, scoreRect.MinY + ident)
									  : new Rect(scoreRect.MinX + ident, scoreRect.MinY, scoreRect.MinX + ident, scoreRect.MaxY);

										var scoresFound = scoreRow.GetArray1D(csvData, x => ParceScore(x));
										if (scoresFound.All(x => x == 0 || x == null))
											continue;
									}
								}

								L10Accessor.AddMeasurable(s, perms, rt, recurrence, L10Controller.AddMeasurableVm.CreateNewMeasurable(recurrence, measurable), skipRealTime: true, rowNum: ii);
								ii += 1;
								measurableLookup[ident] = measurable;




								if (scoreRect != null) {
									var scoreRow = measurableRectType != "Column"
										? new Rect(scoreRect.MinX, scoreRect.MinY + ident, scoreRect.MaxX, scoreRect.MinY + ident)
										: new Rect(scoreRect.MinX + ident, scoreRect.MinY, scoreRect.MinX + ident, scoreRect.MaxY);

									var scoresFound = scoreRow.GetArray1D(csvData, x => ParceScore(x));

									for (var i = 0; i < dates.Count; i++) {
										var week = TimingUtility.GetWeekSinceEpoch(dates[i].AddDays(7).AddDays(6).StartOfWeek(DayOfWeek.Sunday))+weekShift;
										var score = scoresFound[i];
										L10Accessor._UpdateScore(s, perms, rt, measurable.Id, week, score, null, noSyncException: true, skipRealTime: true);
									}
								}


							}
							var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
								.Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrence)
								.Select(x => x.User.Id)
								.List<long>().ToList();

							foreach (var u in users.Where(x => !existing.Any(y => y == x.Value)).Select(x => x.Value).Distinct()) {
								s.Save(new L10Recurrence.L10Recurrence_Attendee() {
									User = s.Load<UserOrganizationModel>(u),
									L10Recurrence = s.Load<L10Recurrence>(recurrence),
									CreateTime = now,
								});
							}
							tx.Commit();
							s.Flush();
						}
					}
				}

				//ShowAlert("Uploaded Scorecard", AlertType.Success);

				return Json(ResultObject.CreateRedirect("/l10/wizard/" + recurrence + "#Scorecard", "Uploaded Scorecard"));
			} catch (Exception e) {
				throw new Exception(e.Message + "[" + path + "]", e);
			}
		}

		public class UploadScorecardSelectedDataVM {

			public List<UnitType> Units { get; set; }
			public List<LessGreater> Direction { get; set; }
			public List<string> Measurables { get; set; }
			public List<string> Users { get; set; }
			public List<decimal> Goals { get; set; }
			public List<DateTime> Dates { get; set; }

			public List<List<String>> Rows { get; set; }

			public List<Tuple<long, long>> Errors { get; set; }

			public Dictionary<string, DiscreteDistribution<TinyUser>> UserLookup { get; set; }

			public List<List<decimal?>> Scores { get; set; }

			public long RecurrenceId { get; set; }

			public bool UseAWS { get; set; }

			public string Path { get; set; }

			public string ScoreRange { get; set; }

			public string MeasurableRectType { get; set; }

			public string DateRange { get; set; }

			public List<SelectListItem> AllUsers { get; set; }
		}

	}
}