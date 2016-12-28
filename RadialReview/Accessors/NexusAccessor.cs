using FluentNHibernate.Utils;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using NHibernate;
using RadialReview.Utilities.Query;
using RadialReview.Models.Application;
using System.Threading.Tasks;
using RadialReview.Models.Json;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Accountability;
using RadialReview.Utilities.RealTime;

namespace RadialReview.Accessors {
    public class NexusAccessor : BaseAccessor {
        //public static UrlAccessor _UrlAccessor = new UrlAccessor();

        


        public void Execute(NexusModel nexus)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    nexus = s.Get<NexusModel>(nexus.Id);

                    //db.Nexuses.Attach(nexus);
                    nexus.DateExecuted = DateTime.UtcNow;
                    //db.SaveChanges();
                    tx.Commit();
                    s.Flush();
                }
            }
        }

		public static bool IsCorrectUser(UserOrganizationModel caller, NexusModel nexus) {

			if (caller.Id == nexus.ForUserId)
				return true;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					
					var nexUsers = s.Get<UserOrganizationModel>(nexus.ForUserId);
					return caller.User.Id == nexUsers.User.Id;
				}
			}
		}

        public NexusModel Put(NexusModel model)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    model = Put(s.ToUpdateProvider(), model);
                    tx.Commit();
                    s.Flush();
                }
            }
            return model;
        }

        public static NexusModel Put(AbstractUpdate s, NexusModel model)
        {
            s.Save(model);
            return model;
        }


        public NexusModel Get(String id)
        {
            using (var s = HibernateSession.GetCurrentSession()) {
                using (var tx = s.BeginTransaction()) {
                    /*var found = db.Nexuses.Find(id);
                    if (found == null)
                        throw new PermissionsException();
                    return found;*/
                    var found = s.Get<NexusModel>(id);
                    if (found == null)
                        throw new PermissionsException("The request was not found.");
                    if (found.DeleteTime != null && DateTime.UtcNow > found.DeleteTime) {
                        var message = "The request has expired.";
                        if (found.ActionCode == NexusActions.ResetPassword) {
                            message += " You can only use this password reset code once.";
                        }
                        throw new PermissionsException(message);
                    }

                    return found;
                }
            }
        }
    }
}