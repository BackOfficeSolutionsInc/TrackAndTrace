using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class ReviewsListViewModel
    {
        public UserOrganizationModel ForUser { get;set;}
        public List<ReviewModel> Reviews { get; set; }
    }
    /*
    public class ReviewViewModel
    {
        public long Id { get; set; }
        public String Name { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Completion { get; set; }
        public bool Complete { get; set; } 
        public bool FullyComplete { get; set; }
    }*/
}