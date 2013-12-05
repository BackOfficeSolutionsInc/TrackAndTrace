using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Responsibilities;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class ApplicationAccessor : BaseAccessor
    {
        public Boolean EnsureApplicationExists()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    ConstructPositions(s);
                    tx.Commit();
                    s.Flush();
                }
                using (var tx = s.BeginTransaction())
                {
                    var application = s.Get<ApplicationWideModel>(1L);
                    if (application == null)
                    {
                        s.Save(new ApplicationWideModel(1));
                        tx.Commit();
                        s.Flush();
                        return true;
                    }
                    return false;
                }
            }
        }

        private void ConstructPositions(ISession session)
        {
            string[] positions = new String[]{
                "Account Coordinator",
                "Account Manager",
                "Accountant",
                "Accounting",
                "Art Director",
                "Business Development",
                "CEO",
                "CFO",
                "Client Retention",
                "Content Marketing Strategist",
                "Content Writer",
                "COO",
                "Copywriter",
                "Creative Director",
                "Cross Media Programmer",
                "Cross Media Strategist",
                "Data Developer",
                "Database Administrator",
                "Delivery",
                "Developer",
                "Direct Sales",
                "Director",
                "Executive Assistant",
                "Executive Director",
                "Facilities",
                "Finance",
                "Graphic Designer",
                "Help Desk Technician",
                "Human Relations",
                "Information Technology",
                "Inside Sales",
                "Intern",
                "Mailing Services",
                "Manager",
                "Marketing ",
                "Marketing Coordinator",
                "Marketing Publications Writer",
                "Multimedia Strategist",
                "Online Marketing Strategist",
                "Operator",
                "Produciton Manager",
                "Project Manager",
                "Project Strategist",
                "Quality Assurance Engineer",
                "Receptionist",
                "Relationship Manager",
                "Sales",
                "Seminar Coordinator",
                "Shift Supervisor",
                "Shipping",
                "Signage",
                "Social Media Strategist",
                "Software Engineer",
                "Solutions Coordinator",
                "Strategist",
                "Supervisor",
                "Support",
                "System Administrator",
                "Team Lead",
                "UI/UX Developer",
                "VP of Operations",
                "VP of Sales and Marketing",
                "VP of Support Services",
                "VP of Technology",
                "Web Application Engineer",
                "Web Developer",

            };
            var found = session.QueryOver<PositionModel>().List().ToList();
            foreach (var p in positions)
            {
                if (!found.Any(x => x.Name.Default.Value == p))
                {
                    session.Save(new PositionModel() { Name = new LocalizedStringModel(p) });
                }
            }
        }
    }
}