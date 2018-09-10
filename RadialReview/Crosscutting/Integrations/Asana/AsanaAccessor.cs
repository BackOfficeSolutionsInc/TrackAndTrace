using Newtonsoft.Json;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Integrations;
using RadialReview.Models.Integrations.Asana;
using RadialReview.Utilities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Crosscutting.Integrations.Asana {
	public class AsanaAccessor {

		public static string GetRedirectUrl() {
			return Config.BaseUrl(null, "/Integrations/AsanaRedirect");
		}

		public static string AsanaUrl(string append) {
			return "https://app.asana.com/api/1.0/" + append.NotNull(x => x.TrimStart('/'));
		}


		public static async Task<AsanaToken> Register(UserOrganizationModel caller, long userId, string code) {
			//Token Exchange endpoint.. Get Bearer and Refresh token
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);

					var user = s.Get<UserOrganizationModel>(userId);

					var token = await CreateToken_Unsafe(s, userId, user.Organization.Id, code);
					tx.Commit();
					s.Flush();
					return token;
				}
			}
		}

		public static async Task<AsanaToken> GetTokenForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);

					var token = await GetUserToken_Unsafe(s, userId);

					tx.Commit();
					s.Flush();
					return token;
				}
			}

		}

		public static async Task<AsanaToken> GetToken(UserOrganizationModel caller, long tokenId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewAsanaToken(tokenId);
					return s.Get<AsanaToken>(tokenId);
				}
			}
		}


		public static async Task<AsanaAction> GetAction(UserOrganizationModel caller, long actionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewAsanaAction(actionId);
					var action = s.Get<AsanaAction>(actionId);
					action.PopulateDescription(s);

					return action;
				}
			}
		}

		public static async Task DeleteAction(UserOrganizationModel caller, long actionId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditAsanaAction(actionId);
					var action = s.Get<AsanaAction>(actionId);
					action.DeleteTime = DateTime.UtcNow;
					s.Update(action);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static async Task DeleteToken(UserOrganizationModel caller, long tokenId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditAsanaToken(tokenId);
					var token = s.Get<AsanaToken>(tokenId);
					token.DeleteTime = DateTime.UtcNow;
					s.Update(token);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task<List<AsanaProject>> GetAvailableProjects(UserOrganizationModel caller, long userId) {
			AsanaToken token;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);
					token = await GetUserToken_Unsafe(s, userId);
					if (token == null) {
						throw new PermissionsException("Asana is not connected. Connect Asana first.");
					}
					tx.Commit();
					s.Flush();
				}
			}

			//using (var client = new WebClient()) {
			//client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token.AccessToken;

			//client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
			//string responseBody = await client.DownloadStringTaskAsync(AsanaUrl("workspaces"));
			//var response = JsonConvert.DeserializeObject<AsanaWorkspaceDTO>(responseBody);

			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			var client = new RestClient("https://app.asana.com/api/1.0/workspaces");
			var request = new RestRequest(Method.GET);
			request.AddHeader("postman-token", "98b69928-ad68-4fc6-ee7e-700c7e43cd88");
			request.AddHeader("cache-control", "no-cache");
			request.AddHeader("authorization", "Bearer " + token.AccessToken);
			request.AddHeader("accept", "application/json");
			request.Timeout = 4000;			
			IRestResponse response = client.Execute(request);
			
			var workspaces = JsonConvert.DeserializeObject<AsanaWorkspaceDTO>(response.Content);

			var results = new List<AsanaProject>();

			foreach (var workspace in workspaces.data) {
				var pclient = new RestClient("https://app.asana.com/api/1.0/projects?workspace="+workspace.id);
				var prequest = new RestRequest(Method.GET);
				prequest.AddHeader("postman-token", "98b69928-ad68-4fc6-ee7e-700c7e43cd88");
				prequest.AddHeader("cache-control", "no-cache");
				prequest.AddHeader("authorization", "Bearer " + token.AccessToken);
				prequest.AddHeader("accept", "application/json");
				prequest.Timeout = 4000;
				response = pclient.Execute(prequest);

				var projects = JsonConvert.DeserializeObject<AsanaWorkspaceDTO>(response.Content);
				foreach (var project in projects.data) {
					results.Add(new AsanaProject() {
						Id = project.id,
						Name = project.name,
						Workspace = workspace.name,
						WorkspaceId = workspace.id
					});
				}
			}

			return results;
		}
		public static async Task<AsanaAction> CreateAction(UserOrganizationModel caller, long tokenId, long projectId, bool syncMyTodos,string workspaceName, string projectName, long workspaceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditAsanaToken(tokenId);
					var action = new AsanaAction() {
						ProjectId = projectId,
						AsanaTokenId = tokenId,
						WorkspaceId = workspaceId,
					};
					action.ActionType = AsanaActionType.NoAction;
					if (syncMyTodos) {
						action.ActionType = action.ActionType | AsanaActionType.SyncMyTodos;
					}
					action.WorkspaceName = workspaceName;
					action.ProjectName = projectName;
					//TODO: send updates to asana also
					s.Save(action);

					action.PopulateDescription(s);

					tx.Commit();
					s.Flush();
					return action;
				}
			}
		}

		public static async Task<AsanaAction> UpdateAction(UserOrganizationModel caller, long actionId, long projectId, bool syncMyTodos, string workspaceName, string projectName,long workspaceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.EditAsanaAction(actionId);
					var action = s.Get<AsanaAction>(actionId);
					action.ProjectId = projectId;
					action.WorkspaceId = workspaceId;
					action.ActionType = AsanaActionType.NoAction;
					if (syncMyTodos) {
						action.ActionType = action.ActionType | AsanaActionType.SyncMyTodos;
					}
					action.WorkspaceName = workspaceName;
					action.ProjectName = projectName;
					//TODO: send updates to asana also
					s.Update(action);
					action.PopulateDescription(s);
					tx.Commit();
					s.Flush();
					return action;
				}
			}
		}

		public static async Task<List<AsanaAction>> GetActionsForToken_Unsafe(ISession s, long tokenId) {
			return s.QueryOver<AsanaAction>().Where(x => x.DeleteTime == null && x.AsanaTokenId == tokenId).List().ToList();
		}

		public static async Task<List<AsanaAction>> GetUsersActions(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);
					var tokenIds = s.QueryOver<AsanaToken>()
						.Where(x => x.DeleteTime == null && x.CreatorId == userId)
						.Select(x => x.Id)
						.List<long>().ToList();
					var list = s.QueryOver<AsanaAction>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.AsanaTokenId).IsIn(tokenIds)
						.List().ToList();
					foreach (var i in list) {
						i.PopulateDescription(s);
					}
					return list;
				}
			}
		}

		[Obsolete("must call commit")]
		public static async Task<AsanaToken> GetUserToken_Unsafe(ISession s, long userId) {
			var tokens = s.QueryOver<AsanaToken>()
				.Where(x => x.DeleteTime == null && x.CreatorId == userId)
				.List().ToList();
			var token = tokens.FirstOrDefault();
			return await RefreshToken(s, token);
		}

		//public static string LastResponse1;
		//public static string LastResponse2;

		private static async Task<AsanaToken> CreateToken_Unsafe(ISession s, long userId, long orgId, string code) {
			var asanaData = Config.Asana();
			string responseBody;
			var redirectUri = GetRedirectUrl();
			try {
				using (var client = new WebClient()) {
					var param = new NameValueCollection();
					param.Add("grant_type", "authorization_code");
					param.Add("client_id", asanaData.ClientId);
					param.Add("client_secret", asanaData.ClientSecret);
					param.Add("redirect_uri", redirectUri);
					param.Add("code", code);

					client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
					byte[] responsebytes = await client.UploadValuesTaskAsync("https://app.asana.com/-/oauth_token", "POST", param);
					responseBody = Encoding.UTF8.GetString(responsebytes);
					//LastResponse1 = responseBody;
				}
			} catch (Exception e) {
				throw;
			}

			return SaveToken(s, userId, orgId, responseBody, redirectUri);
		}

		private static AsanaToken SaveToken(ISession s, long userId, long orgId, string responseBody, string redirectUri) {
			var response = JsonConvert.DeserializeObject<TokenExchangeEndpointResponse>(responseBody);
			//LastResponse2 = response.data;
			//var responseUserData = JsonConvert.DeserializeObject<TokenExchangeEndpointResponseDataResponse>(response.data);


			var token = new AsanaToken() {
				Expires = CalculateExpiration(response.expires_in),
				RefreshToken = response.refresh_token,
				AccessToken = response.access_token,
				CreatorId = userId,
				RedirectUri = redirectUri,
				OrganizationId = orgId,
				AsanaUserId = response.data.id,
				AsanaEmail = response.data.email,
			};

			s.Save(token);

			return token;
		}

		private static DateTime CalculateExpiration(int expires_in) {
			return DateTime.UtcNow.AddSeconds(expires_in * .9);
		}

		//private static async Task<List<AsanaToken>> GetTokensFor(ISession s, ForModel resource) {
		//	var asanaData = Config.Asana();
		//	var tokens = s.QueryOver<AsanaToken>()
		//		.Where(x => x.DeleteTime == null && x.Resource.ModelId == resource.ModelId && x.Resource.ModelType == resource.ModelType)
		//		.List().ToList();
		//	var results = new List<AsanaToken>();
		//	var now = DateTime.UtcNow;
		//	//Refresh all
		//	foreach (var token in tokens) {
		//		results.Add(await RefreshToken(s, token, now));
		//	}
		//	return results;
		//}


		private static async Task<AsanaToken> RefreshToken(ISession s, AsanaToken token, DateTime? now = null) {
			if (token == null)
				return null;

			now = now ?? DateTime.UtcNow;
			var asanaData = Config.Asana();

			if (token.Expires < now) {
				string responseBody;
				using (var client = new WebClient()) {
					var param = new NameValueCollection();
					param.Add("grant_type", "refresh_token");
					param.Add("client_id", asanaData.ClientId);
					param.Add("client_secret", asanaData.ClientSecret);
					param.Add("redirect_uri", token.RedirectUri);
					param.Add("refresh_token", token.RefreshToken);

					client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
					byte[] responsebytes = client.UploadValues("https://app.asana.com/-/oauth_token", "POST", param);
					responseBody = Encoding.UTF8.GetString(responsebytes);
				}
				var response = JsonConvert.DeserializeObject<TokenExchangeEndpointResponse>(responseBody);

				token.Expires = CalculateExpiration(response.expires_in);
				token.AccessToken = response.access_token;
				token.RefreshToken = response.refresh_token;

				s.Update(token);

			}
			return token;
		}
	}
}