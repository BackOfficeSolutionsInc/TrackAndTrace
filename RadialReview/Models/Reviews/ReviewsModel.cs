using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Periods;

namespace RadialReview.Models
{
    public class ReviewsModel : ILongIdentifiable, ICompletable,IDeletable
    {
        public virtual long Id { get; protected set; }
        public virtual long CreatedById { get; set; }
        public virtual DateTime DateCreated { get; set; }
        public virtual DateTime DueDate { get; set; }
        public virtual DateTime? ReportsDueDate { get; set; }
        public virtual DateTime? PrereviewDueDate { get; set; }
        public virtual bool HasPrereview { get; set; }
		public virtual String ReviewName { get; set; }
		public virtual bool AnonymousByDefault { get; set; }
        public virtual bool EnsureDefault { get; set; }
        public virtual bool ReviewManagers { get; set; }
        public virtual bool ReviewSelf { get; set; }
        public virtual bool ReviewSubordinates { get; set; }
        public virtual bool ReviewTeammates { get; set; }
        public virtual bool ReviewPeers { get; set; }

	    public virtual long PeriodId { get; set; }
	    public virtual PeriodModel Period { get; set; }

        public virtual List<ReviewModel> Reviews { get; set; }
        public virtual long ForOrganizationId { get; set; }
        public virtual OrganizationModel ForOrganization { get; set; }
        public virtual long ForTeamId { get; set; }
        public virtual CompletionModel Completion { get; set; }
        public virtual long? TaskId { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual ICompletionModel GetCompletion(bool split=false)
        {
            return Completion;
            //return CompletionModel.FromList(Reviews.Select(x => x.GetCompletion()));
            /*foreach (var r in Reviews)
            {
                var c = r.GetCompletion();
                if (c!=null)
                {
                    count++;
                    sum += Math.Min(1,c.Value);
                }
            }
            return new CompletionModel(sum, count);*/
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

            Map(x => x.TaskId);
            
            Map(x => x.ReviewManagers);
            Map(x => x.ReviewSelf);
            Map(x => x.ReviewSubordinates);
            Map(x => x.ReviewTeammates);
            Map(x => x.ReviewPeers);
            Map(x => x.ReportsDueDate);
            Map(x => x.EnsureDefault);
            Map(x => x.DeleteTime);
			Map(x => x.HasPrereview);
			Map(x => x.PrereviewDueDate);
			Map(x => x.AnonymousByDefault);

			Map(x => x.PeriodId).Column("PeriodId");
			References(x => x.Period).Column("PeriodId").LazyLoad().ReadOnly();

            Map(x => x.ForTeamId);

            Map(x => x.ForOrganizationId).Column("ForOrganization_Id");
            References(x => x.ForOrganization).Column("ForOrganization_Id").LazyLoad().ReadOnly();
        }
    }
}