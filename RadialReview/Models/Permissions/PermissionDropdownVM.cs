using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Permissions
{
	public class PermItemVM
	{
		public long Id { get; set; }

		public bool CanView { get; set; }
		public bool CanEdit { get; set; }
		public bool CanAdmin { get; set; }
		public PermItem.AccessType AccessorType { get; set; }
		public long AccessorId { get; set; }

		public bool Edited { get; set; }
		
		public bool Deleted { get; set; }

		public string ImageUrl { get; set; }

		public string Title { get; set; }

		public string Initials { get; set; }

		public static PermItemVM Create(PermItem item)
		{
			return new PermItemVM(){
				Id = item.Id,

				AccessorId = item.AccessorId,
				AccessorType = item.AccessorType,
				ImageUrl = item._ImageUrl,
				Title = item._DisplayText,
				CanAdmin = item.CanAdmin,
				CanEdit = item.CanEdit,
				CanView = item.CanView,
				Deleted = item.DeleteTime!=null,
				Edited = false,
				Initials= item._DisplayInitials
			};
		}
	}

	public class PermissionDropdownVM
	{
		public HtmlString DisplayText { get; set; }
		public List<PermItemVM> Items { get; set; }

		public bool CanEdit_View { get; set; }
		public bool CanEdit_Edit { get; set; }
		public bool CanEdit_Admin { get; set; }

		public PermItem.ResourceType ResType { get; set; }
		public long ResId { get; set; }

	}
}