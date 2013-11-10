using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class QuestionAccessor : BaseAccessor
    {
        public QuestionModel CreateQuestion(QuestionModel question)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    db.Save(question);
                    tx.Commit();
                    db.Flush();
                    //db.Questions.Add(question);
                    //db.SaveChanges();
                }
            }
            return question;
        }

        public void SetQuestionsEnabled(UserOrganizationModel caller, long forUserId, List<long> enabled, List<long> disabled)
        {
            //Yea this is pretty nasty.
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    caller = s.Get<UserOrganizationModel>(caller.Id);
                    if (!caller.ManagingUsers.Any(x => x.Id == forUserId))
                        throw new PermissionsException();

                    //Enable
                    var foundEnabled = s.GetByMultipleIds<QuestionModel>(enabled);
                    foreach (var f in foundEnabled)
                    {
                        var found = f.DisabledFor.ToList().FirstOrDefault(x => x.Long == forUserId);
                        if (found != null)
                        {
                            var newList = f.DisabledFor.ToList();
                            newList.RemoveAll(x => x.Long == forUserId);
                            f.DisabledFor = newList;
                            s.Delete(s.Load<LongModel>(found.Id));
                        }
                    }
                    tx.Commit();
                    s.Flush();
                }
                using (var tx = s.BeginTransaction())
                {
                    //Disable
                    var foundDisabled = s.GetByMultipleIds<QuestionModel>(disabled);
                    foreach (var f in foundDisabled)
                    {
                        if (!f.DisabledFor.Any(x => x.Long == forUserId))
                        {
                            f.DisabledFor.Add(new LongModel() { Long = forUserId });
                            s.SaveOrUpdate(f);
                        }
                    }
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public List<QuestionModel> GetQuestionsForUser(UserOrganizationModel caller, UserOrganizationModel forUser)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    if (!caller.GetManagingUsersAndSelf().Any(x => x.Id == forUser.Id))
                        throw new PermissionsException();
                    forUser = s.Get<UserOrganizationModel>(forUser.Id);
                    List<QuestionModel> questions = new List<QuestionModel>();
                    //Self Questions
                    questions.AddRange(forUser.CustomQuestions);
                    //Group Questions
                    questions.AddRange(forUser.Groups.SelectMany(x => x.CustomQuestions));
                    //Organization Questions
                    questions.AddRange(forUser.Organization.CustomQuestions);
                    //Application Questions
                    var applicationQuestions = s.QueryOver<ApplicationWideModel>().List().SelectMany(x => x.CustomQuestions).ToList();
                    questions.AddRange(applicationQuestions);
                    return questions;
                }
            }
        }

        public Boolean AttachQuestionToOrganization(UserModel owner, OrganizationModel organization, QuestionModel question)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    throw new Exception("owner.Id != userOrg.id");
                    /*
                    var user = db.Get<UserOrganizationModel>(owner.Id);//db.UserOrganizationModels.Find(owner.Id);
                    var asManager = organization.ManagersCanAddQuestions && user.ManagerAtOrganization.Any(x => x.Id == organization.Id);
                    var asOverseer = user.ManagingOrganizations.Any(x => x.Id == organization.Id);
                    if (asManager || asOverseer)
                    {
                        organization = db.Get<OrganizationModel>(organization.Id);//s db.Organizations.Attach(organization);
                        organization.CustomQuestions.Add(question);
                        db.SaveOrUpdate(organization);
                        tx.Commit();
                        db.Flush();
                        return true;
                    }
                    throw new PermissionsException();*/
                }
            }
        }

        public Boolean AttachQuestionToGroup(UserModel owner, GroupModel group, QuestionModel question)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    var user = db.Get<UserOrganizationModel>(owner.Id);//db.UserOrganizationModels.Find(owner.Id);
                    var asManager = group.Managers.Any(x => x.Id == group.Id);
                    if (asManager)
                    {
                        group = db.Get<GroupModel>(group.Id);// db.Groups.Attach(group);
                        group.CustomQuestions.Add(question);
                        db.SaveOrUpdate(group);
                        return true;
                    }
                    throw new PermissionsException();
                }
            }
        }

        public QuestionCategoryModel GetCategory(UserOrganizationModel user, long categoryId, Boolean overridePermissions)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var category = s.Get<QuestionCategoryModel>(categoryId);
                    if (!overridePermissions)
                    {
                        if (user.Organization.Id != category.Organization.Id)
                            throw new PermissionsException();
                    }
                    return category;
                }
            }
        }

        public QuestionModel EditQuestion(UserOrganizationModel caller, OriginType origin, long forOriginId, QuestionModel question)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    caller = s.Get<UserOrganizationModel>(caller.Id);
                    if (question.Id == 0) //Creating
                    {
                        question.DateCreated = DateTime.UtcNow;
                        question.CreatedBy = caller;
                    }
                    question.Category = s.Get<QuestionCategoryModel>(question.Category.Id);

                    if (question.Category.Organization.Id != caller.Organization.Id)
                        throw new PermissionsException(ExceptionStrings.CategoryAccessability);

                    switch (origin)
                    {
                        case OriginType.User:
                            {
                                PermissionsUtility.EditUserOrganization(s, caller, forOriginId);
                                var user = s.Get<UserOrganizationModel>(forOriginId);
                                question.ForUser = user;
                                break;
                            }
                        case OriginType.Group:
                            {
                                PermissionsUtility.EditGroup(s, caller, forOriginId);
                                var group = s.Get<GroupModel>(forOriginId);
                                question.ForGroup = group;
                                break;
                            }
                        case OriginType.Organization:
                            {
                                PermissionsUtility.EditOrganization(s, caller, forOriginId);
                                var org = s.Get<OrganizationModel>(forOriginId);
                                question.ForOrganization = org;
                                break;
                            }
                        case OriginType.Industry:
                            {
                                PermissionsUtility.EditIndustry(s, caller, forOriginId);
                                var ind = s.Get<IndustryModel>(forOriginId);
                                question.ForIndustry = ind;
                                break;
                            }
                        case OriginType.Application:
                            {
                                PermissionsUtility.EditApplication(s, caller, forOriginId);
                                var app = s.Get<ApplicationWideModel>(forOriginId);
                                question.ForApplication = app;
                                break;
                            }
                        default: throw new PermissionsException();
                    }
                    s.SaveOrUpdate(question);
                    tx.Commit();
                    s.Flush();
                }
            }
            return question;
        }

        public QuestionModel GetQuestion(UserOrganizationModel caller, long id)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var question=s.Get<QuestionModel>(id);
                    PermissionsUtility.ViewQuestion(s, caller, question);
                    return question;
                }
            }
        }


    }
}