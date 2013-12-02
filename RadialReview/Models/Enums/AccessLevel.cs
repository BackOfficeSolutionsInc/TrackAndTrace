using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Controllers
{
    public enum AccessLevel
    {
        Any = 1,
        User = 2,
        UserOrganization = 3,
        Manager = 4,
    }
}
