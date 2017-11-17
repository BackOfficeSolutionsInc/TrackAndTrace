using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.Enums;
using RadialReview;
using RadialReview.Models.Accountability;
using System.Linq.Expressions;
using RadialReview.Models.Askables;
using RadialReview.NHibernate;
using RadialReview.Controllers;
using RadialReview.Models.ViewModels;
using RadialReview.Reflection;
using NHibernate;
using System.Threading.Tasks;
using TractionTools.Tests.Utilities;

namespace SmokeTester {
	class Program {
		static void Main(string[] args) {

			new BasePermissionsTest.Ctx();

			Console.WriteLine("Done. Press enter");
			Console.ReadKey();
		}

	}
	public class SessionTransaction {
		public ISession s { get; set; }
		public ITransaction tx { get; set; }
	}
	public class Org {

		public static NHibernateUserManager UserManager {
			get {
				if (_UserManager == null)
					_UserManager = new NHibernateUserManager(new NHibernateUserStore());
				return _UserManager;
			}
		}
		protected static NHibernateUserManager _UserManager { get; set; }

		public long Id { get { return Organization.Id; } }
		public OrganizationModel Organization { get; set; }
		public UserOrganizationModel Employee { get; set; }
		public UserOrganizationModel Manager { get; set; }

		public AccountabilityNode ManagerNode { get; set; }
		public AccountabilityNode EmployeeNode { get; set; }

		public DateTime CreateTime { get; set; }
		public long UID { get; set; }

		public List<AccountabilityNode> AllUserNodes { get; set; }
		public List<UserOrganizationModel> AllUsers { get; set; }


		public List<UserOrganizationModel> AllAdmins { get { return new[] { Manager }.ToList(); } }
		public List<UserOrganizationModel> AllManagers { get { return new[] { Manager }.ToList(); } }
		public List<UserOrganizationModel> AllFrontLine { get { return new[] { Employee }.ToList(); } }
		public List<UserOrganizationModel> AllNonmanagers { get { return new[] { Employee }.ToList(); } }

		protected Dictionary<long, TractionTools.Tests.Utilities.Credentials> ExistingCreds { get; set; }

		public Org() {
			ExistingCreds = new Dictionary<long, TractionTools.Tests.Utilities.Credentials>();
		}


		public async Task RegisterAllUsers() {
			var s = HibernateSession.GetCurrentSession();
			try {
				var tx = s.BeginTransaction();
				try {
					var stx = new SessionTransaction() {
						s = s,
						tx = tx
					};

					await RegisterAllUsers(stx);
					tx.Commit();
					s.Flush();
				} finally {
					tx.Dispose();
				}
			} finally {
				s.Dispose();
			}
		}
		public async Task RegisterUser(UserOrganizationModel user) {
			var s = HibernateSession.GetCurrentSession();
			try {
				var tx = s.BeginTransaction();
				try {
					var stx = new SessionTransaction() {
						s = s,
						tx = tx
					};
					await RegisterUser(stx, user);
					tx.Commit();
					s.Flush();
				} finally {
					tx.Dispose();
				}
			} finally {
				s.Dispose();
			}

		}

		public async Task RegisterUser(SessionTransaction stx, UserOrganizationModel user) {
			bool _nil;
			await GetCredentials(stx, user, out _nil);
		}
		public async Task<Credentials> GetCredentials(UserOrganizationModel userOrg) {
			var s = HibernateSession.GetCurrentSession();
			try {
				var tx = s.BeginTransaction();
				try {
					bool wasCreated = false;

					var stx = new SessionTransaction() { s = s, tx = tx };

					var credsWasCreated = await GetCredentials(stx, userOrg, out wasCreated);
					if (credsWasCreated.Item2) {
						tx.Commit();
						s.Flush();
					}
					return credsWasCreated.Item1;
				} finally {
					tx.Dispose();
				}
			} finally {
				s.Dispose();
			}
		}
		public async Task<Tuple<Credentials, bool>> GetCredentials(SessionTransaction stx, UserOrganizationModel userOrg) {
			var wasCreated = false;
			if (!ExistingCreds.ContainsKey(userOrg.Id)) {
				BasePermissionsTest.MockHttpContext();
				var password = Guid.NewGuid().ToString();

				var user = new UserModel() {
					UserName = userOrg.TempUser.Email.ToLower(),
					FirstName = userOrg.TempUser.FirstName,
					LastName = userOrg.TempUser.LastName,
				};

				OrgUtil.CreateUser(stx, user, password);

				//new AccountController().UserManager.Create(user, password);
				var newUserOrg = await OrganizationAccessor.JoinOrganization_Test(s, user, Manager.Id, userOrg.Id);

				userOrg.User = newUserOrg.User;
				userOrg.TempUser = newUserOrg.TempUser;


				ExistingCreds[userOrg.Id] = new TractionTools.Tests.Utilities.Credentials(user.UserName, password, userOrg);
				wasCreated = true;
			}
			return Tuple.Create(ExistingCreds[userOrg.Id], wasCreated);
		}

		public async Task RegisterAllUsers(SessionTransaction stx) {
			foreach (var u in AllUsers) {
				await GetCredentials(stx, u);
			}
		}

		public void AddCredentials(UserOrganizationModel user, string username, string password) {
			ExistingCreds[user.Id] = new TractionTools.Tests.Utilities.Credentials(username, password, user);
		}

		public void AssertAllUsers(Predicate<UserOrganizationModel> testFunction, params UserOrganizationModel[] trueFor) {
			AssertAllUsers(testFunction, trueFor.ToList());
		}

		public void AssertAllUsers(Predicate<UserOrganizationModel> testFunction, IEnumerable<UserOrganizationModel> trueFor) {
			var exceptions = new List<Exception>();
			foreach (var user in AllUsers) {
				var expecting = false;
				if (trueFor.Any(x => x.Id == user.Id))
					expecting = true;
				var found = testFunction(user);
				if (expecting != found)
					exceptions.Add(new Exception("Assertion failed for " + user.GetName() + " (" + user.Id + "). Expecting: " + expecting + ".  Found: " + found + "."));
			}

			if (exceptions.Count == 1)
				throw exceptions[0];
			if (exceptions.Count > 1) {
				foreach (var e in exceptions) {
					Console.WriteLine(e.Message);
				}
				throw new Exception("Assertion failed for " + exceptions.Count + " users.");

			}
		}



	}

	public class FullOrg : Org {

		public UserOrganizationModel Client { get; set; }
		public UserOrganizationModel Middle { get; set; }
		public UserOrganizationModel E1 { get; set; }
		public UserOrganizationModel E2 { get; set; }
		public UserOrganizationModel E3 { get; set; }
		public UserOrganizationModel E4 { get; set; }
		public UserOrganizationModel E5 { get; set; }
		public UserOrganizationModel E6 { get; set; }
		public UserOrganizationModel E7 { get; set; }


		public AccountabilityNode MiddleNode { get; set; }
		public AccountabilityNode E1MiddleNode { get; set; }
		public AccountabilityNode E1BottomNode { get; set; }
		public AccountabilityNode E2Node { get; set; }
		public AccountabilityNode E3Node { get; set; }
		public AccountabilityNode E4Node { get; set; }
		public AccountabilityNode E5Node { get; set; }
		public AccountabilityNode E6Node { get; set; }

		[Obsolete("E7 is not on the AC", true)]
		public AccountabilityNode E7Node { get; set; }
		[Obsolete("Client is not on the AC", true)]
		public AccountabilityNode ClientNode { get; set; }


		public OrganizationTeamModel AllMembersTeam { get; set; }
		public OrganizationTeamModel AllManagersTeam { get; set; }
		public OrganizationTeamModel MiddleSubordinatesTeam { get; set; }

		public OrganizationTeamModel NonreviewTeam { get; set; }
		public OrganizationTeamModel InterreviewTeam { get; set; }

		public new List<UserOrganizationModel> AllManagers { get { return new[] { Manager, Middle, E1, E2, }.ToList(); } }
		public new List<UserOrganizationModel> AllNonmanagers { get { return new[] { Employee, E6, E3, E4, E5, E7 }.ToList(); } }
		public new List<UserOrganizationModel> AllFrontLine { get { return new[] { Employee, E1, E6, E3, E4, E5, E7 }.ToList(); } }
		public List<UserOrganizationModel> AllClients { get { return new[] { Client }.ToList(); } }

	}

	public class OrgUtil {

		public static async Task<Org> CreateOrganization(string name = null, DateTime? time = null) {
			var s = HibernateSession.GetCurrentSession();
			try {
				var tx = s.BeginTransaction();
				try {
					var stx = new SessionTransaction() { s = s, tx = tx };
					var org = await CreateOrganization(stx, name, time);
					tx.Commit();
					s.Flush();
					return org;
					//Your action here
					//tx.Commit();
					//s.Flush();
				} finally {
					tx.Dispose();
				}
			} finally {
				s.Dispose();
			}
		}



		public static async Task<Org> CreateOrganization(ref ISession s, ref ITransaction tx, string name = null, DateTime? time = null) {
			var now = DateTime.UtcNow;
			var time1 = time ?? DateTime.UtcNow;
			var nowMs = now.ToJavascriptMilliseconds() / 10000;
			name = name ?? ("TestOrg_" + nowMs);

			var org = new FullOrg();
			org.CreateTime = time1;
			org.UID = new Random().Next();


			UserOrganizationModel employee = null;
			UserOrganizationModel manager = null;
			AccountabilityNode managerNode = null;
			OrganizationModel o = null;

			var managerUser = new UserModel() {
				UserName = "manager@test_" + org.UID + ".com",
				FirstName = "manager",
				LastName = "" + nowMs,
			};
			var password = Guid.NewGuid().ToString();

			CreateUser(ref s, ref tx, managerUser, password);// Expensive

			BasePermissionsTest.MockHttpContext();
			var organization = await new OrganizationAccessor().CreateOrganization_Test(s, managerUser, name,
				PaymentPlanType.Professional_Monthly_March2016, time1, out manager, out managerNode, true, true);//Very Expensive
			org.Organization = organization;
			org.AddCredentials(manager, managerUser.UserName, password);

			org.ManagerNode = managerNode;
			org.ManagerNode._Name = "manager " + nowMs;

			org.Manager = manager;

			var employeeName = "employee";

			var settings = new CreateUserOrganizationViewModel() {
				ManagerNodeId = null,
				IsManager = false,
				OrgPositionId = -2,
				Email = employeeName + "@test_" + org.UID + ".com",
				FirstName = employeeName,
				LastName = "" + nowMs,
				IsClient = false,
				ClientOrganizationName = "",
			};


			var managerPerms = PermissionsUtility.Create(s, manager);
			var tempUser = await JoinOrganizationAccessor.CreateUserUnderManager_Test(s, managerPerms, settings, out employee);// null, false, -2, employeeName + "@test_" + org.UID + ".com", employeeName, "" + nowMs, out employee, false, "");

			org.Employee = employee;
			org.EmployeeNode = AccountabilityAccessor.AppendNode(s, managerPerms, null, managerNode.Id, userId: employee.Id);// Expensive
			org.EmployeeNode._Name = "employee " + nowMs;

			org.AllUserNodes = new List<AccountabilityNode>() { org.ManagerNode, org.EmployeeNode };
			org.AllUsers = new List<UserOrganizationModel>() { org.Manager, org.Employee };

			return org;
		}

		public static void CreateUser(ref ISession s, ref ITransaction tx, UserModel managerUser, string password) {
			s.Flush();
			tx.Commit();
			tx.Dispose();
			s.Dispose();
			try {
				new AccountController().UserManager.CreateAsync(managerUser, password).RunSynchronously();
			} catch (Exception e) {
				Console.WriteLine(e.Message);
			}

			s = HibernateSession.GetCurrentSession();
			tx = s.BeginTransaction();
		}

		private static async Task AddUserToOrg(ISession s, PermissionsUtility managerPerm, FullOrg org, AccountabilityNode managerNode, string uname, Expression<Func<FullOrg, UserOrganizationModel>> userSelector, Expression<Func<FullOrg, AccountabilityNode>> nodeSelector = null, bool isClient = false) {

			var ms = org.CreateTime.ToJavascriptMilliseconds() / 10000;
			UserOrganizationModel user = null;

			var settings = new CreateUserOrganizationViewModel() {
				ManagerNodeId = null,
				IsManager = false,
				OrgPositionId = -2,
				Email = uname.ToLower() + "@test_" + org.UID + ".com",
				FirstName = uname,
				LastName = "" + ms,
				IsClient = isClient,
				ClientOrganizationName = isClient ? "ClientOrg" : ""
			};

			var temp = await JoinOrganizationAccessor.CreateUserUnderManager_Test(s, managerPerm, settings, out user);// null, false, -2, uname.ToLower() + "@test_" + org.UID + ".com", uname, "" + ms, out user, isClient, isClient ? "ClientOrg" : "");
			org.AllUsers.Add(user);
			org.Set(userSelector, user);


			if (managerNode != null) {
				var userNode = AccountabilityAccessor.AppendNode(s, managerPerm, null, managerNode.Id, userId: user.Id);
				userNode._Name = user.GetName();
				org.Set(nodeSelector, userNode);
				org.AllUserNodes.Add(userNode);
			}
		}

		public static AccountabilityNode AddUserToOrg(Org org, AccountabilityNode managerNode, string uname) {
			var ms = org.CreateTime.ToJavascriptMilliseconds() / 10000;
			UserOrganizationModel user = null;

			var settings = new CreateUserOrganizationViewModel() {
				ManagerNodeId = null,
				IsManager = false,
				OrgPositionId = -2,
				Email = uname.ToLower() + "@test_" + org.UID + ".com",
				FirstName = uname,
				LastName = "" + ms,
			};

			var temp = JoinOrganizationAccessor.CreateUserUnderManager(org.Manager, settings, out user);// null, false, -2, uname.ToLower() + "@test_" + org.UID + ".com", uname, "" + ms, out user, isClient, isClient ? "ClientOrg" : "");
			org.AllUsers.Add(user);

			if (managerNode != null) {
				var userNode = AccountabilityAccessor.AppendNode(org.Manager, managerNode.Id, userId: user.Id);
				userNode._Name = user.GetName();
				org.AllUserNodes.Add(userNode);
				return userNode;
			}
			return null;
		}


		public static FullOrg CreateFullOrganization(string name = null, DateTime? time = null) {
			var s = HibernateSession.GetCurrentSession();
			try {
				var tx = s.BeginTransaction();
				try {
					var org = CreateFullOrganization(ref s, ref tx, name, time);
					tx.Commit();
					s.Flush();
					return org;
				} finally {
					tx.Dispose();
				}
			} finally {
				s.Dispose();
			}
		}

		/// <summary>
		/// See \TractionTools.Tests\Utilities\FullOrganization.png
		/// </summary>
		///
		public static FullOrg CreateFullOrganization(ref ISession s, ref ITransaction tx, string name = null, DateTime? time = null) {
			FullOrg org = (FullOrg)CreateOrganization(ref s, ref tx, name, time);

			var managerPerms = PermissionsUtility.Create(s, org.Manager);

			AddUserToOrg(s, managerPerms, org, null, "Client", x => x.Client, null, true);
			AddUserToOrg(s, managerPerms, org, org.ManagerNode, "Middle", x => x.Middle, x => x.MiddleNode);
			AddUserToOrg(s, managerPerms, org, org.ManagerNode, "E1", x => x.E1, x => x.E1MiddleNode);
			AddUserToOrg(s, managerPerms, org, org.MiddleNode, "E2", x => x.E2, x => x.E2Node);
			AddUserToOrg(s, managerPerms, org, org.MiddleNode, "E3", x => x.E3, x => x.E3Node);
			AddUserToOrg(s, managerPerms, org, org.E1MiddleNode, "E4", x => x.E4, x => x.E4Node);
			AddUserToOrg(s, managerPerms, org, org.E1MiddleNode, "E5", x => x.E5, x => x.E5Node);
			AddUserToOrg(s, managerPerms, org, org.E2Node, "E6", x => x.E6, x => x.E6Node);
			AddUserToOrg(s, managerPerms, org, null, "E7", x => x.E7, null);

			org.E1BottomNode = AccountabilityAccessor.AppendNode(s, managerPerms, null, org.MiddleNode.Id, userId: org.E1.Id);
			org.E1BottomNode._Name = org.E1.GetName();
			org.AllUserNodes.Add(org.E1BottomNode);

			//Create inter-reviewing team
			org.InterreviewTeam = TeamAccessor.EditTeam(s, managerPerms, 0, "interreviewing-team", true, false, org.E5.Id);
			TeamAccessor.AddMember(s, managerPerms, org.InterreviewTeam.Id, org.E5.Id);
			TeamAccessor.AddMember(s, managerPerms, org.InterreviewTeam.Id, org.E6.Id);

			//Create non-reviewing team
			org.NonreviewTeam = TeamAccessor.EditTeam(s, managerPerms, 0, "non-interreviewing-team", false, false, org.E3.Id);
			TeamAccessor.AddMember(s, managerPerms, org.NonreviewTeam.Id, org.E3.Id);
			TeamAccessor.AddMember(s, managerPerms, org.NonreviewTeam.Id, org.E4.Id);


			var allTeams = TeamAccessor.GetOrganizationTeams(s, managerPerms, org.Id);

			org.AllMembersTeam = allTeams.First(x => x.Type == TeamType.AllMembers);
			org.AllManagersTeam = allTeams.First(x => x.Type == TeamType.Managers);
			org.MiddleSubordinatesTeam = allTeams.First(x => x.Type == TeamType.Subordinates && x.ManagedBy == org.Middle.Id);

			org.RegisterUser(ref s, ref tx, org.E3);

			return org;
		}

	}

	public class BasePermissionsTest : TractionTools.Tests.TestUtils.BaseTest {
		public class Ctx {
			public FullOrg Org { get; set; }
			public Org OtherOrg { get; set; }

			//Helpers to dive into Org
			public long Id { get { return Org.Id; } }
			public UserOrganizationModel Manager { get { return Org.Manager; } }
			public UserOrganizationModel Employee { get { return Org.Employee; } }
			public UserOrganizationModel Middle { get { return Org.Middle; } }
			public UserOrganizationModel E1 { get { return Org.E1; } }
			public UserOrganizationModel E2 { get { return Org.E2; } }
			public UserOrganizationModel E3 { get { return Org.E3; } }
			public UserOrganizationModel E4 { get { return Org.E4; } }
			public UserOrganizationModel E5 { get { return Org.E5; } }
			public UserOrganizationModel E6 { get { return Org.E6; } }
			public UserOrganizationModel E7 { get { return Org.E7; } }
			public UserOrganizationModel Client { get { return Org.Client; } }
			public List<UserOrganizationModel> AllManagers { get { return Org.AllManagers; } }
			public List<UserOrganizationModel> AllNonmanagers { get { return Org.AllNonmanagers; } }
			public List<UserOrganizationModel> AllUsers { get { return Org.AllUsers; } }
			public List<UserOrganizationModel> AllAdmins { get { return Org.AllAdmins; } }


			public PermissionsAccessor Perms { get; set; }

			public Ctx() {
				//MockHttpContext(false);
				var s = HibernateSession.GetCurrentSession();
				try {
					var tx = s.BeginTransaction();
					try {
						Org = OrgUtil.CreateFullOrganization(ref s, ref tx);
						tx.Commit();
						s.Flush();
					} finally {
						tx.Dispose();
					}
				} finally {
					s.Dispose();
				}

				MockHttpContext(false);
				s = HibernateSession.GetCurrentSession();
				try {
					var tx = s.BeginTransaction();
					try {
						OtherOrg = OrgUtil.CreateOrganization(ref s, ref tx);
						Perms = new PermissionsAccessor();
						tx.Commit();
						s.Flush();
					} finally {
						tx.Dispose();
					}
				} finally {
					s.Dispose();
				}
				Perms = new PermissionsAccessor();
			}

			public void AssertAll(Action<PermissionsUtility> ensurePermitted, IEnumerable<UserOrganizationModel> trueFor) {
				AssertAll(ensurePermitted, trueFor.ToArray());
			}
			public void AssertAll(Action<PermissionsUtility> ensurePermitted, params UserOrganizationModel[] trueFor) {
				var myOrgUsers = Org.AllUsers.Where(x => trueFor.Any(y => y.Id == x.Id));
				var otherOrgUsers = OtherOrg.AllUsers.Where(x => trueFor.Any(y => y.Id == x.Id));

				Org.AssertAllUsers(user => Perms.IsPermitted(user, ensurePermitted), myOrgUsers);
				OtherOrg.AssertAllUsers(user => Perms.IsPermitted(user, ensurePermitted), otherOrgUsers);

			}

		}

	}
}
