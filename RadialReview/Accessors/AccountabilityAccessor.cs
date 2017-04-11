using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hooks;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Permissions;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.Query;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static RadialReview.Models.PermItem;
using static RadialReview.Utilities.RealTime.RealTimeUtility;

namespace RadialReview.Accessors {
    public class AccountabilityAccessor : BaseAccessor {

        #region Single call
        public static void Update(UserOrganizationModel caller, IAngularItem model, string connectionId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    using (var rt = RealTimeUtility.Create(connectionId)) {
                        var perms = PermissionsUtility.Create(s, caller);

                        if (model.Type == typeof(AngularAccountabilityNode).Name) {
                            var m = (AngularAccountabilityNode)model;
                            //UpdateIssue(caller, (long)model.GetOrDefault("Id", null), (string)model.GetOrDefault("Name", null), (string)model.GetOrDefault("Details", null), (bool?)model.GetOrDefault("Complete", null), connectionId);
                            UpdateAccountabilityNode(s, rt, perms, m.Id, m.Group, m.User.NotNull(x => (long?)x.Id));
                        } else if (model.Type == typeof(AngularRole).Name) {
                            var m = (AngularRole)model;
                            //UpdateIssue(caller, (long)model.GetOrDefault("Id", null), (string)model.GetOrDefault("Name", null), (string)model.GetOrDefault("Details", null), (bool?)model.GetOrDefault("Complete", null), connectionId);
                            UpdateRole(s, rt, perms, m.Id, m.Name);
                        } else {
                            throw new PermissionsException("Unhandled type: " + model.Type);
                        }

                        tx.Commit();
                        s.Flush();
                    }
                }
            }
        }

        #endregion
        #region old
        protected class LONG {
            public long id { get; set; }
        }
        [Obsolete("Do not use", true)]
        protected static AccountabilityTree DiveOld(long caller, LONG id, long parent, List<UserOrganizationModel> users, List<RoleModel> roles, List<DeepAccountability> links, List<ManagerDuration> mds) {
            var own = links.Any(x => x.Parent.UserId == caller && x.Child.UserId == parent);
            // var children = links.Where(x=>x.ManagerId==parent);
            var me = users.FirstOrDefault(x => x.Id == parent);
            var children = mds.Where(x => x.ManagerId == parent).ToList();


            var childDive = children.Select(x => DiveOld(caller, id, x.SubordinateId, users, roles, links, mds)).ToList();

            id.id += 1;
            throw new Exception("Fix me");
            //return new AccountabilityTree()
            //{
            //    id = id.id,
            //    user = AngularUser.CreateUser(me, managing: own),
            //    roles = roles.Where(x => x.Owner.Id == parent).Select(x => new AngularRole(x)).ToList(),
            //    children = childDive,
            //    collapsed = !links.Any(x => (x.Parent.UserId == caller || x.Child.UserId == caller) && x.Parent.UserId == parent)
            //};
        }

        public static List<AccountabilityNode> GetNodesForUser(UserOrganizationModel caller, long userId) {

            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetNodesForUser(s, perms, userId);
                }
            }
        }

        public static List<AccountabilityNode> GetNodesForUser(ISession s, PermissionsUtility perms, long userId) {           
            perms.ViewUserOrganization(userId, false);
            return s.QueryOver<AccountabilityNode>().Where(x => x.DeleteTime == null && x.UserId == userId).List().ToList();
        }

        public static AccountabilityNode GetNodeById(UserOrganizationModel caller, long seatId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetNodeById(s, perms, seatId);
                }
            }
        }

        public static AccountabilityNode GetNodeById(ISession s, PermissionsUtility perms, long seatId)
        {
            var node = s.QueryOver<AccountabilityNode>().Where(x => x.DeleteTime == null && x.Id == seatId).List().FirstOrDefault();
            perms.CanView(ResourceType.AccountabilityHierarchy, node.AccountabilityChartId);
            return node;
        }

        [Obsolete("Do not use", true)]
        public static AccountabilityTree GetTreeOld(UserOrganizationModel caller, long organizationId, long? parentId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    //var tree = OrganizationAccessor.GetOrganizationTree(s, perms, organizationId, parentId, false, true);
                    var map = DeepAccessor.GetOrganizationMap(s, perms, organizationId);// DeepSubordianteAccessor.GetOrganizationMap(s, perms, organizationId);

                    var org = s.Get<OrganizationModel>(organizationId);

                    var userIds = map.SelectMany(x => new List<long?> { x.Parent.UserId, x.Child.UserId }).Distinct().ToArray();

                    var usersF = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null)
                        .WhereRestrictionOn(x => x.Id).IsIn(userIds)
                        .Future();

                    throw new Exception("Fix me");
                    //var rolesF = s.QueryOver<RoleModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId)
                    //    .WhereRestrictionOn(x => x.Owner.Id).IsIn(userIds)
                    //    .Future();

                    //var managerLinksF = s.QueryOver<ManagerDuration>().Where(x => x.DeletedBy == null)
                    //    .WhereRestrictionOn(x => x.ManagerId).IsIn(userIds)
                    //    .Future();


                    //var users = usersF.ToList();
                    //var roles = rolesF.ToList();
                    //var managerLinks = managerLinksF.ToList();

                    //List<long> tln;

                    //if (parentId == null)
                    //{
                    //    tln = users.Where(x => x.ManagingOrganization).Select(x => x.Id).ToList();
                    //}
                    //else
                    //{
                    //    var parent = users.FirstOrDefault(x => x.Id == parentId.Value);

                    //    if (organizationId != parent.Organization.Id)
                    //        throw new PermissionsException("Organizations do not match");

                    //    perms.ViewOrganization(parent.Organization.Id);
                    //    tln = parent.Id.AsList();
                    //}


                    //var id = new LONG();

                    //var trees = new List<AccountabilityTree>();
                    //foreach (var topLevelNode in tln)
                    //{
                    //    trees.Add(DiveOld(caller.Id, id, topLevelNode, users, roles, map, managerLinks));
                    //}
                    //return new AccountabilityTree()
                    //{
                    //    children = trees,
                    //    name = org.GetName(),
                    //    id = 0,
                    //};
                }
            }
        }

        #endregion

        protected static AngularAccountabilityNode Dive(UserOrganizationModel caller, long nodeId, List<AccountabilityNode> nodes,
            List<AccountabilityRolesGroup> groups, Dictionary<long, RoleModel> rolesLU, List<RoleLink> links, List<PosDur> positions,
            List<TeamDur> teams, List<AngularAccountabilityNode> parents, HashSet<long> allManagingUserIds, long? selectedNode = null,
            bool? editableBelow = null, bool expandAll = false) {
            // var children = links.Where(x=>x.ManagerId==parent);
            var me = nodes.FirstOrDefault(x => x.Id == nodeId);
            var children = nodes.Where(x => x.ParentNodeId == nodeId).ToList();

            //Calculate Permissions
            var isEditable = false;
            if (caller.Id == me.UserId && !children.Any() && !caller.ManagingOrganization) {
                isEditable = caller.Organization.Settings.EmployeesCanEditSelf;
            } else if (editableBelow == true) {
                isEditable = true;
            }

            var isMe = false;
            if (editableBelow != null && me.UserId != null && caller.Id == me.UserId) {
                editableBelow = true;
                isMe = true;
            }

            var group = groups.First(x => x.Id == me.AccountabilityRolesGroupId);

            group._Roles = RoleAccessor.ConstructRolesForNode(me.UserId, me.AccountabilityRolesGroup.PositionId, rolesLU, links, positions, teams); //roles.Where(x => x.AccountabilityGroupId == me.AccountabilityRolesGroupId).ToList();
            var aaGroup = new AngularAccountabilityGroup(group, editable: isEditable);
            var aan = new AngularAccountabilityNode() {
                Id = nodeId,
                User = AngularUser.CreateUser(me.User),
                Group = aaGroup,
                collapsed = !expandAll,
                Editable = isEditable,
                Me = isMe,
                order = me.Ordering,
            };
			aan.Name = aan.User.NotNull(x => x.Name);

			if (isEditable && me.UserId.HasValue)
                allManagingUserIds.Add(me.UserId.Value);

            var parentsCopy = parents.ToList();
            parentsCopy.Add(aan);
            if (selectedNode == nodeId) {
                foreach (var p in parentsCopy) {
                    p.collapsed = false;
                }
            }


            aan.SetChildren(children.Select(x => Dive(caller, x.Id, nodes, groups, rolesLU, links, positions, teams, parentsCopy, allManagingUserIds, selectedNode, editableBelow, expandAll)).ToList());
            return aan;
        }
        public static AngularAccountabilityChart GetTree(UserOrganizationModel caller, long chartId, long? centerUserId = null, long? centerNodeId = null, DateRange range = null, bool expandAll = false) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetTree(s, perms, chartId, centerUserId, centerNodeId, range, expandAll: expandAll);
                }
            }
        }

        public static AngularAccountabilityChart GetTree(ISession s, PermissionsUtility perms, long chartId, long? centerUserId = null, long? centerNodeId = null, DateRange range = null, bool expandAll = false) {
            perms.ViewHierarchy(chartId);

            var chart = s.Get<AccountabilityChart>(chartId);

            var nodes = s.QueryOver<AccountabilityNode>().Where(x => x.AccountabilityChartId == chartId).Where(range.Filter<AccountabilityNode>()).Future();
            var groups = s.QueryOver<AccountabilityRolesGroup>().Where(x => x.AccountabilityChartId == chartId).Where(range.Filter<AccountabilityRolesGroup>()).Future();
            var userTemplates = s.QueryOver<UserTemplate>().Where(x => x.OrganizationId == chart.OrganizationId).Where(range.Filter<UserTemplate>()).Future();

            var roles = s.QueryOver<RoleModel>().Where(x => x.OrganizationId == chart.OrganizationId).Where(range.Filter<RoleModel>()).Future();
            var roleLinks = s.QueryOver<RoleLink>().Where(x => x.OrganizationId == chart.OrganizationId).Where(range.Filter<RoleLink>()).Future();

            var teams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == chart.OrganizationId).Where(range.Filter<OrganizationTeamModel>()).Select(x => x.Id, x => x.Name).Future<object[]>();
            var positions = s.QueryOver<OrganizationPositionModel>().Where(x => x.Organization.Id == chart.OrganizationId).Where(range.Filter<OrganizationPositionModel>()).Select(x => x.Id, x => x.CustomName).Future<object[]>();

            var teamDurs = s.QueryOver<TeamDurationModel>().Where(x => x.OrganizationId == chart.OrganizationId).Where(range.Filter<TeamDurationModel>()).Select(x => x.TeamId, x => x.UserId).Future<object[]>();
            var posDurs = s.QueryOver<PositionDurationModel>().Where(x => x.OrganizationId == chart.OrganizationId).Where(range.Filter<PositionDurationModel>()).Select(x => x.Position.Id, x => x.UserId).Future<object[]>();


            var usersF = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == chart.OrganizationId && !x.IsClient).Where(range.Filter<UserOrganizationModel>()).List().ToList();

            var teamName = teams.ToDictionary(x => (long)x[0], x => (string)x[1]);
            var posName = positions.ToDictionary(x => (long)x[0], x => (string)x[1]);

            var pd = posDurs.Select(x => new PosDur { PosId = (long)x[0], PosName = posName.GetOrDefault((long)x[0], null), UserId = (long)x[1] }).ToList();
            var td = teamDurs.Select(x => new TeamDur { TeamId = (long)x[0], TeamName = posName.GetOrDefault((long)x[0], null), UserId = (long)x[1] }).ToList();

            var centerNode = chart.RootId;
            if (centerNodeId != null) {
                var cn = nodes.FirstOrDefault(x => x.Id == centerNodeId);
                if (cn != null)
                    centerNode = cn.Id;
            } else if (centerUserId != null) {
                var cn = nodes.FirstOrDefault(x => x.UserId == centerUserId);
                if (cn != null)
                    centerNode = cn.Id;
            }

            var editAll = perms.IsPermitted(x => x.Or(y => y.ManagingOrganization(chart.OrganizationId), y => y.EditHierarchy(chart.Id)));

            var allManaging = new HashSet<long>();

            var root = Dive(perms.GetCaller(), chart.RootId, nodes.ToList(), groups.ToList(), roles.ToDictionary(x => x.Id, x => x), roleLinks.ToList(), pd, td, new List<AngularAccountabilityNode>(), allManaging, centerNode, editableBelow: editAll, expandAll: expandAll);

            var allUsers = usersF.ToList().Select(x =>
                AngularUser.CreateUser(x, managing: editAll || allManaging.Contains(x.Id) || (perms.GetCaller().IsManager() && perms.GetCaller().Id == x.Id))
            ).ToList();

            var c = new AngularAccountabilityChart(chartId) {
                Root = root,
                CenterNode = centerNode,
                AllUsers = allUsers,
            };

            c.Root.Name = chart.Name;

            return c;
        }

        public static List<AccountabilityNode> GetOrganizationManagerNodes(UserOrganizationModel caller, long orgId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.ViewOrganization(orgId);

                    UserOrganizationModel user = null;
                    var nodes = s.QueryOver<AccountabilityNode>()
                        .Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.UserId != null)
                        .JoinAlias(x => x.User, () => user)
                            .Where(x => user.DeleteTime == null && user.ManagerAtOrganization || user.ManagingOrganization)
                        .List().ToList();

                    return nodes;

                }
            }
        }
        public static void SetPosition(ISession s, PermissionsUtility perms, RealTimeUtility rt, long nodeId, long? positionId) {
            perms.ManagesAccountabilityNodeOrSelf(nodeId);
            var now = DateTime.UtcNow;
            UpdatePosition_Unsafe(s, rt, perms, nodeId, positionId, now);

        }
        public static void SetUser(UserOrganizationModel caller, long nodeId, long? userId, string connectionId = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    using (var rt = RealTimeUtility.Create(connectionId)) {
                        var perms = PermissionsUtility.Create(s, caller);
                        SetUser(s, rt, perms, nodeId, userId);
                        tx.Commit();
                        s.Flush();
                    }
                }
            }
        }

        public static void SetUser(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, long? userId) {
#pragma warning disable CS0618 // Type or member is obsolete
            SetUser(s, rt, perms, nodeId, userId, false, false, DateTime.UtcNow);
#pragma warning restore CS0618 // Type or member is obsolete
        }


        [Obsolete("Use the other SetUser.")]
        public static void SetUser(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, long? userId, bool skipAddManger, bool skipPosition, DateTime now) {

            var updateUsers = new List<long>();
            UserOrganizationModel user = null;

            if (userId.HasValue)
                perms.ManagesUserOrganization(userId.Value, true, PermissionType.EditEmployeeManagers);

            perms.ManagesAccountabilityNodeOrSelf(nodeId, PermissionType.EditEmployeeManagers);

            //SetUser_Unsafe(s, nodeId, userId);
            var n = s.Get<AccountabilityNode>(nodeId);
            if (n.UserId != null)
                perms.ManagesUserOrganization(n.UserId.Value, true, PermissionType.EditEmployeeManagers);

            //var now = DateTime.UtcNow;

            if (userId != n.UserId) {

                var updater = rt.UpdateOrganization(n.OrganizationId);
                //The old user
                if (n.UserId != null) {
                    //REMOVING USER FROM NODE



                    //REMOVE MANAGER
                    if (n.ParentNode != null && n.ParentNode.UserId != null) {
                        var found = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.ManagerId == n.ParentNode.UserId && x.SubordinateId == n.UserId).Take(1).SingleOrDefault();
                        if (found != null) {
                            found.DeleteTime = now;
                            s.Update(found);
                        }

                        if (n.ParentNode.UserId != null) {
                            updateUsers.Add(n.ParentNode.UserId.Value);
                            //s.GetFresh<UserOrganizationModel>(n.ParentNode.UserId).UpdateCache(s);
                        }
                    }

                    //REMOVE SUBORDINATES
                    var childs = s.QueryOver<AccountabilityNode>()
                        .Where(x => x.DeleteTime == null && x.ParentNodeId == n.Id && x.UserId != null)
                        .List().ToList();

                    foreach (var c in childs) {
                        var found = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.ManagerId == n.UserId && x.SubordinateId == c.UserId).Take(1).SingleOrDefault();
                        if (found != null) {
                            found.DeleteTime = now;
                            s.Update(found);

                            updateUsers.Add(c.UserId.Value);
                            //s.GetFresh<UserOrganizationModel>(c.UserId).UpdateCache(s);
                        }
                    }


                    if (n.UserId != null) {
                        //Update positions
                        if (!skipPosition && n.AccountabilityRolesGroup != null) {
                            if (n.AccountabilityRolesGroup.PositionId != null) {
                                var pd = s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null && x.Position.Id == n.AccountabilityRolesGroup.PositionId && x.UserId == n.UserId).Take(1).SingleOrDefault();
                                if (pd != null) {
                                    pd.DeleteTime = DateTime.UtcNow;
                                    pd.DeletedBy = perms.GetCaller().Id;
                                    s.Update(pd);
                                }
                            }
                        }
                        updateUsers.Add(n.UserId.Value);
                        //s.GetFresh<UserOrganizationModel>(n.UserId).UpdateCache(s);
                    }
                    //User is removed from updater below...
                    updater.ForceUpdate(new AngularAccountabilityGroup(n.AccountabilityRolesGroupId) {
                        RoleGroups = AngularList.CreateFrom(AngularListType.Remove, new AngularRoleGroup(new Attach(AttachType.User, n.UserId.Value), null))
                    });

                    n.UserId = null;
                    s.Update(n);
                }


                //The new user
                if (userId != null) {
                    //ADDING USER TO NODE

                    n.User = s.Get<UserOrganizationModel>(userId);
                    n.UserId = n.User.Id;
                    user = n.User;

                    if (DeepAccessor.HasChildren(s, n.Id)) {
                        //UPDATE MANAGER STATUS,
                        if (!n.User.IsManager()) {
                            UserAccessor.EditUser(s, perms, n.User.Id, true);
                            n.User.ManagerAtOrganization = true;
                        }
                        //ADD SUBORDINATES
                        if (!skipAddManger) {
                            var childs = s.QueryOver<AccountabilityNode>()
                                   .Where(x => x.DeleteTime == null && x.ParentNodeId == n.Id && x.UserId != null)
                                   .List().ToList();

                            foreach (var c in childs) {
                                var md = new ManagerDuration() {
                                    ManagerId = userId.Value,
                                    Manager = s.Load<UserOrganizationModel>(userId.Value),
                                    SubordinateId = c.UserId.Value,
                                    Subordinate = s.Load<UserOrganizationModel>(c.UserId.Value),
                                    PromotedBy = perms.GetCaller().Id,
                                    CreateTime = now,

                                };
                                s.Save(md);
                                //s.GetFresh<UserOrganizationModel>(c.UserId).UpdateCache(s);

                                updateUsers.Add(c.UserId.Value);
                            }
                        }
                    }
                    //ADD MANAGER
                    if (n.ParentNode != null && n.ParentNode.UserId != null && !skipAddManger) {
                        var md = new ManagerDuration() {
                            ManagerId = n.ParentNode.UserId.Value,
                            Manager = s.Load<UserOrganizationModel>(n.ParentNode.UserId.Value),
                            SubordinateId = userId.Value,
                            Subordinate = s.Load<UserOrganizationModel>(userId.Value),
                            PromotedBy = perms.GetCaller().Id,
                            CreateTime = now
                        };
                        s.Save(md);
                        updateUsers.Add(n.ParentNode.UserId.Value);
                        //s.GetFresh<UserOrganizationModel>(n.ParentNode.UserId).UpdateCache(s);
                    }
                    s.Update(n);
                    //Update positions
                    if (!skipPosition) {
                        if (n.AccountabilityRolesGroup.PositionId != null) {
                            var pd = new PositionDurationModel() {
                                UserId = n.UserId.Value,
                                CreateTime = now,
                                Position = n.AccountabilityRolesGroup.Position,
                                PromotedBy = perms.GetCaller().Id,
                                OrganizationId = n.OrganizationId,
                            };
                            s.Save(pd);
                        }
                    }

                    s.Flush();

                    updateUsers.Add(n.UserId.Value);
                    //s.GetFresh<UserOrganizationModel>(n.UserId).UpdateCache(s);

                    updater.Update(new AngularAccountabilityNode(n.Id) {
                        User = AngularUser.CreateUser(n.User),
                    });


                    var addedRoles = RoleAccessor.GetRolesForAttach_Unsafe(s, new Attach(AttachType.User, n.UserId.Value));
                    var angRoles = addedRoles.Select(x => new AngularRole(x));
                    if (angRoles.Any()) {
                        updater.ForceUpdate(new AngularAccountabilityGroup(n.AccountabilityRolesGroupId) {
                            RoleGroups = AngularList.CreateFrom(AngularListType.Add, new AngularRoleGroup(new Attach(AttachType.User, n.UserId.Value), angRoles))
                        });
                    }


                } else {


                    n.User = null;
                    n.UserId = null;
                    s.Update(n);

                    updater.Update(new AngularAccountabilityNode(n.Id) {
                        User = Removed.From<AngularUser>()
                    });
                }
            } else {
                //No change?
            }
            s.Flush();
            foreach (var u in updateUsers.Distinct()) {
                s.GetFresh<UserOrganizationModel>(u).UpdateCache(s);
            }
            if (user != null)
                s.Evict(user);

        }


        public static void RemoveRole(ISession s, PermissionsUtility perms, RealTimeUtility rt, long roleId) {
            var role = s.Get<RoleModel>(roleId);
            perms.ViewOrganization(role.OrganizationId).EditRole(roleId);

            var links = s.QueryOver<RoleLink>()
                .Where(x => x.DeleteTime == null && x.RoleId == roleId && x.OrganizationId == role.OrganizationId)
                .List().ToList();

            var now = DateTime.UtcNow;
            role.DeleteTime = now;
            s.Update(role);

            var roleGroupUpdates = new List<AngularRoleGroup>();
            foreach (var link in links) {
                link.DeleteTime = now;
                s.Update(link);

                var arg = new AngularRoleGroup(link.GetAttach(), AngularList.CreateFrom(AngularListType.Remove, new AngularRole(role)));
                rt.UpdateOrganization(role.OrganizationId).Update(arg);
            }
        }

        public static void RemoveRole(UserOrganizationModel caller, long roleId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    using (var rt = RealTimeUtility.Create()) {
                        var perms = PermissionsUtility.Create(s, caller);
                        RemoveRole(s, perms, rt, roleId);
                        tx.Commit();
                        s.Flush();
                    }
                }
            }
        }
        public static void UnremoveRole(UserOrganizationModel caller, long roleId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    using (var rt = RealTimeUtility.Create()) {
                        try {
                            var perms = PermissionsUtility.Create(s, caller);
                            var role = s.Get<RoleModel>(roleId);
                            if (role.DeleteTime == null)
                                throw new PermissionsException("Role already exists") { NoErrorReport = true };

                            var dt = role.DeleteTime;
                            role.DeleteTime = null;
                            s.Update(role);
                            var links = s.QueryOver<RoleLink>()
                                .Where(x => x.DeleteTime == dt && x.RoleId == roleId && x.OrganizationId == role.OrganizationId)
                                .List().ToList();
                            //s.Flush();
                            var roleGroupUpdates = new List<AngularRoleGroup>();
                            foreach (var link in links) {
                                link.DeleteTime = null;
                                s.Update(link);

                                var arg = new AngularRoleGroup(link.GetAttach(), AngularList.CreateFrom(AngularListType.Add, new AngularRole(role)));
                                rt.UpdateOrganization(role.OrganizationId).Update(arg);
                            }
                            s.Flush();
                            //Permissions need to happen after update. (DeleteTime !=null)
                            perms.ViewOrganization(role.OrganizationId).EditRole(roleId);



                            tx.Commit();
                            s.Flush();
                        } catch (Exception e) {
                            tx.Rollback();
                            throw e;
                        }
                    }
                }
            }
        }

        public static AccountabilityNode GetRoot(ISession s, PermissionsUtility perms, long chartId) {
            var c = s.Get<AccountabilityChart>(chartId);
            perms.ViewOrganization(c.OrganizationId);
            return s.Get<AccountabilityNode>(c.RootId);
        }

        public static AccountabilityNode GetRoot(UserOrganizationModel caller, long chartId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    return GetRoot(s, perms, chartId);

                }
            }
        }

        public static void RemoveNode(UserOrganizationModel caller, long nodeId) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    using (var do_not_use = RealTimeUtility.Create(false)) {
                        var perms = PermissionsUtility.Create(s, caller);
                        var node = s.Get<AccountabilityNode>(nodeId);
                        if (node.DeleteTime != null)
                            throw new PermissionsException("Node does not exist.");
                        perms.ManagesAccountabilityNodeOrSelf(nodeId);
                        var children = s.QueryOver<AccountabilityNode>().Where(x => x.ParentNodeId == nodeId && x.DeleteTime == null).RowCount();

                        if (children > 0)
                            throw new PermissionsException("Cannot delete a node with children.");
                        //HEEYY!!!!! if you remove ^ condition, you must update manager status for node. You must also remove subordinates.

                        if (node.ParentNodeId == null)
                            throw new PermissionsException("Cannot delete the root node.");


                        var now = DateTime.UtcNow;
                        node.DeleteTime = now;
                        s.Update(node);

                        DeepAccessor.RemoveAll(s, node, now);

#pragma warning disable CS0618 // Type or member is obsolete
                        SetUser(s, do_not_use, perms, node.Id, null, false, false, now);
#pragma warning restore CS0618 // Type or member is obsolete
                        UpdatePosition_Unsafe(s, do_not_use, perms, node.Id, null, now);

						////REMOVE MANAGER
						//Handled in set user i think...
						//if (node.UserId != null && node.ParentNode != null && node.ParentNode.UserId != null) {
						//	var found = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.ManagerId == node.ParentNode.UserId && x.SubordinateId == node.UserId).Take(1).SingleOrDefault();
						//	if (found != null) {
						//		found.DeleteTime = now;
						//		s.Update(found);

						//		s.GetFresh<UserOrganizationModel>(found.SubordinateId).UpdateCache(s);
						//		s.GetFresh<UserOrganizationModel>(found.ManagerId).UpdateCache(s);

						//	} else {
						//		log.Error("Removing manager. ManagerDuration not found. " + node.ParentNode.UserId + " " + node.UserId);
						//	}
						//}

						if (node.ParentNode.User != null && node.ParentNode.User.IsManager() && !DeepAccessor.HasChildren(s, node.ParentNode.Id)) {
							UserAccessor.EditUser(s, perms, node.ParentNode.User.Id, false);
							node.ParentNode.User.ManagerAtOrganization = false;
						}


						tx.Commit();
                        s.Flush();

                        var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
                        var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId));

                        orgHub.update(new AngularUpdate() {
                            new AngularAccountabilityChart(node.AccountabilityChartId) {
                                ShowNode = nodeId
                            },
                            new AngularAccountabilityNode(node.ParentNodeId.Value){
                                children = AngularList.CreateFrom(AngularListType.Remove,new AngularAccountabilityNode(node.Id))
                            },
                        });

                    }
                }
            }
        }

        public static void SwapParents(UserOrganizationModel caller, long nodeId, long newParentId, string connectionId = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var ordering = 0;
                    var node = s.Get<AccountabilityNode>(nodeId);
                    if (node == null)
                        throw new PermissionsException("Node does not exist");
                    perms.ManagesAccountabilityNodeOrSelf(node.Id)
                        .ManagesAccountabilityNodeOrSelf(newParentId);

                    var newParent = s.Get<AccountabilityNode>(newParentId);

                    if (node.AccountabilityChartId != newParent.AccountabilityChartId)
                        throw new PermissionsException("Nodes are not on the same chart.");

                    if (node.ParentNodeId == null)
                        throw new PermissionsException("Cannot move the root node.");

                    var oldParentId = node.ParentNodeId;
                    node.ParentNodeId = newParentId;

                    var max = s.QueryOver<AccountabilityNode>().Where(x => x.ParentNodeId == newParentId && x.DeleteTime == null).Select(x => x.Ordering).List<int?>().Max();
                    ordering = 1 + max ?? 0;
                    node.Ordering = ordering;
                    s.Update(node);

                    var nodes = s.QueryOver<AccountabilityNode>().Where(x => x.AccountabilityChartId == node.AccountabilityChartId && x.DeleteTime == null)
                        .Select(x => x.Id, x => x.ParentNodeId)
                        .List<object[]>()
                        .Select(x => new {
                            nodeId = (long)x[0],
                            parentId = (long?)x[1],
                        }).ToList();

                    if (GraphUtility.HasCircularDependency(nodes, x => x.nodeId, x => x.parentId)) {
                        throw new PermissionsException("A circular dependancy was found. Node cannot be a parent of itself.");
                    }
                    if (oldParentId != newParentId) {
                        var now = DateTime.UtcNow;
                        if (oldParentId != null) {
                            var oldParentNode = s.Get<AccountabilityNode>(oldParentId.Value);
#pragma warning disable CS0618 // Type or member is obsolete
                            DeepAccessor.Remove(s, oldParentNode, node, now);
#pragma warning restore CS0618 // Type or member is obsolete

                            //REMOVE MANAGER
                            if (oldParentNode.UserId != null && node.UserId != null) {
                                var found = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.ManagerId == oldParentNode.UserId && x.SubordinateId == node.UserId).Take(1).SingleOrDefault();
                                if (found != null) {
                                    found.DeleteTime = now;
                                    s.Update(found);
                                    s.GetFresh<UserOrganizationModel>(oldParentNode.UserId).UpdateCache(s);
                                }
                            }

                            if (oldParentNode.User != null && oldParentNode.User.IsManager() && !DeepAccessor.HasChildren(s, oldParentNode.Id)){
                            	UserAccessor.EditUser(s, perms, oldParentNode.User.Id, false);
                            	oldParentNode.User.ManagerAtOrganization = false;
                            }
                        }

                        var newParentNode = s.Get<AccountabilityNode>(newParentId);
#pragma warning disable CS0618 // Type or member is obsolete
                        DeepAccessor.Add(s, newParentNode, node, node.OrganizationId, now);
#pragma warning restore CS0618 // Type or member is obsolete
                        if (newParent.User != null && !newParentNode.User.IsManager()) {
                            UserAccessor.EditUser(s, perms, newParentNode.User.Id, true);
                            newParentNode.User.ManagerAtOrganization = true;
                        }

                        //ADD MANAGER
                        if (node.UserId != null && newParentNode.UserId != null) {
                            var md = new ManagerDuration() {
                                ManagerId = newParentNode.UserId.Value,
                                Manager = s.Load<UserOrganizationModel>(newParentNode.UserId.Value),
                                SubordinateId = node.UserId.Value,
                                Subordinate = s.Load<UserOrganizationModel>(node.UserId.Value),
                                PromotedBy = perms.GetCaller().Id,
                                CreateTime = now
                            };
                            s.Save(md);

                            s.GetFresh<UserOrganizationModel>(newParentNode.UserId.Value).UpdateCache(s);
                        }

                        if (node.UserId != null) {
                            s.GetFresh<UserOrganizationModel>(node.UserId.Value).UpdateCache(s);
                        }

                        //if (node.UserId != null)
                        //{
                        //	var user = s.Get<UserOrganizationModel>(node.UserId);
                        //	if (oldParentId != null)
                        //	{
                        //		var oldNode = s.Get<AccountabilityNode>(oldParentId);
                        //		if (oldNode.UserId != null)
                        // UserAccessor.RemoveManager(s, perms, node.UserId.Value, oldNode.UserId.Value, now);

                        //	}
                        //	if (newParentId != null)
                        //	{
                        //		var newNode = s.Get<AccountabilityNode>(newParentId);
                        //		if (newNode.UserId != null)
                        //			UserAccessor.AddManager(s, perms, node.UserId.Value, newNode.UserId.Value, now, true);

                        //	}
                        //}

                        // var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
                        // var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId), connectionId);

                        var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
                        var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId), connectionId); //skips updating self
                        var update = new AngularUpdate();
                        update.Add(new AngularAccountabilityNode(newParentId) {
                            children = AngularList.CreateFrom(AngularListType.Add, new AngularAccountabilityNode(node.Id) {
                                order = ordering
                            })
                        });
                        if (oldParentId != null) {
                            update.Add(new AngularAccountabilityNode(oldParentId.Value) {
                                children = AngularList.CreateFrom(AngularListType.Remove, new AngularAccountabilityNode(node.Id))
                            });
                        }
                        orgHub.update(update);
                    } else {
                        var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
                        var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId)); //updates self also
                        var update = new AngularUpdate();
                        update.Add(new AngularAccountabilityNode(node.Id) {
                            order = ordering
                        });
                        orgHub.update(update);
                    }

                    tx.Commit();
                    s.Flush();

                }
            }

        }
#pragma warning disable CS0618 // Type or member is obsolete
        public static AccountabilityNode AppendNode(ISession s, PermissionsUtility perms, RealTimeUtility rt, long parentNodeId, long? rolesGroupId = null, long? userId = null) {
            return AppendNode(s, perms, rt, parentNodeId, rolesGroupId, userId, false);
        }
#pragma warning restore CS0618 // Type or member is obsolete

        [Obsolete("Use the other AppendNode")]
        public static AccountabilityNode AppendNode(ISession s, PermissionsUtility perms, RealTimeUtility rt, long parentNodeId, long? rolesGroupId, long? userId, bool skipAddManager) {
            var now = DateTime.UtcNow;
            var parent = s.Get<AccountabilityNode>(parentNodeId);
            if (parent == null)
                throw new PermissionsException("Parent does not exist");
            perms.ManagesAccountabilityNodeOrSelf(parent.Id);//.EditHierarchy(parent.AccountabilityChartId);
            AccountabilityRolesGroup group = null;
            if (rolesGroupId != null) {
                group = s.Get<AccountabilityRolesGroup>(rolesGroupId);
                if (group.OrganizationId != parent.OrganizationId)
                    throw new PermissionsException("Could not access node");
            } else {
                group = new AccountabilityRolesGroup() {
                    OrganizationId = parent.OrganizationId,
                    AccountabilityChartId = parent.AccountabilityChartId,
                    CreateTime = now
                };
                s.Save(group);
            }

            if (parent.User != null && !parent.User.IsManager()) {
                UserAccessor.EditUser(s, perms, parent.User.Id, true);
                parent.User.ManagerAtOrganization = true;
            }


            var max = s.QueryOver<AccountabilityNode>().Where(x => x.ParentNodeId == parentNodeId && x.DeleteTime == null).Select(x => x.Ordering).List<int?>().Max();
            var ordering = 1 + max ?? 0;

            var node = new AccountabilityNode() {
                OrganizationId = parent.OrganizationId,
                ParentNodeId = parentNodeId,
                ParentNode = parent,
                AccountabilityRolesGroupId = group.Id,
                AccountabilityRolesGroup = group,
                AccountabilityChartId = parent.AccountabilityChartId,
                CreateTime = now,
                Ordering = ordering
            };
            s.Save(node);

            DeepAccessor.Add(s, parent, node, parent.OrganizationId, now, false);

            if (userId != null) {
                SetUser(s, rt, perms, node.Id, userId, skipAddManager, false, now);
            }
            //var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
            //var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId));
            var updater = rt.UpdateOrganization(node.OrganizationId);

            updater.Update(new AngularAccountabilityChart(node.AccountabilityChartId) {
                ExpandNode = parentNodeId
            });
            updater.Update(new AngularAccountabilityNode(parentNodeId) {
                children = AngularList.CreateFrom(AngularListType.Add, new AngularAccountabilityNode(node))
            });

            return node;


        }

        public static AccountabilityNode AppendNode(UserOrganizationModel caller, long parentNodeId, long? rolesGroupId = null, long? userId = null) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    using (var rt = RealTimeUtility.Create()) {
                        var perms = PermissionsUtility.Create(s, caller);
                        var node = AppendNode(s, perms, rt, parentNodeId, rolesGroupId, userId);

                        tx.Commit();
                        s.Flush();

                        return node;
                    }
                }
            }
        }

        public static void AddRole(UserOrganizationModel caller, Attach attach) {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    using (var rt = RealTimeUtility.Create()) {
                        var perms = PermissionsUtility.Create(s, caller);
                        AddRole(s, perms, rt, attach);

                        tx.Commit();
                        s.Flush();
                    }
                }
            }
        }

        public static void AddRole(ISession s, PermissionsUtility perms, RealTimeUtility rt, Attach attachTo) {

            perms.EditAttach(attachTo);

            var orgId = AttachAccessor.GetOrganizationId(s, attachTo);

            var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
            var now = DateTime.UtcNow;

            //long? templateId = null;

            var r = new RoleModel() {
                OrganizationId = orgId,
                Category = category,
                CreateTime = now,
            };
            s.Save(r);
            HooksRegistry.Each<IRolesHook>(x => x.CreateRole(s, r));

            UserTemplateAccessor.AddRoleToAttach_Unsafe(s, perms, orgId, attachTo, r);

            var updatedRoles = AngularList.CreateFrom(AngularListType.Add, new AngularRole(r));
            rt.UpdateOrganization(orgId).Update(new AngularRoleGroup(attachTo, updatedRoles));
        }


        public static AccountabilityChart CreateChart(ISession s, PermissionsUtility perms, long organizationId, bool creatorCanAdmin = true) {
            perms.ViewOrganization(organizationId);
            var now = DateTime.UtcNow;

            var chart = new AccountabilityChart() {
                OrganizationId = organizationId,
                Name = s.Get<OrganizationModel>(organizationId).GetName(),
                CreateTime = now,
            };
            s.Save(chart);

            var group = new AccountabilityRolesGroup() {
                OrganizationId = organizationId,
                AccountabilityChartId = chart.Id,
                CreateTime = now,
            };
            s.Save(group);

            var root = new AccountabilityNode() {
                OrganizationId = organizationId,
                ParentNodeId = null,
                ParentNode = null,
                AccountabilityRolesGroupId = group.Id,
                AccountabilityRolesGroup = group,
                AccountabilityChartId = chart.Id,
                CreateTime = now,
            };
            s.Save(root);
            //DeepAccessor.Add(s, root, root, organizationId, now);

            chart.RootId = root.Id;
            s.Update(chart);

            PermissionsAccessor.CreatePermItems(s, perms.GetCaller(), PermItem.ResourceType.AccountabilityHierarchy, chart.Id,
                PermTiny.Admins(),
                PermTiny.Creator(view: creatorCanAdmin, edit: creatorCanAdmin, admin: creatorCanAdmin),
                PermTiny.Members(edit: false, admin: false)
            );

            return chart;
        }

        public static AccountabilityChart CreateChart(UserOrganizationModel caller, long organizationId, bool creatorCanAdmin = true) {
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

        public static void UpdatePosition_Unsafe(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, long? positionId, DateTime now, bool skipAddPosition = false) {
            var node = s.Get<AccountabilityNode>(nodeId);
            var arg = node.AccountabilityRolesGroup;
            var updater = rt.UpdateOrganization(arg.OrganizationId);

            AngularPosition newPosition = null;

            if (arg.PositionId != positionId) {
                //Delete old position
                if (arg.PositionId != null) {
                    if (node.UserId != null) {
                        var pd = s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null && x.Position.Id == arg.PositionId && x.UserId == node.UserId).Take(1).SingleOrDefault();
                        if (pd != null) {
                            pd.DeleteTime = now;
                            pd.DeletedBy = perms.GetCaller().Id;
                            s.Update(pd);

                            s.GetFresh<UserOrganizationModel>(node.UserId.Value).UpdateCache(s);


                        }
                    }
                    newPosition = positionId == null ? Removed.From<AngularPosition>() : null;
                    updater.ForceUpdate(new AngularAccountabilityGroup(arg.Id) {
                        RoleGroups = AngularList.CreateFrom(AngularListType.Remove, new AngularRoleGroup(new Attach(AttachType.Position, arg.PositionId.Value), null))
                    });
                }

                //Add new position
                if (positionId != null) {
                    perms.ViewOrganizationPosition(positionId.Value);
                    arg.Position = s.Get<OrganizationPositionModel>(positionId);

                    if (!skipAddPosition) {
                        if (node.UserId != null) {
                            var pd = new PositionDurationModel() {
                                UserId = node.UserId.Value,
                                CreateTime = now,
                                Position = arg.Position,
                                PromotedBy = perms.GetCaller().Id,
                                OrganizationId = node.OrganizationId
                            };
                            s.Save(pd);
                            s.GetFresh<UserOrganizationModel>(node.UserId.Value).UpdateCache(s);
                        }
                    }

                    var addedRoles = RoleAccessor.GetRolesForAttach_Unsafe(s, new Attach(AttachType.Position, positionId.Value));
                    var angRoles = addedRoles.Select(x => new AngularRole(x));

                    newPosition = new AngularPosition(arg.Position);

                    updater.ForceUpdate(new AngularAccountabilityGroup(arg.Id) {
                        RoleGroups = AngularList.CreateFrom(AngularListType.Add, new AngularRoleGroup(new Attach(AttachType.Position, positionId.Value), angRoles))

                    });
                } else {
                    //ALREADY DONE ABOVE CORRECT ????

                    //if (arg.PositionId != null && node.UserId != null) {
                    //	var pd = s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null && x.Position.Id == arg.PositionId && x.UserId == node.UserId).Take(1).SingleOrDefault();
                    //	if (pd != null) {
                    //		pd.DeleteTime = DateTime.UtcNow;
                    //		pd.DeletedBy = perms.GetCaller().Id;
                    //		s.Update(pd);
                    //		s.GetFresh<UserOrganizationModel>(node.UserId.Value).UpdateCache(s);
                    //	}
                    //}
                    //updater.Update(new AngularAccountabilityGroup(arg.Id) {
                    //	Position = Removed.From<AngularPosition>()
                    //});
                }

                if (newPosition != null) {
                    updater.Update(new AngularAccountabilityGroup(arg.Id) {
                        Position = newPosition
                    });
                }

                arg.PositionId = positionId;
                s.Update(arg);
            }
        }

        public static void UpdateAccountabilityNode(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, AngularAccountabilityGroup newARG, long? userId) {
            perms.ManagesAccountabilityNodeOrSelf(nodeId);

            var node = s.Get<AccountabilityNode>(nodeId);

            var now = DateTime.UtcNow;

            if (newARG != null) {

                if (newARG.Position.NotNull(x => (long?)x.Id) < 0) {

                    var count = s.QueryOver<OrganizationPositionModel>().Where(x => x.DeleteTime == null && x.CustomName == newARG.Position.Name && x.Organization.Id == node.OrganizationId).RowCount();
                    if (count == 0) {

                        //throw new PermissionsException("Position with this name already exists.");

                        perms.EditPositions(node.OrganizationId);
                        var opm = new OrganizationPositionModel() {
                            CreatedBy = perms.GetCaller().Id,
                            Organization = s.Load<OrganizationModel>(node.OrganizationId),
                            CustomName = newARG.Position.Name
                        };
                        s.Save(opm);
                        newARG.Position.Id = opm.Id;

                    }
                }

                // Do not change to (long?). we want zero when null (UBER HAX)
                if (newARG.Position.NotNull(x => x.Id) >= 0)
                    UpdatePosition_Unsafe(s, rt, perms, nodeId, newARG.Position.NotNull(x => (long?)x.Id), now);

            }
            var updater = rt.UpdateOrganization(node.OrganizationId);
            if (node.UserId != userId) {
                SetUser(s, rt, perms, node.Id, userId);
            }
        }




        public static void UpdateRole(ISession s, RealTimeUtility rt, PermissionsUtility perms, long roleId, string name) {
            perms.EditRole(roleId);

            SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateRole(roleId));

            var role = s.Get<RoleModel>(roleId);

            role.Role = name;
            s.Update(role);
            HooksRegistry.Each<IRolesHook>(x => x.UpdateRole(s, role));

            rt.UpdateOrganization(role.OrganizationId).Update(new AngularRole(roleId) {
                Name = name ?? Removed.String()
            });

        }

        public const string CREATE_TEXT = " (Create)";

        public static void _FinishUploadAccountabilityChart(UserOrganizationModel caller, List<UserOrganizationModel> existingUsers, Dictionary<long, string[]> managerLookup, CounterSet<string> errors) {
            var nodeLookup = new Dictionary<long, AccountabilityNode>();
            var set = new HashSet<long>(existingUsers.Select(x => x.Id));
            var links = new HashSet<Tuple<long, long>>(managerLookup.Select(m => {
                var manager = existingUsers.FirstOrDefault(x => x.GetFirstName() == m.Value[0] && x.GetLastName() == m.Value[1]);
                if (manager == null)
                    return null;
                return Tuple.Create(manager.Id, m.Key);
            }).Where(x => x != null));

            var sort = GraphUtility.TopologicalSort(set, links);

            if (sort == null) {
                throw new PermissionsException("Circular reference detected! ");
            }

            var toAddNodes = set.ToList();

            var root = AccountabilityAccessor.GetRoot(caller, caller.Organization.AccountabilityChartId);


            foreach (var s in set.Where(x => !links.Any(y => y.Item1 == x))) {
                toAddNodes.Remove(s);
                nodeLookup[s] = AccountabilityAccessor.AppendNode(caller, root.Id, userId: s);
            }

            foreach (var managerId in sort) {
                foreach (var link in links.Where(x => x.Item1 == managerId)) {
                    var subId = link.Item2;
                    if (toAddNodes.Any(x => x == subId)) {
                        toAddNodes.Remove(subId);
                        var foundManager = existingUsers.FirstOrDefault(x => x.Id == managerId);
                        if (!foundManager.IsManager()) {
                            new UserAccessor().EditUser(caller, foundManager.Id, true);
                            foundManager.ManagerAtOrganization = true;
                        }
                        long? managerNodeId = null;
                        if (nodeLookup.ContainsKey(managerId)) {
                            managerNodeId = nodeLookup[managerId].Id;
                        } else {
                            var mNode = DeepAccessor.Users.GetNodesForUser(caller, managerId).FirstOrDefault();
                            if (mNode != null)
                                managerNodeId = mNode.Id;
                        }
                        if (managerNodeId != null) {
                            nodeLookup[subId] = AccountabilityAccessor.AppendNode(caller, managerNodeId.Value, userId: subId);
                        } else {
                            errors.Add("Could not create accountability node.");
                        }
                    }
                }
            }
        }
    }
}