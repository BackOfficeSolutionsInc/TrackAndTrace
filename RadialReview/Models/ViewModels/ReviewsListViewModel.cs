using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class ReviewsListViewModel : IPagination
    {
        public UserOrganizationModel ForUser { get;set;}
        public List<ReviewModel> Reviews { get; set; }

        public double NumPages { get; set; }

        public int Page { get; set; }
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