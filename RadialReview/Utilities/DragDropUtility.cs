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
                Classes=(x.IsAttached()?"attached":"unattached")+" "+String.Join(" ",x.Properties.GetOrDefault("classes",new List<String>())),
                AltText = x.Name()+AltTextBuilder(x.Properties.GetOrDefault("altText",new List<String>())),
            }).ToList();
        }

        private static string AltTextBuilder(List<String> alts)
        {
            if (alts.Count > 0)
                return " (" + String.Join(",", alts) + ")";
            return "";
        }
    }
}