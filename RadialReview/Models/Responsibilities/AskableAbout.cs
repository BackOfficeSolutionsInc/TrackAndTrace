using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Responsibilities
{
    public class AskableAbout
    {
        public Askable Askable { get; set; }
        public long AboutUserId { get; set; }
        public AboutType AboutType { get; set; }
    }
}