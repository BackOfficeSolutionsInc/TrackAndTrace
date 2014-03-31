﻿using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public static void PopulateManagers(this UserOrganizationModel self, List<ManagerDuration> allManagers)
        {
            self.ManagedBy = allManagers.Where(x => x.SubordinateId == self.Id).ToList();
        }


        public static void PopulateTeams(this UserOrganizationModel self, List<OrganizationTeamModel> allOrgTeams, List<TeamDurationModel> allTeamDurations)
        {
            var teams = new List<TeamDurationModel>();
            //self.Teams = .ToList();

            if (self.IsManager())
            {
                var managerTeam = allOrgTeams.Where(x => x.Type == TeamType.Managers).SingleOrDefault();
                //Populate(s,managerTeam);
                teams.Add(new TeamDurationModel() { Start = self.AttachTime, Id = -2, Team = managerTeam, User = self });
            }
            var allMembersTeam = allOrgTeams.Where(x => x.Organization.Id == self.Organization.Id && x.Type == TeamType.AllMembers).SingleOrDefault();
            //Populate(s,allMembersTeam);
            teams.Add(new TeamDurationModel() { Start = self.AttachTime, Id = -2, Team = allMembersTeam, User = self });

            teams.AddRange(allTeamDurations.Where(x => x.UserId == self.Id));
            //teams.ForEach(x => Populate(s, x.Team));
            self.Teams = teams;
        }


   

        public static void PopulateLevel(this UserOrganizationModel sub, UserOrganizationModel caller, List<UserOrganizationModel> allSubordinates)
        {
            var found = allSubordinates.FirstOrDefault(x => x.Id == sub.Id);
            if (found!=null)
            {
                var level=found.GetLevel();
                sub.SetLevel(level);
            }
        }
        public static bool PopulatePersonallyManaging(this UserOrganizationModel sub, UserOrganizationModel caller, List<UserOrganizationModel> allSubordinates)
        {
            var output =(caller.IsRadialAdmin ||
                (caller.Organization.Id == sub.Organization.Id && caller.ManagingOrganization) ||
                (allSubordinates.Any(x => x.Id == sub.Id)));
            sub.SetPersonallyManaging(output);
            return output;
        }

        public static void SetPersonallyManaging(this UserOrganizationModel self, Boolean personallyManaging)
        {
            self.Set("_managing", personallyManaging.ToString());
        }
          public static bool GetPersonallyManaging(this UserOrganizationModel self)
        {
            return bool.Parse(self.GetSingle("_managing"));
        }     
        
        public static bool PopulateDirectlyManaging(this UserOrganizationModel sub, UserOrganizationModel caller, List<UserOrganizationModel> directSubordinates)
        {
            var output = directSubordinates.Any(x => x.Id == sub.Id);
            sub.SetDirectlyManaging(output);
            return output;
        }

        public static void SetDirectlyManaging(this UserOrganizationModel self, Boolean directlyManaging)
        {
            self.Set("_directlyManaging", directlyManaging.ToString());
        }

        public static bool GetDirectlyManaging(this UserOrganizationModel self)
        {
            return bool.Parse(self.GetSingle("_directlyManaging"));
        }

      
        public static void SetEditPosition(this UserOrganizationModel self, Boolean editPosition)
        {
            self.Set("_EditPosition", editPosition.ToString());
        }
        public static bool GetEditPosition(this UserOrganizationModel self)
        {
            return bool.Parse(self.GetSingle("_EditPosition"));
        }

        public static void SetLevel(this UserOrganizationModel self, long level)
        {
            self.Set("_Level", level.ToString());
        }
        public static long GetLevel(this UserOrganizationModel self)
        {
            return long.Parse(self.GetSingle("_Level"));
        }

        public static void Set(this UserOrganizationModel self, String key, String value)
        {
            self.Properties[key] = new List<string>();
            self.Properties[key].Add(value);
        }

        public static String GetSingle(this UserOrganizationModel self, String key)
        {
            if (self.Properties.ContainsKey(key))
                return self.Properties[key].NotNull(x => x.FirstOrDefault());
            return null;
        }


        public static String GetNameAndTitle(this UserOrganizationModel self, int positions = int.MaxValue, long youId = -1)
        {
            return self.GetName() + self.GetTitles(positions, youId).Surround(" (", ")");
        }
        public static String GetEmail(this UserOrganizationModel self)
        {
            if (self.User != null)
                return self.User.Email;
            else if (self.TempUser != null)
                return self.TempUser.Email;
            else
                return self.EmailAtOrganization;
        }

        public static String ImageUrl(this UserOrganizationModel self)
        {
            if (self.User == null || self.User.ImageGuid == null)
                return "/i/userplaceholder";
            return "/i/" + self.User.ImageGuid;
        }
        public static String ImageUrl(this UserModel self)
        {
            if (self == null || self.ImageGuid == null)
                return "/i/userplaceholder";
            return "/i/" + self.ImageGuid;
        }
        public static String ImageUrl(this UserOrganizationModel self, int width, int height)
        {
            if (self.User == null || self.User.ImageGuid == null)
                return "/i/userplaceholder?dim=" + width + "x" + height;
            return "/i/" + self.User.ImageGuid + "?dim=" + width + "x" + height;
        }
        public static IList<UserOrganizationModel> GetManagingUsersAndSelf(this UserOrganizationModel self)
        {
            return self.ManagingUsers.ToListAlive().Select(x => x.Subordinate).Union(self.AsList()).ToList();
        }


        public static Tree GetTree(this UserOrganizationModel self,List<long> deepClaims,long? youId=null,bool force=false)
        {
            var managing = force || deepClaims.Any(x => x == self.Id);
            var classes = new List<String>();
            if (self.Teams!=null)
                classes.AddRange(self.Teams.ToListAlive().Select(y => y.Team.Name));

            if (self.ManagingOrganization)
                classes.Add("admin");
            if (self.ManagerAtOrganization)
                classes.Add("manager");
            if (managing)
                classes.Add("managing");
            if (self.Id == youId)
                classes.Add("you");
            classes.Add("employee");

            return new Tree()
            {
                name = self.GetName(),
                id = self.Id,
                subtext = self.GetTitles(),
                @class = String.Join(" ",classes.Select(y => Regex.Replace(y, "[^a-zA-Z0-9]", "_"))),
                managing = managing,
                manager = self.IsManager(),
                children = self.ManagingUsers.NotNull(x=>x.ToListAlive()).Select(x=>x.Subordinate.GetTree(deepClaims,youId,force)).ToList()
            };
        }


        public static List<UserOrganizationModel> AllSubordinatesAndSelf(this UserOrganizationModel self)
        {
            return self.AllSubordinates.Union(new List<UserOrganizationModel> { self }).ToList();
        }
        public static Boolean IsManager(this UserOrganizationModel self)
        {
            return self.IsRadialAdmin || self.ManagerAtOrganization || self.ManagingOrganization;
        }
        public static Boolean IsManagingOrganization(this UserOrganizationModel self)
        {
            return self.IsRadialAdmin || self.ManagingOrganization;
        }
        public static Boolean IsManagerCanEditOrganization(this UserOrganizationModel self)
        {
            return (self.ManagerAtOrganization && self.Organization.ManagersCanEdit) || self.IsRadialAdmin || self.ManagingOrganization;
        }
    }
}