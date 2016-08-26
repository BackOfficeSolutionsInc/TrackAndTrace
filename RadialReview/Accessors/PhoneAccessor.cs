using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Amazon.IdentityManagement.Model;
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

namespace RadialReview.Accessors
{
	public class PhoneAccessor : BaseAccessor
	{

		public static List<CallablePhoneNumber> GetUnusedCallablePhoneNumbersForUser(ISession s,PermissionsUtility perms, long userId)
		{
			perms.Self(userId);

			var numbers = s.QueryOver<CallablePhoneNumber>().Where(x => x.DeleteTime == null).List().ToList();
			var used = s.QueryOver<PhoneActionMap>().Where(x => x.DeleteTime == null && x.Caller.Id == userId).List().ToList();

			return numbers.Where(x => used.All(y => y.SystemNumber != x.Number)).ToList();
		}

		public static List<CallablePhoneNumber> GetUnusedCallablePhoneNumbersForUser(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, caller);
					return GetUnusedCallablePhoneNumbersForUser(s, perms, userId);
				}
			}
		
		}

		public static async Task<string> ReceiveText(long fromNumber, string body, long systemNumber)
		{
			var rnd = new Random();
			PhoneActionMap found;

			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var text = new PhoneTextModel()
					{
						Date = now,
						FromNumber = fromNumber,
						Message = body,
					};
					s.Save(text);

					//CallablePhoneNumber alias = null;


					found = s.QueryOver<PhoneActionMap>()
						.Where(x => x.DeleteTime == null && (x.SystemNumber == systemNumber || x.SystemNumber == systemNumber - 10000000000 || x.SystemNumber == systemNumber + 10000000000) && (x.CallerNumber == fromNumber || x.CallerNumber == fromNumber - 10000000000 || x.CallerNumber == fromNumber + 10000000000)).List().FirstOrDefault();

					if (found == null){
						//Try to register the phone
						var p = body.ToLower();
						var found2 = s.QueryOver<PhoneActionMap>().Where(x => x.DeleteTime != null && x.Placeholder == p).OrderBy(x => x.DeleteTime).Desc.List().FirstOrDefault();
						if (found2 != null){
							if (found2.DeleteTime < DateTime.UtcNow){
								throw new PhoneException("This code has expired. Please try again.");
							}

							found2.DeleteTime = null;
							found2.CallerNumber = fromNumber;
							s.Update(found2);
							tx.Commit();
							s.Flush();
							return "Your phone has been registered. Add this number to your contacts to add "+found2.Action+"s via text message.";
						}
						throw new PhoneException("This number is not set up yet.");
					}

					found.Caller = s.Get<UserOrganizationModel>(found.Caller.Id);

					text.FromUser = found.Caller;
					s.Update(text);

			

					tx.Commit();
					s.Flush();

					if (String.IsNullOrEmpty(body)){
                        var whatResp = new List<string>() { "What was that?", "I didn't get that.", "Huh? I didn't get that.", "Could you repeat that?", "I'm not sure what you mean.", "Hum...Could you repeat that?" };
						var r = rnd.Next(whatResp.Count);
						return whatResp[r];
					}

				}
			}
			switch (found.Action)
			{
				case "todo": await TodoAccessor.CreateTodo(found.Caller, found.ForId, new TodoModel(){
					AccountableUser = found.Caller,
					AccountableUserId = found.Caller.Id,
					CreatedBy = found.Caller,
					CreatedById = found.Caller.Id,
					Message = body,
					CreateTime = now,
					Organization = found.Caller.Organization,
					OrganizationId = found.Caller.Organization.Id,
					DueDate = now.AddDays(7),
					ForRecurrenceId = found.ForId,
					Details = "-sent from phone",
					ForModel = "TodoModel",
					ForModelId = -2,
				});
				return "Added todo.";
				case "issue": await IssuesAccessor.CreateIssue(found.Caller, found.ForId, found.Caller.Id, new IssueModel(){
					CreatedById = found.Caller.Id,
					CreatedDuringMeetingId = null,
					Message = body,
					Description = "-sent from phone",
					//ForRecurrenceId = found.ForId,
					ForModel = "IssueModel",
					ForModelId = -2,
					Organization = found.Caller.Organization,

				});
				return "Added issue.";
				default: throw new Exception();
					
					
			}
		}

		public static void DeleteAction(UserOrganizationModel caller, long phoneActionId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					var found = s.Get<PhoneActionMap>(phoneActionId);
					if (found == null || found.DeleteTime!=null)
						throw new PermissionsException("Does not exist.");
					if (found.Caller.Id!=caller.Id)
						throw new PermissionsException();

					found.DeleteTime = DateTime.UtcNow;
					s.Update(found);
					
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<PhoneActionMap> GetAllPhoneActionsForUser(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					PermissionsUtility.Create(s, caller).Self(userId);

					var map =  s.QueryOver<PhoneActionMap>().Where(x => x.Caller.Id == userId && x.DeleteTime == null).List().ToList();
					var recurrences = s.QueryOver<L10Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(map.Select(x => x.ForId).ToList()).List().ToDictionary(x=>x.Id,x=>x);

					foreach (var m in map){
						L10Recurrence recur=null;
						if (recurrences.TryGetValue(m.ForId, out recur))
							m._Recurrence = recur;
					}
					return map;
				}
			}
		}

		public class PhoneCode
		{
			public string Code { get; set; }
			public long PhoneNumber { get; set; }
		}

		public static PhoneCode AddAction(UserOrganizationModel caller, long userId, string action, long callableId, long recurrenceId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId).Self(userId);

					var unused = GetUnusedCallablePhoneNumbersForUser(s, perms, userId);
					var found = unused.FirstOrDefault(x => x.Id == callableId);
					if (found==null)
						throw new PermissionsException("Phone number is unavailable.");

					var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
					var random = new Random();
					var result = new string(
						Enumerable.Repeat(chars, 6)
								  .Select(x => x[random.Next(x.Length)])
								  .ToArray()).ToLower();

					

					var a = new PhoneActionMap(){
						Action = action,
						Caller = s.Load<UserOrganizationModel>(userId),
						CallerId = userId,
						CallerNumber = -1,
						Placeholder = result,
						SystemNumber = found.Number,
						CreateTime = DateTime.UtcNow,
						ForId = recurrenceId,
						DeleteTime =  DateTime.UtcNow.AddMinutes(15)
					};

					s.Save(a);
					//a.Placeholder += a.Id;
					//s.Update(a);


					tx.Commit();
					s.Flush();

					return new PhoneCode(){
						Code = a.Placeholder,
						PhoneNumber = found.Number
					};
				}
			}
		}
	}
}