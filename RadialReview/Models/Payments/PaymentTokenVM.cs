using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Payments {
    public class PaymentTokenVM {
            
        public string id {get;set;}    
        public string @class {get;set;}
        public string card_type {get;set;}
        public string card_owner_name {get;set;}
        public string last_4 {get;set;}    
        public int card_exp_month {get;set;}    
        public int card_exp_year {get;set;}    
    }
}