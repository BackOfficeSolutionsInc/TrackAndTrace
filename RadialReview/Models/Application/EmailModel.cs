using FluentNHibernate.Mapping;
using Mandrill;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
	public static class EmailType
	{
		public const string JoinOrganization = "JoinOrganization";
	}

    public class EmailModel : ILongIdentifiable
    {
		public virtual long Id { get; set; }
		public virtual String MandrillId { get; set; }
		public virtual string ToAddress { get; set; }
		public virtual string Bcc { get; set; }
        public virtual string Body { get; set; }
        public virtual string Subject { get; set; }
        public virtual DateTime? SentTime { get; set; }
        public virtual DateTime? CompleteTime { get; set; }
        public virtual Boolean Sent { get; set; }
		public virtual string EmailType { get; set; }

        public virtual string _ReplyToEmail { get; set; }
        public virtual string _ReplyToName { get; set; }
    }

    public class EmailModelMap:ClassMap<EmailModel>
    {
        public EmailModelMap()
        {
			Id(x => x.Id);
			Map(x => x.ToAddress);
			Map(x => x.MandrillId).Index("EmailModel_MandrillId");
			Map(x => x.EmailType);
			Map(x => x.Bcc);
            Map(x => x.Body).Length(3000).Not.Nullable();
            Map(x => x.Subject);
            Map(x => x.SentTime);
            Map(x => x.CompleteTime);
            Map(x => x.Sent);
        }
    }


	public class EmailWebhookModel : ILongIdentifiable
	{
		public virtual long Id { get; set; }
		public virtual String MandrillId { get; set; }
		public virtual WebHookEventType EventType { get; set; }
		public virtual DateTime TimeStamp { get; set; }

		public virtual int Opens { get; set; }
		public virtual int Clicks { get; set; }

	}

	public class EmailWebhookMap : ClassMap<EmailWebhookModel>
	{
		public EmailWebhookMap()
		{
			Id(x => x.Id);

			Map(x => x.MandrillId);//.Index("EmailmandrillId;
			Map(x => x.EventType).CustomType<WebHookEventType>();
			Map(x => x.TimeStamp);
			
			Map(x => x.Opens);
			Map(x => x.Clicks);

		}
	}
}