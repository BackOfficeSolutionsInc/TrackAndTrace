using RadialReview.Models;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public static class DragDropUtility
    {
        public static List<DragDropItem> ToDragDropList(this IEnumerable<UserOrganizationModel> users)
        {
            return users.Select(x => new DragDropItem{ 
                Id=x.Id,
                DisplayName=x.Name(),
                ImageUrl=x.ImageUrl(),
                Classes=(x.IsAttached()?"attached":"unattached")+" "+x.Properties.GetOrDefault("classes"),
                AltText = x.IsAttached() ? x.Name() :  x.Name() +" ("+ErrorMessageStrings.notAttached+")",
            }).ToList();
        }
    }
}