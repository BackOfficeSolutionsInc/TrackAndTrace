using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Web;

namespace RadialReview.Accessors
{
    public class Emailer : BaseAccessor
    {
        private static String EmailBodyWrapper(String htmlBody)
        {
            var footer = String.Format(EmailStrings.Footer, ProductStrings.ProductName);
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
            }*/
        }

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
            s.Update(email);*/
            try
            {
                SendEmailSync(toAddress, subject, body, email);
                s.Update(email);
            }
            catch (Exception e)
            {
                log.Error(e);
            }
        }
    }
}