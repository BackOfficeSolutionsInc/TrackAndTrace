using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Permissions {
	public class PermItemVM {
		public long Id { get; set; }

		public bool CanView { get; set; }
		public bool CanEdit { get; set; }
		public bool CanAdmin { get; set; }
		public bool Deletable { get; set; }
		public PermItem.AccessType AccessorType { get; set; }
		public long AccessorId { get; set; }

		public bool Edited { get; set; }

		public bool Deleted { get; set; }

		public string ImageUrl { get; set; }

		public string Title { get; set; }

		public string Initials { get; set; }
		public int Color { get; set; }

		public static PermItemVM Create(PermItem item, bool canAdmin) {
			var o = new PermItemVM() {
				Id = item.Id,

				AccessorId = item.AccessorId,
				AccessorType = item.AccessorType,
				ImageUrl = item._ImageUrl,
				Title = item._DisplayText,
				Color = item._Color,
				CanAdmin = item.CanAdmin,
				CanEdit = item.CanEdit,
				CanView = item.CanView,
				Deleted = item.DeleteTime != null,
				Edited = false,
				Initials = item._DisplayInitials,
			};

			var canDelete = canAdmin;
			switch (item.AccessorType) {
				case PermItem.AccessType.Admins:
					canDelete = false;
					break;
				case PermItem.AccessType.Creator:
					canDelete = false;
					break;
				case PermItem.AccessType.Members:
					canDelete = false;
					break;
				default:
					break;
			}

			o.Deletable = canDelete;

			return o;
		}
	}

	public class PermissionDropdownVM {
		public HtmlString DisplayText { get; set; }
		public List<PermItemVM> Items { get; set; }

		public bool CanEdit_View { get; set; }
		public bool CanEdit_Edit { get; set; }
		public bool CanEdit_Admin { get; set; }
		public bool CanEdit_Delete { get; set; }

		public PermItem.ResourceType ResType { get; set; }
		public long ResId { get; set; }

		public bool Disabled { get; set; }

		public PermissionsHeading GetHeading() {
			return PermissionsHeading.GetHeading(ResType);
		}

		public static PermissionDropdownVM NotPermitted {
			get {
				return new PermissionDropdownVM() {
					DisplayText = new HtmlString("Not permitted"),
					Items = new List<PermItemVM>(),
					CanEdit_View = false,
					CanEdit_Edit = false,
					CanEdit_Admin = false,
					CanEdit_Delete = false,
					ResType = PermItem.ResourceType.Invalid,
					ResId = -1,
					Disabled = true
				};
			}
		}

	}
}