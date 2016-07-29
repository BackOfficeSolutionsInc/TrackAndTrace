using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.Extensions;
using NHibernate.Envers.Query;

namespace RadialReview.Accessors.Hooks {
    public class CreateUserOrganization_UpdateHierarchy : ICreateUserOrganizationHook,IDeleteUserOrganizationHook {

        public void CreateUser(ISession s, UserOrganizationModel newUser)
        {
            var organizationId = newUser.Organization.Id;
            var chartsF = s.QueryOver<AccountabilityChart>().Where(x => x.OrganizationId == newUser.Organization.Id && x.DeleteTime==null).Future();
            
            var parentIds = newUser.ManagedBy.Select(x => x.ManagerId).ToArray();
            var parentF = s.QueryOver<AccountabilityNode>().Where(x => x.OrganizationId == newUser.Organization.Id && x.DeleteTime == null)
                .WhereRestrictionOn(x=>x.UserId).IsIn(parentIds)
                .Future();

            var posId =newUser.Positions.NotNull(x=>x.FirstOrDefault());
            IEnumerable<AccountabilityRolesGroup> groupF = null;
            if (posId!=null){
                groupF = s.QueryOver<AccountabilityRolesGroup>().Where(x => x.OrganizationId == newUser.Organization.Id && x.PositionId == posId.Id && x.DeleteTime == null).Future();
            }

            var charts = chartsF.ToList();
            var parents = parentF.ToList();
            var groups =new List<AccountabilityRolesGroup>();
            if (groupF!=null)
                groups = groupF.ToList();



            foreach (var chart in charts) {
                if (newUser.ManagingOrganization) {
                    parents.Add(s.Get<AccountabilityNode>(chart.RootId));
                }
                foreach (var pid in parents.Where(x => x.AccountabilityChartId == chart.Id)) {
                    var first = groups.FirstOrDefault(x => x.AccountabilityChartId == chart.Id);
                    long? gid = null;
                    if (first!=null)
                        gid = first.Id;

                    var node = AccountabilityAccessor.AppendNode(s, PermissionsUtility.Create(s, UserOrganizationModel.ADMIN), pid.Id, gid);
                    node.UserId = newUser.Id;
                    node.User = newUser;
                    s.Update(node);
                }
            }
        }


        public void DeleteUser(ISession s, UserOrganizationModel user)
        {
            var nodes = s.QueryOver<AccountabilityNode>()
                .Where(x => x.OrganizationId == user.Organization.Id && x.DeleteTime == null && x.UserId==user.Id)
                .List().ToList();

            foreach (var n in nodes) {
                n.UserId = null;
                s.Update(n);
            }

        }


        public void UndeleteUser(ISession s, UserOrganizationModel user)
        {
            //MAYBE THINK ABOUT RE-ADDING TO THE ACCOUNTABILITY CHART IF THE SPOT IS EMPTY?



            //s.AuditReader().CreateQuery().ForRevisionsOf<AccountabilityNode>()
            //    .Add()

            //var nodes = s.QueryOver<AccountabilityNode>()
            //   .Where(x => x.OrganizationId == user.Organization.Id && x.DeleteTime != null && x.UserId == user.Id)
            //   .List().ToList();

            //foreach (var n in nodes) {
            //    if ()
            //    n.UserId = null;
            //    s.Update(n);
            //}
        }
    }
}