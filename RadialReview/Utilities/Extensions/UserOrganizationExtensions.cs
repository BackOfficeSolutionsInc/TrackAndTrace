using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static partial class UserOrganizationExtensions
    {
        /*public static String Name(this UserOrganizationModel self)
        {
            if (self.User == null)
                return self.EmailAtOrganization;
            return self.User.Name();
        }*/
        public static String ImageUrl(this UserOrganizationModel self)
        {
            if (self.User == null || self.User.Image == null)
                return "/i/userplaceholder";
            return "/i/" + self.User.Image.Id;
        }
        public static String ImageUrl(this UserOrganizationModel self, int width, int height)
        {
            if (self.User == null || self.User.Image == null)
                return "/i/userplaceholder?dim=" + width + "x" + height;
            return "/i/" + self.User.Image.Id + "?dim=" + width + "x" + height;
        }
        public static IList<UserOrganizationModel> GetManagingUsersAndSelf(this UserOrganizationModel self)
        {
            return self.ManagingUsers.Union(new List<UserOrganizationModel> { self }).ToList();
        }
        public static List<UserOrganizationModel> AllSubordinatesAndSelf(this UserOrganizationModel self)
        {
            return self.AllSubordinates.Union(new List<UserOrganizationModel> { self }).ToList();
        }
        public static Boolean IsManager(this UserOrganizationModel self)
        {
            return self.IsRadialAdmin || self.ManagerAtOrganization || self.ManagingOrganization;  
        }
        public static Boolean IsManagerCanEditOrganization(this UserOrganizationModel self)
        {
            return (self.ManagerAtOrganization && self.Organization.ManagersCanEdit) || self.IsRadialAdmin || self.ManagingOrganization ; 
        }
    }
}