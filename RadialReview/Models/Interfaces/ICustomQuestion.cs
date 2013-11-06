
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Models.Interfaces
{
    public interface ICustomQuestions
    {
        IList<QuestionModel> CustomQuestions { get;set; }
    }
}
