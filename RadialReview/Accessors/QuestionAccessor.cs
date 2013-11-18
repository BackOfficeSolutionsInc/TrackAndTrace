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
                    PermissionsUtility.Create(s, caller).OwnedBelowOrEqual(x => x.Id == forUserId);

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
                    if (!caller.AllSubordinatesAndSelf().Any(x => x.Id == forUser.Id))
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

        public QuestionCategoryModel GetCategory(UserOrganizationModel caller, long categoryId, Boolean overridePermissions)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var category = s.Get<QuestionCategoryModel>(categoryId);
                    if (!overridePermissions)
                    {
                        PermissionsUtility.Create(s, caller).ViewOrigin(category.OriginType, category.OriginId);
                    }
                    return category;
                }
            }
        }

        public QuestionModel EditQuestion(UserOrganizationModel caller, long questionId,Origin origin=null,
                                                                                        LocalizedStringModel question=null,
                                                                                        long? categoryId=null,
                                                                                        DateTime? deleteTime=null)
        {
            QuestionModel q;
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    caller = s.Get<UserOrganizationModel>(caller.Id);
                    q = s.Get<QuestionModel>(questionId);

                    var permissionUtility=PermissionsUtility.Create(s, caller);

                    if (q.Id == 0) //Creating
                    {
                        q.DateCreated = DateTime.UtcNow;
                        q.CreatedBy = caller;
                    }
                    else
                    {
                        permissionUtility.EditQuestion(q);
                    }

                    /*if (question.CreatedBy.Id == caller.Id)
                        question.CreatedBy = caller;*/
     //Edit Origin
                    if (origin!=null)
                    {
                        permissionUtility.EditOrigin(origin);
                        q.OriginId=origin.OriginId;
                        q.OriginType = origin.OriginType;
                    }
     //Edit Question
                    if (question!=null)
                    {
                        q.Question = question;
                    }
     //Edit CategoryId
                    if (categoryId!=null )
                    {
                        if(categoryId==0)
                            throw new PermissionsException();
                        permissionUtility.PairCategoryToQuestion(categoryId.Value,questionId);
                        var category= s.Get<QuestionCategoryModel>(categoryId);
                        q.Category=category;
                    }             
     //Edit DeleteTime
                    if(deleteTime!=null)
                    {
                        q.DeleteTime = deleteTime.Value;
                    }
                    s.SaveOrUpdate(question);
                    tx.Commit();
                    s.Flush();
                }
            }
            return q;
        }

        public QuestionModel GetQuestion(UserOrganizationModel caller, long id)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var question=s.Get<QuestionModel>(id);
                    PermissionsUtility.Create(s, caller).ViewQuestion(question);
                    return question;
                }
            }
        }


    }
}