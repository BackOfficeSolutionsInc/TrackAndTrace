using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Reviews
{
    public class ReviewContainerStats
    {
        public class OverallStats
        {
            public int ReviewsCompleted { get; set; }
            public int QuestionsAnswered { get; set; }
            public int OptionalsAnswered { get; set; }
            public int TotalQuestions { get; set; }
            public decimal? MinsPerReview { get; set; }
            public int NumberOfPeopleTakingReview { get; set; }
            public int NumberOfUniqueMatches { get; set; }
        }

        public class ReportStats
        {
            public int Total { get; set; }
            public int Unstarted { get; set; }
            public int Started { get; set; }
            public int Visible { get; set; }
            public int Signed { get; set; }
        }
        public class CompletionStats
        {
            public int Total { get; set; }
            public int Unstarted { get; set; }
            public int Started { get; set; }
            public int Finished { get; set; }
        }

        public ReviewContainerStats(long reviewContainerId)
        {
            Completion = new CompletionStats();
            Reports = new ReportStats();
            Stats = new OverallStats();
            ReviewId = reviewContainerId;
        }

        public long ReviewId { get; set; }
        public CompletionStats Completion { get; set; }
        public ReportStats Reports { get; set; }
        public OverallStats Stats { get; set; }
    }
}