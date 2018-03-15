using NHibernate;
using RadialReview.Models.Notifications;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
    public class INotificationHookUpdates {
        public bool StatusChanged { get; set; }
    }


    public interface INotificationHook :IHook {
        Task CreateNotification(ISession s, NotificationModel notification);
        Task UpdateNotification(ISession s, NotificationModel notification, INotificationHookUpdates updates);
    }
}
