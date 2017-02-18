﻿using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Payments
{
	public enum PaymentSpringTokenType {
		CreditCard= 0,
		BankAccount = 1
	}

    public class PaymentSpringsToken : IHistorical, ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        //public virtual String CustomerId { get; set; }
        public virtual String CustomerToken { get; set; }

		public virtual PaymentSpringTokenType TokenType { get; set; }

        public virtual int MonthExpire { get; set; }
        public virtual int YearExpire { get; set; }
        public virtual string CardType { get; set; }
        public virtual string CardOwner { get; set; }
        public virtual string CardLast4 { get; set; }
        public virtual bool Active { get; set; }

		public virtual String ReceiptEmail { get; set; }
		public virtual long CreatedBy { get; set; }

        public virtual long OrganizationId { get; set; }


		public virtual string BankAccountLast4 { get; set; }
		public virtual string BankRouting { get; set; }
		public virtual string BankFirstName { get; set; }
		public virtual string BankLastName { get; set; }
		public virtual string BankAccountType { get; set; }


		public PaymentSpringsToken()        {
            CreateTime = DateTime.UtcNow;
        }

        public class PSTMap : ClassMap<PaymentSpringsToken>
        {
            public PSTMap()
            {
                Id(x => x.Id);
				
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
                //Map(x => x.CustomerId);
                Map(x => x.CustomerToken);
				Map(x => x.MonthExpire);
				Map(x => x.YearExpire);

				Map(x => x.ReceiptEmail);
				Map(x => x.CreatedBy);

                Map(x => x.CardType);
                Map(x => x.CardOwner);
                Map(x => x.CardLast4);

                Map(x => x.Active);

                Map(x => x.OrganizationId);

				Map(x => x.TokenType);
				Map(x => x.BankAccountLast4);
				Map(x => x.BankRouting);
				Map(x => x.BankFirstName);
				Map(x => x.BankLastName);
				Map(x => x.BankAccountType);

			}
		}

	}
}