using RadialReview.Exceptions;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities;
using NHibernate.Linq;

namespace RadialReview.Accessors
{
    public class GroupAccessor
    {

        public GroupModel Get(UserOrganizationModel caller, long groupId)
        {
            using (var session = HibernateSession.GetCurrentSession())
            {
                using (var db = session.BeginTransaction())
                {
                    var owner=session.Get<UserOrganizationModel>(caller.Id);

                    var group = owner.Groups.FirstOrDefault(x => x.Id == groupId);
                    if (group == null)
                    {
                        group=owner.ManagingGroups.FirstOrDefault(x=>x.Id==groupId);
                        if (group==null)
                            throw new PermissionsException();
                    }

                    var result = session.Query<GroupModel>().Where(x => x.Id == groupId).FetchMany(x => x.GroupUsers).ThenFetch(x=>x.User).ToFuture();
                    session.Query<GroupModel>().Where(x => x.Id == groupId).FetchMany(x => x.CustomQuestions).ToFuture();
                    session.Query<GroupModel>().Where(x => x.Id == groupId).FetchMany(x => x.Managers).ToFuture();
                    return result.AsEnumerable().SingleOrDefault();
                }
            }
        }

        public GroupModel Edit(UserOrganizationModel createdBy,GroupModel group)
        {
            using(var session = HibernateSession.GetCurrentSession())
            {
                using(var transaction=session.BeginTransaction())
                {
                    if (group.Id != 0 && !createdBy.ManagingGroups.Any(x => x.Id == group.Id))
                        throw new PermissionsException();

                    if (group.Id == 0 && !group.Managers.Any(x=>x.Id==createdBy.Id)){
                        group.Managers.Add(createdBy);
                    }

                    session.SaveOrUpdate(group);
                    transaction.Commit();
                    session.Flush();
                    return group;
                }
            }
        }
    }
}