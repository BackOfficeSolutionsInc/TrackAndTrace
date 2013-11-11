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
                return Json(new JsonObject(e));
            }
        }

        public ActionResult Modal(long id, long organizationId, String origin = null, long? originId = null)
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
                    questionViewModel = new QuestionModalViewModel(caller.Organization, q.Origin.Id, q.OriginType,false, q);
                }
                else
                {
                    if (origin == null || originId == null)
                        throw new Exception("New question requires an origin information.");
                    var originType = origin.Parse<OriginType>();
                    q = new QuestionModel();
                    q.OriginType = originType;
                    _OriginAccessor.GetOrigin(caller, originType, originId.Value); //To ensure that we have access to this origin.
                    questionViewModel = new QuestionModalViewModel(caller.Organization, originId.Value,originType,true, q);
                }
                return PartialView(questionViewModel);
            }catch(Exception e)
            {
                return PartialView("ModalError", e);
            }
        }
    }
}
