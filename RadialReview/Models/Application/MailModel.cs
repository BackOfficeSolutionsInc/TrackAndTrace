using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RadialReview.Models.Application
{
    public class MailModel
    {
        public class MailIntermediate2
        {
            protected MailModel Email {get;set;}
            
            public MailIntermediate2(MailModel email)
            {
                Email=email;
            }
            public MailModel Body(String htmlBodyFormat, params String[] args)
            {
                Email.HtmlBody = String.Format(htmlBodyFormat, args);
                return Email;
            }

        }
        public class MailIntermediate1 {
            protected MailModel Email { get; set; }
            public MailIntermediate1(MailModel email)
            {
                Email=email;
            }

            public MailIntermediate2 Subject(String subjectFormat, params String[] args)
            {
                var unformatted=String.Format(subjectFormat, args);
                Email.Subject = Regex.Replace(unformatted, @"[^A-Za-z0-9 \.\,&]", "");
                return new MailIntermediate2(Email);
            }
        }


        public string ToAddress { get; set; }
        public string HtmlBody { get; set; }
        public string Subject { get; set; }
        //public virtual bool Send {get;set;}
        //public virtual DateTime? CompleteTime {get;set;}

        protected MailModel(){

        }

        public static MailIntermediate1 To(String toAddress)
        {
            return new MailIntermediate1(new MailModel() {ToAddress=toAddress });
        }
    }

    
}