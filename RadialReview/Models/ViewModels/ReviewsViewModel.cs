﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class ReviewsViewModel
    {
        public ReviewsModel Review { get; set; }

        public ReviewsViewModel(ReviewsModel reviewCollection)
        {
            Review = reviewCollection;
        }
    }
}