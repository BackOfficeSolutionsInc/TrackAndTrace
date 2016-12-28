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

namespace TractionTools.Tests.Utilities {
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

		protected Dictionary<long, Credentials> ExistingCreds { get; set; }

		public Org() {
			ExistingCreds = new Dictionary<long, Credentials>();
		}

		public async Task RegisterUser(UserOrganizationModel user) {
			await GetCredentials(user);
		}

		public async Task<Credentials> GetCredentials(UserOrganizationModel user) {
			if (!ExistingCreds.ContainsKey(user.Id)) {
				BaseTest.MockHttpContext();
				var password = Guid.NewGuid().ToString();

				var u = new UserModel() {
					UserName = user.TempUser.Email.ToLower(),
					FirstName = user.TempUser.FirstName,					
					LastName = user.TempUser.LastName,
				};
				new AccountController().UserManager.Create(u, password);
				var org = OrganizationAccessor.JoinOrganization(u, Manager.Id, user.Id);
				//await new AccountController().Register(new RegisterViewModel() {
				//	Email = user.TempUser.Email,
				//	fname = user.TempUser.FirstName,
				//	lname = user.TempUser.LastName,
				//	Password = password,
				//	ConfirmPassword = password,
				//});
				//var u = new UserModel() { UserName = user.TempUser.Email, FirstName = user.TempUser.FirstName, LastName = user.TempUser.LastName };
				//var result = await UserAccessor.CreateUser(UserManager, u, password);
				ExistingCreds[user.Id] = new Credentials(u.UserName, password, user);
			}
			return ExistingCreds[user.Id];
		}

		public async Task RegisterAllUsers() {
			foreach (var u in AllUsers) {
				await GetCredentials(u);
			}
		}

		public void AddCredentials(UserOrganizationModel user, string username, string password) {
			ExistingCreds[user.Id] = new Credentials(username, password, user);
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
	}

	public class OrgUtil {
		public static Org CreateOrganization(string name = null, DateTime? time = null) {
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

			//UserModel _nilUserModel = null;

			//BaseTest.DbCommit(s => {
			//	s.Save(_nilUserModel);
			//});
			var managerUser = new UserModel() {
				UserName = "manager@test_" + org.UID + ".com",
				FirstName = "manager",
				LastName = "" + nowMs,
			};
			var password = Guid.NewGuid().ToString();
			new AccountController().UserManager.Create(managerUser, password);

			//var u = new UserModel() { UserName = "manager@test_"+org.Id+, FirstName = user.TempUser.FirstName, LastName = user.TempUser.LastName };
			//var result = await UserAccessor.CreateUser(UserManager, u, password);

			BaseTest.MockHttpContext();
			var organization = new OrganizationAccessor().CreateOrganization(managerUser, name,
				PaymentPlanType.Professional_Monthly_March2016, time1, out manager, out managerNode, true, true);
			org.Organization = organization;
			org.AddCredentials(manager, managerUser.UserName, password);

			//Now make them an actual user
			//UserModel managerUser = null;
			//BaseTest.DbCommit(s => {
			//	s.Evict(_nilUserModel);
			//	var u = s.Get<UserModel>(_nilUserModel.Id);
			//	u.UserName = "manager@test_" + org.Id + ".com";
			//	u.FirstName = "manager";
			//	u.LastName = "" + nowMs;
			//	s.Merge(u);
			//	managerUser = u;
			//});
			//var password = Guid.NewGuid().ToString();
			//var result = await new UserAccessor().CreateUser(new AccountController().UserManager, _nilUserModel, password);///.AddLogin(_nilUserModel.UserName, new UserLoginInfo() {
			//new AccountController().UserManager.Create(managerUser, password);
			//org.AddCredentials(manager, _nilUserModel.UserName, password);
			//Registration Complete



			org.ManagerNode = managerNode;
			org.ManagerNode._Name = "manager " + nowMs;

			org.Manager = manager;

			var employeeName = "employee";
			var tempUser = JoinOrganizationAccessor.CreateUserUnderManager(manager, null, false, -2, employeeName + "@test_" + org.UID + ".com", employeeName, "" + nowMs, out employee, false, "");

			org.Employee = employee;
			org.EmployeeNode = AccountabilityAccessor.AppendNode(manager, managerNode.Id, userId: employee.Id);
			org.EmployeeNode._Name = "employee " + nowMs;

			org.AllUserNodes = new List<AccountabilityNode>() { org.ManagerNode, org.EmployeeNode };
			org.AllUsers = new List<UserOrganizationModel>() { org.Manager, org.Employee };

			return org;
		}

		private static void AddUserToOrg(FullOrg org, AccountabilityNode managerNode, string uname, Expression<Func<FullOrg, UserOrganizationModel>> userSelector, Expression<Func<FullOrg, AccountabilityNode>> nodeSelector = null, bool isClient = false) {

			var ms = org.CreateTime.ToJavascriptMilliseconds() / 10000;
			UserOrganizationModel user = null;
			//UserOrganizationModel manager = null;
			//if (managerNode != null) {
			//	manager = org.AllUsers.First(x => x.Id == managerNode.UserId);
			//}
			var temp = JoinOrganizationAccessor.CreateUserUnderManager(org.Manager, null, false, -2, uname.ToLower() + "@test_" + org.UID + ".com", uname, "" + ms, out user, isClient, isClient ? "ClientOrg" : "");
			org.AllUsers.Add(user);
			org.Set(userSelector, user);


			if (managerNode != null) {
				var userNode = AccountabilityAccessor.AppendNode(org.Manager, managerNode.Id, userId: user.Id);
				userNode._Name = user.GetName();
				org.Set(nodeSelector, userNode);
				org.AllUserNodes.Add(userNode);
			}
		}


		/// <summary>
		/// See \TractionTools.Tests\Utilities\FullOrganization.png
		/// </summary>
		///
		public static FullOrg CreateFullOrganization(string name = null, DateTime? time = null) {
			FullOrg org = (FullOrg)CreateOrganization(name, time);

			AddUserToOrg(org, null, "Client", x => x.Client, null, true);
			AddUserToOrg(org, org.ManagerNode, "Middle", x => x.Middle, x => x.MiddleNode);
			AddUserToOrg(org, org.ManagerNode, "E1", x => x.E1, x => x.E1MiddleNode);
			AddUserToOrg(org, org.MiddleNode, "E2", x => x.E2, x => x.E2Node);
			AddUserToOrg(org, org.MiddleNode, "E3", x => x.E3, x => x.E3Node);
			AddUserToOrg(org, org.E1MiddleNode, "E4", x => x.E4, x => x.E4Node);
			AddUserToOrg(org, org.E1MiddleNode, "E5", x => x.E5, x => x.E5Node);
			AddUserToOrg(org, org.E2Node, "E6", x => x.E6, x => x.E6Node);
			AddUserToOrg(org, null, "E7", x => x.E7, null);

			org.E1BottomNode = AccountabilityAccessor.AppendNode(org.Manager, org.MiddleNode.Id, userId: org.E1.Id);
			org.E1BottomNode._Name = org.E1.GetName();
			org.AllUserNodes.Add(org.E1BottomNode);

			//Create inter-reviewing team
			org.InterreviewTeam = TeamAccessor.EditTeam(org.Manager, 0, "interreviewing-team", true, false, org.E5.Id);
			TeamAccessor.AddMember(org.Manager, org.InterreviewTeam.Id, org.E5.Id);
			TeamAccessor.AddMember(org.Manager, org.InterreviewTeam.Id, org.E6.Id);

			//Create non-reviewing team
			org.NonreviewTeam = TeamAccessor.EditTeam(org.Manager, 0, "non-interreviewing-team", false, false, org.E3.Id);
			TeamAccessor.AddMember(org.Manager, org.NonreviewTeam.Id, org.E3.Id);
			TeamAccessor.AddMember(org.Manager, org.NonreviewTeam.Id, org.E4.Id);


			var allTeams = TeamAccessor.GetOrganizationTeams(org.Manager, org.Id);

			org.AllMembersTeam = allTeams.First(x => x.Type == TeamType.AllMembers);
			org.AllManagersTeam = allTeams.First(x => x.Type == TeamType.Managers);
			org.MiddleSubordinatesTeam = allTeams.First(x => x.Type == TeamType.Subordinates && x.ManagedBy == org.Middle.Id);

			//Register E3
			UserModel e3User = null;
			BaseTest.DbCommit(s => {
				e3User = new UserModel();
				s.Save(e3User);
			});
			OrganizationAccessor.JoinOrganization(e3User, org.Middle.Id, org.E3.Id);

			return org;
		}
	}
}
