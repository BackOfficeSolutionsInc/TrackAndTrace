using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.ViewModels;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Models;
using RadialReview.Models.FirePad;
using Microsoft.AspNet.Identity;

namespace RadialReview.Controllers
{
    public class FirePadController : BaseController
    {
        // GET: FirePad
        [Access(AccessLevel.UserOrganization)]
        public async Task<ActionResult> Index(string id,string text)
        {           
            FirePadData firePadData = PadAccessor.GetFirePadData(id,text); 
            return View(firePadData);
        }

    }
}