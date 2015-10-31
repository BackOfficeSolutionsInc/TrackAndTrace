using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using RadialReview.Models.L10;
using RadialReview.Models.Reviews;
using RadialReview.Models.Tests;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Models.Responsibilities;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Controllers
{
	public class MigrationController : BaseController
	{
		#region old

		// GET: Migration
		[Access(AccessLevel.Radial)]
		public string M1_7_2015()
		{
			var teams = new Ratio();

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Make subordinate teams not secret
					foreach (var a in s.QueryOver<OrganizationTeamModel>().List())
					{
						if (a.Secret)
						{
							teams.Denominator++;
							if (a.Type == TeamType.Subordinates)
							{
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
		[Access(AccessLevel.Radial)]
		public int M11_8_2014()
		{
			throw new Exception("Old");
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					foreach (var a in s.QueryOver<Askable>().List())
					{
						if (a.OnlyAsk == AboutType.NoRelationship)
						{
							a.OnlyAsk = (AboutType)long.MaxValue;
							s.Update(a);
							count++;
						}
					}

					foreach (var r in s.QueryOver<RoleModel>().List())
					{
						if (r.OrganizationId == 0)
						{
							r.OrganizationId = s.Get<UserOrganizationModel>(r.ForUserId).Organization.Id;
							s.Update(r);
							count++;
						}
					}


					/*foreach (var r in s.QueryOver<UserOrganizationModel>().List())
					{
						if (r.NumRocks == 0)
						{
							r.NumRocks = s.QueryOver<RockModel>().Where(x => x.ForUserId == r.Id && x.DeleteTime == null).List().Count;
							s.Update(r);
							count++;
						}
						if (r.NumRoles == 0)
						{
							r.NumRoles = s.QueryOver<RoleModel>().Where(x => x.ForUserId == r.Id && x.DeleteTime == null).List().Count;
							s.Update(r);
							count++;
						}
					}*/

					tx.Commit();
					s.Flush();
				}
			}
			return count;
		}

		[Access(AccessLevel.Radial)]
		public int M11_19_2014()
		{
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
		public string M12_09_2014(int orgId)
		{
			var count = 0;
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					foreach (var a in s.QueryOver<ResponsibilityModel>().Where(x => x.ForOrganizationId == orgId).List())
					{
						if (a.GetQuestionType() == QuestionType.Slider)
						{
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
		public string M12_10_2014()
		{
			var count = 0;
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					foreach (var a in s.QueryOver<RockModel>().Where(x => x.OnlyAsk == (AboutType.Self | AboutType.Manager)).List())
					{
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
		public string M12_29_2014(long orgId, long periodId, long nextPeriodId)
		{
			var count = 0;
			var count2 = 0;
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					foreach (var a in s.QueryOver<ReviewsModel>().Where(x => x.ForOrganizationId == orgId).List())
					{
						var update = false;
						if (a.PeriodId == 0)
						{
							a.PeriodId = periodId;
							update = true;
						}
						if (a.NextPeriodId == 0)
						{

							a.NextPeriodId = nextPeriodId;
							update = true;
						}

						if (update)
						{
							s.Update(a);
							count++;
						}
						var rId = a.Id;

						foreach (var b in s.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == rId).List())
						{
							if (b.PeriodId == 0)
							{
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
		public string M2_23_2015_ImageResize()
		{
			var count = 0;
			var existingImg = AwsUtil.GetObjectsInFolder("Radial", "img");

			var existing32 = AwsUtil.GetObjectsInFolder("Radial", "32");
			var existing64 = AwsUtil.GetObjectsInFolder("Radial", "64");
			var existing128 = AwsUtil.GetObjectsInFolder("Radial", "128");


			var toAdd32 = SetUtility.AddRemove(existing32, existingImg, x => x.Key.Substring(x.Key.LastIndexOf("/")));
			var toAdd64 = SetUtility.AddRemove(existing64, existingImg, x => x.Key.Substring(x.Key.LastIndexOf("/")));
			var toAdd128 = SetUtility.AddRemove(existing128, existingImg, x => x.Key.Substring(x.Key.LastIndexOf("/")));

			foreach (var a in toAdd32.AddedValues){
				var found = AwsUtil.GetObject("Radial", a.Key);
				var name = a.Key.Substring(a.Key.LastIndexOf("/")+1);
				ImageAccessor.Upload(found, "32/" + name , ImageAccessor.TINY_INSTRUCTIONS);
			}
			foreach (var a in toAdd64.AddedValues)
			{
				var found = AwsUtil.GetObject("Radial", a.Key);
				var name = a.Key.Substring(a.Key.LastIndexOf("/") + 1);
				ImageAccessor.Upload(found, "64/" + name, ImageAccessor.MED_INSTRUCTIONS);
			} 
			foreach (var a in toAdd128.AddedValues)
			{
				var found = AwsUtil.GetObject("Radial", a.Key);
				var name = a.Key.Substring(a.Key.LastIndexOf("/") + 1);
				ImageAccessor.Upload(found, "128/" + name, ImageAccessor.LARGE_INSTRUCTIONS);
			}

			return "Count:"+count;
		}

		[Access(AccessLevel.Radial)]
		public string M2_23_2015_UpdateMeetingTimes(long orgId, double len = 90, double std1 = 3, double std2 = 8)
		{
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var found = s.QueryOver<L10Meeting>().Where(x => x.Organization.Id == orgId).List().ToList();
					var r = new Random();
					foreach (var x in found)
					{
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
		public string M2_23_2015_UpdateRatings(long orgId)
		{
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var meetingIds = s.QueryOver<L10Meeting>().Where(x => x.Organization.Id == orgId).List().ToList();
					var meeting = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.WhereRestrictionOn(x => x.L10Meeting.Id)
						.IsIn(meetingIds.Select(x => x.Id).ToList())
						.List()
						.OrderBy(x=>x.L10Meeting.StartTime)
						.ToList();
				
					var r = new Random();
					var min=meeting.Select(x=>x.L10Meeting.StartTime.Value).Min();
					var max = meeting.Select(x => x.L10Meeting.StartTime.Value).Max();

					var st = r.NextNormal(4, 1);
					
					foreach (var x in meeting){
						if (x.Rating != null){
							var i = (x.L10Meeting.StartTime.Value.Ticks - min.Ticks)/(1.0*max.Ticks - min.Ticks);
							var ratingMean = (10 - st)*i*i + st + r.NextDouble()*2 - 1;

							x.Rating = (int)Math.Max(1,Math.Min(10,Math.Round(r.NextNormal(ratingMean, 1))));
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
		public string M3_24_2015()
		{
			var count = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var org = s.QueryOver<OrganizationModel>().List().ToList();

					foreach (var x in org){
						if (x.Settings.EnableL10 == false && x.Settings.EnableReview == false){
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
		public string M4_4_2015()
		{
			var count = new StringBuilder();
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var reviews = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null).List().ToList();
					var answers = s.QueryOver<AnswerModel>().Where(x => x.DeleteTime == null).List().ToList();
					
					foreach (var r in reviews)
					{
						if (r.QuestionCompletion.NumRequired == 0)
						{
							var r1 = r;
							r.QuestionCompletion.NumRequired		 = answers.Count(x => x.DeleteTime == null && x.Required && x.ForReviewId == r1.Id);
							r.QuestionCompletion.NumRequiredComplete = answers.Count(x => x.DeleteTime == null && x.Required && x.ForReviewId == r1.Id && x.Complete);
							r.QuestionCompletion.NumOptional		 = answers.Count(x => x.DeleteTime == null && !x.Required && x.ForReviewId == r1.Id);
							r.QuestionCompletion.NumOptionalComplete = answers.Count(x => x.DeleteTime == null && !x.Required && x.ForReviewId == r1.Id && x.Complete);

							s.Update(r);
							count.AppendLine(r.Id + "," + r.QuestionCompletion.NumRequired + "," + r.QuestionCompletion.NumRequiredComplete + "," + r.QuestionCompletion.NumOptional + "," + r.QuestionCompletion.NumOptionalComplete+"<br/>");
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			return count.ToString();
		}

		[Access(AccessLevel.Radial)]
		public string M4_17_2015()
		{
			var c = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var orgs = s.QueryOver<OrganizationModel>().List();
					foreach (var organizationModel in orgs)
					{
						if (organizationModel.Organization == null)
						{
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
		public string M4_22_2015()
		{
			var c = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var orgs = s.QueryOver<OrganizationModel>().List();
					foreach (var organizationModel in orgs)
					{
						if (organizationModel.Settings.RockName == null)
						{
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
		#endregion

		[Access(AccessLevel.Radial)]
		public string M4_24_2015()
		{
			var c = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var cr = s.QueryOver<ClientReviewModel>().List();
					foreach (var x in cr)
					{
						if (x.ReviewContainerId == 0){

							var review = s.Get<ReviewModel>(x.ReviewId);
							if (review != null){
								x.ReviewContainerId = review.ForReviewsId;
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
		public string M4_27_2015()
		{
			var c = 0;
			var d = 0;
			var e = 0;
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds
					var cr = s.QueryOver<UserOrganizationModel>().List();
					foreach (var x in cr)
					{
						if (x.TempUser != null)
						{

							if (x.TempUser.UserOrganizationId==0)
							{
								x.TempUser.UserOrganizationId = x.Id;
								s.Update(x);
								c++;
							}
						}

						if (x.Cache == null){
							x.UpdateCache(s);
							d++;
						}

						if (x.User!=null && x.User.UserOrganizationCount == 0){
							x.User.UserOrganizationCount = x.User.UserOrganization.Count;
							if (x.User.UserOrganizationCount != 0){
								s.Update(x);
								e++;
							}
						}

						if (x.User != null && x.User.UserOrganizationIds==null)
						{
							x.User.UserOrganizationIds = x.User.UserOrganization.Select(y=>y.Id).ToArray();
							s.Update(x);
							f++;
						}

					}
					tx.Commit();
					s.Flush();
				}
			}
			return c + " "+d+" " +e+" "+f;
		}

		[Access(AccessLevel.Radial)]
		public string M5_5_2015()
		{
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds
					var cr = s.QueryOver<OrganizationModel>().List();
					foreach (var o in cr)
					{
						if (o.Settings.TimeZoneId == null)
						{
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
		public string M6_1_2015()
		{
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds
					var cr = s.QueryOver<L10Recurrence>().List();
					foreach (var o in cr)
					{
						if (  
							o.SegueMinutes	==0 && 
							o.ScorecardMinutes==0 && 
							o.RockReviewMinutes==0 && 
							o.HeadlinesMinutes==0 && 
							o.TodoListMinutes	==0 &&
							o.IDSMinutes == 0 && 
							o.ConclusionMinutes == 0
							) 
						{
							o.SegueMinutes	=5;
							o.ScorecardMinutes=5;
							o.RockReviewMinutes=5;
							o.HeadlinesMinutes=5;
							o.TodoListMinutes=5;
							o.IDSMinutes=60;
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
		public string M6_3_2015()
		{
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds
					var cr = s.QueryOver<RockModel>().List();
					foreach (var o in cr)
					{
						if (o.OnlyAsk== (AboutType)long.MaxValue){
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
		public string M6_22_2015(long reviewId)
		{
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds
					var review=s.Get<ReviewsModel>(reviewId);
					if (review == null)
						return "Review not exist";
					if (review.PeriodId == null)
						return "Period is null";


					var cr = s.QueryOver<RockModel>().Where(x=>x.DeleteTime==null && x.PeriodId==review.PeriodId).List().ToList();
					var ar = s.QueryOver<RockAnswer>().Where(x=>x.DeleteTime==null && x.ForReviewContainerId==reviewId).List().ToList();
					var rrs = s.QueryOver<ReviewModel>().Where(x => x.DeleteTime == null && x.ForReviewContainer.Id == reviewId).List().ToList();
					foreach (var o in cr)
					{
						if (!ar.Any(x => x.Askable.Id == o.Id)){
							var rr = rrs.FirstOrDefault(x => x.ForUserId == o.ForUserId);

							if (rr==null)
								continue;
							var rid = rr.Id;

							var rock = new RockAnswer()
							{
								Anonymous = review.AnonymousByDefault,
								Complete = false,
								Finished = Tristate.Indeterminate,
								ManagerOverride = RockState.Indeterminate,
								Completion = RockState.Indeterminate,
								Reason = null,
								Askable = o,
								Required = true,
								ForReviewId = rid,
								ByUserId = o.ForUserId,
								AboutUserId = o.ForUserId,
								AboutUser = s.Load<ResponsibilityGroupModel>(o.ForUserId),
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
		public string M8_7_2015()
		{
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds
					var users = s.QueryOver<UserModel>().Where(x=>x.SendTodoTime==null).List().ToList();
					foreach (var o in users){
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
		public string M10_19_2015()
		{
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds
					var pis = s.QueryOver<PermItem>().List().ToList();

					var l10= s.QueryOver<L10Recurrence>().List().ToList();
					foreach (var o in l10){
						if (pis.Any(x=>x.ResId==o.Id && x.ResType==PermItem.ResourceType.L10Recurrence))
							continue;

						var p1 = new PermItem(){
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
						var p2 = new PermItem(){
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
		public string M10_20_2015()
		{
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds

					var l10 = s.QueryOver<TileModel>().List().ToList();
					foreach (var o in l10)
					{
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
		public string M10_20_2015_2()
		{
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds

					var l10 = s.QueryOver<PermItem>().List().ToList();
					foreach (var o in l10)
					{
						if (o.AccessorType == PermItem.AccessType.Creator && o.AccessorId == 0){
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
		public string M10_27_2015()
		{
			var f = 0;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					//Fix TempUser userIds

					var l10 = s.QueryOver<L10Recurrence>().List().ToList();
					foreach (var o in l10)
					{
						if (String.IsNullOrWhiteSpace(o.VideoId))
						{
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
	}
}