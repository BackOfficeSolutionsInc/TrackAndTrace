using Hangfire;
using NHibernate;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Quarterly;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public class QuarterlyAccessor {

		public static async Task ScheduleQuarterlyEmail(UserOrganizationModel caller, long recurrenceId, string email,DateTime sendTime) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Recurrence(recurrenceId);
					email = email.ToLower();
					var qe= new QuarterlyEmail() {
						Email = email,
						RecurrenceId = recurrenceId,
						ScheduledTime = sendTime,
						OrgId = caller.Organization.Id,
						SenderId = caller.Id,						
					};

					var org = s.Get<OrganizationModel>(caller.Organization.Id);
					org.ImplementerEmail = email;
					s.Update(org);

					s.Save(qe);

					Scheduler.Schedule(()=> ScheduledEmail_HangFire(qe.Id), Math2.Max(TimeSpan.FromMinutes(0),sendTime-DateTime.UtcNow));
					
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<QuarterlyEmail> GetScheduledEmails(UserOrganizationModel caller, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Recurrence(recurrenceId);
					var scheduled = s.QueryOver<QuarterlyEmail>().Where(x => x.DeleteTime == null && x.ScheduledTime > DateTime.UtcNow.AddDays(-1) && x.RecurrenceId == recurrenceId).List().ToList();
					return scheduled;
				}
			}
		}

		[Queue(HangfireQueues.Immediate.SCHEDULED_QUARTERLY_EMAIL)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task<string> ScheduledEmail_HangFire(long quarterlyEmailId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var qe = s.Get<QuarterlyEmail>(quarterlyEmailId);

					if (qe.SentTime != null)
						throw new Exception("Already sent quarterly printout");
					if (qe.DeleteTime != null)
						throw new Exception("Request deleted");

					var caller = s.Get<UserOrganizationModel>(qe.SenderId);
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Recurrence(qe.RecurrenceId);

					var pdf = await PdfAccessor.QuarterlyPrintout(caller, qe.RecurrenceId, false, false, true, true, true, true, true, true, false, true, false, null);

					var orgName = caller.Organization.GetName();
					var mail = Mail.To("QuarterlyPrintout", qe.Email).AddBcc(caller.GetEmail())
								   .SubjectPlainText("Quarterly Printout - " + orgName)
								   .Body(EmailStrings.QuarterlyPrintout_Body, orgName, ProductStrings.ProductName);
					mail.ReplyToAddress= caller.GetEmail();
					mail.ReplyToName = caller.GetName();


					MemoryStream stream = new MemoryStream();
					pdf.Document.Save(stream, false);
					var base64=Convert.ToBase64String(stream.ToArray());
					stream.Close();


					mail.AddAttachment(new Mandrill.Models.EmailAttachment() {
						Base64 = true,
						Content = base64,
						Type = "application/pdf",
						Name = pdf.CreateTime.ToString("yyyyMMdd") + " - " + orgName + ".pdf",						
					});

					await Emailer.SendEmail(mail);					

					qe.SentTime = DateTime.UtcNow;
					s.Update(qe);
					tx.Commit();
					s.Flush();
					return qe.Email;		
				}
			}
		}
	}
}