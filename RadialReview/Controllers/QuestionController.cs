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
        protected static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
        protected static OriginAccessor _OriginAccessor = new OriginAccessor();


        [HttpPost]
        public JsonResult Edit(long questionId, long organizationId, String question, long categoryId, String originType, long forOriginId,String questionType)
        {
            try
            {
                var caller = GetOneUserOrganization(organizationId);
                var category = _QuestionAccessor.GetCategory(caller, categoryId, false);

                QuestionModel q = new QuestionModel();
                if (questionId != 0)
                    q = _QuestionAccessor.GetQuestion(caller, questionId);
                
                q.Category = category;
                q.Question.UpdateDefault(question);

                q.QuestionType = questionType.Parse<QuestionType>();
                var origin=new Origin(originType.Parse<OriginType>(), forOriginId);
                _QuestionAccessor.EditQuestion(caller, questionId, origin:      origin, 
                                                                   question:    q.Question,
                                                                   categoryId:  categoryId);

                return Json(JsonObject.Success);
            }
            catch (Exception e)
            {
                return Json(new JsonObject(e));
            }
        }
                
        public JsonResult Delete(long id, long organizationId)
        {
            try
            {
                var caller = GetOneUserOrganization(organizationId);
                //var q = _QuestionAccessor.GetQuestion(caller, id);
                _QuestionAccessor.EditQuestion(caller,id, deleteTime: DateTime.UtcNow);
                return Json(JsonObject.Success,JsonRequestBehavior.AllowGet);
            }catch(Exception e){
                return Json(new JsonObject(e), JsonRequestBehavior.AllowGet);
            }
        }
        
        public ActionResult Modal(long organizationId,long id=0, String origin = null, long? originId = null)
        {
            try
            {
                var caller = GetOneUserOrganization(organizationId).Hydrate()
                    .Organization(questions: true)
                    .ManagingGroups(questions:true)
                    .Execute();
                QuestionModel q = null;
                QuestionModalViewModel questionViewModel = null;

                if (id != 0)
                {
                    q = _QuestionAccessor.GetQuestion(caller, id);
                    questionViewModel = new QuestionModalViewModel(caller.Organization, q.OriginId, q.OriginType, false, q);
                    throw new NotImplementedException();
                }
                else
                {
                    if (origin == null || originId == null)
                        throw new Exception("New question requires an origin information.");
                    var originType = origin.Parse<OriginType>();
                    q = new QuestionModel();
                    q.OriginType=originType;
                    _OriginAccessor.GetOrigin(caller, originType, originId.Value); //To ensure that we have access to this origin.
                    questionViewModel = new QuestionModalViewModel(caller.Organization, originId.Value,originType,true, q);
                }
                return PartialView(questionViewModel);
            }catch(Exception e)
            {
                return PartialView("ModalError", e);
            }
        }

        public ActionResult Admin(long id=0,long? organizationId=null)
        {
            ViewBag.originType = OriginType.Invalid;
            ViewBag.originId =   0;
            if (id != 0)
            {
                var caller = GetOneUserOrganization(organizationId);
                var question = _QuestionAccessor.GetQuestion(caller, id);
                ViewBag.originType = question.OriginType;
                ViewBag.originId = question.OriginId;
                return View(question);
            }
            return View();
        }

        [HttpPost]
        public ActionResult Admin(QuestionModel model,String question,OriginType originType,long originId,long? organizationId)
        {
            if (originId == 0)
                throw new Exception("Need origin id");
            if (originType == OriginType.Invalid)
                throw new Exception("Cannot be invalid");

            var caller=GetOneUserOrganization(organizationId);
            var origin=new Origin(originType, originId);
            var q = _QuestionAccessor.GetQuestion(caller, model.Id);

            q.Question.UpdateDefault(question);

            _QuestionAccessor.EditQuestion(caller,model.Id,origin,q.Question,model.Category.Id);
            return RedirectToAction("Admin", new { id = model.Id,organizationId=organizationId});
        }

    }
}
