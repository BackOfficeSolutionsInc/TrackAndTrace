using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class QuestionsViewModel
    {
        public List<QuestionModel> Questions { get; set; }        
        public long OrganizationId { get; set; }

        public OriginType OriginType { get;set;}

        public long OriginId { get; set; }

        public QuestionsViewModel(long organizationId,OriginType originType,long originId,IEnumerable<QuestionModel> questions)
        {
            Questions = questions.ToList();
            OrganizationId = organizationId;
            OriginType = originType;
            OriginId = originId;
        }
    }
}