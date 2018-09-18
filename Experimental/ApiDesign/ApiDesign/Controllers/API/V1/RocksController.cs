using ApiDesign.Accessors;
using ApiDesign.Models.Interfaces;
using ApiDesign.Utilities.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ApiDesign.Controllers.V1 {

	public class RocksController : ApiController {
				
		public IEnumerable<IRockModel> Get() {
			return RockAccessor.GetRocks();
		}
		
		public IRockModel Get(int id) {
			return RockAccessor.GetRocks().Where(x=>x.GetId()==id).FirstOrDefault();
		}
		
		public void Post([FromBody]string value) {
		}
		
		public void Put(int id, [FromBody]string value) {
		}
		
		public void Delete(int id) {
		}
	}
}