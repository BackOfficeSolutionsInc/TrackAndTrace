using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
    public class AccountabilityAccessor : BaseAccessor {
        #region old
        protected class LONG {
            public long id { get; set; }
        }
        protected static AccountabilityTree DiveOld(long caller, LONG id, long parent, List<UserOrganizationModel> users, List<RoleModel> roles, List<DeepSubordinateModel> links, List<ManagerDuration> mds)
        {
            var own = links.Any(x => x.ManagerId == caller && x.SubordinateId == parent);
            // var children = links.Where(x=>x.ManagerId==parent);
            var me = users.FirstOrDefault(x => x.Id == parent);
            var children = mds.Where(x => x.ManagerId == parent).ToList();


            var childDive = children.Select(x => DiveOld(caller, id, x.SubordinateId, users, roles, links, mds)).ToList();

            id.id += 1;
            return new AccountabilityTree() {
                id = id.id,
                user = AngularUser.CreateUser(me, managing: own),
                roles = roles.Where(x => x.Owner.Id == parent).Select(x => new AngularRole(x)).ToList(),
                children = childDive,
                collapsed = !links.Any(x => (x.ManagerId == caller || x.SubordinateId == caller) && x.ManagerId == parent)
            };
        }

        public static AccountabilityTree GetTreeOld(UserOrganizationModel caller, long organizationId, long? parentId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    //var tree = OrganizationAccessor.GetOrganizationTree(s, perms, organizationId, parentId, false, true);
                    var map = DeepSubordianteAccessor.GetOrganizationMap(s, perms, organizationId);

                    var org = s.Get<OrganizationModel>(organizationId);

                    var userIds = map.SelectMany(x => new List<long> { x.ManagerId, x.SubordinateId }).Distinct().ToArray();

                    var usersF = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.Id).IsIn(userIds)
                        .Future();

                    var rolesF = s.QueryOver<RoleModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId)
                        .WhereRestrictionOn(x => x.Owner.Id).IsIn(userIds)
                        .Future();

                    var managerLinksF = s.QueryOver<ManagerDuration>().Where(x => x.DeletedBy == null)
                        .WhereRestrictionOn(x => x.ManagerId).IsIn(userIds)
                        .Future();


                    var users = usersF.ToList();
                    var roles = rolesF.ToList();
                    var managerLinks = managerLinksF.ToList();

                    List<long> tln;

                    if (parentId == null) {
                        tln = users.Where(x => x.ManagingOrganization).Select(x => x.Id).ToList();
                    } else {
                        var parent = users.FirstOrDefault(x => x.Id == parentId.Value);

                        if (organizationId != parent.Organization.Id)
                            throw new PermissionsException("Organizations do not match");

                        perms.ViewOrganization(parent.Organization.Id);
                        tln = parent.Id.AsList();
                    }


                    var id = new LONG();

                    var trees = new List<AccountabilityTree>();
                    foreach (var topLevelNode in tln) {
                        trees.Add(DiveOld(caller.Id, id, topLevelNode, users, roles, map, managerLinks));
                    }
                    return new AccountabilityTree() {
                        children = trees,
                        name = org.GetName(),
                        id = 0,
                    };
                }
            }
        }

        #endregion

        protected static AngularAccountabilityNode Dive(long nodeId, List<AccountabilityNode> nodes, List<AccountabilityRolesGroup> groups, List<AccountabilityNodeRoleMap> roles,List<UserTemplate> templates, List<RoleModel> templateRoles)
        {
            // var children = links.Where(x=>x.ManagerId==parent);
            var me = nodes.FirstOrDefault(x => x.Id == nodeId);
            var children = nodes.Where(x => x.ParentNodeId == nodeId).ToList();


            var childDive = children.Select(x => Dive(x.Id, nodes, groups, roles, templates, templateRoles)).ToList();

            var group = groups.First(x => x.Id == me.AccountabilityRolesGroupId);
            group._Roles = roles.Where(x => x.AccountabilityGroupId == me.AccountabilityRolesGroupId).ToList();



            var aaGroup = new AngularAccountabilityGroup(group);

            var aan = new AngularAccountabilityNode() {
                Id = nodeId,
                User = AngularUser.CreateUser(me.User),
                Group = aaGroup,
                children = childDive,
            };

            var myTemplates = templates.Where(x => x.AttachType == Models.Enums.AttachType.Position && group.PositionId==x.AttachId);
            var myTemplateIds = myTemplates.Select(x=>x.Id).ToList();
            var myTemplateRoles = templateRoles.Where(x => myTemplateIds.Any(y => y == x.FromTemplateItemId)).ToList();
            
            var newRoles = aan.Group.Roles.ToList();
            newRoles.AddRange(myTemplateRoles.Select(x=>new AngularRole(x)));
            aan.Group.Roles = newRoles;

            return aan;
        }

        public static AngularAccountabilityChart GetTree(UserOrganizationModel caller, long chartId, long? centerUserId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewHierarchy(chartId);

                    var chart = s.Get<AccountabilityChart>(chartId);

                    var nodes = s.QueryOver<AccountabilityNode>().Where(x => x.AccountabilityChartId == chartId && x.DeleteTime == null).Future();
                    var groups = s.QueryOver<AccountabilityRolesGroup>().Where(x => x.AccountabilityChartId == chartId && x.DeleteTime == null).Future();
                    var nodeRoles = s.QueryOver<AccountabilityNodeRoleMap>().Where(x => x.AccountabilityChartId == chartId && x.DeleteTime == null).Future();
                    var userTemplates = s.QueryOver<UserTemplate>().Where(x => x.OrganizationId == chart.OrganizationId && x.DeleteTime == null).Future();

                    var positionRoles = s.QueryOver<RoleModel>().Where(x => x.OrganizationId == chart.OrganizationId && x.DeleteTime == null && x.FromTemplateItemId!=null).Future();

                    var usersF = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null && x.Organization.Id == chart.OrganizationId).List().ToList();


                    var root = Dive(chart.RootId, nodes.ToList(), groups.ToList(), nodeRoles.ToList(), userTemplates.ToList(), positionRoles.ToList());

                    var centerNode = root.Id;
                    if (centerUserId != null) {
                        var cn = nodes.FirstOrDefault(x => x.UserId == centerUserId);
                        if (cn != null)
                            centerNode = cn.Id;
                    }

                    return new AngularAccountabilityChart(chartId) {
                        Root = root,
                        CenterNode = centerNode,
                        AllUsers = usersF.ToList().Select(x=>AngularUser.CreateUser(x)).ToList()
                    };

                }
            }
        }



        public static void RemoveNode(UserOrganizationModel caller, long nodeId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var node = s.Get<AccountabilityNode>(nodeId);
                    perms.EditHierarchy(node.OrganizationId);
                    var children = s.QueryOver<AccountabilityNode>().Where(x => x.ParentNodeId == nodeId && x.DeleteTime == null).RowCount();

                    if (children > 0)
                        throw new PermissionsException("Cannot delete a node with children.");

                    if (node.ParentNodeId == null)
                        throw new PermissionsException("Cannot delete the root node.");

                    node.DeleteTime = DateTime.UtcNow;
                    s.Update(node);


                    var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
                    var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId));

                    orgHub.update(new AngularUpdate() {
                        new AngularAccountabilityNode(node.ParentNodeId.Value){
                            children = AngularList.CreateFrom(AngularListType.Remove,new AngularAccountabilityNode(node.Id))
                        }
                    });

                    tx.Commit();
                    s.Flush();

                }
            }
        }

        public static void SwapParents(UserOrganizationModel caller, long nodeId, long newParentId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);

                    var node = s.Get<AccountabilityNode>(nodeId);
                    if (node == null)
                        throw new PermissionsException("Node does not exist");
                    perms.EditHierarchy(node.OrganizationId);

                    var newParent = s.Get<AccountabilityNode>(nodeId);

                    if (node.AccountabilityChartId == newParent.AccountabilityChartId)
                        throw new PermissionsException("Nodes are not on the same chart.");

                    if (node.ParentNodeId == null)
                        throw new PermissionsException("Cannot move the root node.");

                    var oldParent = node.ParentNodeId.Value;
                    node.ParentNodeId = newParentId;

                    s.Update(node);

                    var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
                    var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId));

                    orgHub.update(new AngularUpdate() {
                        new AngularAccountabilityNode(oldParent){
                            children = AngularList.CreateFrom(AngularListType.Add,new AngularAccountabilityNode(node.Id))
                        },
                        new AngularAccountabilityNode(oldParent){
                            children = AngularList.CreateFrom(AngularListType.Remove,new AngularAccountabilityNode(node.Id))
                        },

                    });

                }
            }

        }

        public static AccountabilityNode AppendNode(ISession s, PermissionsUtility perms, long parentNodeId, long? rolesGroupId = null)
        {

            var parent = s.Get<AccountabilityNode>(parentNodeId);
            if (parent == null)
                throw new PermissionsException("Parent does not exist");
            perms.EditHierarchy(parent.OrganizationId);
            AccountabilityRolesGroup group = null;
            if (rolesGroupId != null) {
                group = s.Get<AccountabilityRolesGroup>(rolesGroupId);
                if (group.OrganizationId != parent.OrganizationId)
                    throw new PermissionsException("Could not access node");
            } else {
                group = new AccountabilityRolesGroup() {
                    OrganizationId = parent.OrganizationId,
                    AccountabilityChartId = parent.AccountabilityChartId,
                };
                s.Save(group);

            }
            var node = new AccountabilityNode() {
                OrganizationId = parent.OrganizationId,
                ParentNodeId = parentNodeId,
                ParentNode = parent,
                AccountabilityRolesGroupId = group.Id,
                AccountabilityRolesGroup = group,
                AccountabilityChartId = parent.AccountabilityChartId,
            };
            s.Save(node);

            var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
            var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId));

            orgHub.update(new AngularUpdate() {
                        new AngularAccountabilityNode(parentNodeId){
                            children = AngularList.CreateFrom(AngularListType.Add,new AngularAccountabilityNode(node))
                        }
                    });

            return node;


        }

        public static AccountabilityNode AppendNode(UserOrganizationModel caller, long parentNodeId, long? rolesGroupId = null)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return AppendNode(s,perms,parentNodeId,rolesGroupId);

                }
            }

        }
        public static AccountabilityChart CreateChart(ISession s, PermissionsUtility perms, long organizationId, bool creatorCanAdmin = true)
        {
            perms.ViewOrganization(organizationId);

            var chart = new AccountabilityChart() {
                OrganizationId = organizationId,
                Name = s.Get<OrganizationModel>(organizationId).GetName(),
            };
            s.Save(chart);

            var group = new AccountabilityRolesGroup() {
                OrganizationId = organizationId,
                AccountabilityChartId = chart.Id,
            };
            s.Save(group);

            var root = new AccountabilityNode() {
                OrganizationId = organizationId,
                ParentNodeId = null,
                ParentNode = null,
                AccountabilityRolesGroupId = group.Id,
                AccountabilityRolesGroup = group,
                AccountabilityChartId = chart.Id,
            };
            s.Save(root);

            chart.RootId = root.Id;
            s.Update(chart);

            PermissionsAccessor.CreatePermItems(s, perms.GetCaller(), PermItem.ResourceType.AccountabilityHierarchy, chart.Id,
                PermTiny.Admins(),
                PermTiny.Creator(view: creatorCanAdmin, edit: creatorCanAdmin, admin: creatorCanAdmin),
                PermTiny.Members(edit: false, admin: false)
            );

            return chart;
        }

        public static AccountabilityChart CreateChart(UserOrganizationModel caller, long organizationId, bool creatorCanAdmin = true)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);

                    var chart = CreateChart(s, perms, organizationId, creatorCanAdmin);

                    tx.Commit();
                    s.Flush();

                    return chart;

                }
            }
        }
    }
}