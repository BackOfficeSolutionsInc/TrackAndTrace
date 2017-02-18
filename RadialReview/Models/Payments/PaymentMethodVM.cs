using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Payments
{
    public class PaymentMethodVM
    {
        public long CardId { get; set; }
        public string Last4 { get; set; }
        public string Owner { get; set; }
        public DateTime Created { get; set; }
        public bool Active { get; set; }
		public PaymentSpringTokenType TokenType { get; set; }
		public string ImageUrl { get; set; }

		public PaymentMethodVM(PaymentSpringsToken x) {
			var last4 = "";
			var owner = "";
			var img = "";

			switch (x.CardType) {
				case "visa":
					img = "/Content/creditcard_icon/visa.png";
					break;
				case "amex":
					img = "/Content/creditcard_icon/visa.png";
					break;
				case "discovery":
					img = "/Content/creditcard_icon/discovery.png";
					break;
				case "mastercard":
					img = "/Content/creditcard_icon/mastercard.png";
					break;
				default:
					img = "";
					break;
			}

			switch (x.TokenType) {
				case PaymentSpringTokenType.CreditCard: {
						last4 = x.CardLast4;
						owner = x.CardOwner;
					}
					break;
				case PaymentSpringTokenType.BankAccount: {
						last4 = x.BankAccountLast4;
						owner = x.BankFirstName + " " + x.BankLastName;
						img = "/Content/creditcard_icon/bank.png";
					}
					break;
				default:
					break;
			}
			
			Active = x.Active;
			CardId = x.Id;
			Created = x.CreateTime;
			Last4 = last4;
			Owner = owner;
			ImageUrl = img;


		}
    }
}