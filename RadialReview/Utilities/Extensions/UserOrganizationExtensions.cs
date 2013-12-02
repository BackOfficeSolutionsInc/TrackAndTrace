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
        public static void SetPersonallyManaging(this UserOrganizationModel self, Boolean personallyManaging)
        {
            self.Set("_managing",personallyManaging.ToString());
        }
        public static bool GetPersonallyManaging(this UserOrganizationModel self)
        {
            return bool.Parse(self.GetSingle("_managing"));
        }

        public static void Set(this UserOrganizationModel self,String key,String value)
        {
            self.Properties[key] = new List<string>();
            self.Properties[key].Add(value);
        }

        public static String GetSingle(this UserOrganizationModel self,String key)
        {
            if (self.Properties.ContainsKey(key))
                return self.Properties[key].NotNull(x=>x.FirstOrDefault());
            return null;
        }


        public static String GetNameAndTitle(this UserOrganizationModel self,Boolean fullTitle=false,long youId=-1)
        {
            return self.GetName() + self.GetTitles(fullTitle, youId).Surround(" (",")");
        }

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