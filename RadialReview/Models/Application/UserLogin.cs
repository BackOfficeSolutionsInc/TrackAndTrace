using Microsoft.AspNet.Identity.EntityFramework;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Models
{
    public class UserLogin : IdentityUserLogin, ILongIdentifiable
    {
        public long Id { get; protected set; }        
    }
}
