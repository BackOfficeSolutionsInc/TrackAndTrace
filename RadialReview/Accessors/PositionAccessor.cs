using NHibernate;
using NHibernate.Criterion;
using RadialReview.Exceptions;
using RadialReview.Hooks;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public static List<OrganizationPositionModel> SearchPositions(UserOrganizationModel caller, long orgId, string query)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewOrganization(orgId);
                    var positions = s.QueryOver<OrganizationPositionModel>()
                        .Where(x => x.Organization.Id == orgId && x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.CustomName).IsInsensitiveLike(query, MatchMode.Anywhere)
                        .List().ToList();
                    return positions;
                }
            }
        }
        public List<PositionModel> AllPositions()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    return s.QueryOver<PositionModel>().List().OrderBy(x => x.Name.Translate()).ToList();
                }
            }
        }

        public static async Task DeletePosition(UserOrganizationModel caller, long id)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var orgPos = s.Get<OrganizationPositionModel>(id);
                    PermissionsUtility.Create(s, caller).EditPositions(orgPos.Organization.Id);

                    orgPos.DeleteTime = DateTime.UtcNow;

                    s.Update(orgPos);

                    tx.Commit();
                    s.Flush();

					await HooksRegistry.Each<IPositionHooks>((ses, x) => x.UpdatePosition(ses, orgPos, new IPositionHookUpdates() { WasDeleted = true }));
				}
            }
        }

        public async Task AddPositionToUser(UserOrganizationModel caller, long forUserId, long positionId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms =PermissionsUtility.Create(s, caller).Or(x => x.ManagesUserOrganization(forUserId, false), x => x.EditUserDetails(forUserId));
                    var position = s.Get<OrganizationPositionModel>(positionId);
                    if (position.Organization.Id != caller.Organization.Id)
                        throw new PermissionsException("Position not available.");

                    var pd = new PositionDurationModel(position, caller.Id, forUserId);

                    var forUser = s.Get<UserOrganizationModel>(forUserId);
                    forUser.Positions.Add(pd);
                    s.Update(forUser);
                    forUser.UpdateCache(s);

                    var template = UserTemplateAccessor._GetAttachedUserTemplateUnsafe(s, positionId, AttachType.Position);
                    if (template != null)
                        await UserTemplateAccessor._AddUserToTemplateUnsafe(s,perms, template.Organization, template.Id, forUserId, false);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public void RemovePositionFromUser(UserOrganizationModel caller, long positionDurationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var posDur = s.Get<PositionDurationModel>(positionDurationId);
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

        public static IEnumerable<long> GetPositionIdsForUser(ISession s, PermissionsUtility perms, long userId)
        {
            perms.ViewUserOrganization(userId, false);

            return s.QueryOver<PositionDurationModel>()
                .Where(x => x.DeleteTime == null && x.UserId == userId)
                .Select(x => x.Position.Id)
                .Future<long>();
        }

        public static IEnumerable<OrganizationPositionModel> GetPositionModelForUser(UserOrganizationModel caller, long userId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    
                    var positionIds = GetPositionIdsForUser(s, perms, userId).ToArray();

                    return s.QueryOver<OrganizationPositionModel>()
                        .Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(positionIds).List().ToList();
                }
            }
        }

        public List<PositionDurationModel> GetUsersWithPosition(UserOrganizationModel caller, long orgPositionId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var orgPos = s.Get<OrganizationPositionModel>(orgPositionId);
                    PermissionsUtility.Create(s, caller).ViewOrganization(orgPos.Organization.Id);


                    var positions = s.QueryOver<PositionDurationModel>().Where(x => x.Position.Id == orgPositionId && x.DeleteTime == null)
                        .List().ToList();

                    var deadUsers = s.QueryOver<UserOrganizationModel>()
                        .Where(x => x.DeleteTime != null)
                        .WhereRestrictionOn(x => x.Id).IsIn(positions.Select(x => x.UserId).Distinct().ToArray())
                        .Select(x => x.Id)
                        .List<long>().ToList();

                    return positions.Where(x => !deadUsers.Any(y => y == x.UserId)).ToList();

                }
            }
        }
        public static List<RoleModel> GetPositionRoles(UserOrganizationModel caller, long positionId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganizationPosition(positionId);

                    var roleLinks = s.QueryOver<RoleLink>().Where(x => x.AttachType == AttachType.Position 
                    && x.AttachId == positionId
                    && x.DeleteTime == null).Select(x => x.RoleId).List<long>().ToList();
                    
                    return s.QueryOver<RoleModel>().WhereRestrictionOn(x => x.Id).IsIn(roleLinks).List().ToList();                    
                }
            }
        }
    }
}