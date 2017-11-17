using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmazonSDK
{
    public class MessageModel
    {
        public string Body { get; set; }
        public string MessageId { get; set; }
        public string ReceiptHandle { get; set; }
    }

    public class MessageQueue
    {
        public virtual long Id { get; set; }
        public virtual string IdentifierId { get; set; }
        public virtual long? UserOrgId { get; set; } // orgId from radial
        public virtual string UserName { get; set; }  // user name from radial
        public virtual string ReceiptHandle { get; set; }
        public virtual string Status { get; set; }
    }

    public class TokenModel
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string issued { get; set; }
        public string expires { get; set; }
        public string error_description { get; set; }
        public string error { get; set; }
    }

    public enum MessageQueueStatus
    {
        Start,
        Complete
    }
    public class MessageQueueMap : ClassMap<MessageQueue>
    {
        public MessageQueueMap()
        {
            Id(x => x.Id);
            Map(x => x.IdentifierId).Length(256);
            Map(x => x.UserOrgId);
            Map(x => x.UserName);
            Map(x => x.ReceiptHandle);
            Map(x => x.Status);
        }
    }
}
