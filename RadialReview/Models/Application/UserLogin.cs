using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Models
{
    public class UserLogin : IdentityUserLogin
    {
        public long Id { get; protected set; }        
    }
}
