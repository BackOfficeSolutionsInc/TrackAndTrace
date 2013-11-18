using RadialReview.Models;
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
                    var application=s.Get<ApplicationWideModel>(1L);
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
    }
}