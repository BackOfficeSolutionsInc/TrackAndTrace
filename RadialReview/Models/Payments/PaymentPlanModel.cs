using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Tasks;
using System;

namespace RadialReview.Models
{
    public class PaymentPlanModel
    {
        public virtual long Id { get; protected set; }
        public virtual String Description { get; set; }
        public virtual DateTime PlanCreated { get; set; }
        public virtual Boolean IsDefault { get; set; }
		public virtual DateTime FreeUntil { get; set; }
		public virtual ScheduledTask Task { get; set; }
		public virtual ScheduledTask _CurrentTask { get; set; }
        public virtual PaymentPlanType PlanType { get; set; }
		public virtual DateTime? LastExecuted { get; set; }

        public virtual TimeSpan SchedulerPeriod()        {
            return TimeSpan.MaxValue;
        }

        public PaymentPlanModel()
        {
            PlanCreated = DateTime.UtcNow;
        }
        public class PaymentPlanModelMap : ClassMap<PaymentPlanModel>
        {
            public PaymentPlanModelMap()
            {
                Id(x => x.Id);
                Map(x => x.IsDefault);
                Map(x => x.Description);
				Map(x => x.PlanCreated);
				Map(x => x.LastExecuted);
				Map(x => x.FreeUntil);
                References(x => x.Task).Not.Nullable().Not.LazyLoad();
                Map(x => x.PlanType).CustomType<PaymentPlanType>();
            }
        }

        public virtual string TaskName()
        {
            return "PAYMENT_PLAN";
        }
    }

    public class PaymentPlan_Monthly : PaymentPlanModel
    {
        public virtual decimal L10PricePerPerson { get; set; }
        public virtual decimal ReviewPricePerPerson { get; set; }
        public virtual int FirstN_Users_Free { get; set; }

        public virtual decimal BaselinePrice { get; set; }

        public virtual long OrgId { get; set; }

		public virtual OrganizationModel _Org { get; set; }

		public virtual DateTime? ReviewFreeUntil { get; set; }
		public virtual DateTime? L10FreeUntil { get; set; }

		public virtual bool NoChargeForClients { get; set; }

		public virtual bool NoChargeForUnregisteredUsers { get; set; }

		public override TimeSpan SchedulerPeriod()
        {
            return TimespanExtensions.OneMonth();
        }

		public PaymentPlan_Monthly() {

		}

        public class PaymentPlan_MonthlyMap : SubclassMap<PaymentPlan_Monthly>
        {
            public PaymentPlan_MonthlyMap()
            {
                Map(x => x.L10PricePerPerson);
                Map(x => x.ReviewPricePerPerson);
				Map(x => x.FirstN_Users_Free);
				Map(x => x.OrgId).Column("OrganizationId");
                Map(x => x.ReviewFreeUntil);
                Map(x => x.L10FreeUntil);
				Map(x => x.BaselinePrice);
				Map(x => x.NoChargeForClients);
				Map(x => x.NoChargeForUnregisteredUsers);
			}
        }

        public override string TaskName()
        {
            return ScheduledTask.MonthlyPaymentPlan;
        }
        
    }

   
}