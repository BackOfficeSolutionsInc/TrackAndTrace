﻿using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Positions;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Permissions;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static RadialReview.Utilities.RealTime.RealTimeUtility;

namespace RadialReview.Accessors {
	public class AccountabilityAccessor : BaseAccessor {
		#region old
		protected class LONG {
			public long id { get; set; }
		}
		protected static AccountabilityTree DiveOld(long caller, LONG id, long parent, List<UserOrganizationModel> users, List<RoleModel> roles, List<DeepAccountability> links, List<ManagerDuration> mds) {
			var own = links.Any(x => x.Parent.UserId == caller && x.Child.UserId == parent);
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
				collapsed = !links.Any(x => (x.Parent.UserId == caller || x.Child.UserId == caller) && x.Parent.UserId == parent)
			};
		}

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

		public static void FixDeep() {



		}


		#endregion

		protected static AngularAccountabilityNode Dive(long nodeId, List<AccountabilityNode> nodes, List<AccountabilityRolesGroup> groups, List<AccountabilityNodeRoleMap> roles, List<UserTemplate> templates, List<RoleModel> templateRoles) {
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

			var myTemplates = templates.Where(x => x.AttachType == Models.Enums.AttachType.Position && group.PositionId == x.AttachId);
			var myTemplateIds = myTemplates.Select(x => x.Id).ToList();
			var myTemplateRoles = templateRoles.Where(x => myTemplateIds.Any(y => y == x.FromTemplateItemId)).ToList();

			var newRoles = aan.Group.Roles.ToList();
			newRoles.AddRange(myTemplateRoles.Select(x => new AngularRole(x)));
			aan.Group.Roles = newRoles;

			return aan;
		}

		public static AngularAccountabilityChart GetTree(UserOrganizationModel caller, long chartId, long? centerUserId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewHierarchy(chartId);

					var chart = s.Get<AccountabilityChart>(chartId);

					var nodes = s.QueryOver<AccountabilityNode>().Where(x => x.AccountabilityChartId == chartId && x.DeleteTime == null).Future();
					var groups = s.QueryOver<AccountabilityRolesGroup>().Where(x => x.AccountabilityChartId == chartId && x.DeleteTime == null).Future();
					var nodeRoles = s.QueryOver<AccountabilityNodeRoleMap>().Where(x => x.AccountabilityChartId == chartId && x.DeleteTime == null).Future();
					var userTemplates = s.QueryOver<UserTemplate>().Where(x => x.OrganizationId == chart.OrganizationId && x.DeleteTime == null).Future();

					var positionRoles = s.QueryOver<RoleModel>().Where(x => x.OrganizationId == chart.OrganizationId && x.DeleteTime == null && x.FromTemplateItemId != null).Future();

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
						AllUsers = usersF.ToList().Select(x => AngularUser.CreateUser(x)).ToList()
					};

				}
			}
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

		public static void SetUser(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, long? userId, bool skipAddManger = false) {
			perms.ManagesUserOrganization(userId.Value, true, PermissionType.EditEmployeeManagers)
				 .ManagesAccountabilityNode(nodeId, PermissionType.EditEmployeeManagers);

			//SetUser_Unsafe(s, nodeId, userId);
			var n = s.Get<AccountabilityNode>(nodeId);

			var now = DateTime.UtcNow;

			if (userId != n.UserId) {

				if (userId != null) {
					//ADDING USER TO NODE

					n.User = s.Get<UserOrganizationModel>(userId);
					n.UserId = n.User.Id;

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
									Start = now
								};
								s.Save(md);
								s.Get<UserOrganizationModel>(c.UserId).UpdateCache(s);
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
							Start = now
						};
						s.Save(md);						
						s.Get<UserOrganizationModel>(n.ParentNode.UserId).UpdateCache(s);
					}
					s.Update(n);
					
					n.User.UpdateCache(s);

					rt.UpdateOrganization(n.OrganizationId).Update(new AngularAccountabilityNode(n.Id) {
						User = AngularUser.CreateUser(n.User)
					});
				} else {
					//REMOVING USER FROM NODE

					//do this before setting null
					//if (n.User!=null && n.User.IsManager() && !DeepAccessor.Users.GetSubordinatesAndSelf(s,perms.GetCaller(), n.UserId.Value).Any()){
					//	UserAccessor.EditUser(s, perms, n.User.Id, false);
					//	n.User.ManagerAtOrganization = false;
					//}

					//REMOVE MANAGER
					if (n.ParentNode != null && n.ParentNode.UserId != null) {
						var found = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.ManagerId == n.ParentNode.UserId && x.SubordinateId == n.UserId).Take(1).SingleOrDefault();
						if (found != null) {
							found.DeleteTime = now;
							s.Update(found);
						}

						if (n.ParentNode.UserId != null) {
							s.Get<UserOrganizationModel>(n.ParentNode.UserId).UpdateCache(s);
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

							s.Get<UserOrganizationModel>(c.UserId).UpdateCache(s);
						}
					}


					if (n.UserId != null) {
						s.Get<UserOrganizationModel>(n.UserId).UpdateCache(s);
					}

					n.User = null;
					n.UserId = null;
					s.Update(n);

					rt.UpdateOrganization(n.OrganizationId).Update(new AngularAccountabilityNode(n.Id) {
						User = Removed.From<AngularUser>()
					});
				}
			} else {
				//No change?
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
					var perms = PermissionsUtility.Create(s, caller);
					var node = s.Get<AccountabilityNode>(nodeId);
					perms.EditHierarchy(node.OrganizationId);
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
					//REMOVE MANAGER
					if (node.UserId != null && node.ParentNode != null && node.ParentNode.UserId != null) {
						var found = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.ManagerId == node.ParentNode.UserId && x.SubordinateId == node.UserId).Take(1).SingleOrDefault();
						if (found != null) {
							found.DeleteTime = now;
							s.Update(found);

							s.Get<UserOrganizationModel>(found.SubordinateId).UpdateCache(s);
							s.Get<UserOrganizationModel>(found.ManagerId).UpdateCache(s);

						} else {
							log.Error("Removing manager. ManagerDuration not found. " + node.ParentNode.UserId + " " + node.UserId);
						}
					}

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

		public static void SwapParents(UserOrganizationModel caller, long nodeId, long newParentId, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					var node = s.Get<AccountabilityNode>(nodeId);
					if (node == null)
						throw new PermissionsException("Node does not exist");
					perms.EditHierarchy(node.OrganizationId);

					var newParent = s.Get<AccountabilityNode>(newParentId);

					if (node.AccountabilityChartId != newParent.AccountabilityChartId)
						throw new PermissionsException("Nodes are not on the same chart.");

					if (node.ParentNodeId == null)
						throw new PermissionsException("Cannot move the root node.");

					var oldParentId = node.ParentNodeId;
					node.ParentNodeId = newParentId;

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
							DeepAccessor.Remove(s, oldParentNode, node, now);

							//REMOVE MANAGER
							if (oldParentNode.UserId != null && node.UserId != null) {
								var found = s.QueryOver<ManagerDuration>().Where(x => x.DeleteTime == null && x.ManagerId == oldParentNode.UserId && x.SubordinateId == node.UserId).Take(1).SingleOrDefault();
								if (found != null) {
									found.DeleteTime = now;
									s.Update(found);
									s.Get<UserOrganizationModel>(oldParentNode.UserId).UpdateCache(s);
								}
							}
							
							//if (oldNode.User != null && oldNode.User.IsManager() && !DeepAccessor.HasChildren(s, oldNode.Id)){
							//	UserAccessor.EditUser(s, perms, oldNode.User.Id, false);
							//	oldNode.User.ManagerAtOrganization = false;
							//}
						}

						var newParentNode = s.Get<AccountabilityNode>(newParentId);
						DeepAccessor.Add(s, newParentNode, node, node.OrganizationId, now);
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
								Start = now
							};
							s.Save(md);
							s.Get<UserOrganizationModel>(newParentNode.UserId.Value).UpdateCache(s);
						}
						
						if (node.UserId != null) {
							s.Get<UserOrganizationModel>(node.UserId.Value).UpdateCache(s);
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

						var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
						var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId), connectionId);

						var update = new AngularUpdate();
						update.Add(new AngularAccountabilityNode(newParentId) {
							children = AngularList.CreateFrom(AngularListType.Add, new AngularAccountabilityNode(node.Id))
						});
						if (oldParentId != null) {
							update.Add(new AngularAccountabilityNode(oldParentId.Value) {
								children = AngularList.CreateFrom(AngularListType.Remove, new AngularAccountabilityNode(node.Id))
							});
						}
					}

					tx.Commit();
					s.Flush();

				}
			}

		}

		public static AccountabilityNode AppendNode(ISession s, PermissionsUtility perms, RealTimeUtility rt, long parentNodeId, long? rolesGroupId = null, long? userId = null, bool skipAddManager = false) {
			var now = DateTime.UtcNow;
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
					CreateTime = now
				};
				s.Save(group);
			}

			if (parent.User != null && !parent.User.IsManager()) {
				UserAccessor.EditUser(s, perms, parent.User.Id, true);
				parent.User.ManagerAtOrganization = true;
			}

			var node = new AccountabilityNode() {
				OrganizationId = parent.OrganizationId,
				ParentNodeId = parentNodeId,
				ParentNode = parent,
				AccountabilityRolesGroupId = group.Id,
				AccountabilityRolesGroup = group,
				AccountabilityChartId = parent.AccountabilityChartId,
				CreateTime = now
			};
			s.Save(node);

			DeepAccessor.Add(s, parent, node, parent.OrganizationId, now, false);



			if (userId != null) {
				SetUser(s, rt, perms, node.Id, userId, skipAddManager);
			}
			//var hub = GlobalHost.ConnectionManager.GetHubContext<OrganizationHub>();
			//var orgHub = hub.Clients.Group(OrganizationHub.GenerateId(node.OrganizationId));

			rt.UpdateOrganization(node.OrganizationId).Update(new AngularAccountabilityNode(parentNodeId) {
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
		public static void UpdateAccountabilityRolesGroup_Unsafe(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, long? positionId) {
			var node = s.Get<AccountabilityNode>(nodeId);
			var arg = node.AccountabilityRolesGroup;
			var updater = rt.UpdateOrganization(arg.OrganizationId);

			if (arg.PositionId != positionId) {
				if (positionId != null) {
					perms.ViewOrganizationPosition(positionId.Value);
					arg.Position = s.Get<OrganizationPositionModel>(positionId);
					updater.Update(new AngularAccountabilityGroup(arg.Id) {
						Position = new AngularPosition(arg.Position)
					});
				} else {
					updater.Update(new AngularAccountabilityGroup(arg.Id) {
						Position = Removed.From<AngularPosition>()
					});
				}
				arg.PositionId = positionId;
				s.Update(arg);
			}
		}

		public static void UpdateAccountabilityNode(ISession s, RealTimeUtility rt, PermissionsUtility perms, long nodeId, AngularAccountabilityGroup newARG, long? userId) {
			perms.EditAccountabilityNode(nodeId);

			var node = s.Get<AccountabilityNode>(nodeId);

			if (newARG != null) {
				if (newARG.Position.NotNull(x => x.Id) == -2) {
					perms.EditPositions(node.OrganizationId);
					var opm = new OrganizationPositionModel() {
						CreatedBy = perms.GetCaller().Id,
						Organization = s.Load<OrganizationModel>(node.OrganizationId),
						CustomName = newARG.Position.Name.SubstringBefore(CREATE_TEXT)
					};
					s.Save(opm);
					newARG.Position.Id = opm.Id;
				}

				UpdateAccountabilityRolesGroup_Unsafe(s, rt, perms, nodeId, newARG.Position.NotNull(x => (long?)x.Id));
			}
			var updater = rt.UpdateOrganization(node.OrganizationId);
			if (node.UserId != userId) {
				SetUser(s, rt, perms, node.Id, userId);
				//node.UserId = userId;
				//s.Update(node);
			}

		}

		public static void Update(UserOrganizationModel caller, IAngularItem model, string connectionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					using (var rt = RealTimeUtility.Create(connectionId)) {
						var perms = PermissionsUtility.Create(s, caller);

						if (model.Type == typeof(AngularAccountabilityNode).Name) {
							var m = (AngularAccountabilityNode)model;
							//UpdateIssue(caller, (long)model.GetOrDefault("Id", null), (string)model.GetOrDefault("Name", null), (string)model.GetOrDefault("Details", null), (bool?)model.GetOrDefault("Complete", null), connectionId);
							UpdateAccountabilityNode(s, rt, perms, m.Id, m.Group, m.User.NotNull(x => (long?)x.Id));
						} else {
							throw new PermissionsException("Unhandled type: " + model.Type);
						}

						tx.Commit();
						s.Flush();
					}
				}
			}
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