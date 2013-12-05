using RadialReview.Exceptions;
using RadialReview.Models;
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
                    PermissionsUtility.Create(s, caller).EditUserOrganization(forUserId);
                    var position = s.Get<OrganizationPositionModel>(positionId);

                    var pd = new PositionDurationModel(position, caller.Id, forUserId);

                    var forUser=s.Get<UserOrganizationModel>(forUserId);
                    forUser.Positions.Add(pd);
                    s.Update(forUser);
                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void RemovePositionFromUser(UserOrganizationModel caller,long forUserId,long positionDurationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditUserOrganization(forUserId);
                    var positionDuration = s.Get<PositionDurationModel>(positionDurationId);

                    if (positionDuration.DeleteTime != null)
                        throw new PermissionsException();

                    positionDuration.DeleteTime = DateTime.UtcNow;
                    positionDuration.DeletedBy = caller.Id;
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