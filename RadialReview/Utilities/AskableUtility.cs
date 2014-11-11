using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities
{
    public class AskableUtility
    {
        public List<Tuple<UserOrganizationModel, AboutType>> AllUsers { get; set; }

        public List<AskableAbout> Askables { get; set; }

        public AskableUtility()
        {
            Askables = new List<AskableAbout>();
            AllUsers = new List<Tuple<UserOrganizationModel, AboutType>>();
        }

        public void AddUnique(Askable askable, AboutType about, long aboutUserId)
        {
            foreach (var a in Askables)
            {
                if (a.AboutUserId == aboutUserId && a.Askable.Id == askable.Id)
                {
                    a.AboutType = a.AboutType | about;
                    return;
                }
            }
            Askables.Add(new AskableAbout() { AboutType = about, Askable = askable, AboutUserId = aboutUserId });
        }
        public void AddUnique(IEnumerable<Askable> askables, AboutType about, long aboutUserId)
        {
            foreach (var a in askables)
            {
                AddUnique(a, about, aboutUserId);
            }
        }

        public void AddUser(UserOrganizationModel user, AboutType aboutType)
        {
            AllUsers.Add(Tuple.Create(user, aboutType));
        }

    }
}