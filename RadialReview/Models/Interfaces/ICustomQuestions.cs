
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Models.Interfaces
{
    public interface ICustomQuestions : ILongIdentifiable
    {
        IList<QuestionModel> CustomQuestions { get;set; }
        /// <summary>
        /// Organization,User,Application,Group
        /// </summary>
        OriginType QuestionOwner { get; }
    }
}
