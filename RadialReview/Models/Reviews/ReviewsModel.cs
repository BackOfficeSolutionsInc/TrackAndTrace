using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class ReviewsModel : ILongIdentifiable
    {
        public virtual long Id { get;protected set; }
        public virtual long CreatedById { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual DateTime DueDate { get; set; }
        public virtual String ReviewName { get; set; }
        /// <summary>
        /// UserOrganization Id
        /// </summary>
        public virtual List<ReviewModel> Reviews { get; set; }

        public ReviewsModel()
        {
            Reviews = null;
        }


    }

    public class ReviewsModelMap : ClassMap<ReviewsModel>
    {
        public ReviewsModelMap()
        {
            Id(x => x.Id);
            Map(x => x.ReviewName);
            Map(x => x.DateCreated);
            Map(x => x.DueDate);
            Map(x => x.CreatedById);
        }
    }
}