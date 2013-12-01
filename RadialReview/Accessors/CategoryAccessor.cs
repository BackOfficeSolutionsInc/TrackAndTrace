using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;

namespace RadialReview.Accessors
{
    public class CategoryAccessor : BaseAccessor
    {
        protected static OriginAccessor _OriginAccessor = new OriginAccessor();


        public QuestionCategoryModel Edit(UserOrganizationModel user,long categoryId,   Origin origin=null,
                                                                                        LocalizedStringModel categoryName=null,
                                                                                        Boolean? active=null,
                                                                                        DateTime? deleteTime=null)
        {
            QuestionCategoryModel category = new QuestionCategoryModel();
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var permissions=PermissionsUtility.Create(s, user);
                    permissions.EditOrganization(user.Organization.Id);
                    
                    if (categoryId != 0)
                    {
                        category = s.Get<QuestionCategoryModel>(categoryId);
                    }else{
                        if (origin == null || categoryName==null)
                            throw new PermissionsException();
                        category.Active = true;
                    }

                    if (origin != null){
                        permissions.EditOrigin(origin);                        
                        category.OriginType = origin.OriginType;
                        category.OriginId = origin.OriginId;
                    }

                    if(categoryName!=null){
                        category.Category = categoryName;
                    }

                    if (active!=null){
                        category.Active = active.Value;
                    }

                    if (deleteTime!=null){
                        category.DeleteTime = deleteTime;
                    }

                    s.SaveOrUpdate(category);
                    tx.Commit();
                    s.Flush();
                }
            }
            return category;
        }

        public QuestionCategoryModel Get(UserOrganizationModel user, long id)
        {
            using(var s=HibernateSession.GetCurrentSession())
            {
                using(var tx=s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, user).ViewCategory(id);

                    var category=s.Get<QuestionCategoryModel>(id);
                    return category;
                }
            }
        }

        public List<QuestionCategoryModel> GetCategories(UserOrganizationModel caller,long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    var category = s.QueryOver<QuestionCategoryModel>().Where(x=> x.OriginId==organizationId && x.OriginType==OriginType.Organization).List().ToList();
                    return category;
                }
            }
        }
        /*
        public List<QuestionCategoryModel> GetForOrganization(UserOrganizationModel user)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    return ;

                }
            }
        }*/
    }
}