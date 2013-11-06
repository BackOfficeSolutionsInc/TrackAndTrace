﻿using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class OrganizationAccessor
    {
        public void CreateOrganization(UserModel user, OrganizationModel organization)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    db.Save(organization);
                    //db.Organizations.Add(organization);
                    //db.SaveChanges();
                    //db.UserModels.Attach(user);
                    user = db.Get<UserModel>(user.Id);

                    var userOrgModel = new UserOrganizationModel()
                    {
                        Organization = organization,
                        User = user,
                        ManagerAtOrganization = true,
                        ManagingOrganization = true,
                    };

                    //userOrgModel.ManagingOrganizations.Add(organization);
                    //userOrgModel.BelongingToOrganizations.Add(organization);
                    //userOrgModel.ManagerAtOrganization.Add(organization);

                    user.UserOrganization.Add(userOrgModel);

                    //organization.ManagedBy.Add(userOrgModel);
                    organization.Members.Add(userOrgModel);

                    db.Save(user);
                    tx.Commit();
                    db.Flush();

                    //db.UserOrganizationModels.Add(userOrgModel);
                    //db.SaveChanges();

                    //organization.ManagedBy.Add(userOrgModel);
                    //db.SaveChanges();
                }
            }
        }

        public UserOrganizationModel JoinOrganization(UserModel user, long managerId,long userOrgPlaceholder)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    var manager = db.Get<UserOrganizationModel>(managerId);
                    var orgId = manager.Organization.Id;
                    var organization = db.Get<OrganizationModel>(orgId);
                    user = db.Get<UserModel>(user.Id);
                    var userOrg = db.Get<UserOrganizationModel>(userOrgPlaceholder);

                    userOrg.User= user;
                    userOrg.Organization = organization;

                    user.UserOrganization.Add(userOrg);
                    manager.ManagingUsers.Add(userOrg);
                    organization.Members.Add(userOrg);

                    db.SaveOrUpdate(user);

                    tx.Commit();
                    db.Flush();
                    return userOrg;
                }
            }
        }

        public QuestionModel EditQuestion(UserOrganizationModel user, QuestionModel question)
        {
            if (!user.IsManagerCanEdit)//(!user.IsManager || !user.Organization.ManagersCanEdit)
                throw new PermissionsException();

            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var org=s.Get<OrganizationModel>(user.Organization.Id);
                    if (question.Id == 0) //Creating
                    {
                        question.DateCreated = DateTime.UtcNow;
                        user = s.Get<UserOrganizationModel>(user.Id);
                        question.CreatedBy = user;
                    }
                    question.Category = s.Get<QuestionCategoryModel>(question.Category.Id);
                    if (question.Category.Organization.Id != user.Organization.Id)
                        throw new PermissionsException();


                    question.ForOrganization=org;

                    s.SaveOrUpdate(question);
                    tx.Commit();
                    s.Flush();
                }
            }
            return question;
        }

        public QuestionCategoryModel GetCategory(UserOrganizationModel user, long categoryId,Boolean overridePermissions)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var category=s.Get<QuestionCategoryModel>(categoryId);
                    if (!overridePermissions)
                    {
                        if (user.Organization.Id != category.Organization.Id)
                            throw new PermissionsException();
                    }
                    return category;
                }
            }
        }
    }
}