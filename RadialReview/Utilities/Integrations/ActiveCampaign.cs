using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using static RadialReview.Utilities.Config;
using log4net;
using RadialReview.Models;
using System.Linq;

namespace RadialReview.Utilities.Integrations {
	public class ActiveCampaignConnector {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public class ActiveCampaignRequest{
			public string Url { get; set; }
			public string ApiAction { get; set; }
			public Dictionary<string,string> Parameters { get; set; }
		}

		protected List<ActiveCampaignRequest> TestRequests { get; set; }

		private ActiveCampaignConfig Configs;

		public ActiveCampaignConnector(ActiveCampaignConfig config) {
			if (string.IsNullOrEmpty(config.ApiKey))
				throw new ArgumentNullException(nameof(config.ApiKey));

			if (string.IsNullOrEmpty(config.BaseUrl))
				throw new ArgumentNullException(nameof(config.BaseUrl));
			if (string.IsNullOrEmpty(config.EventUrl))
				throw new ArgumentNullException(nameof(config.EventUrl));
			if (string.IsNullOrEmpty(config.TrackKey))
				throw new ArgumentNullException(nameof(config.TrackKey));
			if (string.IsNullOrEmpty(config.ActId))
				throw new ArgumentNullException(nameof(config.ActId));
			this.Configs = config;
		}

		public async Task TestConnection() {
			var result = await ApiAsync("user_me");
			if (result.IsSuccessful == false)
				throw new Exception("ActiveCampaign Connection Failed");
		}

		private string CreateBaseUrl(string apiAction) {
			return $"{Configs.BaseUrl}/admin/api.php?api_action={apiAction}&api_key={Configs.ApiKey}&api_output=json";
		}
			

		public async Task<ApiResult> EventAsync(string eventName, string email, Dictionary<string, string> parameters = null, Dictionary<string, string> visit = null) {

			parameters = parameters ?? new Dictionary<string, string>();

			//var transform =

			parameters["actid"] = Configs.ActId;
			parameters["key"] = Configs.TrackKey;
			parameters["event"] = eventName;
			visit = visit ?? new Dictionary<string, string>();
			email = Config.ModifyEmail(email);
			visit["email"]=  email;

			if (visit != null) {
				parameters["visit"] = JsonConvert.SerializeObject(visit);
			}

			return await ApiAsync(null, parameters, "https://trackcmp.net/event");

		}

		public async Task<ApiResult> ApiAsync(string apiAction, Dictionary<string, string> parameters = null, string uri = null)  {
			try {
				//var payload = PreparePayload(parameters);
				parameters = parameters ?? new Dictionary<string, string>();
				uri = uri ?? CreateBaseUrl(apiAction);	
				log.Info("ActiveCampaign: " + uri + " - " + String.Join("&", parameters.ToList()));
				if (!Configs.TestMode) {
					using (HttpClient httpClient = new HttpClient()) {
						using (var postContent = new FormUrlEncodedContent(parameters)) {						
							using (HttpResponseMessage response = await httpClient.PostAsync(uri, postContent)) {
								response.EnsureSuccessStatusCode(); //throw if httpcode is an error
								using (HttpContent content = response.Content) {
									string rawData = await content.ReadAsStringAsync();
									var result = JsonConvert.DeserializeObject<ApiResult>(rawData);
									result.Data = rawData;
									return result;
								}
							}
						}
					}
				} else {

					TestRequests = TestRequests ?? new List<ActiveCampaignRequest>();
					TestRequests.Add(new ActiveCampaignRequest() {
						Url = uri,
						ApiAction = apiAction,
						Parameters = parameters
					});

					return new ApiResult() {
						Message = "Active Campaign is in test mode",
					}.SetTestSuccess();
				}
			} catch (Exception e) {
				log.Error("ActiveCampaign Connector Error", e);
				return new ApiResult() {
					Message = "Failed",
				};
			}
		}

		public async Task SyncContact(Config.ActiveCampaignConfig configs, string contactEmail,
			List<long> listIds=null, List<string> tags = null, Dictionary<long, string> fieldVals = null) {

			var connector = this;

			var email = contactEmail;
			if (Config.IsLocal()) {
				email = "clay.upton+" + (email ?? "").Replace("@", "_") + "@mytractiontools.com";
			}

			var dict = new Dictionary<string, string>() {
					{"email",email },			
			};

			if (Config.IsLocal()) {
				dict["field[" + configs.Fields.IsTest + ",0]"] = "Yes";
			}

			if (fieldVals != null) {
				foreach (var f in fieldVals) {
					dict["field[" + f.Key + ",0]"] = f.Value;
				}
			}

			if (tags != null) {
				dict["tags"] = string.Join(",", tags);
			}

			if (listIds != null) {
				listIds.Add(configs.Lists.ContactList);
				foreach (var listId in listIds.Distinct(x => x)) {
					dict["p[" + listId + "]"] = listId + "";
					dict["status[" + listId + "]"] = "" + 1;        //Auto subscribe to primary (Is this right?)
				}
			}

			await connector.ApiAsync("contact_sync", dict);
		}

		public async Task SyncContact(Config.ActiveCampaignConfig configs, UserOrganizationModel contact,
			List<long> listIds, List<string> tags = null, Dictionary<long, string> fieldVals = null) {

			var connector = this;

			var email = contact.GetEmail();
			email = Config.ModifyEmail(email);
		

			var dict = new Dictionary<string, string>() {
					{"email",email },
					{"first_name",contact.GetFirstName() },
					{"last_name",contact.GetLastName() },
					{"orgname",contact.Organization.GetName() },
					{"field["+configs.Fields.OrgId+",0]",""+contact.Organization.Id }, // Field specifying the orgId
					{"field["+configs.Fields.Autogenerated+",0]","Yes" }, // Field specifying Autogenerated
					{"field["+configs.Fields.UserId+",0]", ""+contact.Id }, // Field specifying UserId
					{"field["+configs.Fields.Title+",0]", contact.GetTitles(1) }, // Field specifying Title
			};

			if (Config.IsLocal()) {
				dict["field[" + configs.Fields.IsTest + ",0]"] = "Yes";
			}

			if (fieldVals != null) {
				foreach (var f in fieldVals) {
					dict["field[" + f.Key + ",0]"] = f.Value;
				}
			}

			if (tags != null) {
				dict["tags"] = string.Join(",", tags);
			}

			if (listIds != null) {
				listIds.Add(configs.Lists.ContactList);
				foreach (var listId in listIds.Distinct(x=>x)) {
					dict["p[" + listId + "]"] = listId + "";
					dict["status[" + listId + "]"] = "" + 1;        //Auto subscribe to primary (Is this right?)
				}
			}

			await connector.ApiAsync("contact_sync", dict);
		}
	}
	
	public class ApiResult {

		[JsonProperty("message")]
		public string EvtMessage { set { Message = value; } }

		[JsonProperty("result_message")]
		public string Message { get; set; }

		[JsonProperty("result_output")]
		public string Output { get; set; }

		public string Data { get; set; }

		public bool IsSuccessful => Code == 1 || EventSuccess == 1;

		[JsonProperty("result_code")]
		protected int Code { get; set; }

		[JsonProperty("success")]
		protected int EventSuccess { get; set; }

		protected bool TestMode { get; set; }

		public bool InTestMode() {
			return TestMode;
		}

		public ApiResult SetTestSuccess() {
			EventSuccess = 1;
			Code = 1;
			TestMode = true;
			return this;
		}

	}
}