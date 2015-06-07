using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class PositionAccessor : BaseAccessor
    {

        /*public List<PositionModel> Search(String search)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    s.QueryOver<PositionModel>().Where(x=>x.Name.Default.Value.Contains(search))
                    tx.Commit();
                    s.Flush();
                }
            }
        }*/

        public List<PositionModel> AllPositions()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    return s.QueryOver<PositionModel>().List().OrderBy(x=>x.Name.Translate()).ToList();
                }
            }
        }

        public void AddPositionToUser(UserOrganizationModel caller,long forUserId,long positionId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
					PermissionsUtility.Create(s, caller).Or(x => x.ManagesUserOrganization(forUserId, false), x => x.EditUserDetails(forUserId));
                    var position = s.Get<OrganizationPositionModel>(positionId);
					if (position.Organization.Id!= caller.Organization.Id)
						throw new PermissionsException("Position not available.");

                    var pd = new PositionDurationModel(position, caller.Id, forUserId);

                    var forUser=s.Get<UserOrganizationModel>(forUserId);
                    forUser.Positions.Add(pd);
					s.Update(forUser);
					forUser.UpdateCache(s);

	                var template=UserTemplateAccessor._GetAttachedUserTemplateUnsafe(s, positionId, AttachType.Position);
					if (template!=null)
						UserTemplateAccessor._AddUserToTemplateUnsafe(s,template.Organization,template.Id,forUserId,false);
					
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void RemovePositionFromUser(UserOrganizationModel caller,long positionDurationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var posDur=s.Get<PositionDurationModel>(positionDurationId);
					PermissionsUtility.Create(s, caller).Or(x => x.ManagesUserOrganization(posDur.UserId, false), x => x.EditUserDetails(posDur.UserId));
                    if (posDur.DeleteTime != null)
                        throw new PermissionsException();

                    posDur.DeleteTime = DateTime.UtcNow;
                    posDur.DeletedBy = caller.Id;
                    s.Update(posDur);

					s.Get<UserOrganizationModel>(posDur.UserId)
						.UpdateCache(s);

					var template = UserTemplateAccessor._GetAttachedUserTemplateUnsafe(s, posDur.Position.Id, AttachType.Position);

					if (template != null)
						UserTemplateAccessor._RemoveUserToTemplateUnsafe(s, template.Id, posDur.UserId);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public List<PositionDurationModel> GetUsersWithPosition(UserOrganizationModel caller,long orgPositionId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var orgPos=s.Get<OrganizationPositionModel>(orgPositionId);
                    PermissionsUtility.Create(s, caller).ViewOrganization(orgPos.Organization.Id);
                    return s.QueryOver<PositionDurationModel>().Where(x => x.Position.Id == orgPositionId).List().ToList();
                }
            }
        }


    }
}