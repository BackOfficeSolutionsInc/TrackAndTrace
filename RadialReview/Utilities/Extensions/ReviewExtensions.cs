using RadialReview.Models;
using RadialReview.Models.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class ReviewExtensions
    {
        public static ReviewParameters GetParameters(this ReviewsModel self)
        {
            return new ReviewParameters()
            {
                ReviewManagers = self.ReviewManagers,
                ReviewPeers = self.ReviewPeers,
                ReviewSelf = self.ReviewSelf,
                ReviewSubordinates = self.ReviewSubordinates,
                ReviewTeammates = self.ReviewTeammates
            };
        }
    }
}