﻿using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors {
	public class ForModelAccessor {


		public static string GetEmail_Unsafe(IForModel forModel) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetEmail_Unsafe(s, forModel);
				}
			}
		}

		public static TinyUser GetTinyUser_Unsafe(IForModel forModel) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetTinyUser_Unsafe(s, forModel);
				}
			}
		}


		public static string GetEmail_Unsafe(ISession s, IForModel forModel) {
			if (forModel.Is<UserOrganizationModel>()) {
				return s.Get<UserOrganizationModel>(forModel.ModelId).NotNull(x => x.GetEmail());
			} else if (forModel.Is<AccountabilityNode>()) {
				return s.Get<AccountabilityNode>(forModel.ModelId).NotNull(x => x.User.GetEmail());
			} else {
				Console.WriteLine("Unhandled type:" + forModel.ModelType);
			}
			return null;
		}

		public static TinyUser GetTinyUser_Unsafe(ISession s, IForModel forModel) {
			if (forModel.Is<UserOrganizationModel>()) {
				return s.Get<UserOrganizationModel>(forModel.ModelId).NotNull(x => TinyUser.FromUserOrganization(x));
			} else if (forModel.Is<AccountabilityNode>()) {
				return s.Get<AccountabilityNode>(forModel.ModelId).NotNull(x => TinyUser.FromUserOrganization(s.Get<UserOrganizationModel>(x.User)));
			} else {
				Console.WriteLine("Unhandled type:" + forModel.ModelType);
			}
			return null;
		}
	}
}