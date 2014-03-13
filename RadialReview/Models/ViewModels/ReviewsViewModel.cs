using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class ReviewsViewModel 
    {

        public ReviewsModel Review {get;set;}

        public long? TakableId { get; set; }
        public bool Viewable { get; set; }
        public bool Editable { get; set; }

        //public Dictionary<long,RatioModel> Completed { get; set; }
        //public RatioModel Signed { get; set; }
        
        public ReviewsViewModel(ReviewsModel review)
        {
            Review = review;
        }

    }
}