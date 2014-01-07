using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class EmailModel : ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual string ToAddress { get; set; }
        public virtual string Body { get; set; }
        public virtual string Subject { get; set; }
        public virtual DateTime? SentTime { get; set; }
        public virtual DateTime? CompleteTime { get; set; }
        public virtual Boolean Sent { get; set; }
        
    }

    public class EmailModelMap:ClassMap<EmailModel>
    {
        public EmailModelMap()
        {
            Id(x => x.Id);
            Map(x => x.ToAddress);
            Map(x => x.Body).Length(3000).Not.Nullable();
            Map(x => x.Subject);
            Map(x => x.SentTime);
            Map(x => x.CompleteTime);
            Map(x => x.Sent);
        }
    }
}