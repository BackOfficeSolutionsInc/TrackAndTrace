using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TractionTools.Tests.Utilities {
	

	public class Credentials {
		public string Username { get; private set; }
		public string Password { get; private set; }
		public UserOrganizationModel User { get; private set; }

		public Credentials(String username, string password, UserOrganizationModel user = null,bool wasCreated = false) {
			Username = username;
			Password = password;
			User = user;
		}

		public override bool Equals(object obj) {
			if (obj is Credentials) {
				var o = (Credentials)obj;
				return o.Username == Username && o.Password == Password;
			}
			return false;
		}

		public override int GetHashCode() {
			return Username.GetHashCode() + Password.GetHashCode();
		}
	}
}
