using Newtonsoft.Json;
using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using RadialReview.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using static RadialReview.Utilities.SelectExistingOrCreateUtility;

namespace RadialReview.Utilities.DataTypes {
	[DataContract]
	public class TinyUser : IForModel {
		[DataMember]
		[JsonProperty("Id")]
		public long UserOrgId { get; set; }
		[DataMember]
		public string FirstName { get; set; }
		[DataMember]
		public string LastName { get; set; }
		[DataMember]
		public string Email { get; set; }
		public string ImageGuid { get; set; }
		[DataMember]
		public string ImageUrl { get { return GetImageUrl(); } }
		[DataMember]
		public string Name { get { return GetName(); } }
		[DataMember]
		public string Initials { get { return GetInitials(); } }

		public long ModelId { get { return UserOrgId; } }
		public string ModelType { get { return ForModel.GetModelType<UserOrganizationModel>(); } }

		public string Description {
			get {
				return null;
			}
		}

		public string ItemValue {get {return "" + UserOrgId;}}

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


		public string GetImageUrl(ImageSize size = ImageSize._64) {
			
			return UserLookup.TransformImageSuffix(ImageGuid.NotNull(x=>"/"+x+".png"), size);
		}

		public string GetName() {
			return ((FirstName ?? "").Trim() + " " + (LastName ?? "").Trim()).Trim();
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
				FirstName = x.GetFirstName(),
				LastName = x.GetLastName(),
				UserOrgId = x.Id,
				ImageGuid = x.User.NotNull(y=>y.ImageGuid)
			};
		}

		public bool Is<T>() {
#pragma warning disable CS0618 // Type or member is obsolete
			return ForModel.GetModelType<UserOrganizationModel>() == ForModel.GetModelType(typeof(T));
#pragma warning restore CS0618 // Type or member is obsolete
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