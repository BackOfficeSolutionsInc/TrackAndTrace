using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Enums
{
    public enum NexusActions : int
    {
        None = 0,
        JoinOrganizationUnderManager = 1, //[organizationId,EmailAddress,userOrgId,Firstname,Lastname]
        TakeReview = 2, //[]
    }
}