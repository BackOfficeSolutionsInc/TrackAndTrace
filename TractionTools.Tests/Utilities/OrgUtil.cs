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

namespace TractionTools.Tests.Utilities {
	public class Org {
		public long Id { get { return Organization.Id; } }
		public OrganizationModel Organization { get; set; }
		public UserOrganizationModel Employee { get; set; }
		public UserOrganizationModel Manager { get; set; }

		public AccountabilityNode ManagerNode { get; set; }
		public AccountabilityNode EmployeeNode { get; set; }
	}


	public class OrgUtil {
		public static Org CreateOrganization(string name = null,DateTime? time=null) {
			var now = DateTime.UtcNow;
			var time1 = time ?? DateTime.UtcNow;
			var nowMs = now.ToJavascriptMilliseconds();
			name = name ?? ("TestOrg_" + nowMs);

			var org = new Org();

			UserOrganizationModel employee = null;
			UserOrganizationModel manager = null;
			AccountabilityNode managerNode = null;
			OrganizationModel o = null;

			UserModel _nilUserModel = null;

			BaseTest.DbCommit(s => {
				_nilUserModel = new UserModel();
				s.Save(_nilUserModel);
			});

			var organization = new OrganizationAccessor().CreateOrganization(_nilUserModel, name,
				PaymentPlanType.Professional_Monthly_March2016, time1, out manager, out managerNode, true, true);

			org.ManagerNode = managerNode;
			org.Manager = manager;
			org.Organization = organization;

			var employeeName = "employee_" + nowMs;
			var tempUser = new NexusAccessor().CreateUserUnderManager(manager, managerNode.Id, false, -2, employeeName + "@test.com", employeeName, "employee", out employee, false, "");

			org.Employee = employee;
			org.EmployeeNode = AccountabilityAccessor.AppendNode(manager, managerNode.Id, userId: employee.Id);

			return org;
		}
	}
}
