using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using FluentNHibernate.Utils;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Nhibernate;

namespace RadialReview.Accessors {
	public class PhoneAccessor : BaseAccessor {


		public const string TODO = "todo";
		public const string HEADLINE = "headline";
		public const string ISSUE = "issue";

		public static DefaultDictionary<string, string> PossibleActions = new DefaultDictionary<string, string>(x => (x ?? "").ToTitleCase()){
			{ PhoneAccessor.ISSUE, "Add an Issue"  },
			{ PhoneAccessor.TODO, "Add a To-Do"},
			{ PhoneAccessor.HEADLINE, "Add a People Headline" },
		};

		public static List<CallablePhoneNumber> GetUnusedCallablePhoneNumbersForUser(ISession s, PermissionsUtility perms, long userId) {
			perms.Self(userId);

			var numbers = s.QueryOver<CallablePhoneNumber>().Where(x => x.DeleteTime == null).List().ToList();

			var user = s.Get<UserOrganizationModel>(userId);
			var ids = user.UserIds.ToArray();

			var used = s.QueryOver<PhoneActionMap>().Where(x => x.DeleteTime == null)
				.WhereRestrictionOn(x => x.Caller.Id).IsIn(ids)
				.List().ToList();

			return numbers.Where(x => used.All(y => y.SystemNumber != x.Number)).ToList();
		}

		public static List<CallablePhoneNumber> GetUnusedCallablePhoneNumbersForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetUnusedCallablePhoneNumbersForUser(s, perms, userId);
				}
			}

		}
		[Untested("Receive Text", "L10 text", "PersonalText")]
		public static async Task<string> ReceiveText(long fromNumber, string body, long systemNumber) {
			try {
				var rnd = new Random();
				PhoneActionMap found;

				var now = DateTime.UtcNow;
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var text = new PhoneTextModel() {
							Date = now,
							FromNumber = fromNumber,
							Message = body,
						};
						s.Save(text);

						//CallablePhoneNumber alias = null;


						found = s.QueryOver<PhoneActionMap>()
							.Where(x => x.DeleteTime == null && (x.SystemNumber == systemNumber || x.SystemNumber == systemNumber - 10000000000 || x.SystemNumber == systemNumber + 10000000000) && (x.CallerNumber == fromNumber || x.CallerNumber == fromNumber - 10000000000 || x.CallerNumber == fromNumber + 10000000000)).List().FirstOrDefault();

						if (found == null) {
							//Try to register the phone
							var p = (body.Trim().Split(new[] { ' ', '\n', '\r' }).FirstOrDefault() ?? "").Trim().ToLower();
							var found2 = s.QueryOver<PhoneActionMap>().Where(x => x.DeleteTime != null && x.Placeholder == p).OrderBy(x => x.DeleteTime).Desc.List().FirstOrDefault();
							if (found2 != null) {
								if (found2.DeleteTime < DateTime.UtcNow) {
									throw new PhoneException("This code has expired. Please try again.");
								}

								found2.DeleteTime = null;
								found2.CallerNumber = fromNumber;
								s.Update(found2);
								tx.Commit();
								s.Flush();
								return "Your phone has been registered. Add this number to your contacts to add " + found2.Action + "s via text message.";
							}
							throw new PhoneException("This number is not set up yet.");
						}

						found.Caller = s.Get<UserOrganizationModel>(found.Caller.Id);

						text.FromUser = found.Caller;
						s.Update(text);



						tx.Commit();
						s.Flush();

						if (String.IsNullOrEmpty(body)) {
							var whatResp = new List<string>() { "What was that?", "I didn't get that.", "Huh? I didn't get that.", "Could you repeat that?", "I'm not sure what you mean.", "Hum...Could you repeat that?" };
							var r = rnd.Next(whatResp.Count);
							return whatResp[r];
						}

					}
				}


				switch (found.Action) {
					case TODO:

						//var todoModel = new TodoModel() {
						//	AccountableUser = found.Caller,
						//	AccountableUserId = found.Caller.Id,
						//	CreatedBy = found.Caller,
						//	CreatedById = found.Caller.Id,
						//	Message = body,
						//	CreateTime = now,
						//	Organization = found.Caller.Organization,
						//	OrganizationId = found.Caller.Organization.Id,
						//	DueDate = now.AddDays(7),
						//	ForRecurrenceId = found.ForId,
						//	Details = "-sent from phone",
						//	ForModel = "TodoModel",
						//	ForModelId = -2,
						//};

						TodoCreation todoModel;

						if (found.ForId == -2) { // Personal todo list
												 //todoModel.ForRecurrenceId = null;
												 //todoModel.CreatedDuringMeetingId = null;
												 //todoModel.TodoType = TodoType.Personal;
							todoModel = TodoCreation.GeneratePersonalTodo(body, "-sent from phone", found.Caller.Id, now.AddDays(7), now);
						} else {
							todoModel = TodoCreation.GenerateL10Todo(found.ForId, body, "-sent from phone", found.Caller.Id, now.AddDays(7), null, "TodoModel", -2, now);
						}


						await TodoAccessor.CreateTodo(found.Caller, todoModel);
						//await TodoAccessor.CreateTodo(found.Caller, found.ForId, todoModel);
						return "To-do added.";
					case ISSUE:
						var creation = IssueCreation.CreateL10Issue(body, "-sent from phone", found.Caller.Id, found.ForId, null, modelType: "IssueModel", modelId: -2);
						await IssuesAccessor.CreateIssue(found.Caller, creation); /*found.ForId, found.Caller.Id, new IssueModel() {
						CreatedById = found.Caller.Id,
						CreatedDuringMeetingId = null,
						Message = body,
						Description = "-sent from phone",
						//ForRecurrenceId = found.ForId,
						ForModel = "IssueModel",
						ForModelId = -2,
						Organization = found.Caller.Organization,
					});*/
						return "Issue added.";
					case HEADLINE: {
							//var allGrams = new List<String>();
							//allGrams.AddRange(StringExtensions.GetNGrams(body, 2));
							//allGrams.AddRange(StringExtensions.GetNGrams(body, 1));
							//var users = L10Accessor.GetAttendees(found.Caller, found.ForId);
							//var userLookup = DistanceUtility.TryMatch(allGrams, users);

							await HeadlineAccessor.CreateHeadline(found.Caller, new PeopleHeadline() {
								AboutId = null,
								CreatedBy = found.Caller.Id,
								OrganizationId = found.Caller.Organization.Id,
								Owner = found.Caller,
								OwnerId = found.Caller.Id,
								RecurrenceId = found.ForId,
								AboutName = "n/a",
								Message = body,
								_Details = "-sent from phone",

							});
							return "People Headline added.";
						}
					default:
						throw new Exception();


				}
			} catch (PhoneException e) {
				log.Error("ReceiveText Error", e);
				return e.Message;
			} catch (Exception e) {
				log.Error("ReceiveText Error", e);
				return "Sorry, an error occurred.";
			}
		}

		[Untested("Add issue")]
		public static async Task<string> ReceiveForumText(string fromNumber, string body, string systemNumber) {
			try {
				var rnd = new Random();
				ExternalUserPhone found;
				var now = DateTime.UtcNow;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
				Func<Task> afterward = new Func<Task>(async () => { });
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
				try {
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							var text = new PhoneTextModel() {
								TextType = TextType.Forum,
								Date = now,
								FromNumber = fromNumber.ToLong(),
								Message = body,
							};
							s.Save(text);
							tx.Commit();
							s.Flush();
						}
					}

					if (body.ToLower().Contains("unlink me")) {
						using (var s = HibernateSession.GetCurrentSession()) {
							using (var tx = s.BeginTransaction()) {
								found = s.QueryOver<ExternalUserPhone>()
									.Where(x => (x.SystemNumber == systemNumber) && (x.UserNumber == fromNumber))
									.Where(x => x.DeleteTime == null || x.DeleteTime > now)
									.List().FirstOrDefault();

								found.DeleteTime = now;
								s.Update(found);
								tx.Commit();
								s.Flush();
								return "You're no longer attached.";
							}
						}
					}

					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							found = s.QueryOver<ExternalUserPhone>()
								.Where(x => (x.SystemNumber == systemNumber) && (x.UserNumber == fromNumber))
								.Where(x => x.DeleteTime == null || x.DeleteTime > now)
								.List().FirstOrDefault();

							//Number exists?
							if (found == null) {
								//No?
								if (string.IsNullOrWhiteSpace(body))
									throw new PhoneException("Please send your meeting code.");

								var trimmed = body.Trim().ToLower();
								//Does meeting code exist?
								var l10 = s.QueryOver<L10Recurrence>().Where(x => x.DeleteTime == null && (x.ForumCode == trimmed || x.ForumCode == trimmed.Substring(1, trimmed.Length - 1))).SingleOrDefault();

								if (l10 == null)
									throw new PhoneException("Please send your meeting code.");


								var phoneUser = new ExternalUserPhone() {
									CreateTime = now,
									DeleteTime = now.AddDays(1),
									Step = "Username",
									SystemNumber = systemNumber,
									UserNumber = fromNumber,
									ForModel = ForModel.Create(l10),
									//UserId = addedUser.User.Id
								};

								s.Save(phoneUser);

								var name = l10.Name;
								if (string.IsNullOrWhiteSpace(name))
									name = "meeting";

								tx.Commit();
								s.Flush();
								return "Welcome to the " + l10.Name + "! Please send your name.";
								//No? Register
							} else {
								//Yes?
								if (found.Step == "Username") {
									//Step = "Get name"
									if (found.ForModel.ModelType == ForModel.GetModelType<L10Recurrence>()) {
										if (string.IsNullOrWhiteSpace(body.Trim()))
											throw new PhoneException("Sorry I didn't get that. Please text your name.");


										var name = body.Trim();
										var nameParts = name.Split(' ');
										var firstName = "";
										var lastName = "";
										if (nameParts.Count() > 0)
											firstName = nameParts.First().ToTitleCase();
										if (nameParts.Count() > 1)
											lastName = nameParts.Last().ToTitleCase();

										var l10 = s.Get<L10Recurrence>(found.ForModel.ModelId);

										found.Name = body.Trim();
										found.Step = "StateMachine";

										s.Update(found);
										var org = l10.Organization;
										var _nil = org.Settings.DisableUpgradeUsers;

										tx.Commit();
										s.Flush();


										//wow look at this hack here folks
										afterward = new Func<Task>(async () => {
											var admin = UserOrganizationModel.CreateAdmin();
											admin.Organization = org;

											var addedUser = await JoinOrganizationAccessor.AddUser(admin,
												new Models.ViewModels.CreateUserOrganizationViewModel() {
													PhoneNumber = fromNumber,
													OrgId = l10.OrganizationId,
													Email = "noemail@noemail.com",
													FirstName = firstName,
													LastName = lastName,
													SendEmail = false,
												});
											await L10Accessor.AddAttendee(admin, l10.Id, addedUser.User.Id);
											//sessionseption?
											using (var ss = HibernateSession.GetCurrentSession()) {
												using (var txx = ss.BeginTransaction()) {
													found.UserId = addedUser.User.Id;
													ss.Update(found);
													txx.Commit();
													ss.Flush();
												}
											}
										});

										//var recur = s.Get<L10Recurrence>(found.ForModel.ModelId);

										var greeting = "Hi there";
										if (!String.IsNullOrWhiteSpace(firstName)) {
											greeting = "Hi " + firstName;
										}

										switch (l10.ForumStep) {
											case ForumStep.AddIssues:
												return greeting + ", what issues do you want to add? Try and keep them to 3 words.\nOne issue per text.";
											case ForumStep.RateMeeting:
												return "What rating do you give the meeting?";
											default:
												throw new PhoneException("");
										}
									} else {
										throw new PhoneException("Unknown action");
									}
									//return "You're connected.";
								} else if (found.Step == "StateMachine") {
									if (found.ForModel.ModelType == ForModel.GetModelType<L10Recurrence>()) {
										var recur = s.Get<L10Recurrence>(found.ForModel.ModelId);
										var user = s.Get<UserOrganizationModel>(found.UserId);
										var perms = PermissionsUtility.Create(s, user);

										switch (recur.ForumStep) {
											case ForumStep.AddIssues:
												if (string.IsNullOrWhiteSpace(body))
													throw new PhoneException("");

												var creation = IssueCreation.CreateL10Issue(body, null, found.UserId, recur.Id);
												await IssuesAccessor.CreateIssue(s, perms, creation);/*recur.Id, found.UserId, new IssueModel() {
												Message = body,
												OrganizationId = recur.OrganizationId,
												Organization = recur.Organization,
												CreatedById = user.Id
											});*/
												tx.Commit();
												s.Flush();
												return null;
											case ForumStep.RateMeeting:
												throw new PhoneException("RateMeeting not completed.");
											//break;
											default:
												throw new PhoneException("Unknown action");
										}
									} else {
										throw new PhoneException("Unknown action");
									}
								} else {
									throw new PhoneException("Unknown action");
								}
							}
						}
						throw new PhoneException("Unknown.");
					}

				} finally {
					await afterward();
				}








				//		var now = DateTime.UtcNow;
				//using (var s = HibernateSession.GetCurrentSession()) {
				//	using (var tx = s.BeginTransaction()) {
				//		var text = new PhoneTextModel() {
				//			TextType= TextType.Forum,
				//			Date = now,
				//			FromNumber = fromNumber,
				//			Message = body,
				//		};
				//		s.Save(text);					

				//		found = s.QueryOver<PhoneActionMap>()
				//			.Where(x => x.DeleteTime == null && (x.SystemNumber == systemNumber || x.SystemNumber == systemNumber - 10000000000 || x.SystemNumber == systemNumber + 10000000000) && (x.CallerNumber == fromNumber || x.CallerNumber == fromNumber - 10000000000 || x.CallerNumber == fromNumber + 10000000000)).List().FirstOrDefault();

				//		if (found == null) {
				//			//Try to register the phone
				//			var p = (body.Trim().Split(new[] { ' ', '\n', '\r' }).FirstOrDefault() ?? "").Trim().ToLower();
				//			var found2 = s.QueryOver<PhoneActionMap>().Where(x => x.DeleteTime != null && x.Placeholder == p).OrderBy(x => x.DeleteTime).Desc.List().FirstOrDefault();
				//			if (found2 != null) {
				//				if (found2.DeleteTime < DateTime.UtcNow) {
				//					throw new PhoneException("This code has expired. Please try again.");
				//				}

				//				found2.DeleteTime = null;
				//				found2.CallerNumber = fromNumber;
				//				s.Update(found2);
				//				tx.Commit();
				//				s.Flush();
				//				return "Your phone has been registered. Add this number to your contacts to add " + found2.Action + "s via text message.";
				//			}
				//			throw new PhoneException("This number is not set up yet.");
				//		}

				//		found.Caller = s.Get<UserOrganizationModel>(found.Caller.Id);

				//		text.FromUser = found.Caller;
				//		s.Update(text);



				//		tx.Commit();
				//		s.Flush();

				//		if (String.IsNullOrEmpty(body)) {
				//			var whatResp = new List<string>() { "What was that?", "I didn't get that.", "Huh? I didn't get that.", "Could you repeat that?", "I'm not sure what you mean.", "Hum...Could you repeat that?" };
				//			var r = rnd.Next(whatResp.Count);
				//			return whatResp[r];
				//		}

				//	}
				//}


				//switch (found.Action) {
				//	case TODO:

				//		var todoModel = new TodoModel() {
				//			AccountableUser = found.Caller,
				//			AccountableUserId = found.Caller.Id,
				//			CreatedBy = found.Caller,
				//			CreatedById = found.Caller.Id,
				//			Message = body,
				//			CreateTime = now,
				//			Organization = found.Caller.Organization,
				//			OrganizationId = found.Caller.Organization.Id,
				//			DueDate = now.AddDays(7),
				//			ForRecurrenceId = found.ForId,
				//			Details = "-sent from phone",
				//			ForModel = "TodoModel",
				//			ForModelId = -2,
				//		};

				//		if (found.ForId == -2) { // Personal todo list
				//			todoModel.ForRecurrenceId = null;
				//			todoModel.CreatedDuringMeetingId = null;
				//			todoModel.TodoType = TodoType.Personal;
				//		}

				//		await TodoAccessor.CreateTodo(found.Caller, found.ForId, todoModel);
				//		return "To-do added.";
				//	case ISSUE:
				//		await IssuesAccessor.CreateIssue(found.Caller, found.ForId, found.Caller.Id, new IssueModel() {
				//			CreatedById = found.Caller.Id,
				//			CreatedDuringMeetingId = null,
				//			Message = body,
				//			Description = "-sent from phone",
				//			//ForRecurrenceId = found.ForId,
				//			ForModel = "IssueModel",
				//			ForModelId = -2,
				//			Organization = found.Caller.Organization,

				//		});
				//		return "Issue added.";
				//	case HEADLINE: {
				//			//var allGrams = new List<String>();
				//			//allGrams.AddRange(StringExtensions.GetNGrams(body, 2));
				//			//allGrams.AddRange(StringExtensions.GetNGrams(body, 1));
				//			//var users = L10Accessor.GetAttendees(found.Caller, found.ForId);
				//			//var userLookup = DistanceUtility.TryMatch(allGrams, users);

				//			await HeadlineAccessor.CreateHeadline(found.Caller, new PeopleHeadline() {
				//				AboutId = null,
				//				CreatedBy = found.Caller.Id,
				//				OrganizationId = found.Caller.Organization.Id,
				//				Owner = found.Caller,
				//				OwnerId = found.Caller.Id,
				//				RecurrenceId = found.ForId,
				//				AboutName = "n/a",
				//				Message = body,
				//				_Details = "-sent from phone",

				//			});
				//			return "People Headline added.";
				//		}
				//	default:
				//		throw new Exception();


				//}

			} catch (PhoneException e) {
				log.Error("ReceiveForumText Error", e);
				return e.Message;
			} catch (Exception e) {
				log.Error("ReceiveForumText Error", e);
				return "Sorry, an error occurred.";
			}
		}

		public static void DeleteAction(UserOrganizationModel caller, long phoneActionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var found = s.Get<PhoneActionMap>(phoneActionId);
					if (found == null || found.DeleteTime != null)
						throw new PermissionsException("Does not exist.");
					if (found.Caller.Id != caller.Id)
						throw new PermissionsException();

					found.DeleteTime = DateTime.UtcNow;
					s.Update(found);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<PhoneActionMap> GetAllPhoneActionsForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).Self(userId);

					var map = s.QueryOver<PhoneActionMap>().Where(x => x.Caller.Id == userId && x.DeleteTime == null).List().ToList();
					var recurrences = s.QueryOver<L10Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(map.Select(x => x.ForId).ToList()).List().ToDictionary(x => x.Id, x => x);

					foreach (var m in map) {
						L10Recurrence recur = null;
						if (recurrences.TryGetValue(m.ForId, out recur))
							m._Recurrence = recur;
					}
					return map;
				}
			}
		}

		public class PhoneCode {
			public string Code { get; set; }
			public long PhoneNumber { get; set; }
		}

		public static PhoneCode AddAction(UserOrganizationModel caller, long userId, string action, long callableId, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).Self(userId);
					if (recurrenceId != -2)
						perms.ViewL10Recurrence(recurrenceId);

					var unused = GetUnusedCallablePhoneNumbersForUser(s, perms, userId);
					var found = unused.FirstOrDefault(x => x.Id == callableId);
					if (found == null)
						throw new PermissionsException("Phone number is unavailable.");


					//s.QueryOver<PhoneActionMap>().Where(x=>x.DeleteTime==null && x.CallerId == userId && CallerNumber!=-1 && )

					var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
					var random = new Random();
					var result = new string(
						Enumerable.Repeat(chars, 6)
								  .Select(x => x[random.Next(x.Length)])
								  .ToArray()).ToLower();



					var a = new PhoneActionMap() {
						Action = action,
						Caller = s.Load<UserOrganizationModel>(userId),
						CallerId = userId,
						CallerNumber = -1,
						Placeholder = result,
						SystemNumber = found.Number,
						CreateTime = DateTime.UtcNow,
						ForId = recurrenceId,
						DeleteTime = DateTime.UtcNow.AddMinutes(15)
					};

					s.Save(a);
					//a.Placeholder += a.Id;
					//s.Update(a);


					tx.Commit();
					s.Flush();

					return new PhoneCode() {
						Code = a.Placeholder,
						PhoneNumber = found.Number
					};
				}
			}
		}

		public static PhoneActionMap GetPersonalTextATodo(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (caller.Id != userId)
						throw new PermissionsException("Cannot view Personal Text A Todo number");
					return s.QueryOver<PhoneActionMap>().Where(x => x.CallerId == userId && x.DeleteTime == null && x.ForId == -2).Take(1).SingleOrDefault();
				}
			}
		}
	}
}