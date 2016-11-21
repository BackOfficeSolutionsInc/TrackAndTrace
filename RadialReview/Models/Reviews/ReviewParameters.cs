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


		public static ReviewParameters AllTrue() {
			return new ReviewParameters() {
				ReviewTeammates = true,
				ReviewSubordinates = true,
				ReviewSelf = true,
				ReviewPeers = true,
				ReviewManagers = true,
			};
		}
		public static ReviewParameters AllFalse() {
			return new ReviewParameters() {
				ReviewTeammates = false,
				ReviewSubordinates = false,
				ReviewSelf = false,
				ReviewPeers = false,
				ReviewManagers = false,
			};
		}
	}
}