using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Enums;

namespace RadialReview.Models.ViewModels
{
    public class QuestionModalViewModel
    {
        public List<QuestionCategoryModel> Categories { get; set; }
        public long OriginId { get; set; }
        public OriginType OriginType { get; set; }
        public long OrganizationId { get; set; }
        public QuestionModel Question { get; set; }

        public QuestionModalViewModel(OrganizationModel organization, long originId, OriginType originType, QuestionModel question=null)
        {
            Question = question ?? new QuestionModel();
            Categories      = organization.QuestionCategories.ToList();
            OriginId = originId;
            OriginType= originType;
            OrganizationId = organization.Id;
        }
    }
}