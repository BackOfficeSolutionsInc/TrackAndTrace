using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.IdentityManagement.Model;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Models.Todo;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public class PhoneAccessor : BaseAccessor
	{

		public static void RegisterPhone(UserOrganizationModel caller, long number)
		{
			
		}

		public static List<CallablePhoneNumber> GetUnusedCallablePhoneNumbersForUser(UserOrganizationModel caller, long userId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var numbers = s.QueryOver<CallablePhoneNumber>().Where(x => x.DeleteTime == null).List().ToList();
					var used = s.QueryOver<PhoneActionMap>().Where(x => x.DeleteTime == null && x.Caller.Id == userId).List().ToList();

					return numbers.Where(x => used.All(y => y.SystemNumber != x.Number)).ToList();
				}
			}
		}

		public static string ReceiveText(long fromNumber, string body, long systemNumber)
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

					CallablePhoneNumber alias = null;

					found = s.QueryOver<PhoneActionMap>().Where(x => x.DeleteTime == null && x.SystemNumber == systemNumber && x.CallerNumber == fromNumber).List().FirstOrDefault();

					if (found == null)
						throw new PhoneException("This number is not set up yet.");

					text.FromUser = found.Caller;
					s.Update(text);

			

					tx.Commit();
					s.Flush();

					if (String.IsNullOrEmpty(body)){
						var whatResp = new List<string>(){"What was that?", "I didn't get that.", "Huh?", "Could you repeat that?","I'm not sure what you mean.","Hum..."};
						var r = rnd.Next(whatResp.Count);
						return whatResp[r];
					}

				}
			}
			switch (found.Action)
			{
				case "todo": TodoAccessor.CreateTodo(found.Caller, found.ForId, new TodoModel(){
					AccountableUser = found.Caller,
					AccountableUserId = found.Caller.Id,
					CreatedBy = found.Caller,
					CreatedById = found.Caller.Id,
					Message = body,
					CreateTime = now,
					Organization = found.Caller.Organization,
					OrganizationId = found.Caller.Organization.Id,
					DueDate = now.AddDays(7),
					ForModel = "TodoModel",
					ForModelId = -2,
				});
				return "Added todo.";
				case "issue": IssuesAccessor.CreateIssue(found.Caller, found.ForId, found.Caller.Id, new IssueModel(){
					CreatedById = found.Caller.Id,
					//MeetingRecurrenceId = model.RecurrenceId,
					CreatedDuringMeetingId = 0,
					Message = body,
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
					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}