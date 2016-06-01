using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Onboard {
    public class OnboardingUser {
        public virtual long Id { get; set; }
        public virtual String Guid { get; set; }
        public virtual DateTime StartTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual DateTime? ContactCompleteTime { get; set; }
        public virtual DateTime? OutcomeCompleteTime { get; set; }
        public virtual DateTime? L10CompleteTime { get; set; }
        public virtual DateTime? CreditCardCompleteTime { get; set; }
        public virtual DateTime? CreateOrganizationTime { get; set; }
        public virtual String CurrentPage { get; set; }

        public virtual String FirstName { get; set; }
        public virtual String LastName { get; set; }
        public virtual String Email { get; set; }
        public virtual String Phone { get; set; }
        public virtual String Position { get; set; }
        public virtual String ProfilePicture { get; set; }
        public virtual String CompanyName { get; set; }
        public virtual String CompanyLogo { get; set; }
        public virtual bool? CurrentlyImplementingEOS { get; set; }
        public virtual bool? CurrentlyRunningLevel10Meetings { get; set; }
        public virtual String DesiredOutcome { get; set; }
        public virtual String ImplementerName { get; set; }
        public virtual DateTime? EosStartTime { get; set; }
        public virtual long? OrganizationId { get; set; }
        public virtual long? UserId { get; set; }

        public virtual string UserAgent { get; set; }
        public virtual string Languages { get; set; }

        public class Map : ClassMap<OnboardingUser> {
            public Map()
            {
                Id(x => x.Id);
                Map(x => x.CompanyLogo);
                Map(x => x.CompanyName);
                Map(x => x.ContactCompleteTime);
                Map(x => x.CreateOrganizationTime);
                Map(x => x.CreditCardCompleteTime);
                Map(x => x.CurrentlyImplementingEOS);
                Map(x => x.CurrentlyRunningLevel10Meetings);
                Map(x => x.CurrentPage);
                Map(x => x.DesiredOutcome);
                Map(x => x.Email);
                Map(x => x.EosStartTime);
                Map(x => x.FirstName);
                Map(x => x.ImplementerName);
                Map(x => x.L10CompleteTime);
                Map(x => x.LastName);
                Map(x => x.OutcomeCompleteTime);
                Map(x => x.Phone);
                Map(x => x.Position);
                Map(x => x.ProfilePicture);
                Map(x => x.StartTime);
                Map(x => x.OrganizationId);
                Map(x => x.UserId);
                Map(x => x.UserAgent);
                Map(x => x.Languages);
                Map(x => x.DeleteTime);
                Map(x => x.Guid);
            }
        }

    }
}