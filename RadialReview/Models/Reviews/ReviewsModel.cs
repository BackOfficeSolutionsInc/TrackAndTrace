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
        public virtual long Id { get; protected set; }
        public virtual long CreatedById { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual DateTime DueDate { get; set; }
        public virtual String ReviewName { get; set; }
        /// <summary>
        /// UserOrganization Id
        /// </summary>
        public virtual List<ReviewModel> Reviews { get; set; }

        public virtual OrganizationModel ForOrganization { get; set; }

        public virtual decimal? GetCompletion()
        {
            if (Reviews == null)
                return null;
            decimal count = 0;
            decimal sum = 0;
            foreach (var r in Reviews)
            {
                var c = r.GetCompletion();
                if (c!=null)
                {
                    count++;
                    sum += Math.Min(1m,c.Value);
                }
            }
            if (count == 0)
                return 0;
            return sum / count;

        }

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
            References(x => x.ForOrganization)
                .Not.LazyLoad();
        }
    }
}