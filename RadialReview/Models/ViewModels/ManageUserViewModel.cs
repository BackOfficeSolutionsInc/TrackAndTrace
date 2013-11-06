using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class ManagerUserViewModel
    {
        public UserOrganizationModel User { get; set; }
        public List<QuestionModel> MatchingQuestions { get;set; }

        public long OrganizationId { get;set;}

    }
}