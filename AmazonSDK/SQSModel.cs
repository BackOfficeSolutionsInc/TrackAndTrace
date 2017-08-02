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
}
