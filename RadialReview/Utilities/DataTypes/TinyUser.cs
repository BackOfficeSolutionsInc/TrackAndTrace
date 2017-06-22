using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
	public class TinyUser : IForModel {
		public long UserOrgId { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }

		public long ModelId {get {return UserOrgId;}}
		public string ModelType {get {return ForModel.GetModelType<UserOrganizationModel>();}}

		public Tuple<string, string, string, long> Tuplize() {
			return Tuple.Create(FirstName, LastName, Email, UserOrgId);
		}

		public override bool Equals(object obj) {
			if (obj is TinyUser) {
				return this.Tuplize().Equals(((TinyUser)obj).Tuplize());
			}
			return false;
		}

		public override int GetHashCode() {
			return this.Tuplize().GetHashCode();
		}

		public string GetName() {
			return ((FirstName ?? "") + " " + (LastName ?? "")).Trim();
		}

		public string GetInitials() {
			var inits = new List<string>();
			if (FirstName != null && FirstName.Length > 0)
				inits.Add(FirstName.Substring(0, 1));
			if (LastName != null && LastName.Length > 0)
				inits.Add(LastName.Substring(0, 1));
			return string.Join(" ", inits).ToUpperInvariant();
		}

		public TinyUser Standardize() {
			var x = this;
			return new TinyUser() {
				Email = x.Email.NotNull(y => y.ToLower()),
				FirstName = x.FirstName.NotNull(y => y.ToLower()),
				LastName = x.LastName.NotNull(y => y.ToLower()),
				UserOrgId = x.UserOrgId
			};
		}

		public static TinyUser FromUserOrganization(UserOrganizationModel x) {
			if (x == null)
				return null;

			return new TinyUser() {
				Email = x.GetEmail().NotNull(y => y.ToLower()),
				FirstName = x.GetFirstName().NotNull(y => y.ToLower()),
				LastName = x.GetLastName().NotNull(y => y.ToLower()),
				UserOrgId = x.Id
			};
		}

		public bool Is<T>() {
			return ForModel.GetModelType<UserOrganizationModel>() == ForModel.GetModelType(typeof(T));
		}

		public string ToPrettyString() {
			return GetName();
		}

		//public static Expression<Func<UserOrganizationModel, object>>[] Projections = new[] { x => userAlias.FirstName, x => userAlias.LastName, x => x.Id, x => tempUserAlias.FirstName, x => tempUserAlias.LastName, x => userAlias.UserName, x => tempUserAlias.Email };

		//public static Func<object[], TinyUser> Unpackage = new Func<object[], TinyUser>(x => {
		//	var fname = (string)x[0];
		//	var lname = (string)x[1];
		//	var email = (string)x[5];
		//	var uoId = (long)x[2];
		//	if (fname == null && lname == null) {
		//		fname = (string)x[3];
		//		lname = (string)x[4];
		//		email = (string)x[6];
		//	}
		//	return new TinyUser() {
		//		FirstName = fname,
		//		LastName = lname,
		//		Email = email,
		//		UserOrgId = uoId
		//	};
		//});


	}

}