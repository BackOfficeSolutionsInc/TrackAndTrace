using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RadialReview.Utilities.Integrations;
using RadialReview.Utilities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TractionTools.Tests.Integrations {
	[TestClass]
	public class ActiveCampaignTest {
		[TestMethod]
		public async Task CreateEvent() {
			var config = Config.GetActiveCampaignConfig();
			config.TestMode = false;
			var connector = new ActiveCampaignConnector(config);
			var result = await connector.EventAsync("Test",null,new Dictionary<string, string>() {
				{"email","clay.upton@mytractiontools.com" }
			});

			Assert.IsTrue(result.IsSuccessful);

			//var result = client.Api("contact_add", new Dictionary<string, string>
			// {
			//	 {"email", "someemail@gmail.com"},
			//	 {"first_name", "mathieu"},
			//	 {"last_name", "kempe"},
			//	 {"p[1]", "1"}
			// });

			//if (result.IsSuccessful) {
			//	Console.WriteLine(result.Message);
			//}

		}


		[TestMethod]
		public async Task ConnectionTest() {
			var config = Config.GetActiveCampaignConfig();
			config.TestMode = false;
			var connector = new ActiveCampaignConnector(config);
			await connector.TestConnection();
		}
	}
}
