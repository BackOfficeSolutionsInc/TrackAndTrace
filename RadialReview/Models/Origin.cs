using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class Origin
    {
        public virtual OriginType OriginType { get;set;}
        public virtual long OriginId { get;set; }

        public Origin()
        {

        }
        public Origin(OriginType originType, long originId):this()
        {
            OriginType = originType;
            OriginId = originId;
        }

        public Boolean AreEqual(IOrigin other)
        {
            return (other.Id == OriginId && other.GetOriginType() == OriginType);
        }
    }
}