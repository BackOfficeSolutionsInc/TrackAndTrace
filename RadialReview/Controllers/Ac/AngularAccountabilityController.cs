using RadialReview.Accessors;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public partial class AccountabilityController : BaseController
    {
        // GET: AngularAccountability
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult UpdateAngularAccountabilityNode(AngularAccountabilityNode model, string connectionId = null)
        {
            AccountabilityAccessor.Update(GetUser(), model, connectionId);
            return Json(ResultObject.SilentSuccess());
        }
    }
}