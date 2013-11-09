using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

using RadialReview.Models;
using RadialReview.Models.Json;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using RadialReview;

namespace RadialReview.Controllers
{
    public class QuestionController : BaseController
    {
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static GroupAccessor _GroupAccessor = new GroupAccessor();
        protected static UserAccessor _UserAccessor = new UserAccessor();
        protected static QuestionAccessor _QuestionAccessor = new QuestionAccessor();


        public JsonResult AddQuestion(long organizationId, String question,  long categoryId, String questionType, long forId)
        {
            try
            {
                var caller = GetOneUserOrganization(organizationId);
                var category = _QuestionAccessor.GetCategory(caller, categoryId, false);

                var q = new QuestionModel()
                {
                    Category = category,
                    Question = question
                };
                _QuestionAccessor.EditQuestion(caller, questionType.Parse<OriginType>(), forId, q);
                
                return Json(JsonObject.Success);
            }
            catch (Exception e)
            {
                return Json(new JsonObject(true, e.Message));
            }
        }

        /*private ApplicationDbContext db = new ApplicationDbContext();

        // GET: /Question/
        public ActionResult Index()
        {
            return View(db.Questions.ToList());
        }

        // GET: /Question/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            QuestionModel questionmodel = db.Questions.Find(id);
            if (questionmodel == null)
            {
                return HttpNotFound();
            }
            return View(questionmodel);
        }

        // GET: /Question/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /Question/Create
		// To protect from over posting attacks, please enable the specific properties you want to bind to, for 
		// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
		// 
		// Example: public ActionResult Update([Bind(Include="ExampleProperty1,ExampleProperty2")] Model model)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(QuestionModel questionmodel)
        {
            GetUser();

            if (ModelState.IsValid)
            {
                db.Questions.Add(questionmodel);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(questionmodel);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }*/
    }
}
