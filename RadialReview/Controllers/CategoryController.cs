using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class CategoryController : BaseController
    {
        private static CategoryAccessor _CategoryAccessor = new CategoryAccessor();

        //
        // GET: /Category/
        [Access(AccessLevel.Manager)]
        public ActionResult Index()
        {
            var user=GetUser()
                .Hydrate()
                .Organization(questions:true)
                .Execute();



            ManagerAndCanEditOrException(user);

            var categories = user.Organization.QuestionCategories;// _CategoryAccessor.GetForOrganization(user);

            return View(categories);
        }

        //
        // GET: /Category/Details/5
        /*public ActionResult Details(int id)
        {
            return View();
        }*/

        //
        // GET: /Category/Create
        [Access(AccessLevel.Manager)]
        public ActionResult Create()
        {
            var user = GetUser();
            return View("Edit", new QuestionCategoryModel() { 
                OriginType=OriginType.Organization,
                OriginId = user.Organization.Id 
            });
        }
        
        //
        // GET: /Category/Edit/5
        [Access(AccessLevel.Manager)]
        public ActionResult Edit(int id)
        {
            var user = GetUser().Hydrate().Organization(questions:true).Execute();
            ManagerAndCanEditOrException(user);
            var category=_CategoryAccessor.Get(user, id);

            return View(category);
        }

        //
        // POST: /Category/Edit/5
        [HttpPost]
        [Access(AccessLevel.Manager)]
        public ActionResult Edit(int id, QuestionCategoryModel collection)
        {
            try
            {
                //collection.Organization.Id = long.Parse(Request.Params["Organization.Id"]);

                var user=GetUser();
                //EditableOrException(user);
                //var category=_CategoryAccessor.Get(user,id);
                var origin = new Origin(collection.OriginType, collection.OriginId);
                _CategoryAccessor.Edit(user, id, origin, collection.Category, collection.Active);
                return RedirectToAction("Index");
            }
            catch
            {
                return View(collection);
            }
        }

        //
        // GET: /Category/Delete/5
        /*public ActionResult Delete(int id)
        {
            return View();
        }*/

        //
        // POST: /Category/Delete/5
        /*[HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }*/
    }
}
