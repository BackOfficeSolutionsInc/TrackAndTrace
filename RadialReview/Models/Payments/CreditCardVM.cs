using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Payments
{
    public class CreditCardVM
    {
        public long CardId { get; set; }
        public string Last4 { get; set; }
        public string Owner { get; set; }
        public DateTime Created { get; set; }
        public bool Active { get; set; }
    }
}