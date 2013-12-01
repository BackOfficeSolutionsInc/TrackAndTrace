using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class ReviewModel
    {
        public virtual long Id { get; protected set; }
        public virtual long ForReviewsId { get;set;}
        public virtual long ForUserId { get; set; }
        public virtual String Name { get; set; }
        public virtual DateTime DueDate { get; set; }
        public virtual List<AnswerModel> Answers { get; set; }
        public virtual decimal Completion { get; set; }
        public virtual Boolean Complete { get; set; }
        public virtual Boolean FullyComplete { get; set; }


        private  decimal Divide(decimal numerator, decimal denomiator)
        {
            if (denomiator == 0)
                return 1;
            return numerator / denomiator;
        }


        public virtual decimal? GetCompletion()
        {
            if (Answers == null)
                return null;
            if (Answers.Count() == 0)
                return 1m;
            
            decimal requiredComplete = Answers.Count(x => x.Required && x.Complete);
            decimal required = Answers.Count(x => x.Required);
            decimal total = Answers.Count();
            if (requiredComplete < required)
            {
                return Divide(requiredComplete, required);
            }
            else
            {
                var complete = Answers.Count(x => x.Complete);
                return Divide(complete, required);
            }
        }


        public ReviewModel()
        {
            Answers = null;//new List<AnswerModel>();
        }
        
    }

    public class ReviewModelMap :ClassMap<ReviewModel>
    {
        public ReviewModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Complete);
            Map(x => x.Completion);
            Map(x => x.FullyComplete);
            Map(x => x.ForUserId);
            Map(x => x.ForReviewsId);
            Map(x => x.Name);
            Map(x => x.DueDate);
            /*HasMany(x => x.Answers)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();*/
        }
    }
}