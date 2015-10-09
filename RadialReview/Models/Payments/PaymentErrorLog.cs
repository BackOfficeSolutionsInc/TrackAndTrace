using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Exceptions;

namespace RadialReview.Models.Payments
{
	public class PaymentErrorLog
	{
		public virtual long Id { get; set; }
		public virtual long TaskId { get; set; }
		public virtual decimal Amount { get; set; }
		public virtual String Message { get; set; }
		public virtual DateTime? HandledAt { get; set; }
		public virtual  DateTime OccurredAt { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual String OrganizationName { get; set; }
		public virtual PaymentExceptionType Type { get; set; }


		public PaymentErrorLog()
		{
			OccurredAt = DateTime.UtcNow;
		}

		public static PaymentErrorLog Create(PaymentException e,long taskId)
		{
			return new PaymentErrorLog(){
				TaskId = taskId,
				Amount = e.ChargeAmount,
				Message = e.Message,
				OrganizationId = e.OrganizationId,
				OrganizationName = e.OrganizationName,
				OccurredAt = e.OccurredAt,
				Type = e.Type,
			};
		}

		public class PaymentErrorLogMap : ClassMap<PaymentErrorLog>
		{
			public PaymentErrorLogMap()
			{
				Id(x => x.Id);
				Map(x => x.TaskId);
				Map(x => x.Amount);
				Map(x => x.Message);
				Map(x => x.HandledAt);
				Map(x => x.OccurredAt);
				Map(x => x.OrganizationId);
				Map(x => x.OrganizationName);
				Map(x => x.Type);
			}
		}
	}
}