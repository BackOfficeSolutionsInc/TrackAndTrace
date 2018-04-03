using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

    public class IPositionHookUpdates {
        public bool NameChanged { get; set; }
        public bool WasDeleted { get; set; }
    }

    public interface IPositionHooks : IHook {

        Task CreatePosition(ISession s, OrganizationPositionModel position);

        Task UpdatePosition(ISession s, OrganizationPositionModel position, IPositionHookUpdates updates);
    }
}
