using NHibernate;
using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Accountability;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;

namespace RadialReview.Areas.People.Engines.Surveys.Strategies.Transformers {
	public class TransformAboutAccountabilityNodes : ITransformByAbout {
		protected ISession s;
		public TransformAboutAccountabilityNodes(ISession s) {
			this.s = s;
		}
		//convert by's that are AccountabilityNodes to UserOrgs
		public IEnumerable<IByAbout> TransformForCreation(IEnumerable<IByAbout> byAbout) {
			var bys = byAbout.Select(x => x.GetBy()).ToList();
			var accNodeBys = bys.Where(x => x.Is<AccountabilityNode>());

			var newByAbouts = new List<IByAbout>();
			if (accNodeBys.Any()) {
				var accNodeIds = accNodeBys.Select(x => x.ModelId).ToArray();
				var accNodes = s.QueryOver<AccountabilityNode>().Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.Id).IsIn(accNodeIds)
					.List().ToList();
				foreach (var ba in byAbout) {
					var toAdd = ba.ToImpl();
					if (ba.GetBy().Is<AccountabilityNode>()) {
						var foundUser = accNodes.FirstOrDefault(x => x.Id == ba.GetBy().ModelId).NotNull(x => x.User);
						if (foundUser != null) {
							toAdd.By = foundUser;
						} else {
							throw new Exception("User does not exist");
						}
					}
					newByAbouts.Add(toAdd);
				}
			} else {
				newByAbouts = byAbout.ToList();
			}
			return newByAbouts.Distinct(x => x.ToKey()).ToList();
		}

		//public IEnumerable<IByAbout> ReconstructTransform(IEnumerable<IByAbout> byAbout) {
		//	byAbout = CreateTransform(byAbout);
		//	var abouts = byAbout.Select(x => x.GetAbout()).ToList();
		//	var userOrgAbouts = abouts.Where(x => x.Is<UserOrganizationModel>());
		//	if (userOrgAbouts.Any()) {
		//		var accNodeIds = userOrgAbouts.Select(x => x.ModelId).ToArray();
		//		var accNodes = s.QueryOver<AccountabilityNode>().Where(x => x.DeleteTime == null)
		//			.WhereRestrictionOn(x => x.Id).IsIn(accNodeIds)
		//			.List().ToList();
		//		foreach (var ba in byAbout) {
		//			var toAdd = ba.ToImpl();
		//			if (ba.GetBy().Is<AccountabilityNode>()) {
		//				var foundUser = accNodes.FirstOrDefault(x => x.Id == ba.GetBy().ModelId).NotNull(x => x.User);
		//				if (foundUser != null) {
		//					toAdd.By = foundUser;
		//				} else {
		//					throw new Exception("User does not exist");
		//				}
		//			}
		//			newByAbouts.Add(toAdd);
		//		}
		//	} else {
		//		newByAbouts = byAbout.ToList();
		//	}
		//	return newByAbouts.Distinct(x => x.ToKey()).ToList();


		//	return byAbout;
		//}
	}
}