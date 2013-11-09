using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Exceptions;

namespace RadialReview.Accessors
{
    public class CategoryAccessor
    {

        public QuestionCategoryModel Edit(UserOrganizationModel user, QuestionCategoryModel category)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    if (category.Id == 0)
                        category.Organization = user.Organization;

                    if (!user.IsManagerCanEditOrganization || category.Organization.Id != user.Organization.Id)
                        throw new PermissionsException();
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
                    var category=s.Get<QuestionCategoryModel>(id);
                    if (category.Organization.Id != user.Organization.Id)
                        throw new PermissionsException();
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