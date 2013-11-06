using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Json;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    

    public class UserController : BaseController
    {
        public class SaveUserModel{
            public class Tup {
                public long Id { get; set; }
                public string Value { get; set; }
                public string Type { get; set; }
            }

            public Tup[] toSave { get; set; }
            public long forUser {get;set;}
            public long OrganizationId { get;set; }
        }


        private static QuestionAccessor _QuestionAccessor = new QuestionAccessor();


        public ActionResult Index()
        {
            return View();
        }
        
        public ActionResult Manage(long id,long? organizationId)
        {
            var caller=GetOneUserOrganization(organizationId)
                        .Hydrate()
                        .ManagingUsers()
                        .Execute();
            var found=caller.ManagingUsers.FirstOrDefault(x => x.Id == id);
            if (found == null)
                throw new PermissionsException();


            return View(new ManagerUserViewModel()
            {
                MatchingQuestions = _QuestionAccessor.GetQuestionsForUser(caller, found),
                User = found,
                OrganizationId=caller.Organization.Id
            });
        }

        public ActionResult Save(SaveUserModel save)
        {
            try
            {
                var user = GetOneUserOrganization(save.OrganizationId);
                if (user == null)
                    return Json(new JsonObject(true, ExceptionStrings.DefaultPermissionsException));
                if (save.toSave == null)
                    return Json(JsonObject.Success);

                var questionsToEdit = save.toSave.Where(x => x.Type == "questionEnabled").ToList();
                var enabledQuestions = questionsToEdit.Where(x => x.Value == "true").Select(x => x.Id).ToList();
                var disabledQuestions = questionsToEdit.Where(x => x.Value == "false").Select(x => x.Id).ToList();
                _QuestionAccessor.SetQuestionsEnabled(user, save.forUser, enabledQuestions, disabledQuestions);

                return Json(JsonObject.Success);
            }
            catch (Exception e)
            {
                return Json(new JsonObject(e));
            }
        }
    }
}
