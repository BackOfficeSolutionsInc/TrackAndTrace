using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class AnswerModel
    {
        public long Id { get; set; }
        public virtual QuestionModel Question { get; set; }

        public virtual long Answer { get; set; }

    }
}