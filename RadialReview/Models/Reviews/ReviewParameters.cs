using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Reviews
{
    public class ReviewParameters
    {
        public bool ReviewSelf { get; set; }
        public bool ReviewManagers { get; set; }
        public bool ReviewSubordinates { get; set; }
        public bool ReviewPeers { get; set; }
        public bool ReviewTeammates { get; set; }

    }
}