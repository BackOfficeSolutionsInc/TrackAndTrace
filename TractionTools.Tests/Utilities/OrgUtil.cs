using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TractionTools.Tests.TestUtils;
using RadialReview;
using RadialReview.Models.Accountability;
using System.Linq.Expressions;
using RadialReview.Models.Askables;
using RadialReview.NHibernate;
using RadialReview.Controllers;
using RadialReview.Utilities;
using Microsoft.AspNet.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using RadialReview.Models.ViewModels;
using RadialReview.Reflection;
using NHibernate;
using RadialReview.Utilities.RealTime;
using System.Web;
using RadialReview.Models.L10;

namespace TractionTools.Tests.Utilities {
	/*
		var s = HibernateSession.GetCurrentSession();
		try {
			var tx = s.BeginTransaction();
			try {
				//Your action here
				//tx.Commit();
				//s.Flush();
			} finally {
				tx.Dispose();
			}
		} finally {
			s.Dispose();
		}	*/

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

		protected Dictionary<long, Credentials> ExistingCreds { get; set; }

		public Org() {
			ExistingCreds = new Dictionary<long, Credentials>();
		}


		public async Task RegisterAllUsers() {
			var stx = new SessionTransaction();
			stx.s = HibernateSession.GetCurrentSession();
			try {
				stx.tx = stx.s.BeginTransaction();
				try {

					await RegisterAllUsers(stx);
					stx.tx.Commit();
					stx.s.Flush();
				} finally {
					stx.tx.Dispose();
				}
			} finally {
				stx.s.Dispose();
			}
		}
		public async Task RegisterUser(UserOrganizationModel user) {
			var stx = new SessionTransaction();
			stx.s = HibernateSession.GetCurrentSession();
			try {
				stx.tx = stx.s.BeginTransaction();
				try {
					await RegisterUser(stx, user);
					stx.tx.Commit();
					stx.s.Flush();
				} finally {
					stx.tx.Dispose();
				}
			} finally {
				stx.s.Dispose();
			}

		}

		public async Task RegisterUser(SessionTransaction stx, UserOrganizationModel user) {
			await GetCredentials(stx, user);
		}
		public async Task<Credentials> GetCredentials(UserOrganizationModel userOrg) {
			var stx = new SessionTransaction();
			stx.s = HibernateSession.GetCurrentSession();
			try {
				stx.tx = stx.s.BeginTransaction();
				try {
					var credsCreated = await GetCredentials(stx, userOrg);
					if (credsCreated.Item2) {
						stx.tx.Commit();
						stx.s.Flush();
					}
					return credsCreated.Item1;
				} finally {
					stx.tx.Dispose();
				}
			} finally {
				stx.s.Dispose();
			}
		}
		public async Task<Tuple<Credentials, bool>> GetCredentials(SessionTransaction stx, UserOrganizationModel userOrg) {
			var wasCreated = false;
			if (!ExistingCreds.ContainsKey(userOrg.Id)) {
				BaseTest.MockHttpContext();
				var password = Guid.NewGuid().ToString();

				var user = new UserModel() {
					UserName = userOrg.TempUser.Email.ToLower(),
					FirstName = userOrg.TempUser.FirstName,
					LastName = userOrg.TempUser.LastName,
				};

				OrgUtil.CreateUser(stx, user, password);

				//new AccountController().UserManager.Create(user, password);
				var newUserOrg = await OrganizationAccessor.JoinOrganization_Test(stx.s, user, Manager.Id, userOrg.Id);

				userOrg.User = newUserOrg.User;
				userOrg.TempUser = newUserOrg.TempUser;


				ExistingCreds[userOrg.Id] = new Credentials(user.UserName, password, userOrg);
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
			ExistingCreds[user.Id] = new Credentials(username, password, user);
		}
		public void AssertAllUsers(Predicate<UserOrganizationModel> testFunction, params UserOrganizationModel[] trueFor) {
			AssertAllUsers(testFunction, trueFor.ToList());
		}
		public void AssertAllUsers(Predicate<UserOrganizationModel> testFunction, IEnumerable<UserOrganizationModel> trueFor) {
			var exceptions = new List<AssertFailedException>();
			foreach (var user in AllUsers) {
				var expecting = false;
				if (trueFor.Any(x => x.Id == user.Id))
					expecting = true;
				var found = testFunction(user);
				if (expecting != found)
					exceptions.Add(new AssertFailedException("Assertion failed for " + user.GetName() + " (" + user.Id + "). Expecting: " + expecting + ".  Found: " + found + "."));
			}

			if (exceptions.Count == 1)
				throw exceptions[0];
			if (exceptions.Count > 1) {
				foreach (var e in exceptions) {
					Console.WriteLine(e.Message);
				}
				throw new AssertFailedException("Assertion failed for " + exceptions.Count + " users.");

			}
		}


		public async Task<L10> CreateL10(params UserOrganizationModel[] users) {
			var l10= await L10Utility.CreateRecurrence(org: this);
			foreach (var u in users) {
				await l10.AddAttendee(u);
			}
			return l10;
		}

		public async Task<L10> CreateL10(string name=null) {
			return await L10Utility.CreateRecurrence(name:name,org: this);
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

			BaseTest.RemoveIsTest();
			var stx = new SessionTransaction();
			stx.s = HibernateSession.GetCurrentSession();
			try {
				stx.tx = stx.s.BeginTransaction();
				try {
					var org = await CreateOrganization(stx, name, time);
					stx.tx.Commit();
					stx.s.Flush();
					return org;
					//Your action here
					//tx.Commit();
					//s.Flush();
				} finally {
					stx.tx.Dispose();
				}
			} finally {
				stx.s.Dispose();
			}
		}



		public static async Task<Org> CreateOrganization(SessionTransaction stx, string name = null, DateTime? time = null) {
			var now = DateTime.UtcNow;
			var time1 = time ?? DateTime.UtcNow;
			var nowMs = now.ToJavascriptMilliseconds() / 10000;
			name = name ?? ("TestOrg_" + nowMs);

			var org = new FullOrg();
			org.CreateTime = time1;
			org.UID = new Random().Next();


			//UserOrganizationModel employee = null;
			//UserOrganizationModel manager = null;
			//AccountabilityNode managerNode = null;
			//OrganizationModel o = null;

			var managerUser = new UserModel() {
				UserName = "manager@test_" + org.UID + ".com",
				FirstName = "manager",
				LastName = "" + nowMs,
			};
			var password = Guid.NewGuid().ToString();

			CreateUser(stx, managerUser, password);// Expensive

			BaseTest.MockHttpContext();
			var ocd = new OrgCreationData() {
				Name = name,
				EnableL10 = true,
				EnableReview = true,
			};

			//var createdOrganization = await new OrganizationAccessor().CreateOrganization(stx.s, managerUser, name, PaymentPlanType.Professional_Monthly_March2016, time1, true, true);//Very Expensive
			var createdOrganization = await new OrganizationAccessor().CreateOrganization(stx.s, managerUser, PaymentPlanType.Professional_Monthly_March2016, time1, ocd);//Very Expensive

			var manager = createdOrganization.NewUser;
			var managerNode = createdOrganization.NewUserNode;

			org.Organization = createdOrganization.organization;
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


			var managerPerms = PermissionsUtility.Create(stx.s, manager);
			var created = await JoinOrganizationAccessor.CreateUserUnderManager_Test(stx.s, managerPerms, settings);// null, false, -2, employeeName + "@test_" + org.UID + ".com", employeeName, "" + nowMs, out employee, false, "");
			var employee = created.User;
			org.Employee = employee;
			using (stx.tx = stx.s.BeginTransaction()) {
				org.EmployeeNode = AccountabilityAccessor.AppendNode(stx.s, managerPerms, null, managerNode.Id, userId: employee.Id);// Expensive

				stx.tx.Commit();
				stx.s.Flush();
			}

			org.EmployeeNode._Name = "employee " + nowMs;

			org.AllUserNodes = new List<AccountabilityNode>() { org.ManagerNode, org.EmployeeNode };
			org.AllUsers = new List<UserOrganizationModel>() { org.Manager, org.Employee };

			return org;
		}

		public static void CreateUser(SessionTransaction stx, UserModel managerUser, string password) {
			stx.tx.Commit();
			stx.s.Flush();
			stx.tx.Dispose();
			stx.s.Dispose();

			new AccountController().UserManager.Create(managerUser, password);

			BaseTest.RemoveIsTest();
			stx.s = HibernateSession.GetCurrentSession();
			stx.tx = stx.s.BeginTransaction();
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

			var addedUser = await JoinOrganizationAccessor.CreateUserUnderManager_Test(s, managerPerm, settings);// null, false, -2, uname.ToLower() + "@test_" + org.UID + ".com", uname, "" + ms, out user, isClient, isClient ? "ClientOrg" : "");
			var temp = addedUser.TempUser;
			user = addedUser.User;

			org.AllUsers.Add(user);
			org.Set(userSelector, user);


			using (var tx = s.BeginTransaction()) {
				if (managerNode != null) {
					var userNode = AccountabilityAccessor.AppendNode(s, managerPerm, null, managerNode.Id, userId: user.Id);
					userNode._Name = user.GetName();
					org.Set(nodeSelector, userNode);
					org.AllUserNodes.Add(userNode);
				}
				tx.Commit();
				s.Flush();
			}
		}

		public static async Task<UserOrganizationModel> AddUserToOrg(Org org, string uname) {
			var ms = org.CreateTime.ToJavascriptMilliseconds() / 10000;
			//UserOrganizationModel user = null;

			var settings = new CreateUserOrganizationViewModel() {
				ManagerNodeId = null,
				IsManager = false,
				OrgPositionId = -2,
				Email = uname.ToLower() + "@test_" + org.UID + ".com",
				FirstName = uname,
				LastName = "" + ms,
			};

			var addedTemp = await JoinOrganizationAccessor.CreateUserUnderManager(org.Manager, settings);// null, false, -2, uname.ToLower() + "@test_" + org.UID + ".com", uname, "" + ms, out user, isClient, isClient ? "ClientOrg" : "");
			org.AllUsers.Add(addedTemp.User);
			return addedTemp.User;
		}

		public static async Task<AccountabilityNode> AddUserToOrg(Org org, AccountabilityNode managerNode, string uname) {
			var user = await AddUserToOrg(org, uname);

			if (managerNode != null) {
				var userNode = AccountabilityAccessor.AppendNode(org.Manager, managerNode.Id, userId: user.Id);
				userNode._Name = user.GetName();
				org.AllUserNodes.Add(userNode);
				return userNode;
			}
			return null;
		}


		public static async Task<FullOrg> CreateFullOrganization(string name = null, DateTime? time = null) {
			var stx = new SessionTransaction();
			stx.s = HibernateSession.GetCurrentSession();
			try {
				stx.tx = stx.s.BeginTransaction();
				try {
					var org = await CreateFullOrganization(stx, name, time);
					stx.tx.Commit();
					stx.s.Flush();
					return org;
				} finally {
					stx.tx.Dispose();
				}
			} finally {
				stx.s.Dispose();
			}
		}

		/// <summary>
		/// See \TractionTools.Tests\Utilities\FullOrganization.png
		/// </summary>
		///
		public static async Task<FullOrg> CreateFullOrganization(SessionTransaction stx, string name = null, DateTime? time = null) {
			BaseTest.MockHttpContext();
			HttpContext.Current.Items["ctx"] = 1;
			FullOrg org = (FullOrg)await CreateOrganization(stx, name, time);
			//var s = stx.s;
			var managerPerms = PermissionsUtility.Create(stx.s, org.Manager);

			await AddUserToOrg(stx.s, managerPerms, org, null, "Client", x => x.Client, null, true);
			await AddUserToOrg(stx.s, managerPerms, org, org.ManagerNode, "Middle", x => x.Middle, x => x.MiddleNode);
			await AddUserToOrg(stx.s, managerPerms, org, org.ManagerNode, "E1", x => x.E1, x => x.E1MiddleNode);
			await AddUserToOrg(stx.s, managerPerms, org, org.MiddleNode, "E2", x => x.E2, x => x.E2Node);
			await AddUserToOrg(stx.s, managerPerms, org, org.MiddleNode, "E3", x => x.E3, x => x.E3Node);
			await AddUserToOrg(stx.s, managerPerms, org, org.E1MiddleNode, "E4", x => x.E4, x => x.E4Node);
			await AddUserToOrg(stx.s, managerPerms, org, org.E1MiddleNode, "E5", x => x.E5, x => x.E5Node);
			await AddUserToOrg(stx.s, managerPerms, org, org.E2Node, "E6", x => x.E6, x => x.E6Node);
			await AddUserToOrg(stx.s, managerPerms, org, null, "E7", x => x.E7, null);

			using (stx.tx = stx.s.BeginTransaction()) {
				org.E1BottomNode = AccountabilityAccessor.AppendNode(stx.s, managerPerms, null, org.MiddleNode.Id, userId: org.E1.Id);


				org.E1BottomNode._Name = org.E1.GetName();
				org.AllUserNodes.Add(org.E1BottomNode);

				//Create inter-reviewing team
				org.InterreviewTeam = TeamAccessor.EditTeam(stx.s, managerPerms, 0, "interreviewing-team", true, false, org.E5.Id);
				TeamAccessor.AddMember(stx.s, managerPerms, org.InterreviewTeam.Id, org.E5.Id);
				TeamAccessor.AddMember(stx.s, managerPerms, org.InterreviewTeam.Id, org.E6.Id);

				//Create non-reviewing team
				org.NonreviewTeam = TeamAccessor.EditTeam(stx.s, managerPerms, 0, "non-interreviewing-team", false, false, org.E3.Id);
				TeamAccessor.AddMember(stx.s, managerPerms, org.NonreviewTeam.Id, org.E3.Id);
				TeamAccessor.AddMember(stx.s, managerPerms, org.NonreviewTeam.Id, org.E4.Id);


				var allTeams = TeamAccessor.GetOrganizationTeams(stx.s, managerPerms, org.Id);

				org.AllMembersTeam = allTeams.First(x => x.Type == TeamType.AllMembers);
				org.AllManagersTeam = allTeams.First(x => x.Type == TeamType.Managers);
				org.MiddleSubordinatesTeam = allTeams.First(x => x.Type == TeamType.Subordinates && x.ManagedBy == org.Middle.Id);

				stx.tx.Commit();
				stx.s.Flush();
			}
			await org.RegisterUser(stx, org.E3);

			return org;
		}

	}
}
