using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RadialReview.Models;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Tests.Utilities;
using RadialReview.Exceptions;
using Moq.Protected;
using RadialReview.Models.UserModels;
using System.Collections.Generic;


namespace RadialReview.Tests.PermissionsTests
{
    [TestClass]
    public class PermissionsTest
    {
        [TestMethod]
        public void RadialAdmin()
        {
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };

            var s = SessionUtility.GetSession(factory);
            //Test Admin
            {
                var admin1 = s.CreateUser(1);
                admin1.Setup(x => x.IsRadialAdmin).Returns(true);
                PermissionsUtility.Create(s, admin1.Object).RadialAdmin();
            }

            //Test Managers
            //Manager 1
            {
                var manager1 = s.CreateUser(2);
                manager1.Setup(x => x.IsRadialAdmin).Returns(false);
                var permManager1 = PermissionsUtility.Create(s, manager1.Object);
                TestExtensions.AssertException<PermissionsException>(() => { permManager1.RadialAdmin(); });
            }
            //Test Users
            //User 1
            {
                var user1 = s.CreateUser(4);
                user1.Setup(x => x.ManagerAtOrganization).Returns(false);
                var permUser1 = PermissionsUtility.Create(s, user1.Object);
                TestExtensions.AssertException<PermissionsException>(() => { permUser1.RadialAdmin(); });
            }
            Assert.Fail();
        }

        [TestMethod]
        public void EditUserModel()
        {
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };
            var s = SessionUtility.GetSession(factory);

            //Test Admin
            {
                var admin1 = s.CreateUser(1);
                admin1.Setup(x => x.IsRadialAdmin).Returns(true);
                var user=factory.Create<UserModel>();
                user.Protected().Setup<String>("Id").Returns("asdf");
                admin1.Setup(x => x.User).Returns(user.Object);

                PermissionsUtility.Create(s, admin1.Object).EditUserModel("asdf");
                PermissionsUtility.Create(s, admin1.Object).EditUserModel("qwer");                
            }
            //Test Managers
            //Manager 1
            {
                var manager1 = s.CreateUser(2);
                manager1.Setup(x => x.IsRadialAdmin).Returns(false);
                var permManager1 = PermissionsUtility.Create(s, manager1.Object);
                var user = factory.Create<UserModel>();
                user.Protected().Setup<String>("Id").Returns("asdf");
                manager1.Setup(x => x.User).Returns(user.Object);

                permManager1.EditUserModel("asdf");
                TestExtensions.AssertException<PermissionsException>(() => { permManager1.EditUserModel("qwer"); });
            }
            Assert.Fail();
        }

        [TestMethod]
        public void EditUserOrganization()
        {
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };
            var s = SessionUtility.GetSession(factory);
                        
            var org1 = s.CreateOrganization(1);
            var org2 = s.CreateOrganization(2);

            var M0_O1 = 101;
            var U1_O1 = 211;
            var U2_O1 = 221;
            var U3_O1 = 231;
            var U4_O2 = 242;
                        
            var manager1_O1 = s.CreateUser(M0_O1);
            var user1_O1 = s.CreateUser(U1_O1);
            var user2_O1 = s.CreateUser(U2_O1);
            var user3_O1 = s.CreateUser(U3_O1);
            var user4_O2 = s.CreateUser(U4_O2);

            var managing = new List<ManagerDuration>();
            managing.Add(new ManagerDuration(M0_O1,U1_O1,-1));
            Assert.Fail("Finish writing test");
            /*
            managing.Add(new ManagerDuration(M0_O1,U2_O1,-1){DeleteTime);

            manager1_O1.Setup(x => x.Organization).Returns(org1.Object);


            user1_O1.Setup(x => x.Organization).Returns(org1.Object);
            user1_O1.Setup(x=>  x.ManagingUsers = new 

            user2_O1.Setup(x => x.Organization).Returns(org1.Object);

            var user3_O2 = s.CreateUser(U3_O2);
            user3_O2.Setup(x => x.Organization).Returns(org2.Object);
                        
            //Test Admin
            {
                var admin1 = s.CreateUser(1);
                admin1.Setup(x => x.IsRadialAdmin).Returns(true);

                PermissionsUtility.Create(s, admin1.Object).EditUserOrganization(U1_O1);
                PermissionsUtility.Create(s, admin1.Object).EditUserOrganization(U2_O1);
                PermissionsUtility.Create(s, admin1.Object).EditUserOrganization(U3_O2);
            }
            //Test Managers
            //Org Manager
            {
                var manager1 = s.CreateUser(2);
                manager1.Setup(x => x.ManagingOrganization).Returns(true);
                var permManager1 = PermissionsUtility.Create(s, manager1.Object);

                permManager1.EditUserOrganization(U1_O1);
                permManager1.EditUserOrganization(U2_O1);
                TestExtensions.AssertException<PermissionsException>(() => { permManager1.EditUserOrganization(U3_O2); });
            }
            //Manager at Org1
            {
                var manager1 = s.CreateUser(3);
                manager1.Setup(x => x.ManagingOrganization).Returns(false);
                var permManager1 = PermissionsUtility.Create(s, manager1.Object);

                permManager1.EditUserOrganization(U1_O1);
                TestExtensions.AssertException<PermissionsException>(() => { permManager1.EditUserOrganization(U2_O1); });
                TestExtensions.AssertException<PermissionsException>(() => { permManager1.EditUserOrganization(U3_O2); });
            }

            //User1 and User 2

            Assert.Fail();*/
        }

        [TestMethod]
        public void EditOrganization()
        {
            var factory = new MockRepository(MockBehavior.Loose) { DefaultValue = DefaultValue.Mock };
            var s = SessionUtility.GetSession(factory);

            var org1 = s.CreateOrganization(1);
            org1.Setup(x => x.ManagersCanEdit).Returns(false);
            var org2 = s.CreateOrganization(2);
            org2.Setup(x => x.ManagersCanEdit).Returns(true);
            var org3 = s.CreateOrganization(3);
            org3.Setup(x => x.ManagersCanEdit).Returns(false);

            //Test Admin
            {
                var admin1 = s.CreateUser(1);
                admin1.Setup(x => x.Organization).Returns(org1.Object);
                admin1.Setup(x => x.IsRadialAdmin).Returns(true);
                PermissionsUtility.Create(s, admin1.Object).EditOrganization(1).EditOrganization(2).EditOrganization(3);
            }

            //Test Managers
            //Manager 1
            {
                var manager1 = s.CreateUser(2);
                manager1.Setup(x => x.Organization).Returns(org1.Object);
                manager1.Setup(x => x.ManagerAtOrganization).Returns(true);
                var permManager1 = PermissionsUtility.Create(s, manager1.Object);

                TestExtensions.AssertException<PermissionsException>(() => { permManager1.EditOrganization(1); });
                TestExtensions.AssertException<PermissionsException>(() => { permManager1.EditOrganization(2); });
                TestExtensions.AssertException<PermissionsException>(() => { permManager1.EditOrganization(3); });
            }

            //Manager 2
            {
                var manager2 = s.CreateUser(3);
                manager2.Setup(x => x.Organization).Returns(org2.Object);
                manager2.Setup(x => x.ManagerAtOrganization).Returns(true);
                var permManager2 = PermissionsUtility.Create(s, manager2.Object);

                TestExtensions.AssertException<PermissionsException>(() => { permManager2.EditOrganization(1); });
                permManager2.EditOrganization(2);
                TestExtensions.AssertException<PermissionsException>(() => { permManager2.EditOrganization(3); });
            }

            //Test Users
            //User 1
            {
                var user1 = s.CreateUser(4);
                user1.Setup(x => x.Organization).Returns(org1.Object);
                user1.Setup(x => x.ManagerAtOrganization).Returns(false);
                var permUser1 = PermissionsUtility.Create(s, user1.Object);

                TestExtensions.AssertException<PermissionsException>(() => { permUser1.EditOrganization(1); });
                TestExtensions.AssertException<PermissionsException>(() => { permUser1.EditOrganization(2); });
                TestExtensions.AssertException<PermissionsException>(() => { permUser1.EditOrganization(3); });
            }
        }
    }
}
