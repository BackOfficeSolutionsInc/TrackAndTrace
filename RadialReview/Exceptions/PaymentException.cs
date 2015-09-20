using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions
{
    public enum PaymentExceptionType{
        MissingToken = 1,
        ResponseError =2,
        
    }
    public class PaymentException : Exception
    {
        public long OrganizationId { get; set; }
        public DateTime OccurredAt { get; set; }
        public decimal ChargeAmount { get; set; }
        public PaymentExceptionType Type {get;set;}
        public PaymentException(long organization,decimal chargeAmount,PaymentExceptionType type,String message=null):base(message ?? "An error occurred in making a payment.")
        {
            OrganizationId = organization;
            OccurredAt = DateTime.UtcNow;
            ChargeAmount = chargeAmount;
            Type = type;

        }
    }
}