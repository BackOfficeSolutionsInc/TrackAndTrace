using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Models.Enums
{
    public enum AboutType : long
    {
        None        = 0,
        Self        = 1,
        Peer        = 2,
        Teammate    = 4,
        Manager     = 8,
        Subordinate = 16,
    }

}
