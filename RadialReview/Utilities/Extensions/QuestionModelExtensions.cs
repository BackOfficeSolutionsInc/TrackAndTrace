using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class QuestionModelExtensions
    {
        /*public static IOrigin Origin(this QuestionModel self)
        {
                if (self.ForApplication != null) return self.ForApplication;
                else if (self.ForIndustry != null) return self.ForIndustry;
                else if (self.ForOrganization != null) return self.ForOrganization;
                else if (self.ForGroup != null) return self.ForGroup;
                else if (self.ForUser != null) return self.ForUser;
                return null;
        }*/
        /*public static OriginType GetOriginType(this QuestionModel self)
        {


                if (self._OriginType.HasValue)
                    return self._OriginType.Value;
                if (self.ForApplication != null)        return OriginType.Application;
                else if (self.ForIndustry != null)      return OriginType.Industry;
                else if (self.ForOrganization != null)  return OriginType.Organization;
                else if (self.ForGroup != null)         return OriginType.Group;
                else if (self.ForUser != null)          return OriginType.User;
                else                                    return OriginType.Invalid;
        }*/

       /* public static void SetOriginType(this QuestionModel self, OriginType originType)
        {
            self._OriginType = originType;
        }
        */
        public static long CategoryId(this QuestionModel self)
        {
            return self.Category.NotNull(x => x.Id);
        }

        public static String GetQuestion(this QuestionModel self){
            return self.NotNull(x=>x.Question.NotNull(y=>y.Translate()))??"";
        }
    }
}