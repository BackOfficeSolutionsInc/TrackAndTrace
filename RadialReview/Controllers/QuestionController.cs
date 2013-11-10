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
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public class QuestionController : BaseController
    {
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static GroupAccessor _GroupAccessor = new GroupAccessor();
        protected static UserAccessor _UserAccessor = new UserAccessor();
        protected static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
        protected static OriginAccessor _OriginAccessor = new OriginAccessor();


        [HttpPost]
        public JsonResult Edit(String question, long categoryId, long questionId, long organizationId, String questionType, long forOriginId)
        {
            try
            {
                var caller = GetOneUserOrganization(organizationId);
                var category = _QuestionAccessor.GetCategory(caller, categoryId, false);

                QuestionModel q = null;
                if (questionId != 0)
                    q = _QuestionAccessor.GetQuestion(caller, questionId);

                if (q == null)
                    q = new QuestionModel();

                q.Category = category;
                q.Question = question;

                _QuestionAccessor.EditQuestion(caller, questionType.Parse<OriginType>(), forOriginId, q);

                return Json(JsonObject.Success);
            }
            catch (Exception e)
            {
                return Json(new JsonObject(true, e.Message));
            }
        }

        public ActionResult Modal(long id, long organizationId, String origin = null, long? originId = null)
        {
            try
            {
                var caller = GetOneUserOrganization(organizationId).Hydrate().Organization(questions: true).Execute();
                QuestionModel q = null;

                QuestionModalViewModel questionViewModel = null;

                if (id != 0)
                {
                    q = _QuestionAccessor.GetQuestion(caller, id);
                    questionViewModel = new QuestionModalViewModel(caller.Organization, q.Origin.Id, q.OriginType, q);
                }
                else
                {
                    if (origin == null || originId == null)
                        throw new Exception("New question requires an origin information.");
                    var originType = origin.Parse<OriginType>();
                    q = new QuestionModel();
                    q.OriginType = originType;
                    _OriginAccessor.GetOrigin(caller, originType, originId.Value); //To ensure that we have access to this origin.
                    questionViewModel = new QuestionModalViewModel(caller.Organization, originId.Value,originType, q);
                }
                return PartialView(questionViewModel);
            }catch(Exception e)
            {
                return RedirectToAction("Modal", "Error", e);
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
