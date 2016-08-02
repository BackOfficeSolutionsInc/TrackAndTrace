using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
    public class TinyUser {
        public long UserOrgId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public Tuple<string, string, string, long> Tuplize(){
            return Tuple.Create(FirstName, LastName, Email, UserOrgId);
        }

        public override bool Equals(object obj){
            if (obj is TinyUser) {
                return this.Tuplize().Equals(((TinyUser)obj).Tuplize());
            }
            return false;
        }

        public override int GetHashCode(){
            return this.Tuplize().GetHashCode();
        }

        public TinyUser Standardize()
        {
            var x = this;
            return new TinyUser() {
                Email = x.Email.NotNull(y => y.ToLower()),
                FirstName = x.FirstName.NotNull(y => y.ToLower()),
                LastName = x.LastName.NotNull(y => y.ToLower()),
                UserOrgId = x.UserOrgId
            };
        }

        public static TinyUser FromUserOrganization(UserOrganizationModel x)
        {
            if (x == null)
                return null;

            return new TinyUser() {
                Email = x.GetEmail().NotNull(y => y.ToLower()),
                FirstName = x.GetFirstName().NotNull(y => y.ToLower()),
                LastName = x.GetLastName().NotNull(y => y.ToLower()),
                UserOrgId = x.Id
            };
        }
    }

}