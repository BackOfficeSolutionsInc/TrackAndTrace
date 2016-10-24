using log4net;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Payments;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Helpers;
using static RadialReview.Accessors.PaymentAccessor;

namespace RadialReview.Utilities {
	public class PaymentSpringUtil {

		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public class PaymentResult {
			// private string _cardNumber;
			public string id { get; set; }
			public string @class { get; set; }
			public DateTime created_at { get; set; }
			public string status { get; set; }
			public string reference_number { get; set; }
			public decimal amount_refunded { get; set; }
			public decimal amount_settled { get; set; }
			public string card_owner_name { get; set; }
			public string email { get; set; }
			public string description { get; set; }
			public string customer_id { get; set; }
			public string merchant_id { get; set; }

			public string card_number { get; set; }
		}

		public static async Task<PaymentResult> ChargeToken(OrganizationModel org, PaymentSpringsToken token, decimal amount, bool forceTest = false) {
			//CURL 
			var client = new HttpClient();
			// Create the HttpContent for the form to be posted.
			var requestContent = new FormUrlEncodedContent(new[] {
				new KeyValuePair<string, string>("customer_id", token.CustomerToken),
				new KeyValuePair<string, string>("amount", ""+((int)(amount*100))),
			});

			var privateApi = Config.PaymentSpring_PrivateKey(forceTest);
			var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
			var response = await client.PostAsync("https://api.paymentspring.com/api/v1/charge", requestContent);
			var responseContent = response.Content;
			using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
				var result = await reader.ReadToEndAsync();
				log.Info("Charged Card: " + result);
				if (Json.Decode(result).errors != null) {
					var builder = new List<string>();
					for (var i = 0; i < Json.Decode(result).errors.Length; i++) {
						builder.Add(Json.Decode(result).errors[i].message + " (" + Json.Decode(result).errors[i].code + ").");
					}
					throw new PaymentException(org, amount, PaymentExceptionType.ResponseError, String.Join(" ", builder));
				}
				if (Json.Decode(result).@class != "transaction")
					throw new PermissionsException("Response must be of type 'transaction'.");
				return new PaymentResult {
					id = Json.Decode(result).id,
					@class = Json.Decode(result).@class,
					created_at = DateTime.Parse(Json.Decode(result).created_at),
					status = Json.Decode(result).status,
					reference_number = Json.Decode(result).reference_number,
					amount_settled = Json.Decode(result).amount_settled,
					amount_refunded = Json.Decode(result).amount_refunded,
					card_owner_name = Json.Decode(result).card_owner_name,
					email = Json.Decode(result).email,
					description = Json.Decode(result).description,
					customer_id = Json.Decode(result).customer_id,
					merchant_id = Json.Decode(result).merchant_id,
					card_number = Json.Decode(result).card_number
				};
			}
		}

		public class LogResult {
			public string id { get; set; }
			public string @class { get; set; }
			public DateTime date { get; set; }
			public string subject_class { get; set; }
			public string subject_id { get; set; }
			public string action { get; set; }
			public string method { get; set; }
			public dynamic request { get; set; }
			public dynamic response { get; set; }


			public static Func<dynamic, LogResult> GetProcessor() {
				return (dynamic x) => new LogResult {
					id = x.id,
					@class = x.@class,
					date = DateTime.Parse(x.date),
					subject_class = x.subject_class,
					subject_id = x.subject_id,
					action = x.action,
					method = x.method,
					request = x.request,
					response = x.response,
				};
			}
		}

		public static PaymentSpringsToken GetToken(ISession s, long organizationId) {
			var tokens = s.QueryOver<PaymentSpringsToken>()
					.Where(x => x.OrganizationId == organizationId && x.Active && x.DeleteTime == null)
					.List().ToList();

			return tokens.OrderByDescending(x => x.CreateTime).FirstOrDefault();
		}
		public static PaymentSpringsToken GetToken(long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetToken(s, organizationId);
				}
			}
		}

		public static async Task<List<LogResult>> GetAllLogs(bool forceTest = false, int maxPage = int.MaxValue,int resultsPerPage = 100) {
			var request = new Curl<LogResult>("https://api.paymentspring.com/api/v1/transactions/log", HttpMethod.Get, LogResult.GetProcessor());
			return await RequestPagesOfData(request, maxPage, resultsPerPage);
		}


		public static async Task<List<LogResult>> GetCustomerLogs(PaymentSpringsToken token, bool forceTest = false, int maxPage = int.MaxValue, int resultsPerPage = 100) {
			var request = new Curl<LogResult>("https://api.paymentspring.com/api/v1/customers/" + token.CustomerToken + "/log", HttpMethod.Get, LogResult.GetProcessor());
			return await RequestPagesOfData(request, maxPage, resultsPerPage);
		}

		public class PaymentSpringError {
			public string Message { get; set; }
			public long Code { get; set; }
		}

		public class Curl<RESULT> {
			public string Url { get; set; }
			public HttpMethod Method { get; set; }
			public List<KeyValuePair<string, string>> Arguments { get; set; }
			public bool ForceTest { get; set; }
			public Func<dynamic, RESULT> ProcessResult { get; set; }
			public Action<List<PaymentSpringError>> ProcessErrors { get; set; }

			public Curl(string url, HttpMethod method, bool forceTest, Func<dynamic, RESULT> processResult) {
				ForceTest = forceTest;
				Url = url;
				Method = method;
				ProcessResult = processResult;
				Arguments = new List<KeyValuePair<string, string>>();
			}

			public Curl(string url, HttpMethod method, Func<dynamic, RESULT> processResult) : this(url, method, false, processResult) {
			}

			public void Add(string key, string val) {
				Arguments.Add(new KeyValuePair<string, string>(key, val));
			}

		}

		protected static async Task<RESULT> MakeRequest<RESULT>(Curl<RESULT> request) {

			var client = new HttpClient();

			// Create the HttpContent for the form to be posted.
			var requestContent = new FormUrlEncodedContent(request.Arguments);
			var privateApi = Config.PaymentSpring_PrivateKey(request.ForceTest);
			var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
			HttpResponseMessage response;

			if (request.Method == HttpMethod.Post) {
				response = await client.PostAsync(request.Url, requestContent);
			} else if (request.Method == HttpMethod.Get) {
				if (request.Arguments != null && request.Arguments.Any()) {
					if (!request.Url.Contains("?"))
						request.Url += "?";
					else
						request.Url += "&";
					request.Url += string.Join("&", request.Arguments.Select(x => x.Key.UrlEncode() + "=" + x.Value.UrlEncode()));
				}
				response = await client.GetAsync(request.Url);
			} else {
				throw new Exception("Unrecognized request method:" + request.Method);
			}
			var responseContent = response.Content;
			using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
				var result = await reader.ReadToEndAsync();
				if (Json.Decode(result).errors != null) {
					var builder = new List<string>();
					var errors = new List<PaymentSpringError>();

					for (var i = 0; i < Json.Decode(result).errors.Length; i++) {
						errors.Add(new PaymentSpringError {
							Message = (string)Json.Decode(result).errors[i].message,
							Code = (long)Json.Decode(result).errors[i].code
						});
					}
					if (request.ProcessErrors != null)
						request.ProcessErrors(errors);
					else {
						throw new Exception("Unhandled PaymentSpring Exception");
					}
				}
				dynamic res = Json.Decode(result);
				return request.ProcessResult(res);
			}
		}


		protected static async Task<List<SINGLE_RESULT>> RequestPagesOfData<SINGLE_RESULT>(Curl<SINGLE_RESULT> request, int maxPage, int resultsPerPage) {

			var processSingle = request.ProcessResult;

			var finalResults = new List<SINGLE_RESULT>();

			resultsPerPage = Math.Min(100, Math.Max(1, resultsPerPage));

			var total_pages = 1;

			for (var i = 0; i < total_pages && i < maxPage; i++) {

				var args = new List<KeyValuePair<string, string>>();
				args.Add(new KeyValuePair<string, string>("limit",  "" + resultsPerPage));
				args.Add(new KeyValuePair<string, string>("offset", "" + i));
				args.AddRange(request.Arguments);

				Func<dynamic, List<SINGLE_RESULT>> processList = (dynamic x) => {
					var results = new List<SINGLE_RESULT>();
					for (var j = 0; j < x.list.Length; j++) {
						results.Add(processSingle(x.list[j]));
					}
					total_pages = (int)Math.Ceiling((double)(x.meta.total_results / (double)resultsPerPage));
					return results;
				};

				var pageRequest = new Curl<List<SINGLE_RESULT>>(request.Url, request.Method, processList) {
					Arguments = args,
					ForceTest = request.ForceTest,
					ProcessErrors = request.ProcessErrors,
				};

				finalResults.AddRange(await MakeRequest(pageRequest));
			}

			return finalResults;
		}
	}
}