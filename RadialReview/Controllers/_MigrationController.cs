using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SimpleWorkflow.Model;
using RadialReview.Accessors;
using RadialReview.Engines;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Reviews;
using RadialReview.Models.Tests;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Models.Responsibilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.VTO;
using NHibernate;
using RadialReview.Models.Accountability;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.Scorecard;
using System.Web.Routing;
using RadialReview.Utilities.RealTime;
using RadialReview.Model.Enums;
using System.Linq.Expressions;
using RadialReview.Reflection;
using RadialReview.Controllers.AbstractController;
using RadialReview.Variables;
using RadialReview.Crosscutting.Hooks.Payment;

#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS0618 // Type or member is obsolete
namespace RadialReview.Controllers {
	public class MigrationController : BaseExpensiveController {
		#region old
		#region 11/06/2016
		// GET: Migration
		[Access(AccessLevel.Radial)]
		public string M1_7_2015() {
			var teams = new Ratio();

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Make subordinate teams not secret
					foreach (var a in s.QueryOver<OrganizationTeamModel>().List()) {
						if (a.Secret) {
							teams.Denominator++;
							if (a.Type == TeamType.Subordinates) {
								teams.Numerator++;
								a.Secret = false;
								s.Update(a);
							}
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			return teams.ToString();
		}

		// GET: Migration
		[Obsolete("Do not use", true)]
		[Access(AccessLevel.Radial)]
		public int M11_8_2014() {
			throw new Exception("Old");
			//var count = 0;
			//using (var s = HibernateSession.GetCurrentSession()) {
			//	using (var tx = s.BeginTransaction()) {
			//		foreach (var a in s.QueryOver<Askable>().List()) {
			//			if (a.OnlyAsk == AboutType.NoRelationship) {
			//				a.OnlyAsk = (AboutType)long.MaxValue;
			//				s.Update(a);
			//				count++;
			//			}
			//		}

			//		foreach (var r in s.QueryOver<RoleModel>().List()) {
			//			if (r.OrganizationId == 0) {
			//				r.OrganizationId = s.Get<UserOrganizationModel>(r.ForUserId).Organization.Id;
			//				s.Update(r);
			//				count++;
			//			}
			//		}


			//		/*foreach (var r in s.QueryOver<UserOrganizationModel>().List())
			//                 {
			//                     if (r.NumRocks == 0)
			//                     {
			//                         r.NumRocks = s.QueryOver<RockModel>().Where(x => x.ForUserId == r.Id && x.DeleteTime == null).List().Count;
			//                         s.Update(r);
			//                         count++;
			//                     }
			//                     if (r.NumRoles == 0)
			//                     {
			//                         r.NumRoles = s.QueryOver<RoleModel>().Where(x => x.ForUserId == r.Id && x.DeleteTime == null).List().Count;
			//                         s.Update(r);
			//                         count++;
			//                     }
			//                 }*/

			//		tx.Commit();
			//		s.Flush();
			//	}
			//}
			//return count;
		}

		[Access(AccessLevel.Radial)]
		public int M11_19_2014() {
			var count = 0;
			/*using (var s = HibernateSession.GetCurrentSession()){
                using (var tx = s.BeginTransaction()){
                    foreach (var a in s.QueryOver<OrganizationModel>().Where(x=>x.Settings == null || x.Settings.TimeZoneOffsetMinutes==0).List()){
                        if (a.Settings==null)
                            a.Settings=new OrganizationModel.OrganizationSettings();
						

                        a.Settings.TimeZoneOffsetMinutes = -360;
                        a.Settings.ManagersCanViewScorecard = true;
                        s.Update(a);
                        count++;
                    }
                    tx.Commit();
                    s.Flush();
                }
            }*/
			return count;
		}

		[Access(AccessLevel.Radial)]
		public string M12_09_2014(int orgId) {
			var count = 0;
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					foreach (var a in s.QueryOver<ResponsibilityModel>().Where(x => x.ForOrganizationId == orgId).List()) {
						if (a.GetQuestionType() == QuestionType.Slider) {
							a.DeleteTime = now;
							s.Update(a);
							count++;
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count + " " + now.Ticks;
		}

		[Access(AccessLevel.Radial)]
		public string M12_10_2014() {
			var count = 0;
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					foreach (var a in s.QueryOver<RockModel>().Where(x => x.OnlyAsk == (AboutType.Self | AboutType.Manager)).List()) {
						a.OnlyAsk = AboutType.Self;
						s.Update(a);
						count++;
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count + "";
		}

		[Access(AccessLevel.Radial)]
		public string M12_29_2014(long orgId, long periodId, long nextPeriodId) {
			var count = 0;
			var count2 = 0;
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					foreach (var a in s.QueryOver<ReviewsModel>().Where(x => x.OrganizationId == orgId).List()) {
						var update = false;
						/* if (a.PeriodId == 0) {
                             a.PeriodId = periodId;
                             update = true;
                         }
                         if (a.NextPeriodId == 0) {

                             a.NextPeriodId = nextPeriodId;
                             update = true;
                         }*/

						if (update) {
							s.Update(a);
							count++;
						}
						var rId = a.Id;

						foreach (var b in s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == rId).List()) {
							if (b.PeriodId == 0) {
								b.PeriodId = periodId;
								s.Update(b);
								count2++;
							}
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count + "  " + count2;
		}

		[Access(AccessLevel.Radial)]
		public string M2_23_2015_ImageResize() {
			var count = 0;
			var existingImg = AwsUtil.GetObjectsInFolder("Radial", "img");

			var existing32 = AwsUtil.GetObjectsInFolder("Radial", "32");
			var existing64 = AwsUtil.GetObjectsInFolder("Radial", "64");
			var existing128 = AwsUtil.GetObjectsInFolder("Radial", "128");


			var toAdd32 = SetUtility.AddRemove(existing32, existingImg, x => x.Key.Substring(x.Key.LastIndexOf("/")));
			var toAdd64 = SetUtility.AddRemove(existing64, existingImg, x => x.Key.Substring(x.Key.LastIndexOf("/")));
			var toAdd128 = SetUtility.AddRemove(existing128, existingImg, x => x.Key.Substring(x.Key.LastIndexOf("/")));

			foreach (var a in toAdd32.AddedValues) {
				var found = AwsUtil.GetObject("Radial", a.Key);
				var name = a.Key.Substring(a.Key.LastIndexOf("/") + 1);
				ImageAccessor.Upload(found, "32/" + name, ImageAccessor.TINY_INSTRUCTIONS);
			}
			foreach (var a in toAdd64.AddedValues) {
				var found = AwsUtil.GetObject("Radial", a.Key);
				var name = a.Key.Substring(a.Key.LastIndexOf("/") + 1);
				ImageAccessor.Upload(found, "64/" + name, ImageAccessor.MED_INSTRUCTIONS);
			}
			foreach (var a in toAdd128.AddedValues) {
				var found = AwsUtil.GetObject("Radial", a.Key);
				var name = a.Key.Substring(a.Key.LastIndexOf("/") + 1);
				ImageAccessor.Upload(found, "128/" + name, ImageAccessor.LARGE_INSTRUCTIONS);
			}

			return "Count:" + count;
		}

		[Access(AccessLevel.Radial)]
		public string M2_23_2015_UpdateMeetingTimes(long orgId, double len = 90, double std1 = 3, double std2 = 8) {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var found = s.QueryOver<L10Meeting>().Where(x => x.Organization.Id == orgId).List().ToList();
					var r = new Random();
					foreach (var x in found) {
						var start = x.StartTime.Value.Date.AddHours(18).AddMinutes(r.NextNormal(0, std1));
						x.CompleteTime = start.AddMinutes(r.NextNormal(len, std2));
						x.StartTime = start;
						count += 1;
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count + "";
		}

		[Access(AccessLevel.Radial)]
		public string M2_23_2015_UpdateRatings(long orgId) {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var meetingIds = s.QueryOver<L10Meeting>().Where(x => x.Organization.Id == orgId).List().ToList();
					var meeting = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.WhereRestrictionOn(x => x.L10Meeting.Id)
						.IsIn(meetingIds.Select(x => x.Id).ToList())
						.List()
						.OrderBy(x => x.L10Meeting.StartTime)
						.ToList();

					var r = new Random();
					var min = meeting.Select(x => x.L10Meeting.StartTime.Value).Min();
					var max = meeting.Select(x => x.L10Meeting.StartTime.Value).Max();

					var st = r.NextNormal(4, 1);

					foreach (var x in meeting) {
						if (x.Rating != null) {
							var i = (x.L10Meeting.StartTime.Value.Ticks - min.Ticks) / (1.0 * max.Ticks - min.Ticks);
							var ratingMean = (10 - st) * i * i + st + r.NextDouble() * 2 - 1;

							x.Rating = (int)Math.Max(1, Math.Min(10, Math.Round(r.NextNormal(ratingMean, 1))));
							count += 1;
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count + "";
		}
		[Access(AccessLevel.Radial)]
		public string M3_24_2015() {
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.QueryOver<OrganizationModel>().List().ToList();

					foreach (var x in org) {
						if (x.Settings.EnableL10 == false && x.Settings.EnableReview == false) {
							x.Settings.EnableReview = true;
							s.Update(x);
							count++;
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count + "";
		}
		[Access(AccessLevel.Radial)]
		public string M4_4_2015() {
			var count = new StringBuilder();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var reviews = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null).List().ToList();
					var answers = s.QueryOver<AnswerModel>().Where(x => x.DeleteTime == null).List().ToList();

					foreach (var r in reviews) {
						if (r.QuestionCompletion.NumRequired == 0) {
							var r1 = r;
							r.QuestionCompletion.NumRequired = answers.Count(x => x.DeleteTime == null && x.Required && x.ForReviewId == r1.Id);
							r.QuestionCompletion.NumRequiredComplete = answers.Count(x => x.DeleteTime == null && x.Required && x.ForReviewId == r1.Id && x.Complete);
							r.QuestionCompletion.NumOptional = answers.Count(x => x.DeleteTime == null && !x.Required && x.ForReviewId == r1.Id);
							r.QuestionCompletion.NumOptionalComplete = answers.Count(x => x.DeleteTime == null && !x.Required && x.ForReviewId == r1.Id && x.Complete);

							s.Update(r);
							count.AppendLine(r.Id + "," + r.QuestionCompletion.NumRequired + "," + r.QuestionCompletion.NumRequiredComplete + "," + r.QuestionCompletion.NumOptional + "," + r.QuestionCompletion.NumOptionalComplete + "<br/>");
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count.ToString();
		}

		[Access(AccessLevel.Radial)]
		public string M4_17_2015() {
			var c = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var orgs = s.QueryOver<OrganizationModel>().List();
					foreach (var organizationModel in orgs) {
						if (organizationModel.Organization == null) {
							organizationModel.Organization = organizationModel;
							s.Update(organizationModel);
							c++;
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return c + "";
		}

		[Access(AccessLevel.Radial)]
		public string M4_22_2015() {
			var c = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var orgs = s.QueryOver<OrganizationModel>().List();
					foreach (var organizationModel in orgs) {
						if (organizationModel.Settings.RockName == null) {
							organizationModel.Settings.RockName = "Rocks";
							s.Update(organizationModel);
							c++;
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return c + "";
		}

		[Access(AccessLevel.Radial)]
		public string M4_24_2015() {
			var c = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var cr = s.QueryOver<ClientReviewModel>().List();
					foreach (var x in cr) {
						if (x.ReviewContainerId == 0) {

							var review = s.Get<ReviewModel>(x.ReviewId);
							if (review != null) {
								x.ReviewContainerId = review.ForReviewContainerId;
								s.Update(x);
								c++;
							}
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return c + "";
		}

		[Access(AccessLevel.Radial)]
		public string M4_27_2015() {
			var c = 0;
			var d = 0;
			var e = 0;
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds
					var cr = s.QueryOver<UserOrganizationModel>().List();
					foreach (var x in cr) {
						if (x.TempUser != null) {

							if (x.TempUser.UserOrganizationId == 0) {
								x.TempUser.UserOrganizationId = x.Id;
								s.Update(x);
								c++;
							}
						}

						if (x.Cache == null) {
							x.UpdateCache(s);
							d++;
						}

						if (x.User != null && x.User.UserOrganizationCount == 0) {
							x.User.UserOrganizationCount = x.User.UserOrganization.Count;
							if (x.User.UserOrganizationCount != 0) {
								s.Update(x);
								e++;
							}
						}

						if (x.User != null && x.User.UserOrganizationIds == null) {
							x.User.UserOrganizationIds = x.User.UserOrganization.Select(y => y.Id).ToArray();
							s.Update(x);
							f++;
						}

					}
					tx.Commit();
					s.Flush();
				}
			}
			return c + " " + d + " " + e + " " + f;
		}

		[Access(AccessLevel.Radial)]
		public string M5_5_2015() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds
					var cr = s.QueryOver<OrganizationModel>().List();
					foreach (var o in cr) {
						if (o.Settings.TimeZoneId == null) {
							o.Settings.TimeZoneId = "Central Standard Time";
							f++;
							s.Update(o);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		[Access(AccessLevel.Radial)]
		public string M6_1_2015() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds
					var cr = s.QueryOver<L10Recurrence>().List();
					foreach (var o in cr) {
						if (
							o.SegueMinutes == 0 &&
							o.ScorecardMinutes == 0 &&
							o.RockReviewMinutes == 0 &&
							o.HeadlinesMinutes == 0 &&
							o.TodoListMinutes == 0 &&
							o.IDSMinutes == 0 &&
							o.ConclusionMinutes == 0
							) {
							o.SegueMinutes = 5;
							o.ScorecardMinutes = 5;
							o.RockReviewMinutes = 5;
							o.HeadlinesMinutes = 5;
							o.TodoListMinutes = 5;
							o.IDSMinutes = 60;
							o.ConclusionMinutes = 5;
							f++;
							s.Update(o);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		[Access(AccessLevel.Radial)]
		public string M6_3_2015() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds
					var cr = s.QueryOver<RockModel>().List();
					foreach (var o in cr) {
						if (o.OnlyAsk == (AboutType)long.MaxValue) {
							o.OnlyAsk = AboutType.Self;
							f++;
							s.Update(o);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		[Access(AccessLevel.Radial)]
		public string M6_22_2015(long reviewId) {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds
					var review = s.Get<ReviewsModel>(reviewId);
					if (review == null)
						return "Review not exist";
					/*if (review.PeriodId == null)
                        return "Period is null";*/


					var cr = s.QueryOver<RockModel>().Where(x => x.DeleteTime == null /*&& x.PeriodId == review.PeriodId*/).List().ToList();
					var ar = s.QueryOver<RockAnswer>().Where(x => x.DeleteTime == null && x.ForReviewContainerId == reviewId).List().ToList();
					var rrs = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewContainer.Id == reviewId).List().ToList();
					foreach (var o in cr) {
						if (!ar.Any(x => x.Askable.Id == o.Id)) {
							var rr = rrs.FirstOrDefault(x => x.ReviewerUserId == o.ForUserId);

							if (rr == null)
								continue;
							var rid = rr.Id;

							var rock = new RockAnswer() {
								Anonymous = review.AnonymousByDefault,
								Complete = false,
								Finished = Tristate.Indeterminate,
								ManagerOverride = RockState.Indeterminate,
								Completion = RockState.Indeterminate,
								Reason = null,
								Askable = o,
								Required = true,
								ForReviewId = rid,
								ReviewerUserId = o.ForUserId,
								RevieweeUserId = o.ForUserId,
								RevieweeUser = s.Load<ResponsibilityGroupModel>(o.ForUserId),
								ForReviewContainerId = review.Id,
								AboutType = AboutType.Self
							};
							s.Save(rock);
							f++;
						}

					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		[Access(AccessLevel.Radial)]
		public string M8_7_2015() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds
					var users = s.QueryOver<UserModel>().Where(x => x.SendTodoTime == null).List().ToList();
					foreach (var o in users) {
						o.SendTodoTime = 10;
						s.Update(o);
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		[Access(AccessLevel.Radial)]
		public string M10_19_2015() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds
					var pis = s.QueryOver<PermItem>().List().ToList();

					var l10 = s.QueryOver<L10Recurrence>().List().ToList();
					foreach (var o in l10) {
						if (pis.Any(x => x.ResId == o.Id && x.ResType == PermItem.ResourceType.L10Recurrence))
							continue;

						var p1 = new PermItem() {
							AccessorId = o.CreatedById,
							AccessorType = PermItem.AccessType.Creator,
							CanAdmin = true,
							CanView = true,
							CanEdit = true,
							CreatorId = -2,
							CreateTime = DateTime.UtcNow,
							OrganizationId = o.OrganizationId,
							IsArchtype = false,
							ResId = o.Id,
							ResType = PermItem.ResourceType.L10Recurrence,
						};
						s.Save(p1);
						var p2 = new PermItem() {
							AccessorId = 0,
							AccessorType = PermItem.AccessType.Members,
							CanAdmin = true,
							CanView = true,
							CanEdit = true,
							CreatorId = -2,
							CreateTime = DateTime.UtcNow,
							OrganizationId = o.OrganizationId,
							IsArchtype = false,
							ResId = o.Id,
							ResType = PermItem.ResourceType.L10Recurrence,
						};
						s.Save(p2);
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}
		[Access(AccessLevel.Radial)]
		public string M10_20_2015() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var l10 = s.QueryOver<TileModel>().List().ToList();
					foreach (var o in l10) {
						if (o.DataUrl.EndsWith("2"))
							continue;
						f++;
						o.DataUrl += "2";
						s.Update(o);
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		[Access(AccessLevel.Radial)]
		public string M10_20_2015_2() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var l10 = s.QueryOver<PermItem>().List().ToList();
					foreach (var o in l10) {
						if (o.AccessorType == PermItem.AccessType.Creator && o.AccessorId == 0) {
							o.DeleteTime = DateTime.UtcNow;
							f++;
							s.Update(o);
						}
						/*
                        if (!(o.AccessorType == PermItem.AccessType.Creator && o.AccessorId == -2 && o.ResType == PermItem.ResourceType.L10Recurrence))
                            continue;
                        o.AccessorId = s.Get<L10Recurrence>(o.ResId).CreatedById;*/
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		[Access(AccessLevel.Radial)]
		public string M10_27_2015() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var l10 = s.QueryOver<L10Recurrence>().List().ToList();
					foreach (var o in l10) {
						if (String.IsNullOrWhiteSpace(o.VideoId)) {
							o.VideoId = Guid.NewGuid().ToString();
							f++;
							s.Update(o);
						}
						/*
                        if (!(o.AccessorType == PermItem.AccessType.Creator && o.AccessorId == -2 && o.ResType == PermItem.ResourceType.L10Recurrence))
                            continue;
                        o.AccessorId = s.Get<L10Recurrence>(o.ResId).CreatedById;*/
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		[Access(AccessLevel.Radial)]
		public string M11_05_2015() {
			//var f = 0;
			/*  using (var s = HibernateSession.GetCurrentSession())
              {
                  using (var tx = s.BeginTransaction())
                  {
                      //Fix TempUser userIds

                      var u = s.QueryOver<UserOrganizationModel>().Where(x=>x.IsClient!=true).List().ToList();
                      foreach (var o in u)
                      {
                          o.IsClient=false;
                          f++;
                          s.Update(o);
                          /*
                          if (!(o.AccessorType == PermItem.AccessType.Creator && o.AccessorId == -2 && o.ResType == PermItem.ResourceType.L10Recurrence))
                              continue;
                          o.AccessorId = s.Get<L10Recurrence>(o.ResId).CreatedById;*
                      }
                      tx.Commit();
                      s.Flush();
                  }
              }*/
			return "Run the following:  update `userorganizationmodel` set IsClient=false where IsClient is Null;";
			//return "IsClient fixed for " + f;
		}

		[Access(AccessLevel.Radial)]
		public string M12_07_2015_2() {
			var f = 0;
			var g = 0;
			var h = 0;
			var f2 = 0;
			var g2 = 0;
			var h2 = 0;

			//var i = 0;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var u = s.QueryOver<TodoModel>().List().ToList();
					var v = s.QueryOver<L10Note>().List().ToList();
					var w = s.QueryOver<IssueModel>().List().ToList();

					var allTasks = new List<string>();

					foreach (var o in v) {
						//o.PadId = Guid.NewGuid().ToString();
						if (!string.IsNullOrEmpty(o.Contents)) {
							allTasks.Add(o.PadId + "," + o.Contents);
							g2++;
						}
						//s.Update(o);
						g++;

					}


					foreach (var o in u) {
						//o.PadId = Guid.NewGuid().ToString();
						if (!string.IsNullOrEmpty(o.Details)) {
							allTasks.Add(o.PadId + "," + o.Details);
							f2++;
						}
						//s.Update(o);
						f++;


					}


					foreach (var o in w) {
						//o.PadId = Guid.NewGuid().ToString();
						if (!string.IsNullOrEmpty(o.Description)) {
							allTasks.Add(o.PadId + "," + o.Description);
							h2++;
						}
						//s.Update(o);
						h++;


					}



					//await Task.WhenAll(allTasks);


					//tx.Commit();
					//s.Flush();
					return "" + f + ", " + g + ", " + h + "   --   " + f2 + ", " + g2 + ", " + h2 + "\n" + String.Join("\n", allTasks);
				}

			}
		}

		[Access(AccessLevel.Radial)]
		public string M12_07_2015() {
			var f = 0;
			var g = 0;
			var h = 0;
			var f2 = 0;
			var g2 = 0;
			var h2 = 0;

			var i = 0;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var u = s.QueryOver<TodoModel>().Where(x => (x.PadId == null || x.PadId == "")).List().ToList();
					var v = s.QueryOver<L10Note>().Where(x => (x.PadId == null || x.PadId == "")).List().ToList();
					var w = s.QueryOver<IssueModel>().Where(x => (x.PadId == null || x.PadId == "")).List().ToList();

					//var allTasks = new List<Task<bool>>();

					foreach (var o in v) {
						o.PadId = Guid.NewGuid().ToString();
						if (!string.IsNullOrEmpty(o.Contents)) {
							//allTasks.Add(PadAccessor.CreatePad(o.PadId, o.Contents));
							g2++;
						}
						s.Update(o);
						g++;

					}


					foreach (var o in u) {
						o.PadId = Guid.NewGuid().ToString();
						if (!string.IsNullOrEmpty(o.Details)) {
							//allTasks.Add(PadAccessor.CreatePad(o.PadId, o.Details));
							f2++;
						}
						s.Update(o);
						f++;


					}


					foreach (var o in w) {
						o.PadId = Guid.NewGuid().ToString();
						if (!string.IsNullOrEmpty(o.Description)) {
							//allTasks.Add(PadAccessor.CreatePad(o.PadId, o.Description));
							h2++;
						}
						s.Update(o);
						h++;


					}



					//await Task.WhenAll(allTasks);


					tx.Commit();
					s.Flush();
				}

			}
			return "" + f + ", " + g + ", " + h + "   --   " + f2 + ", " + g2 + ", " + h2;
		}

		[Access(AccessLevel.Radial)]
		public string M1_18_2016() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var l10 = s.QueryOver<L10Recurrence>().List().ToList();
					foreach (var o in l10) {
						if (String.IsNullOrWhiteSpace(o.HeadlinesId)) {
							o.HeadlinesId = Guid.NewGuid().ToString();
							f++;
							s.Update(o);
						}
						/*
                        if (!(o.AccessorType == PermItem.AccessType.Creator && o.AccessorId == -2 && o.ResType == PermItem.ResourceType.L10Recurrence))
                            continue;
                        o.AccessorId = s.Get<L10Recurrence>(o.ResId).CreatedById;*/
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		[Access(AccessLevel.Radial)]
		public string M2_17_2016() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var l10 = s.QueryOver<L10Recurrence>().List().ToList();
					foreach (var o in l10) {
						if (o.VtoId == 0) {
							var model = VtoAccessor.CreateRecurrenceVTO(s, PermissionsUtility.Create(s, GetUser()), o.Id);

							f++;
							//s.Update(o);
						}
						/*
                        if (!(o.AccessorType == PermItem.AccessType.Creator && o.AccessorId == -2 && o.ResType == PermItem.ResourceType.L10Recurrence))
                            continue;
                        o.AccessorId = s.Get<L10Recurrence>(o.ResId).CreatedById;*/
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "" + f;
		}

		//[Access(AccessLevel.Radial)]
		//public string M2_21_2016() {
		//	var f = 0;
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			//Fix TempUser userIds

		//			var rocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null).List().ToList();
		//			var vtoRocks = s.QueryOver<Vto_Rocks>().Where(x => x.DeleteTime == null).List().ToList();
		//			var perm = PermissionsUtility.Create(s, GetUser());
		//			var now = DateTime.UtcNow;
		//			foreach (var rock in rocks) {
		//				if (!vtoRocks.Any(vto => vto.Rock.Id == rock.ForRock.Id)) {
		//					var recur = s.Get<L10Recurrence>(rock.L10Recurrence.Id);
		//					if (recur.VtoId != 0) {
		//						rock.ForRock._AddedToL10 = false;
		//						rock.ForRock._AddedToVTO = false;
		//						var vto = s.Get<VtoModel>(recur.VtoId);
		//						var vtoRock = new Vto_Rocks {
		//							CreateTime = now,
		//							Rock = rock.ForRock,
		//							Vto = vto,

		//						};
		//						s.Save(vtoRock);
		//						f++;
		//					}
		//				}
		//			}

		//			tx.Commit();
		//			s.Flush();

		//			return "" + f;
		//		}
		//	}
		//}

		[Access(AccessLevel.Radial)]
		public string M3_08_2016() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var rocks = s.QueryOver<RockModel>().Where(x => x.PadId == null).List().ToList();

					foreach (var rock in rocks) {
						rock.PadId = Guid.NewGuid().ToString();
						s.Update(rock);
						f++;
					}

					tx.Commit();
					s.Flush();

					return "" + f;
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public string M4_01_2016() {

			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var recur = s.QueryOver<L10Recurrence>().List().ToList();

					foreach (var r in recur) {
						r.ShowHeadlinesBox = true;// = Guid.NewGuid().ToString();
						s.Update(r);
						f++;
					}

					tx.Commit();
					s.Flush();

					return "" + f;
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public string M4_15_2016() {

			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					throw new Exception("Old");
#pragma warning disable CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'
					//					var recur = s.QueryOver<L10Recurrence>().Where(x => x.TeamType == L10TeamType.Invalid || x.TeamType == null).List().ToList();
					//#pragma warning restore CS0472 // The result of the expression is always the same since a value of this type is never equal to 'null'

					//					foreach (var r in recur) {
					//						r.TeamType = r.IsLeadershipTeam ? L10TeamType.LeadershipTeam : L10TeamType.Other;
					//						s.Update(r);
					//						f++;
					//					}

					//					tx.Commit();
					//					s.Flush();

					//					return "" + f;
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public string M4_17_2016() {
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//Fix TempUser userIds

					var eosWW = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == 1795).List().ToList();

					foreach (var e in eosWW) {
						if (e.User.SendTodoTime != -1) {

							e.User.SendTodoTime = -1;
							s.Update(e.User);
							f += 1;
						}
					}

					tx.Commit();
					s.Flush();

					return "EOSWW SetTime:" + f;
				}
			}

		}

		[Access(Controllers.AccessLevel.Radial)]
		public string M05_23_2016(long id) {
			var caller = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var recur = s.Get<L10Recurrence>(id);

					//s.Save(recur);
					var perms = PermissionsUtility.Create(s, caller);
					var build = "";
					if (recur.VtoId == 0) {
						VtoAccessor.CreateRecurrenceVTO(s, perms, recur.Id);
						build += " VTO";
					}
					var egs = s.QueryOver<PermItem>().Where(x =>
						x.ResType == PermItem.ResourceType.L10Recurrence &&
						x.ResId == recur.Id)
					.List().ToList();

					if (!egs.Any(x => x.AccessorType == PermItem.AccessType.Creator)) {
						s.Save(new PermItem() {
							CanAdmin = true,
							CanEdit = true,
							CanView = true,
							AccessorType = PermItem.AccessType.Creator,
							AccessorId = caller.Id,
							ResType = PermItem.ResourceType.L10Recurrence,
							ResId = recur.Id,
							CreatorId = caller.Id,
							OrganizationId = caller.Organization.Id,
							IsArchtype = false,
						});

						build += " Creator";
					}
					if (!egs.Any(x => x.AccessorType == PermItem.AccessType.Members)) {
						s.Save(new PermItem() {
							CanAdmin = true,
							CanEdit = true,
							CanView = true,
							AccessorType = PermItem.AccessType.Members,
							AccessorId = -1,
							ResType = PermItem.ResourceType.L10Recurrence,
							ResId = recur.Id,
							CreatorId = caller.Id,
							OrganizationId = caller.Organization.Id,
							IsArchtype = false,
						});
						build += " Creator";
					}

					tx.Commit();
					s.Flush();

					return "Added." + build;
				}
			}
		}

		[Access(Controllers.AccessLevel.Radial)]
		public string M06_04_2016() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var users = s.QueryOver<UserModel>().List();
					var build = 0;

					foreach (var u in users) {
						var style = s.Get<UserStyleSettings>(u.Id);
						if (style == null) {
							style = new UserStyleSettings() {
								Id = u.Id,
								ShowScorecardColors = true,
							};
							s.Save(style);
							build += 1;
						}
					}
					var b2 = 0;
					var pictures = s.QueryOver<ThreeYearPictureModel>().List().ToList();
					foreach (var p in pictures) {
						var any = false;
						if (p.Profit != null && p.ProfitStr == null) {
							p.ProfitStr = string.Format("{0:c0}", p.Profit);
							any = true;
						}
						if (p.Revenue != null && p.RevenueStr == null) {
							p.RevenueStr = string.Format("{0:c0}", p.Revenue);
							any = true;
						}
						if (any) {
							b2 += 1;
							s.Update(p);
						}
					}
					var one = s.QueryOver<OneYearPlanModel>().List().ToList();
					foreach (var p in one) {
						var any = false;
						if (p.Profit != null && p.ProfitStr == null) {
							p.ProfitStr = string.Format("{0:c0}", p.Profit);
							any = true;
						}
						if (p.Revenue != null && p.RevenueStr == null) {
							p.RevenueStr = string.Format("{0:c0}", p.Revenue);
							any = true;
						}
						if (any) {
							b2 += 1;
							s.Update(p);
						}
					}
					var rocks = s.QueryOver<QuarterlyRocksModel>().List().ToList();
					foreach (var p in rocks) {
						var any = false;
						if (p.Profit != null && p.ProfitStr == null) {
							p.ProfitStr = string.Format("{0:c0}", p.Profit);
							any = true;
						}
						if (p.Revenue != null && p.RevenueStr == null) {
							p.RevenueStr = string.Format("{0:c0}", p.Revenue);
							any = true;
						}
						if (any) {
							b2 += 1;
							s.Update(p);
						}
					}

					tx.Commit();
					s.Flush();

					return "Added." + build + " adjusted VTO sections:" + b2;
				}
			}
		}

		[Access(Controllers.AccessLevel.Radial)]
		public string M06_27_2016() {
			var updated = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var users = s.QueryOver<PermItem>().List();
					var now = DateTime.UtcNow;
					foreach (var u in users.Where(x => x.AccessorType == RadialReview.Models.PermItem.AccessType.Creator)) {
						if (users.Any(x => x.ResType == u.ResType && x.ResId == u.ResId && x.AccessorType == RadialReview.Models.PermItem.AccessType.Admins))
							continue;
						var item = new PermItem() {
							AccessorId = -1,
							AccessorType = PermItem.AccessType.Admins,
							CanAdmin = true,
							CanEdit = true,
							CanView = true,
							CreateTime = now,
							CreatorId = u.CreatorId,
							IsArchtype = false,
							OrganizationId = u.OrganizationId,
							ResId = u.ResId,
							ResType = u.ResType
						};
						s.Save(item);
						updated += 1;
					}
					var b2 = 0;


					tx.Commit();
					s.Flush();
				}
			}
			return "" + updated;
		}

		[Access(Controllers.AccessLevel.Radial)]
		[Obsolete("Do not use", true)]
		public string M07_12_2016() {
			HttpContext.Server.ScriptTimeout = 60 * 20;
			var updatedA = 0;
			var updatedB = 0;
			var updatedC = 0;
			var caller = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var os = s.QueryOver<OrganizationModel>().Where(x => x.DeleteTime == null).List().ToList();
					var allNodes = s.QueryOver<AccountabilityNode>().List().ToList();
					var perms = PermissionsUtility.Create(s, caller);

					var usersF = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).Future();
					var rolesF = s.QueryOver<RoleModel>().Where(x => x.DeleteTime == null).Future();
					var managerLinksF = s.QueryOver<ManagerDuration>().Where(x => x.DeletedBy == null).Future();


					var users = usersF.ToList();
					var roles = rolesF.ToList();
					var managerLinks = managerLinksF.ToList();

					foreach (var o in os) {
						if (!(o.AccountabilityChartId > 0)) {
							o.AccountabilityChartId = AccountabilityAccessor.CreateChart(s, perms, o.Id, false).Id;
							s.Update(o);
							updatedA += 1;
						}

						var c = s.Get<AccountabilityChart>(o.AccountabilityChartId);
						var nodes = allNodes.Where(x => x.ParentNodeId == c.RootId);
						if (!nodes.Any()) {
							makeTree(s, caller, perms, c.RootId, o.AccountabilityChartId, o.Id, users, roles, managerLinks);
							updatedB += 1;
						}
					}

					allNodes = s.QueryOver<AccountabilityNode>().List().ToList();
					var rs = s.QueryOver<RoleModel>().Where(x => x.DeleteTime == null).List().ToList();
					var roleMaps = s.QueryOver<AccountabilityNodeRoleMap>().Where(x => x.DeleteTime == null).List().ToList();

					foreach (var r in rs.Where(x => x.FromTemplateItemId == null).GroupBy(x => x.ForUserId)) {
						var uNodes = allNodes.Where(x => x.UserId == r.Key);
						foreach (var u in uNodes) {
							var myMaps = roleMaps.Where(x => x.AccountabilityGroupId == u.AccountabilityRolesGroupId).ToList();
							var myRoles = SetUtility.AddRemove(myMaps.Select(x => x.RoleId), r.Select(x => x.Id));

							foreach (var addedRole in myRoles.AddedValues) {
								s.Save(new AccountabilityNodeRoleMap() {
									RoleId = addedRole,
									OrganizationId = u.OrganizationId,
									PositionId = null,
									AccountabilityChartId = u.AccountabilityChartId,
									AccountabilityGroupId = u.AccountabilityRolesGroupId,
								});
								updatedC++;
							}

						}
					}


					tx.Commit();
					s.Flush();
				}
			}


			return "(" + updatedA + ")  " + updatedB + " (" + updatedC + ")";
		}

		protected static void makeTreeDive(ISession s, long chartId, long orgId, long caller, long myId, long parentId, List<UserOrganizationModel> users, List<RoleModel> roles, List<DeepAccountability> links, List<ManagerDuration> mds) {
			var own = links.Any(x => x.Parent.UserId == caller && x.Child.UserId == myId);
			// var children = links.Where(x=>x.ManagerId==parent);
			var me = users.FirstOrDefault(x => x.Id == myId);
			var children = mds.Where(x => x.ManagerId == myId).ToList();


			var group = new AccountabilityRolesGroup() {
				AccountabilityChartId = chartId,
				OrganizationId = orgId,
				PositionId = me.Positions.NotNull(x => x.FirstOrDefault().NotNull(y => y.Position.Id)),
			};
			if (group.PositionId == 0)
				group.PositionId = null;

			s.Save(group);

			var node = new AccountabilityNode() {
				AccountabilityChartId = chartId,
				OrganizationId = orgId,
				ParentNodeId = parentId,
				UserId = me.Id,
				AccountabilityRolesGroupId = group.Id
			};
			s.Save(node);



			//node.AccountabilityRolesGroupId = group.Id;
			//s.Update(node);

			/* var templates = new List<UserTemplate>();

			 if (group.PositionId != null) {
				 templates = s.QueryOver<UserTemplate>().Where(x => x.AttachType == AttachType.Position && x.AttachId == group.PositionId).List().ToList();

			 }

			 foreach (var r in roles.Where(x => x.ForUserId == myId)) {
				 var position = templates.FirstOrDefault(x=>x.Id==r.FromTemplateItemId);
				 long? positionId = null;
				 if (position != null) {
					 positionId = position.AttachId;
				 }

				 s.Save(new AccountabilityNodeRoleMap() {
					 AccountabilityGroupId = group.Id,
					 RoleId = r.Id,
					 OrganizationId = orgId,
					 AccountabilityChartId = chartId,
					 PositionId = positionId
				 });
			 }*/

			children.ForEach(x => makeTreeDive(s, chartId, orgId, caller, x.SubordinateId, node.Id, users, roles, links, mds));
		}

		protected void makeTree(ISession s, UserOrganizationModel caller, PermissionsUtility perms, long parentNode, long chartId, long orgId,
			List<UserOrganizationModel> allUsers, List<RoleModel> allRoles, List<ManagerDuration> allManagerDurations) {
			var map = DeepAccessor.GetOrganizationMap(s, perms, orgId);// DeepSubordianteAccessor.GetOrganizationMap(s, perms, orgId);

			var org = s.Get<OrganizationModel>(orgId);

			var userIds = map.SelectMany(x => new List<long?> { x.Parent.UserId, x.Child.UserId }).Distinct().ToArray();

			var users = allUsers.Where(x => x.Organization.Id == orgId && userIds.Any(y => y == x.Id)).ToList();
			var roles = allRoles.Where(x => x.OrganizationId == orgId).ToList();
			var managerLinks = allManagerDurations.Where(x => userIds.Any(y => y == x.ManagerId)).ToList();



			List<long> tln = users.Where(x => x.ManagingOrganization).Select(x => x.Id).ToList();

			var trees = new List<AccountabilityTree>();
			foreach (var topLevelNode in tln) {
				makeTreeDive(s, chartId, orgId, caller.Id, topLevelNode, parentNode, users, roles, map, managerLinks);
			}

		}

		[Access(Controllers.AccessLevel.Radial)]
		[AsyncTimeout(20 * 60 * 1000)]
		public string M07_21_2016() {
			HttpContext.Server.ScriptTimeout = 60 * 20;
			var updatedA = 0;
			var caller = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var scores = s.QueryOver<ScoreModel>().Where(x => x.OriginalGoal == null).List().ToList();

					foreach (var score in scores) {
						score.OriginalGoal = score.Measurable.Goal;
						score.OriginalGoalDirection = score.Measurable.GoalDirection;
						s.Update(score);
						updatedA += 1;

						if (updatedA % 20 == 0) { //20, same as the ADO batch size
												  //flush a batch of inserts and release memory:
							s.Flush();
							s.Clear();
						}
					}


					tx.Commit();
					s.Flush();
				}
			}


			return "(" + updatedA + ") ";
		}

		[Access(Controllers.AccessLevel.Radial)]
		[AsyncTimeout(20 * 60 * 1000)]
		public string M07_28_2016(long id = 0, int take = 100) {
			var lastId = 0L;
			HttpContext.Server.ScriptTimeout = 60 * 20;
			var updatedA = 0;
			var caller = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var td = s.QueryOver<TodoModel>().Where(x => x.CloseTime == null && x.Id > id).Take(take).List().ToList();
					var meetings = s.QueryOver<L10Meeting>().Select(x => x.Id, x => x.L10RecurrenceId, x => x.CompleteTime).List<object[]>()
						.Select(x => new {
							meeting = (long)x[0],
							recur = (long)x[1],
							time = (DateTime?)x[2]
						})
						.ToList();
					var mLu = meetings.ToDictionary(
							x => x.meeting,
							x => x
						);
					var rLu = meetings.GroupBy(x => x.recur).ToDictionary(
						 x => x.Key,
						 x => x.OrderBy(y => y.time).Select(y => y.time).ToList()
					 );
					foreach (var t in td) {

						if (t.CompleteDuringMeetingId.HasValue) {
							t.CloseTime = mLu[t.CompleteDuringMeetingId.Value].time;
						} else if (t.ForRecurrenceId.HasValue && rLu.ContainsKey(t.ForRecurrenceId.Value)) {
							t.CloseTime = rLu[t.ForRecurrenceId.Value].LastOrDefault(x => x != null && t.CompleteTime <= x);
						}
						s.Update(t);
						updatedA += 1;
						lastId = t.Id;

						//if (updatedA % 20 == 0) { //20, same as the ADO batch size
						//    //flush a batch of inserts and release memory:
						//    s.Flush();
						//    s.Clear();
						//}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return "(" + updatedA + ") last:" + lastId;
		}

		[Access(Controllers.AccessLevel.Radial)]
		[AsyncTimeout(20 * 60 * 1000)]
		public ActionResult M08_14_2016(long? orgId = null, int countUsers = 0, int skipUsers = 0, int countNodesDeleted = 0, int roleLinksDeleted = 0, int countOrgs = 0, DateTime? now = null, int exceptionCount = 0, int deletedCharts = 0) {
			//var countOrgs = 0;
			//var countNodesDeleted= 0;

			Server.ScriptTimeout = 30 * 60;
			Session.Timeout = 30;

			if (orgId == null) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var orgs = s.QueryOver<OrganizationModel>()
							.Where(x => x.DeleteTime == null)
							.List().ToList();
						var perms = PermissionsUtility.Create(s, GetUser());


						var nodes = s.QueryOver<AccountabilityNode>()
							.Where(x => x.DeleteTime == null)
							.List().ToList();
						now = now ?? DateTime.UtcNow;
						foreach (var n in nodes) {
							n.DeleteTime = now;
							s.Update(n);
							countNodesDeleted += 1;
						}

						var roleLinks = s.QueryOver<RoleLink>().Where(x => x.DeleteTime == null).List().ToList();

						foreach (var n in roleLinks) {
							n.DeleteTime = now;
							s.Update(n);
							roleLinksDeleted += 1;
						}


						foreach (var o in orgs) {
							if (o.AccountabilityChartId > 0) {
								var f = s.Get<AccountabilityChart>(o.AccountabilityChartId);
								f.DeleteTime = now;
								s.Update(f);
								deletedCharts += 1;
							}
							var chart = AccountabilityAccessor.CreateChart(s, perms, o.Id, false);
							o.AccountabilityChartId = chart.Id;
							s.Update(o);
							//countOrgs += 1;
						}



						var org = s.QueryOver<OrganizationModel>().Take(1).SingleOrDefault();

						tx.Commit();
						s.Flush();

						return RedirectToAction("M08_14_2016", "Migration", routeValues: new { orgId = org.Id, now, countNodesDeleted, deletedCharts, roleLinksDeleted });
					}
				}
			}

			//var countUsers = 0;
			//var skipUsers = 0;
			OrganizationModel nextOrg = null;
			var skipped = false;
			var laterNodePosition = new List<Tuple<long, long?>>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(false)) {
						var perms = PermissionsUtility.Create(s, GetUser());

						UserOrganizationModel managerA = null;
						UserOrganizationModel subA = null;

						var users = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId.Value && x.DeleteTime == null).Select(x => x.Id).List<long>().ToList();
						var links = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null)
							.JoinAlias(x => x.Manager, () => managerA).Where(x => managerA.Organization.Id == orgId.Value && managerA.DeleteTime == null)
							.JoinAlias(x => x.Subordinate, () => subA).Where(x => subA.Organization.Id == orgId.Value && subA.DeleteTime == null)
							.Select(x => x.ManagerId, x => x.SubordinateId).List<object[]>()
							.Select(x => Tuple.Create((long)x[0], (long)x[1]))
							.ToList();

						var usersHit = new List<long>();

						var allUsers = new HashSet<long>(users);
						var m2s = new HashSet<Tuple<long, long>>(links);

						var result = GraphUtility.TopologicalSort(allUsers, m2s);

						if (result != null) {
							var a = result.Count;

							var parentLu = new Dictionary<long, AccountabilityNode>();


							foreach (var u in result) {
								try {
									var user = s.Get<UserOrganizationModel>(u);
									if (user.Organization.DeleteTime != null)
										continue;

									var posId = user.Positions.FirstOrDefault().NotNull(x => (long?)x.Position.Id);


									if (user.ManagingOrganization) {
										var r = AccountabilityAccessor.GetRoot(s, perms, user.Organization.AccountabilityChartId);
										var node = AccountabilityAccessor.AppendNode(s, perms, rt, r.Id, null, user.Id, true);
										parentLu[user.Id] = node;
										laterNodePosition.Add(Tuple.Create(node.Id, posId));
										//if (posId != null) {
										//	AccountabilityAccessor.UpdateAccountabilityRolesGroup_Unsafe(s, rt, perms, node.Id, posId, now.Value);
										//}
									}

									var managedBy = links.Where(x => x.Item2 == user.Id).Select(x => new {
										ManagerId = x.Item1
									});


									foreach (var manager in managedBy) {
										if (parentLu.ContainsKey(manager.ManagerId)) {
											var node = AccountabilityAccessor.AppendNode(s, perms, rt, parentLu[manager.ManagerId].Id, null, user.Id, true);

											laterNodePosition.Add(Tuple.Create(node.Id, posId));
											//if (posId != null) {
											//	AccountabilityAccessor.UpdateAccountabilityRolesGroup_Unsafe(s, rt, perms, node.Id, posId, now.Value);
											//}

											if (!parentLu.ContainsKey(user.Id)) {
												parentLu[user.Id] = node;
											}
										} else {
											skipUsers += 1;
										}
									}

									countUsers += 1;
								} catch (Exception e) {
									var m = e.Message;
									int b = 1;
									exceptionCount += 1;
								}
							}

							//throw new Exception("Users not hit");

						} else {
							exceptionCount += 1;
							skipped = true;
						}
						nextOrg = s.QueryOver<OrganizationModel>().Where(x => x.Id > orgId.Value).OrderBy(x => x.Id).Asc.Take(1).SingleOrDefault();
						countOrgs += 1;

						tx.Commit();
						s.Flush();
					}
				}
			}
			if (!skipped) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						using (var rt = RealTimeUtility.Create(false)) {
							var perms = PermissionsUtility.Create(s, GetUser());
							foreach (var np in laterNodePosition) {
								if (np.Item2 != null) {
									AccountabilityAccessor.UpdatePosition_Unsafe(s, rt, perms, np.Item1, np.Item2, now.Value, true);
								}
							}
							tx.Commit();
							s.Flush();
						}
					}
				}
			}


			if (nextOrg != null) {
				return Content("<script>location.href='/migration/M08_14_2016?orgId=" + nextOrg.Id + "&countUsers=" + countUsers + "&skipUsers=" + skipUsers + "&countNodesDeleted=" + countNodesDeleted + "&countOrgs=" + countOrgs + "&now=" + Url.Encode(now.ToString()) + "&exceptionCount=" + exceptionCount + "&deletedCharts=" + deletedCharts + "&roleLinksDeleted=" + roleLinksDeleted + "';</script>");
			}
			return Content("orgs:" + countOrgs + " - nodesDeleted:" + countNodesDeleted + " roleLinksDeleted:" + roleLinksDeleted + " nodesCreated: " + countUsers + "/" + (countUsers + skipUsers) + " errors: " + exceptionCount + " deletedCharts:" + deletedCharts);
		}
		[Access(Controllers.AccessLevel.Radial)]
		[AsyncTimeout(20 * 60 * 1000)]
		public ActionResult M08_21_2016() {
			var builder = "";
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

					var utCount = 0;
					var tdCount = 0;
					var pdCount = 0;
					var ut = s.QueryOver<UserTemplate.UT_Role>().List().ToList();
					foreach (var u in ut.Where(x => x.RoleId == 0)) {
						var r = new RoleModel() {
							Category = category,
							OrganizationId = u.Template.OrganizationId,
							Role = u.Role,
							CreateTime = now,
						};
						s.Save(r);
						u.RoleId = r.Id;
						s.Update(u);

						utCount += 1;
					}


					var td = s.QueryOver<TeamDurationModel>().List().ToList();
					foreach (var u in td.Where(x => x.OrganizationId == 0)) {
						u.OrganizationId = u.Team.Organization.Id;
						s.Save(u);
						tdCount += 1;
					}
					var pd = s.QueryOver<PositionDurationModel>().List().ToList();
					foreach (var u in pd.Where(x => x.OrganizationId == 0)) {
						u.OrganizationId = u.Position.Organization.Id;
						s.Save(u);
						pdCount += 1;
					}

					tx.Commit();
					s.Flush();
					builder += ("roles:" + utCount + " teamDuration:" + tdCount + " posDuration:" + pdCount);
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var templates = 0;
					var user = 0;
					var roles = s.QueryOver<RoleModel>().List().ToList();
					var roleLink = s.QueryOver<RoleLink>().Where(x => x.DeleteTime == null).List().ToList();
					var c = 0;
					var d = 0;

					foreach (var r in roles.Where(x => !roleLink.Any(y => y.RoleId == x.Id))) {
						if (r.FromTemplateItemId == null) {
							if (r.ForUserId == 1745) {
								//int a = 0;
								c += 1;
							}
							if (r.ForUserId != null) {
								var link = new RoleLink() {
									AttachId = r.ForUserId.Value,
									AttachType = AttachType.User,
									CreateTime = now,
									RoleId = r.Id,
									OrganizationId = r.OrganizationId
								};

								s.Save(link);
								templates += 1;
							}
						} else {
							var utr = s.Get<UserTemplate.UT_Role>(r.FromTemplateItemId.Value);

							if (utr.Template.AttachId == 1745) {
								d += 1;
							}
							var link = new RoleLink() {
								AttachId = utr.Template.AttachId,
								AttachType = utr.Template.AttachType,
								CreateTime = now,
								RoleId = r.Id,
								OrganizationId = r.OrganizationId
							};

							s.Save(link);
							user += 1;
						}
					}
					tx.Commit();
					s.Flush();
					builder += " roleLink_template:" + templates + " roleLink_user:" + user + " c:" + c + " d:" + d;
					return Content(builder);
				}
			}
		}
		#endregion

		[Access(Controllers.AccessLevel.Radial)]
		[AsyncTimeout(20 * 60 * 1000)]
		public ActionResult M09_17_2016() {
			var builder = "";
			var now = DateTime.UtcNow;
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var pd = s.QueryOver<L10Recurrence>().List().ToList();
					foreach (var u in pd) {
						u.HeadlineType = u.ShowHeadlinesBox ? PeopleHeadlineType.HeadlinesBox : PeopleHeadlineType.HeadlinesList;
						s.Update(u);
						count += 1;
					}

					tx.Commit();
					s.Flush();
				}
			}

			return Content("count:" + count);
		}
		public static String M11_06_2016() {
			var a = 0;
			var b = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var orgs = s.QueryOver<OrganizationModel>().List().ToList();
					foreach (var o in orgs) {

						o.Settings.LimitFiveState = true;
						s.Update(o);
						a += 1;
					}


					var values = s.QueryOver<CompanyValueModel>().List().ToList();
					foreach (var v in values) {

						v.MinimumPercentage = (3 * 100) / 5;
						s.Update(v);
						b += 1;
					}

					tx.Commit();
					s.Flush();
				}
			}


			return "Updated " + a + ", " + b;
		}


		[Access(Controllers.AccessLevel.Radial)]
		public String M12_13_2016(double id = 5) {
			var a = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var tiles = s.QueryOver<TileModel>().Where(x => x.DeleteTime == null).List().ToList();
					foreach (var o in tiles) {
						o.Height = (int)Math.Round(o.Height * id);
						o.Y = (int)Math.Round(o.Y * id);
						s.Update(o);
						a++;
					}

					tx.Commit();
					s.Flush();
				}
			}


			return "Updated " + a + " tiles";
		}


		[Access(Controllers.AccessLevel.Radial)]
		public String M01_04_2017() {
			var a = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var orgs = s.QueryOver<OrganizationModel>().List().ToList();
					var teams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Type == TeamType.Managers && x.DeleteTime == null).List().ToList();

					foreach (var org in orgs) {

						var team = teams.Where(x => x.Type == TeamType.Managers && org.Id == x.Organization.Id).FirstOrDefault();

						var items = new List<PermTiny>() {
							PermTiny.Admins()
						};

						if (team != null) {
							items.Add(PermTiny.RGM(team.Id, admin: false));
						}

						var admin = UserOrganizationModel.CreateAdmin();
						admin.Organization = org;



						PermissionsAccessor.CreatePermItems(s, admin, PermItem.ResourceType.UpgradeUsersForOrganization, org.Id,
							items.ToArray()
						);
						a += 1;
					}


					//
					//foreach (var o in tiles) {
					//	o.Height = (int)Math.Round(o.Height * id);
					//	o.Y = (int)Math.Round(o.Y * id);
					//	s.Update(o);
					//	a++;
					//}

					tx.Commit();
					s.Flush();
				}
			}


			return "Updated " + a + " org permissions";

		}

		[Access(Controllers.AccessLevel.Radial)]
		public String M04_14_2017() {
			var a = 0;
			var pageCount = 0;
			using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession()) {
				using (var tx = s.BeginTransaction()) {

					var recurs = s.QueryOver<L10Recurrence>().List().ToList();
					var pageFunc = new Func<L10Recurrence, Expression<Func<L10Recurrence, decimal>>, L10Recurrence.L10PageType, string, int, L10Recurrence.L10Recurrence_Page>(
						(recur, pageDur, type, subheading, order) => new L10Recurrence.L10Recurrence_Page() {
							AutoGen = true,
							L10Recurrence = recur,
							L10RecurrenceId = recur.Id,
							Minutes = recur.Get(pageDur),
							PageType = type,
							Title = type.GetDisplayName(),
							Subheading = subheading,
							_Ordering = order
						});

					foreach (var recur in recurs) {
						var order = 0;
						if (recur.SegueMinutes > 0) {
							var page = pageFunc(recur, x => x.SegueMinutes, L10Recurrence.L10PageType.Segue, "Share good news from the last 7 days.<br/> One personal and one professional.", order);
							s.Insert(page);
							order += 1;
							pageCount += 1;
						}
						if (recur.ScorecardMinutes > 0) {
							s.Insert(pageFunc(recur, x => x.ScorecardMinutes, L10Recurrence.L10PageType.Scorecard, null, order));
							order += 1;
							pageCount += 1;
						}
						if (recur.RockReviewMinutes > 0) {
							s.Insert(pageFunc(recur, x => x.RockReviewMinutes, L10Recurrence.L10PageType.Rocks, null, order));
							order += 1;
							pageCount += 1;
						}
						if (recur.HeadlinesMinutes > 0) {
							L10Recurrence.L10Recurrence_Page page;
							s.Insert(pageFunc(recur, x => x.HeadlinesMinutes, L10Recurrence.L10PageType.Headlines, "Share headlines about customers/clients and people in the company.<br/> Good and bad. Drop down (to the issues list) anything that needs discussion.", order));
							//switch (recur.HeadlineType) {
							//	case PeopleHeadlineType.None:
							//		page = pageFunc(recur, x => x.HeadlinesMinutes, L10Recurrence.L10PageType.Empty, "Share headlines about customers/clients and people in the company.<br/> Good and bad. Drop down (to the issues list) anything that needs discussion.", order);
							//		page.Title = "People Headlines";
							//		break;
							//	case PeopleHeadlineType.HeadlinesBox:
							//		page = pageFunc(recur, x => x.HeadlinesMinutes, L10Recurrence.L10PageType.NotesBox, null, order);
							//		page.Title = "People Headlines";
							//		break;
							//	case PeopleHeadlineType.HeadlinesList:
							//		page = pageFunc(recur, x => x.HeadlinesMinutes, L10Recurrence.L10PageType.Headlines, null, order);
							//		break;
							//	default:
							//		throw new Exception(recur.HeadlineType + "");
							//}
							//s.Insert(page);
							order += 1;
							pageCount += 1;
						}
						if (recur.TodoListMinutes > 0) {
							s.Insert(pageFunc(recur, x => x.TodoListMinutes, L10Recurrence.L10PageType.Todo, null, order));
							order += 1;
							pageCount += 1;
						}
						if (recur.IDSMinutes > 0) {
							s.Insert(pageFunc(recur, x => x.IDSMinutes, L10Recurrence.L10PageType.IDS, null, order));
							order += 1;
							pageCount += 1;
						}
						s.Insert(pageFunc(recur, x => x.ConclusionMinutes, L10Recurrence.L10PageType.Conclude, null, order));
						pageCount += 1;
						order += 1;
					}

					tx.Commit();
					//s.Flush();
				}
			}


			return "Added " + pageCount + " pages";

		}

		#endregion

		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> RevertScores() {
			var count = 0;
			using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession()) {
				using (var tx = s.BeginTransaction()) {

					var scores = s.QueryOver<ScoreModel>()
						.Where(x => x.DeleteTime == new DateTime(2017, 11, 9))
						.List().ToList();

					foreach (var score in scores) {
						score.DeleteTime = null;
						s.Update(score);
						count += 1;
					}

					tx.Commit();
					//s.Flush();
				}
			}
			return Content("count:" + count);
		}

		public ScoreModel DecideOnScores(List<ScoreModel> scores, TimeSpan threshold) {

			var ordered = scores.OrderByDescending(x => x.DateEntered ?? DateTime.MinValue);

			var prev = DateTime.MinValue;
			var best = ordered.FirstOrDefault();


			foreach (var o in ordered) {
				if (o.DateEntered == null)
					break;
				if (o.DateEntered - prev < threshold) {
					if (best.Measured < o.Measured) {
						best = o;
					}
				}

				prev = o.DateEntered.Value;
			}

			return best;

			//var grouped = new List<List<ScoreModel>>();

			//var currentGroup = new List<ScoreModel>();
			//var currentGroupMaxTime = DateTime.MinValue;
			//ScoreModel currentGroupBest = null;

			//var prevTime = DateTime.MinValue;

			//ScoreModel finalScore = null;
			//var finalTime = DateTime.MinValue;

			//foreach (var cur in ordered) {
			//	var myEntryTime = cur.DateEntered ?? DateTime.MinValue;

			//	currentGroupBest = currentGroupBest ?? cur;
			//	currentGroup.Add(cur);

			//	if (myEntryTime > currentGroupMaxTime) {
			//		currentGroupMaxTime = myEntryTime;
			//	}				

			//	if (Math.Abs(myEntryTime.Ticks - prevTime.Ticks) > threshold.Ticks) {
			//		if (currentGroupMaxTime

			//		//reset;
			//		currentGroup = new List<ScoreModel>();
			//		currentGroupMaxTime = DateTime.MinValue;
			//		currentGroupBest = null;
			//	}
			//	prevTime = myEntryTime;
			//}
			//return finalScore;
		}


		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> FixScores(Divisor d = null) {
			return await BreakUpAction("FixScores", d, dd => {

				using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession()) {
					using (var tx = s.BeginTransaction()) {

						var scores = s.QueryOver<ScoreModel>()
							.Where(Mod<ScoreModel>(x => x.MeasurableId, dd))
							.Where(x => x.DeleteTime == null)
							.List().ToList();

						foreach (var measurableWeekGroup in scores.GroupBy(x => Tuple.Create(x.ForWeek, x.MeasurableId))) {
							if (measurableWeekGroup.Count() > 1) {
								var ordered = measurableWeekGroup.OrderByDescending(x => x.DateEntered ?? DateTime.MinValue);
								var best = DecideOnScores(ordered.ToList(), TimeSpan.FromSeconds(6));


								foreach (var mw in ordered) {
									if (mw.Id == best.Id)
										continue;

									mw.DeleteTime = new DateTime(2017, 11, 10);
									s.Update(mw);
									dd.updates += 1;
								}
							}
						}
						tx.Commit();
					}
				}
			});

		}

		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> FixScoresOld(Divisor d = null) {
			return await BreakUpAction("FixScores", d, dd => {

				using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession()) {
					using (var tx = s.BeginTransaction()) {

						var scores = s.QueryOver<ScoreModel>()
							.Where(Mod<ScoreModel>(x => x.MeasurableId, dd))
							.Where(x => x.DeleteTime == null)
							.List().ToList();

						foreach (var measurableWeekGroup in scores.GroupBy(x => Tuple.Create(x.ForWeek, x.MeasurableId))) {
							if (measurableWeekGroup.Count() > 1) {
								var ordered = measurableWeekGroup.OrderByDescending(x => x.DateEntered ?? DateTime.MinValue);

								foreach (var mw in ordered.Skip(1)) {
									mw.DeleteTime = new DateTime(2017, 11, 7);
									s.Update(mw);
									dd.updates += 1;
								}
							}
						}
						tx.Commit();
					}
				}
			});

		}


		[Access(Controllers.AccessLevel.Radial)]
		public String M10_17_2017() {
			var a = 0;
			var b = 0;
			var pageCount = 0;
			using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession()) {
				using (var tx = s.BeginTransaction()) {
					var _rl = s.QueryOver<RockModel>().List().ToDictionary(x => x.Id, x => x);
					var rocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().List().ToList();
					foreach (var rr in rocks) {
						var rock = _rl[rr.ForRock.Id];

						if (rock.CompanyRock && !rr.VtoRock) {
							rr.VtoRock = true;
							s.Update(rr);
							a += 1;
						} else {
							b += 1;
						}
					}

					tx.Commit();
				}
			}
			return "Updated:" + a + ",  Not Updated:" + b;
		}

		[Access(Controllers.AccessLevel.Radial)]
		[AsyncTimeout(20 * 60 * 1000)]
		public async Task<ActionResult> M11_22_2017(System.Threading.CancellationToken token, Divisor d = null) {

			return await BreakUpAction("M11_22_2017", d, dd => {

				using (var s = HibernateSession.GetDatabaseSessionFactory().OpenSession()) {
					using (var tx = s.BeginTransaction()) {
						var _VtoModel = s.QueryOver<VtoModel>().List().ToList();

						var _VtoItemString = s.QueryOver<VtoItem_String>()
							.Where(Mod<VtoItem_String>(x => x.Id, dd))
							.Where(x => x.Type == VtoItemType.List_Uniques)
							.List().ToList();

						//foreach (var rr in _VtoModel) {
						//    if (!_VtoStrategyMap.Any(x => x.VtoId == rr.Id
						//     && x.MarketingStrategyId == rr.MarketingStrategy.Id
						//    )) {
						//        // save new
						//        VtoStrategyMap _map = new VtoStrategyMap() {
						//            CreateTime = createTime,
						//            VtoId = rr.Id,
						//            MarketingStrategyId = rr.MarketingStrategy.Id,
						//        };

						//        s.Insert(_map);
						//        a++;
						//    }
						//}

						foreach (var item in _VtoItemString) {
							if (item.MarketingStrategyId == null) {
								item.MarketingStrategyId = item.Vto.MarketingStrategy.Id;
								//b++;

								s.Update(item);
							}
						}
						tx.Commit();
						s.Flush();
					}
				}
			});


			//var a = 0;
			//var b = 0;
			//var pageCount = 0;
			//var createTime = new DateTime(2017, 11, 22);
			//using (var s = HibernateSession.GetDatabaseSessionFactory().OpenSession()) {
			//	using (var tx = s.BeginTransaction()) {
			//		var _VtoModel = s.QueryOver<VtoModel>().List().ToList();

			//		var _VtoItemString = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Uniques).List().ToList();

			//		//foreach (var rr in _VtoModel) {
			//		//    if (!_VtoStrategyMap.Any(x => x.VtoId == rr.Id
			//		//     && x.MarketingStrategyId == rr.MarketingStrategy.Id
			//		//    )) {
			//		//        // save new
			//		//        VtoStrategyMap _map = new VtoStrategyMap() {
			//		//            CreateTime = createTime,
			//		//            VtoId = rr.Id,
			//		//            MarketingStrategyId = rr.MarketingStrategy.Id,
			//		//        };

			//		//        s.Insert(_map);
			//		//        a++;
			//		//    }
			//		//}

			//		foreach (var item in _VtoItemString) {
			//			if (item.MarketingStrategyId == null) {
			//				item.MarketingStrategyId = item.Vto.MarketingStrategy.Id;
			//				b++;

			//				s.Update(item);
			//			}
			//		}

			//		tx.Commit();
			//		s.Flush();
			//	}
			//}
			//return "VtoStrategyMap Inserted:" + a + ", VtoITemString Inserted:" + b;
		}
		[Access(Controllers.AccessLevel.Radial)]
		public String M12_01_2017() {
			var a = 0;
			var b = 0;
			var pageCount = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var orgs = s.QueryOver<OrganizationModel>().List().ToList();
					var permItems = s.QueryOver<PermItem>().Where(x => x.ResType == PermItem.ResourceType.UpdatePaymentForOrganization).List().ToList();

					foreach (var org in orgs) {
						if (!permItems.Any(x => x.ResId == org.Id && x.CanAdmin)) {

							var tempUser = new UserOrganizationModel() {
								Id = -11,
								Organization = org,
							};

							PermissionsAccessor.CreatePermItems(s, tempUser, PermItem.ResourceType.UpdatePaymentForOrganization, org.Id,
								PermTiny.Admins(true, true, true)
							);
							a++;
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			return "Updated:" + a;
		}

		[Access(Controllers.AccessLevel.Radial)]
		public String M01_09_2018() {
			var a = 0;
			var b = 0;
			var pageCount = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var invoices = s.QueryOver<InvoiceModel>().Where(x => x.PaidTime == null && x.DeleteTime == null).List().ToList();

					s.GetSettingOrDefault("M01_09_2018", true);

					var forgive = new[] {
						AccountType.Implementer,
						AccountType.Dormant,
						AccountType.SwanServices,
						AccountType.Other,
						AccountType.Coach,
						AccountType.Cancelled,
						AccountType.UserGroup,
					};
					//Keeps all trial, and paying invoices
					foreach (var i in invoices) {
						if (forgive.Contains(i.Organization.AccountType)) {
							i.DeleteTime = new DateTime(2017, 1, 9);
							a += 1;
						}
					}

					tx.Commit();
					s.Flush();
				}
			}
			return "Updated:" + a;
		}

		[Access(Controllers.AccessLevel.Radial)]
		public async Task<string> M01_16_2018() {
			var a = 0;
			var b = 0;
			var pageCount = 0;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var orgs = s.QueryOver<OrganizationModel>().Select(Xls => Xls.Id).List<long>().ToList();

					s.GetSettingOrDefault("M01_16_2018", true);

					var f = new SetDelinquentFlag();

					foreach (var i in orgs) {
						var has = await f.UpdateFlag(s, i);
						if (has)
							a += 1;
						b += 1;
					}

					tx.Commit();
					s.Flush();
				}
			}
			return "Updated: " + a + "/" + b;
		}


		[Access(Controllers.AccessLevel.Radial)]
		public async Task<string> M03_29_2018() {
			var a = 0;
			var b = 0;
			var pageCount = 0;
			using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession()) {
				using (var tx = s.BeginTransaction()) {
					var links = s.QueryOver<RoleLink>().List().ToList();
					s.GetSettingOrDefault("M03_29_2018", true);
					foreach (var i in links) {
						if (i.Ordering == null) {
							i.Ordering = i.Id;
							s.Update(i);
							a += 1;
						}
					}
					tx.Commit();
				}
			}
			return "Updated: " + a ;
		}

	}
}
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0219 // Variable is assigned but its value is never used
