using Amazon.SimpleDB.Model;
using FluentNHibernate.Utils;
using log4net.Core;
using Microsoft.Ajax.Utilities;
using NHibernate;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Hql.Ast.ANTLR.Tree;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Permissions;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebGrease.Css.Extensions;

namespace RadialReview.Accessors {
    public class PermissionsAccessor {

        public void Permitted(UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    ensurePermitted(PermissionsUtility.Create(s, caller));
                }
            }
        }

        public bool IsPermitted(UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted)
        {
            try {
                Permitted(caller, ensurePermitted);
                return true;
            } catch (Exception) {
                return false;
            }
        }



        public List<PermissionOverride> AllPermissionsAtOrganization(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);
                    var ps = s.QueryOver<PermissionOverride>().Where(x => x.DeleteTime == null && x.Organization.Id == organizationId).List().ToList();

                    return ps;
                }
            }
        }

        public static bool AnyTrue(ISession s, UserOrganizationModel caller, PermissionType? type, Predicate<UserOrganizationModel> predicate)
        {
            if (predicate(caller))
                return true;
            if (type != null) {
                var ids = s.QueryOver<PermissionOverride>().Where(x => x.DeleteTime == null && x.Permissions == type && x.ForUser.Id == caller.Id).Select(x => x.AsUser.Id).List<long>().ToList();
                var uorgs = s.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(ids).List().ToList();

                if (uorgs.Any(o => predicate(o))) {
                    return true;
                }
            }
            return false;
        }

        public bool AnyTrue(UserOrganizationModel caller, PermissionType type, Predicate<UserOrganizationModel> predicate)
        {
            if (predicate(caller))
                return true;
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    return AnyTrue(s, caller, type, predicate);
                }
            }
        }

        public static PermissionOverride GetPermission(UserOrganizationModel caller, long overrideId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    if (overrideId == 0)
                        return new PermissionOverride();

                    var p = s.Get<PermissionOverride>(overrideId);
                    PermissionsUtility.Create(s, caller).ViewOrganization(p.Organization.Id);
                    return p;
                }
            }

        }

        public static void EditPermission(UserOrganizationModel caller, long permissionsOverrideId, long forUserId, PermissionType permissionType, long copyFromUserId, DateTime? deleteTime = null)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var p = (permissionsOverrideId == 0) ? new PermissionOverride() : s.Get<PermissionOverride>(permissionsOverrideId);
                    PermissionsUtility.Create(s, caller).EditPermissionOverride(p.Id);

                    p.ForUser = s.Load<UserOrganizationModel>(forUserId);
                    p.AsUser = s.Load<UserOrganizationModel>(copyFromUserId);
                    p.Permissions = permissionType;
                    p.DeleteTime = deleteTime;

                    if (p.Id == 0) {
                        p.Organization = caller.Organization;
                    }

                    s.SaveOrUpdate(p);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static void LoadPermItem(ISession s, IEnumerable<PermItem> items)
        {
            var permItems = items as IList<PermItem> ?? items.ToList();
            if (permItems.Any(x => x.AccessorId > 0)) {
                var found = s.QueryOver<ResponsibilityGroupModel>().Where(x => x.DeleteTime == null).AndRestrictionOn(x => x.Id).IsIn(permItems.Where(x => x.AccessorType == PermItem.AccessType.RGM || x.AccessorType == PermItem.AccessType.Creator).Select(x => x.AccessorId).Distinct().ToList()).List().ToList();
                foreach (var f in found) {
                    permItems.Where(x => x.AccessorId == f.Id && (x.AccessorType == PermItem.AccessType.RGM || x.AccessorType == PermItem.AccessType.Creator)).ForEach(x => {
                        x._DisplayText = f.GetName();
                        x._ImageUrl = f.GetImageUrl();
                        x._DisplayInitials = (f as UserOrganizationModel).NotNull(y => y.GetInitials());
                        x._Color = (f as UserOrganizationModel).NotNull(y => y.GeUserHashCode());
                    });
                }
            }

            permItems.Where(x => x.AccessorType == PermItem.AccessType.Members).ForEach(x => {
                x._DisplayText = "Members";
                x._ImageUrl = ConstantStrings.AmazonS3Location + ConstantStrings.ImageGroupPlaceholder;
            });
            permItems.Where(x => x.AccessorType == PermItem.AccessType.Creator).ForEach(x => {
                x._DisplayText = "Creator";
                x._ImageUrl = x._ImageUrl ?? (ConstantStrings.AmazonS3Location + ConstantStrings.ImageUserPlaceholder);
            });
            permItems.Where(x => x.AccessorType == PermItem.AccessType.Admins).ForEach(x => {
                x._DisplayText = "Admins";
                x._ImageUrl = (ConstantStrings.AmazonS3Location + "placeholder/Star.png");
            });

            var emailPerms = permItems.Where(x =>x.AccessorType == PermItem.AccessType.Email);
            if (emailPerms.Any()){
                 var emails = s.QueryOver<EmailPermItem>()
                     .Where(x=>x.DeleteTime==null)
                     .WhereRestrictionOn(x => x.Id).IsIn(emailPerms.Select(x=>x.AccessorId).ToArray())
                     .List().ToList();
                 emailPerms.ForEach(x => {
                     x._DisplayText = emails.FirstOrDefault(y => y.Id == x.AccessorId).NotNull(y => y.Email);
                     x._ImageUrl = (ConstantStrings.AmazonS3Location + ConstantStrings.ImageUserPlaceholder);
                 });
            }

        }

        public static List<long> GetPermItemsForUser(ISession s, PermissionsUtility perms, long forUserId, PermItem.ResourceType resourceType)
        {
            var groups = ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(s.ToQueryProvider(true), perms, forUserId).ToList();
            var permList = s.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.ResType == resourceType)
                .WhereRestrictionOn(x => x.AccessorId).IsIn(groups.Select(x => x.Id).ToList())
                .Select(x => x.ResId).List<long>().ToList();
            return permList;
        }


        public static PermissionDropdownVM GetPermItems(UserOrganizationModel caller, long resourceId, PermItem.ResourceType resourceType)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanViewPermissions(resourceType, resourceId);
                    var admin = false;
                    try {
                        perms.CanAdmin(resourceType, resourceId);
                        admin = true;
                    } catch {
                    }


                    var items = s.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.ResId == resourceId && x.ResType == resourceType).List().ToList();

                    LoadPermItem(s, items);


                    return new PermissionDropdownVM() {
                        CanEdit_Admin = admin,
                        CanEdit_Edit = admin,
                        CanEdit_View = admin,
                        DisplayText = new HtmlString("Permissions"),
                        ResId = resourceId,
                        ResType = resourceType,
                        Items = items.Select(x=>PermItemVM.Create(x,admin)).ToList()
                    };
                }
            }
        }


        public static PermItemVM CreatePermItem(UserOrganizationModel caller, PermItemVM model, PermItem.ResourceType type, long resourceId)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanAdmin(type, resourceId);

                    var pi = new PermItem() {
                        AccessorId = model.AccessorId,
                        AccessorType = model.AccessorType,
                        CanAdmin = model.CanAdmin,
                        CanEdit = model.CanEdit,
                        CanView = model.CanView,
                        CreateTime = DateTime.UtcNow,
                        OrganizationId = caller.Organization.Id,
                        CreatorId = caller.Id,
                        ResId = resourceId,
                        ResType = type,
                        _DisplayText = model.Title,
                        _ImageUrl = model.ImageUrl,
                        IsArchtype = false,

                    };
                    s.Save(pi);
                    tx.Commit();
                    s.Flush();
                    LoadPermItem(s, pi.AsList());
                    model.Id = pi.Id;
                    return model;
                }
            }
        }

        public static PermItemVM EditPermItem(UserOrganizationModel caller, long id, bool? view, bool? edit, bool? admin)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var perms = PermissionsUtility.Create(s, caller);
                    var model = s.Get<PermItem>(id);
                    if (model == null)
                        throw new PermissionsException("Permission setting does not exist.");
                    perms.CanAdmin(model.ResType, model.ResId);
                    model.CanAdmin = admin ?? model.CanAdmin;
                    model.CanEdit = edit ?? model.CanEdit;
                    model.CanView = view ?? model.CanView;
					//model.CanDelete = model.CanAdmin;
                    s.Update(model);

                    perms.EnsureAdminExists(model.ResType, model.ResId);

                    tx.Commit();
                    s.Flush();


                    return PermItemVM.Create(model, true);
                }
            }
		}
		public static void DeletePermItem(UserOrganizationModel caller, long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var model = s.Get<PermItem>(id);
					if (model == null || model.DeleteTime != null)
						throw new PermissionsException("Permission setting does not exist.");
					perms.CanAdmin(model.ResType, model.ResId);
					model.DeleteTime = DateTime.UtcNow;
					s.Update(model);
					perms.EnsureAdminExists(model.ResType, model.ResId);

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static PermissionDropdownVM EditPermItems(UserOrganizationModel caller, PermissionDropdownVM model)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    var shouldSave = false;
                    var perms = PermissionsUtility.Create(s, caller);

                    perms.CanAdmin(model.ResType, model.ResId);
                    var anyDeleted = false;
                    foreach (var item in model.Items) {
                        if (item.Edited) {
                            shouldSave = true;

                            PermItem lookup;
                            if (item.Id == 0)
                                lookup = new PermItem() {
                                    CreatorId = caller.Id,
                                    ResId = model.ResId,
                                    ResType = model.ResType,
                                };
                            else
                                lookup = s.Get<PermItem>(item.Id);

                            if (lookup == null)
                                throw new PermissionsException("Permission setting does not exist.");

                            if (model.ResId != lookup.ResId)
                                throw new PermissionsException("Permission setting does not match (ResourceId).");
                            if (model.ResType != lookup.ResType)
                                throw new PermissionsException("Permission setting does not match (ResourceType).");

                            lookup.AccessorId = item.AccessorId;
                            lookup.AccessorType = item.AccessorType;
                            lookup.CanAdmin = item.CanAdmin;
                            lookup.CanEdit = item.CanEdit;
                            lookup.CanView = item.CanView;

                            lookup.DeleteTime = item.Deleted ? (DateTime?)DateTime.UtcNow : null;
                            anyDeleted = anyDeleted || item.Deleted;
                        }
                    }

                    if (anyDeleted) {
                        perms.EnsureAdminExists(model.ResType, model.ResId);
                    }

                    if (shouldSave) {
                        tx.Commit();
                        s.Flush();
                    }
                    return model;
                }
            }
        }
        public static void CreatePermItems(UserOrganizationModel caller, PermItem.ResourceType resourceType, long resourceId, params PermTiny[] items)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {

                    var perms = PermissionsUtility.Create(s, caller);
                    perms.CanAdmin(resourceType, resourceId);
                    CreatePermItems(s, caller, resourceType, resourceId, items);

                    tx.Commit();
                    s.Flush();
                }
            }
        }

        public static void CreatePermItems(ISession s, UserOrganizationModel creator, PermItem.ResourceType resourceType, long resourceId, params PermTiny[] items)
        {
            var oneAdmin = false;

            var anyAdmins = s.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.ResId == resourceId && x.ResType == resourceType && x.CanAdmin == true).RowCount();
            if (anyAdmins > 0)
                oneAdmin = true;

            foreach (var i in items) {
                if (i.AccessorType == PermItem.AccessType.Creator)
                    i.AccessorId = creator.Id;

                if (i.AccessorType == PermItem.AccessType.Email) {
                    var epi = new EmailPermItem() {
                        CreatorId = creator.Id,
                        Email = i.EmailAddress,
                    };
                    s.Save(epi);
                    i.AccessorId = epi.Id;
                }

                oneAdmin = oneAdmin || i.CanAdmin;
                var pi = new PermItem() {
                    CanAdmin = i.CanAdmin,
                    CanEdit = i.CanEdit,
                    CanView = i.CanView,
                    AccessorType = i.AccessorType,
                    AccessorId = i.AccessorId,
                    ResType = resourceType,
                    ResId = resourceId,
                    CreatorId = creator.Id,
                    OrganizationId = creator.Organization.Id,
                    IsArchtype = false,
                };
                s.Save(pi);
                i.PermItem = pi;
            }

            if (!oneAdmin)
                throw new PermissionsException("Requires at least one admin");

        }
    }

    public class PermTiny {
        public PermTiny()
        {
            CanAdmin = true;
            CanEdit = true;
            CanView = true;
        }

        public static PermTiny Creator(bool view = true, bool edit = true, bool admin = true)
        {
            return new PermTiny() {
                AccessorType = PermItem.AccessType.Creator,
                CanAdmin = admin,
                CanEdit = edit,
                CanView = view
            };
        }
        public static PermTiny Members(bool view = true, bool edit = true, bool admin = true)
        {
            return new PermTiny() {
                AccessorType = PermItem.AccessType.Members,
                AccessorId = -1,
                CanAdmin = admin,
                CanEdit = edit,
                CanView = view
            };
        }
        public static PermTiny Admins(bool view = true, bool edit = true, bool admin = true)
        {
            return new PermTiny() {
                AccessorType = PermItem.AccessType.Admins,
                AccessorId = -1,
                CanAdmin = admin,
                CanEdit = edit,
                CanView = view
            };
        }
        public static PermTiny RGM(long id, bool view = true, bool edit = true, bool admin = true)
        {
            return new PermTiny() {
                AccessorType = PermItem.AccessType.RGM,
                AccessorId = id,
                CanAdmin = admin,
                CanEdit = edit,
                CanView = view
            };
        }
        public static PermTiny Email(string email, bool view = true, bool edit = true, bool admin = false)
        {
            return new PermTiny() {
                AccessorType = PermItem.AccessType.Email,
                CanAdmin = admin,
                CanEdit = edit,
                CanView = view,
                EmailAddress = email
            };
        }

        public PermItem.AccessType AccessorType { get; set; }
        public long AccessorId { get; set; }
        public bool CanAdmin { get; set; }
        public bool CanEdit { get; set; }
        public bool CanView { get; set; }
        public PermItem PermItem { get; set; }

        public string EmailAddress { get; set; }
    }
}