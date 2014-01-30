﻿using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Reviews
{
 

    public class ClientReviewModel : ILongIdentifiable
    {
        public virtual long Id {get;set;}
        public virtual long ReviewId { get; set; }
        public virtual IList<LongModel> FeedbackIds { get; set; }
        public virtual IList<LongTuple> Charts { get; set; }

        public virtual Boolean IncludeManagerFeedback { get; set; }

        public virtual Boolean IncludeQuestionTable { get; set; }

        public virtual bool Visible { get; set; }
        public virtual String ManagerNotes { get; set; }

        public ClientReviewModel()
        {
            FeedbackIds = new List<LongModel>();
            Charts = new List<LongTuple>();
        }

        public virtual bool IncludeSelfFeedback { get; set; }
    }

    public class ClientReviewModelMap : ClassMap<ClientReviewModel>
    {
        public ClientReviewModelMap()
        {
            Id(x => x.Id);
            Map(x => x.ReviewId);
            Map(x => x.Visible);
            Map(x => x.ManagerNotes);
            Map(x => x.IncludeManagerFeedback);
            Map(x => x.IncludeQuestionTable);
            Map(x => x.IncludeSelfFeedback);

            HasMany(x => x.FeedbackIds)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            HasMany(x => x.Charts)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
        }
    }   
    
    public class LongTuple : IDeletable
    {
        public virtual long Id { get; set; }
        public virtual long Item1 { get; set; }
        public virtual long Item2 { get; set; }
        public virtual bool Grouped { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
    }
    public class LongTupleMap : ClassMap<LongTuple>
    {
        public LongTupleMap()
        {
            Id(x => x.Id);
            Map(x => x.Item1);
            Map(x => x.Item2);
            Map(x => x.Grouped);
            Map(x => x.DeleteTime);
        }
    }
}