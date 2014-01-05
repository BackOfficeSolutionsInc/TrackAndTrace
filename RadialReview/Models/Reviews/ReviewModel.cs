using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class ReviewModel : ICompletable, ILongIdentifiable
    {
        public virtual long Id { get; protected set; }
        public virtual long ForReviewsId { get;set;}
        public virtual UserOrganizationModel ForUser { get; set; }
        public virtual long ForUserId { get; set; }
        public virtual String Name { get; set; }
        public virtual DateTime CreatedAt { get; set; }
        public virtual DateTime DueDate { get; set; }
        public virtual ClientReviewModel ClientReview { get; set; }
        public virtual List<AnswerModel> Answers { get; set; }

        /*public virtual decimal Completion { get; set; }
        public virtual Boolean Complete { get; set; }
        public virtual Boolean FullyComplete { get; set; }*/


        /*private  decimal Divide(decimal numerator, decimal denomiator)
        {
            if (denomiator == 0)
                return 1;
            return numerator / denomiator;
        }*/


        public virtual CompletionModel GetCompletion()
        {
            if (Answers == null)
                return null;
            
            int requiredComplete = Answers.Count(x => x.Required && x.Complete);
            var optionalComplete = Answers.Count(x => x.Complete && !x.Required);
            int required = Answers.Count(x => x.Required);
            int optional = Answers.Count(x => !x.Required);

            return new CompletionModel(requiredComplete, required, optionalComplete, optional)
            {
                ForceInactive= DateTime.UtcNow>DueDate
            };
            /*
            if (requiredComplete < required)
            {
                return Divide(requiredComplete, required);
            }
            else
            {
                return Divide(complete, required);
            }*/
        }


        public ReviewModel()
        {
            Answers = null;//new List<AnswerModel>();
            CreatedAt = DateTime.UtcNow;
            ClientReview = new ClientReviewModel();
        }
        
    }

    public class ReviewModelMap :ClassMap<ReviewModel>
    {
        public ReviewModelMap()
        {
            Id(x => x.Id);
            //Map(x => x.Complete);
            //Map(x => x.Completion);
            //Map(x => x.FullyComplete);
            Map(x => x.ForUserId)
                .Column("ForUserId");
            References(x => x.ForUser)
                .Column("ForUserId")
                .Not.LazyLoad()
                .ReadOnly();
            Map(x => x.ForReviewsId);
            Map(x => x.Name);
            Map(x => x.CreatedAt);
            Map(x => x.DueDate);
            References(x => x.ClientReview)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();

            /*HasMany(x => x.Answers)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();*/
        }
    }
}