using NHibernate;
using RadialReview.Models.L10;
using RadialReview.Models.Todo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
    public interface IHeadlineHook : IHook {

        Task CreateHeadline(ISession s, PeopleHeadline todo);
        Task UpdateHeadlineMessage(ISession s, PeopleHeadline todo);

    }
}
