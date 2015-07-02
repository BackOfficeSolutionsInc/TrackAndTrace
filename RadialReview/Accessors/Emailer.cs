using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Json;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Collections.Concurrent;
using RadialReview.Utilities.DataTypes;
using Mandrill;

namespace RadialReview.Accessors
{
	public class EmailResult
	{
		public int Sent { get; set; }
		public int Unsent { get; set; }
		public int Queued { get; set; }
		public int Total { get; set; }
		public int Faults { get; set; }
		public TimeSpan TimeTaken { get; set; }
		public List<Exception> Errors { get; set; }

		public EmailResult()
		{
			Errors = new List<Exception>();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="successMessage">
		///     {0} = Sent,<br/>
		///     {1} = Unsent,<br/>
		///     {2} = Total,<br/>
		///     {3} = TimeTaken(InSeconds),<br/>
		///     </param>
		/// <returns></returns>
		public ResultObject ToResults(String successMessage)
		{
			if (Errors.Count() > 0)
			{
				var message = String.Join(",\n", Errors.Select(x => x.Message).Distinct());
				return new ResultObject(new RedirectException(Errors.Count() + " errors:\n" + message));
			}
			return ResultObject.Create(false, String.Format(successMessage, Sent, Unsent, Total, TimeTaken.TotalSeconds));

		}
	}

	public class Emailer : BaseAccessor
	{
		#region Helpers
		private static String EmailBodyWrapper(String htmlBody)
		{
			var footer = String.Format(EmailStrings.Footer, ProductStrings.CompanyName);
			return String.Format(EmailStrings.BodyWrapper, htmlBody, footer);
		}

		public static bool IsValid(string emailaddress)
		{
			try
			{
				MailAddress m = new MailAddress(emailaddress);
				return true;
			}
			catch (FormatException)
			{
				return false;
			}
		}
		#endregion
		#region AsyncMailer

		private static MailMessage CreateMessage(EmailModel email)
		{
			MailMessage message = new MailMessage()
			{
				Subject = email.Subject,
				Body = email.Body,
				IsBodyHtml = true,
				From = new MailAddress(ConstantStrings.SmtpFromAddress),
			};
			message.To.Add(email.ToAddress);
			return message;
		}

		private static Pool<SmtpClient> SmtpPool = new Pool<SmtpClient>(30, TimeSpan.FromMinutes(2), () => new SmtpClient
		{
			Host = ConstantStrings.SmtpHost,
			Port = int.Parse(ConstantStrings.SmtpPort),
			Timeout = 50000,
			EnableSsl = true,
			Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword)
		});
		/*
		private static async Task<int> SendEmailsFast(List<EmailModel> emails, EmailResult result)
		{
			SemaphoreSlim throttler = new SemaphoreSlim(3);
			var allTasks = new List<Task>();
			try
			{
				foreach (var email in emails)
				{        
					await throttler.WaitAsync();
                    
					allTasks.Add(Task.Run(async () =>
					{
						try
						{
							var errCount = 0;
							var maxError = 5;
							while (errCount<maxError)
							{
								await Task.Delay(100);
								var smtpClient = SmtpPool.GetObject();
								try{
									smtpClient.Send(CreateMessage(email));
									lock (result){result.Sent += 1;}
									SmtpPool.PutObject(smtpClient);
									break;
								}catch (Exception e){
									errCount++;
									if (errCount == maxError){
										log.Error("Couldnt sent mail (" + email.Id + ")", e);
										lock (result){
											result.Unsent += 1;
											result.Errors.Add(e);
										}
										SmtpPool.DisposeObject(smtpClient);
										return false;
									}else{
										SmtpPool.DisposeObject(smtpClient);
									}
								}
							}
							return true;
						}
						finally
						{
							throttler.Release();
						}
					}));
				}

				await Task.WhenAll(allTasks);


				/*await Task.WhenAll(emails.Select(email =>{
					maxTask.Wait();
					var output =Task.Run(async () =>
						return true;
					});
					maxTask.Release();
					return output;
				}));*
			}
			catch (Exception e)
			{
				log.Error("All emails failed", e);
				result.Errors.Add(e);
			}
			return SmtpPool.Available();
		}*/

		//private static void Complete(object o, AsyncCompletedEventArgs e)
		//{
		//    int a = 0;

		//}

		//private static async Task<int> SendEmailsFast(List<EmailModel> emails, EmailResult result)
		//{
		//    /*var allTasks = new List<Task>();
		//    foreach (var email in emails)
		//    {
		//        SmtpPool.GetObject()

		//        allTasks.Add(Task.Run(async () =>
		//        {
		//            try
		//            {
		//                var errCount = 0;
		//                var maxError = 5;
		//                while (errCount < maxError)
		//                {
		//                    await Task.Delay(100);
		//                    var smtpClient = SmtpPool.GetObject();
		//                    try
		//                    {
		//                        smtpClient.Send(CreateMessage(email));
		//                        lock (result) { result.Sent += 1; }
		//                        SmtpPool.PutObject(smtpClient);
		//                        break;
		//                    }
		//                    catch (Exception e)
		//                    {
		//                        errCount++;
		//                        if (errCount == maxError)
		//                        {
		//                            log.Error("Couldnt sent mail (" + email.Id + ")", e);
		//                            lock (result)
		//                            {
		//                                result.Unsent += 1;
		//                                result.Errors.Add(e);
		//                            }
		//                            SmtpPool.DisposeObject(smtpClient);
		//                            return false;
		//                        }
		//                        else
		//                        {
		//                            SmtpPool.DisposeObject(smtpClient);
		//                        }
		//                    }
		//                }
		//                return true;
		//            }
		//            finally
		//            {
		//                throttler.Release();
		//            }
		//        }));
		//    }*/


		//    await Task.WhenAll(emails.Select(async email =>
		//    {
		//        var errors = 0;
		//        while (true)
		//        {
		//            var smtp = await SmtpPool.GetObject();
		//            try
		//            {
		//                await smtp.SendMailAsync(CreateMessage(email));
		//                lock (result)
		//                {
		//                    result.Sent += 1;
		//                }
		//                SmtpPool.PutObject(smtp);
		//                return true;
		//            }
		//            catch (Exception e)
		//            {
		//                errors++;
		//                SmtpPool.DisposeObject(smtp);
		//                lock (result){
		//                    result.Faults += 1;
		//                }

		//                if (errors == 5){
		//                    lock (result){
		//                        result.Unsent += 1;
		//                        result.Errors.Add(e);
		//                    }
		//                    break;
		//                }
		//            }
		//            await Task.Delay(1000);
		//        }
		//        return false;
		//    })
		//    );
		//    return 1;

		//}

		public class MandrillModel
		{
			public String FirstName { get; set; }
			public String LastName { get; set; }
		}

		private static EmailMessage CreateMandrillMessage(EmailModel email)
		{
			var toAddress = Config.IsLocal() ? "clay.upton+test_" + email.ToAddress.Replace("@", "_at_") + "@radialreview.com" : email.ToAddress;

			return new EmailMessage()
			{
				from_email = MandrillStrings.FromAddress,
				from_name = MandrillStrings.FromName,
				html = email.Body,
				subject = email.Subject,
				to = new EmailAddress(toAddress).AsList(),
				track_opens = true,
				track_clicks = true,
			};
		}

		public static async Task<int> SendMandrillEmails(List<EmailModel> emails, EmailResult result)
		{

			var api = new MandrillApi(ConstantStrings.MandrillApiKey, true);
			var results = new List<Mandrill.EmailResult>();
			if (Config.SendEmails())
			{
				results = (await Task.WhenAll(emails.Select(email => api.SendMessageAsync(CreateMandrillMessage(email))))).SelectMany(x => x).ToList();
			}
			else
			{
				results = emails.Select(x => new Mandrill.EmailResult()
				{
					Status = EmailResultStatus.Sent,
					Email = x.ToAddress,
				}).ToList();
			}
			var now = DateTime.UtcNow;
			foreach (var r in results)
			{
				switch (r.Status)
				{
					case EmailResultStatus.Invalid:
						{
							result.Unsent += 1;
							result.Errors.Add(new Exception("Invalid"));
							break;
						}
					case EmailResultStatus.Queued:
						result.Queued += 1;
						break;
					case EmailResultStatus.Rejected:
						{
							result.Unsent += 1;
							result.Errors.Add(new Exception(r.RejectReason));
							break;
						}
					case EmailResultStatus.Scheduled:
						result.Queued += 1;
						break;
					case EmailResultStatus.Sent:
						{
							result.Sent += 1;
							try
							{
								var found = emails.First(x => x.ToAddress.ToLower() == r.Email.ToLower());
								found.Sent = true;
								found.CompleteTime = now;
							}
							catch (Exception e)
							{
								int a = 0;
							}
						}
						break;
					default:
						break;
				}
			}


			return 1;
		}

		public static async Task<EmailResult> SendEmail(MailModel email)
		{
			return await SendEmails(email.AsList());
		}

		public static async Task<EmailResult> SendEmails(IEnumerable<MailModel> emails)
		{
			return await SendEmailsWrapped(emails);
		}

		private static async Task<EmailResult> SendEmailsWrapped(IEnumerable<MailModel> emails)
		{
			//Register emails
			var unsentEmails = new List<EmailModel>();
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					foreach (var email in emails)
					{
						var unsent = new EmailModel()
						{
							Body = EmailBodyWrapper(email.HtmlBody),
							CompleteTime = null,
							Sent = false,
							Subject = email.Subject,
							ToAddress = email.ToAddress,
							SentTime = now
						};
						s.Save(unsent);
						unsentEmails.Add(unsent);
					}
					tx.Commit();
					s.Flush();
				}
			}

			var result = new EmailResult() { Total = unsentEmails.Count };
			//Now send everything
			var startSending = DateTime.UtcNow;

			//And... Go.
			var threads = await SendMandrillEmails(unsentEmails, result);


			result.TimeTaken = DateTime.UtcNow - startSending;

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					foreach (var email in unsentEmails)
						s.Update(email);
					tx.Commit();
					s.Flush();
				}
			}

			return result;
		}



		#endregion

		#region oldSyncMailer
		/*
        private static void SendEmail(String address, String subject, String htmlHody, int emailId)
        {
            // "clay.upton@gmail.com"
            // "smtp.gmail.com"
            // 587                



            MailMessage message = new MailMessage
            {
                Subject = subject,
                Body = htmlHody,
                IsBodyHtml = true,
                From = new MailAddress(ConstantStrings.SmtpFromAddress),
            };
            message.To.Add(address);
            SmtpClient SmtpMailer = new SmtpClient
            {
                Host = ConstantStrings.SmtpHost,
                Port = int.Parse(ConstantStrings.SmtpPort),
                Timeout = 50000,
                EnableSsl = true
            };
            SmtpMailer.Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword);
            SmtpMailer.SendCompleted += EmailComplete;
            SmtpMailer.SendAsync(message, emailId);
        }

        private static void SendEmailSync(String address, String subject, String body, EmailModel email)
        {
            MailMessage message = new MailMessage
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                From = new MailAddress(ConstantStrings.SmtpFromAddress),
            };
            message.To.Add(address);
            SmtpClient SmtpMailer = new SmtpClient
            {
                Host = ConstantStrings.SmtpHost,
                Port = int.Parse(ConstantStrings.SmtpPort),
                Timeout = 50000,
                EnableSsl = true
            };
            SmtpMailer.Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword);
            SmtpMailer.Send(message);

            email.Sent = true;
            email.CompleteTime = DateTime.UtcNow;
        }
        
        private static void EmailComplete(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var email = s.Get<EmailModel>(e.UserState);
                        email.Sent = true;
                        email.CompleteTime = DateTime.UtcNow;
                        tx.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
            /*
            using (var db = new ApplicationDbContext())
            {
                var email=db.Emails.Find(e.UserState);
                db.SaveChanges();
            }*
        }*/
		/*
		private static async Task SendMailAsync(SmtpClient smtpMailer,MailModel email)
		{
			TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
			SendCompletedEventHandler handler = null;
			handler = delegate(object sender, AsyncCompletedEventArgs e)
			{
				smtpMailer.HandleCompletion(tcs, e, handler);
			};
			smtpMailer.SendCompleted += handler;
			try
			{
				this.SendAsync(message, tcs);
			}
			catch
			{
				this.SendCompleted -= handler;
				throw;
			}
			return tcs.Task;
		}

		public async static Task SendEmails(List<MailModel> emails)
		{
			SmtpClient smtpMailer = new SmtpClient
			{
				Host = ConstantStrings.SmtpHost,
				Port = int.Parse(ConstantStrings.SmtpPort),
				Timeout = 50000,
				EnableSsl = true
			};
			smtpMailer.Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword);

			var tasks =emails.Select(x=>smtpMailer.SendMailAsync(

			Task.WhenAll(tasks);

			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					tx.Commit();
					s.Flush();
				}
			}
		}*/
		/*
		public static void SendEmail(String toAddress, String subject, String htmlBody)
		{
			if (!IsValid(toAddress))
				throw new RedirectException(ExceptionStrings.InvalidEmail);

			var body = EmailBodyWrapper(htmlBody);
			long emailId = -1;
			using (var s = HibernateSession.GetCurrentSession())
			{
				EmailModel email;
				using (var tx = s.BeginTransaction())
				{
					email = new EmailModel()
								{
									Body = body,
									Sent = false,
									SentTime = DateTime.UtcNow,
									Subject = subject,
									ToAddress = toAddress
								};
					s.Save(email);
					//db.SaveChanges();
					emailId = email.Id;

					SendEmailSync(toAddress, subject, body, email);
					s.Update(email);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void SendEmail(AbstractUpdate s, String toAddress, String subject, String htmlBody)
		{
			if (!IsValid(toAddress))
				throw new RedirectException(ExceptionStrings.InvalidEmail);

			var body = EmailBodyWrapper(htmlBody);
			long emailId = -1;
			try
			{
				var email = new EmailModel()
				{
					Body = body,
					Sent = false,
					SentTime = DateTime.UtcNow,
					Subject = subject,
					ToAddress = toAddress
				};
				s.Save(email);


				//db.SaveChanges();
				emailId = email.Id;

				SendEmailSync(toAddress, subject, body, email);
				s.Update(email);
			}
			catch (Exception e)
			{
				log.Error(e);
			}

			//SendEmail(toAddress, subject, body, emailId);

			/*MailMessage message = new MailMessage
			{
				Subject = subject,
				Body = body,
				IsBodyHtml = true,
				From = new MailAddress(ConstantStrings.SmtpFromAddress),
			};
			message.To.Add(toAddress);
			SmtpClient SmtpMailer = new SmtpClient
			{
				Host = ConstantStrings.SmtpHost,
				Port = int.Parse(ConstantStrings.SmtpPort),
				Timeout = 50000,
				EnableSsl = true
			};
			SmtpMailer.Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword);
			SmtpMailer.Send(message);

			email.Sent = true;
			email.CompleteTime = DateTime.UtcNow;
			s.Update(email);*
		}*/
		#endregion
	}

}