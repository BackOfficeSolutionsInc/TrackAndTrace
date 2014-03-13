using Moq;
using NHibernate;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Tests.Utilities
{

    public class SessionUtility
    {
        public static MockSession GetSession(MockRepository factory)
        {
            return new MockSession(factory);
            /*
            var s=factory.Create<ISession>();
            var users=new Dictionary<long,Mock<UserOrganizationModel>>();
            var orgs = new Dictionary<long, Mock<OrganizationModel>>();

            s.Setup(x => x.Get<UserOrganizationModel>(It.IsAny<long>())).Returns<long>(id =>
            {
                if (!users.ContainsKey(id))
                {
                    users[id] = factory.Create<UserOrganizationModel>();
                    users[id].Setup(x => x.Id).Returns(id);
                }

                return users[id].Object;
            });

            s.Setup(x => x.Get<OrganizationModel>(It.IsAny<long>())).Returns<long>(id =>
            {
                if (!users.ContainsKey(id))
                {
                    orgs[id] = factory.Create<OrganizationModel>();
                    orgs[id].Setup(x => x.Id).Returns(id);
                }

                return orgs[id].Object;
            });

            return s.Object;*/
        }

    }
}
