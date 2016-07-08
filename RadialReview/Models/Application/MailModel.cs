using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RadialReview.Models.Application
{
	public enum AddressType
	{
		to,
		cc,
		bcc,
	}

    public class Mail
    {
        public class MailIntermediate2
        {
            protected Mail Email {get;set;}
            
            public MailIntermediate2(Mail email)
            {
                Email=email;
            }
            public Mail Body(String htmlBodyFormat, params String[] args)
            {
                Email.HtmlBody = String.Format(htmlBodyFormat, args);
                return Email;
            }

			public Mail BodyPlainText(string body)
			{
				Email.HtmlBody = body;
				return Email;
			}
		}
		public class MailIntermediate1
		{
			protected Mail Email { get; set; }
			public MailIntermediate1(Mail email)
			{
				Email = email;
			}

			public MailIntermediate1 AddBcc(string email)
			{
				Email.BccList.Add(email);
				return this;
			}

			public MailIntermediate2 Subject(String subjectFormat, params String[] args)
			{
				var unformatted = String.Format(subjectFormat, args);
				Email.Subject = Regex.Replace(unformatted, @"[^A-Za-z0-9 \.\,&]", "");
				return new MailIntermediate2(Email);
			}

			public MailIntermediate2 SubjectPlainText(string subject)
			{
				Email.Subject = subject;
				return new MailIntermediate2(Email);
			}
		}


        public string ToAddress { get; set; }
		public string EmailType { get; set; }
		public List<String> BccList { get; set; } 
        public string HtmlBody { get; set; }
        public string Subject { get; set; }

        public string ReplyToAddress { get; set; }
        public string ReplyToName { get; set; }
        //public virtual bool Send {get;set;}
        //public virtual DateTime? CompleteTime {get;set;}

        protected Mail(){
			BccList = new List<string>();
        }

		public static MailIntermediate1 To(String emailType,String toAddress)
		{
			return new MailIntermediate1(new Mail(){
				EmailType = emailType,
				ToAddress = toAddress
			});
		}
		public static MailIntermediate1 Bcc(String emailType, String toAddress)
		{
			return new MailIntermediate1(new Mail()
			{
				EmailType = emailType,
				BccList = toAddress.AsList(),
				ToAddress = "",
			});
		}


    }

    
}