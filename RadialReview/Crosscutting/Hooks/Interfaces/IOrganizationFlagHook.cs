using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Crosscutting.Flags;
using RadialReview.Utilities.Hooks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
    public interface IOrganizationFlagHook : IHook{
        Task AddFlag(ISession s, long orgId, OrganizationFlagType type);
        Task RemoveFlag(ISession s, long userId, OrganizationFlagType type);
    }
}
