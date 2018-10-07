using ApiDesign.Models.Database;
using ApiDesign.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiDesign.Accessors {
	public class RockAccessor {

		public static IEnumerable<IRockModel> GetRocks() {
			yield return new RockModel() {
				Id = 1,
				OwnerId = 100,
				Rock = "First Rock"
			};
			yield return new RockModel() {
				Id = 2,
				OwnerId = 100,
				Rock = "Second Rock"
			};
			yield return new RockModel() {
				Id = 3,
				OwnerId = 101,
				Rock = "Third Rock"
			};
		}
	}
}