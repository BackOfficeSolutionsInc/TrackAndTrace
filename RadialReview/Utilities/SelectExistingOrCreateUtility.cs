using RadialReview.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities {
	public class SelectExistingOrCreateUtility {


		public static SelectExistingOrCreate Create<T>(string searchUrl, string template,T obj = null, bool showCreateFirst = false) where T : class,new() {
			return new SelectExistingOrCreate() {
				Object = obj??new T(),
				SearchUrl = searchUrl,
				ShowCreateFirst = showCreateFirst,
				Template = template
			};
		}

		public class SelectExistingOrCreateModel<T> {
			public long? SelectedValue { get; set; }
			public T Object { get; set; }

			public bool ShouldCreateNew() {
				if (SelectedValue != null) {
					return false;
				} else if (Object !=null) {
					return true;
				}
				throw new PermissionsException("No selection.");
			}
		}

		public class SelectExistingOrCreate {
			public string SearchUrl { get; set; }
			public string SelectedValue { get; set; }
			public bool ShowCreateFirst { get; set; }
			public object Object { get; set; }
			public string Template { get; set; }
			//public int MinimumInputLength { get; set; }
			//public SelectExistingOrCreate() {
			//	MinimumInputLength = 2;
			//}


		}


		public interface ISelectExistingOrCreateItem {
			string Name { get; }
			string ImageUrl { get; }
			string Description { get; }
			string ItemValue { get; }
			string AltIcon { get; }
		}

		public class BaseSelectExistingOrCreateItem : ISelectExistingOrCreateItem {
			public string Name { get; set; }
			public string ImageUrl { get; set; }
			public string Description { get; set; }
			public string ItemValue { get; set; }
			public string AltIcon { get; set; }
		}

	}
}