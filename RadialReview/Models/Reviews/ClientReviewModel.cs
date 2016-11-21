using System.Web.Security;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Angular.Meeting;

namespace RadialReview.Models.Reviews
{
 

    public class ClientReviewModel : ILongIdentifiable
    {

		public virtual long Id { get; set; }
		public virtual long ReviewContainerId { get; set; }
        public virtual long ReviewId { get; set; }
        public virtual IList<LongModel> FeedbackIds { get; set; }
        public virtual IList<LongTuple> Charts { get; set; }
        
        public virtual LongTuple ScatterChart { get; set; }

        public virtual Boolean IncludeManagerFeedback { get; set; }
		public virtual Boolean IncludeQuestionTable { get; set; }
		public virtual bool IncludeSelfFeedback { get; set; }
		public virtual bool IncludeEvaluation { get; set; }
		public virtual bool IncludeScatterChart { get; set; }
		public virtual bool IncludeTimelineChart { get; set; }
		public virtual bool IncludeNotes { get; set; }
        public virtual bool Visible { get; set; }
        public virtual String ManagerNotes { get; set; }
        public virtual bool IncludeScorecard { get; set; }
        public virtual DateTime? SignedTime { get; set; }

		public virtual AngularRecurrence _ScorecardRecur { get; set; }

        public virtual bool Started()
        {
            return (
				Charts.Any() ||
				FeedbackIds.Any() ||
				IncludeManagerFeedback ||
				IncludeQuestionTable ||
				IncludeSelfFeedback ||
				IncludeEvaluation ||
				IncludeScatterChart ||
				IncludeTimelineChart ||
				!String.IsNullOrEmpty(ManagerNotes) ||
				Visible);
        }

        public ClientReviewModel()
        {
            FeedbackIds = new List<LongModel>();
            Charts = new List<LongTuple>();
            ScatterChart = new LongTuple();

			IncludeScorecard = true;
			IncludeEvaluation = true;
			IncludeScatterChart = true;
		}

	}

    public class ClientReviewModelMap : ClassMap<ClientReviewModel>
    {
        public ClientReviewModelMap()
        {
			Id(x => x.Id);
			Map(x => x.ReviewContainerId);
            Map(x => x.ReviewId);
			Map(x => x.Visible);
			Map(x => x.ManagerNotes);
			Map(x => x.IncludeNotes);
            Map(x => x.IncludeManagerFeedback);
			Map(x => x.IncludeQuestionTable);
			Map(x => x.IncludeSelfFeedback);
			Map(x => x.IncludeEvaluation);

            Map(x => x.IncludeScatterChart);
            Map(x => x.IncludeTimelineChart);
            Map(x => x.IncludeScorecard);

            Map(x => x.SignedTime);

            References(x => x.ScatterChart).Cascade.All().Not.LazyLoad();

            HasMany(x => x.FeedbackIds)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            HasMany(x => x.Charts)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
        }
    }

    /*public class ReportScatter
    {
        public virtual long Id { get; set; }
        public virtual String Groups { get; set; }
        public virtual String Filters { get; set; }
        public virtual String Title { get; set; }
	
        public virtual String AggregateBy { get; set; } 

        public class RSMap:ClassMap<ReportScatter>
        {
            public RSMap()
            {
                Id(x => x.Id);
                Map(x => x.Groups);
                Map(x => x.Filters);
                Map(x => x.Title);
                Map(x => x.AggregateBy);
            }
        }
    }*/

    
    public class LongTuple : IDeletable
    {
        public virtual long Id { get; set; }
        public virtual long Item1 { get; set; }
        public virtual long Item2 { get; set; }
         public virtual String Title { get; set; }
        public virtual String Groups { get; set; }
        public virtual String Filters { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime EndDate { get; set; }
		public virtual bool IncludePrevious { get; set; }
    }
    public class LongTupleMap : ClassMap<LongTuple>
    {
        public LongTupleMap()
        {
            Id(x => x.Id);
            Map(x => x.Item1);
            Map(x => x.Item2);
			Map(x => x.Title);
			Map(x => x.Groups);
			Map(x => x.IncludePrevious);
            Map(x => x.Filters);
            Map(x => x.EndDate);
            Map(x => x.StartDate);
            Map(x => x.DeleteTime);
        }
    }
}