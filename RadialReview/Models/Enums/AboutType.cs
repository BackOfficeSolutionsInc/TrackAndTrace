using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Models.Enums
{
    [Flags]
    public enum AboutType : long
    {
        NoRelationship  = 0,
        OldSelf         = 1,
        Subordinate     = 2,
        Teammate        = 4,
        Peer            = 8,
        Manager         = 16,
        Self            = 32,
    }

}
