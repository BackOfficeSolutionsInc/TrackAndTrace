using Moq;
using RadialReview.Models;
using RadialReview.Tests.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Tests
{
    public static class MockSessionExtensions
    {
        public static Mock<UserOrganizationModel> CreateUser(this MockSession session, long id)
        {
            var user = session.MockRepository.Create<UserOrganizationModel>();
            user.Setup(x => x.Id).Returns(id);
            user.Setup(x => x.IsRadialAdmin).Returns(false);
            session.AddItem(user.Object, id);
            return user;
        }

        public static Mock<OrganizationModel> CreateOrganization(this MockSession session, long id)
        {
            var org = session.MockRepository.Create<OrganizationModel>();
            org.Setup(x => x.Id).Returns(id);
            session.AddItem(org.Object, id);
            return org;
        }
    }
}
