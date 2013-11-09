using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.ViewModels
{
    public class QuestionModalViewModel
    {
        public List<QuestionCategoryModel> Categories { get; set; }
        public ICustomQuestions QuestionOwner { get; set; }
        public long OrganizationId { get;set;}
    }
}