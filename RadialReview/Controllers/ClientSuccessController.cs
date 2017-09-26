using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models.ClientSuccess;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
    public class ClientSuccessController : BaseController {
        // GET: ClientSuccess
        [Access(AccessLevel.UserOrganization)]
        public JsonResult MarkTooltip(long id) {
            try {
                SupportAccessor.MarkTooltipSeen(GetUser(), id);
                return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
            } catch (Exception e) {
                var result = ResultObject.SilentSuccess(e);
                result.Error = true;
                result.ForceNoErrorReport();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        [Access(AccessLevel.Radial)]
        public ActionResult Index() {
            return View();
        }

        [Access(AccessLevel.Radial)]
        public ActionResult Tooltips() {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var tips = s.QueryOver<TooltipTemplate>().List().ToList();
                    return View(tips);
                }
            }
        }
        [Access(AccessLevel.Radial)]
        public PartialViewResult Modal(long id = 0) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var tip = s.Get<TooltipTemplate>(id) ?? new TooltipTemplate();
                    return PartialView(tip);
                }
            }
        }
        [Access(AccessLevel.Radial)]
        [HttpPost]
        public JsonResult Modal(TooltipTemplate model) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    s.Merge(model);
                    tx.Commit();
                    s.Flush();
                    return Json(ResultObject.Create(model));
                }
            }
        }
    }
}