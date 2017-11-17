using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Exceptions;
using System.Threading.Tasks;

namespace TractionTools.Tests.Permissions {
	[TestClass]
	public class QuestionPermissions : BasePermissionsTest {

		[TestMethod]
		[TestCategory("Permissions")]
		public async Task CreateQuestion() {

			var c = await Ctx.Build();

			var qa = new QuestionAccessor();

			//Create Question for organization
			foreach (var u in c.AllUsers) {
				if (!c.AllAdmins.Any(admin=>admin.Id == u.Id)) {
					Throws<PermissionsException>(() => qa.EditQuestion(u, 0, new Origin(OriginType.Organization, c.Id)));
				} else {
					qa.EditQuestion(u, 0, new Origin(OriginType.Organization, c.Id));
				}
			}
			
			//Create Question for user			
			qa.EditQuestion(c.E1, 0, new Origin(OriginType.User, c.E4.Id));

			Throws<PermissionsException>(() => qa.EditQuestion(c.E4, 0, new Origin(OriginType.User, c.E1.Id)));
			Throws<PermissionsException>(() => qa.EditQuestion(c.E2, 0, new Origin(OriginType.User, c.Middle.Id)));
			Throws<PermissionsException>(() => qa.EditQuestion(c.E2, 0, new Origin(OriginType.User, c.E2.Id)));
			Throws<PermissionsException>(() => qa.EditQuestion(c.E6, 0, new Origin(OriginType.User, c.E6.Id)));
			Throws<PermissionsException>(() => qa.EditQuestion(c.E1, 0, new Origin(OriginType.User, c.E6.Id)));
		}


		[TestMethod]
		[TestCategory("Permissions")]
		public async Task EditQuestion() {
			var c = await Ctx.Build();

			var qa= new QuestionAccessor();
			QuestionModel q;
			//Create Question for E4
			q = qa.EditQuestion(c.E1, 0, new Origin(OriginType.User, c.E4.Id));
			c.AssertAll(p => p.EditQuestion(q.Id), c.E1, c.Manager);

			//Create Question for E6
			q = qa.EditQuestion(c.E2, 0, new Origin(OriginType.User, c.E6.Id));
			c.AssertAll(p => p.EditQuestion(q.Id), c.E2, c.Middle, c.Manager);

			//Create Question for Employee
			q = qa.EditQuestion(c.Manager, 0, new Origin(OriginType.User, c.E6.Id));
			c.AssertAll(p => p.EditQuestion(q.Id), c.E2, c.Middle, c.Manager);

			//Create Question for Employee
			q = qa.EditQuestion(c.Manager, 0, new Origin(OriginType.User, c.E7.Id));
			c.AssertAll(p => p.EditQuestion(q.Id), c.Manager);
		}
		/*

			[TestMethod]
			[TestCategory("Permissions")]
			public async Task XXX() {
				var c = await Ctx.Build();
				c.AssertAll(p => p.XXX(YYY), c.Manager);
				//var perm = new Action<PermissionsUtility>(p=>p.XXX(YYY));
				//c.AssertAll(perm, c.Manager);
			}

			 */
	}
}
