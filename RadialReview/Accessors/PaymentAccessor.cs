using System.Drawing.Imaging;
using System.Security.Cryptography.X509Certificates;
using Amazon.SimpleDB.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate;
using NHibernate.Envers;
using NHibernate.Linq;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Payments;
using RadialReview.Models.Tasks;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using RadialReview.Utilities.Extensions;
using RadialReview.Utilities.NHibernate;
using TrelloNet;
using System.Net;
using System.Collections.Specialized;
using NHibernate.Criterion;
using static RadialReview.Utilities.PaymentSpringUtil;
using RadialReview.Models.Components;

namespace RadialReview.Accessors {
	public class PaymentAccessor : BaseAccessor {
		public PaymentPlanModel BasicPaymentPlan() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PaymentPlanModel basicPlan = null;
					try {
						basicPlan = s.QueryOver<PaymentPlanModel>().Where(x => x.IsDefault).SingleOrDefault();
					} catch (Exception e) {
						log.Error(e);
					}
					if (basicPlan == null) {
						basicPlan = new PaymentPlanModel() {
							Description = "Employee count model",
							IsDefault = true,
							PlanCreated = DateTime.UtcNow
						};
						s.Save(basicPlan);
						tx.Commit();
						s.Flush();
					}
					return basicPlan;
				}
			}
		}


		public static async Task<InvoiceModel> SendInvoice(string email, long organizationId, long taskId, DateTime executeTime, bool forceUseTest = false, DateTime? calculateTime = null) {
			//PaymentResult result = null;
			InvoiceModel invoice = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(organizationId);

					if (org == null)
						throw new NullReferenceException("Organization does not exist");

					if (org.DeleteTime != null)
						throw new FallthroughException("Organization was deleted.");

					var plan = org.PaymentPlan;

					if (plan.Task == null)
						throw new PermissionsException("Task was null.");
					if (plan.Task.OriginalTaskId == 0)
						throw new PermissionsException("PaymentPlan OriginalTaskId was 0.");

					var task = s.Get<ScheduledTask>(taskId);

					if (task.OriginalTaskId == 0)
						throw new PermissionsException("ScheduledTask OriginalTaskId was 0.");
					if (plan.Task.OriginalTaskId != task.OriginalTaskId)
						throw new PermissionsException("ScheduledTask and PaymentPlan do not have the same task.");

					if (task.Executed != null)
						throw new PermissionsException("Task was already executed.");

					//executeTime = executeTime;
					calculateTime = calculateTime ?? DateTime.UtcNow;
					try {
						var itemized = CalculateCharge(s, org, plan, calculateTime.Value);
						invoice = CreateInvoice(s, org, plan, executeTime, itemized);
					} finally {

						tx.Commit();
						s.Flush();
					}
				}
			}


#pragma warning disable CS0618 // Type or member is obsolete
			await EmailInvoice(email, invoice, executeTime);
#pragma warning restore CS0618 // Type or member is obsolete

			return invoice;
		}

		[Obsolete("Unsafe")]
		public static async Task<bool> EmailInvoice(string emailAddress, InvoiceModel invoice, DateTime chargeTime) {
			var ProductName = Config.ProductName(invoice.Organization);
			var SupportEmail = ProductStrings.SupportEmail;
			var OrgName = invoice.Organization.GetName();
			var Charged = invoice.AmountDue;
			//var CardLast4 = result.card_number ?? "NA";
			//var TransactionId = result.id ?? "NA";
			var ChargeTime = chargeTime;
			var ServiceThroughDate = invoice.ServiceEnd.ToString("yyyy-MM-dd");
			var Address = ProductStrings.Address;

			var localChargeTime = invoice.Organization.ConvertFromUTC(ChargeTime);
			var lctStr = localChargeTime.ToString("dd MMM yyyy hh:mmtt") + " " + invoice.Organization.GetTimeZoneId(localChargeTime);

			var email = Mail.Bcc(EmailTypes.Receipt, ProductStrings.PaymentReceiptEmail);
			if (emailAddress != null) {
				email = email.AddBcc(emailAddress);
			}
			var toSend = email.SubjectPlainText("[" + ProductName + "] Invoice for " + invoice.Organization.GetName())
				//[ProductName, SupportEmail, OrgName, Charged, CardLast4, TransactionId, ChargeTime, ServiceThroughDate, Address]
				.Body(EmailStrings.PaymentReceipt_Body, ProductName, SupportEmail, OrgName, String.Format("{0:C}", Charged), "", "", lctStr, ServiceThroughDate, Address);
			await Emailer.SendEmail(toSend);
			return true;
		}
		public static async Task<PaymentResult> ChargeOrganization(long organizationId, long taskId, bool forceUseTest = false, bool sendReceipt = true, DateTime? executeTime = null) {
			PaymentResult result = null;
			InvoiceModel invoice = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(organizationId);

					if (org == null)
						throw new NullReferenceException("Organization does not exist");

					if (org.DeleteTime != null)
						throw new FallthroughException("Organization was deleted.");

					var plan = org.PaymentPlan;

					if (plan.Task == null)
						throw new PermissionsException("Task was null.");
					if (plan.Task.OriginalTaskId == 0)
						throw new PermissionsException("PaymentPlan OriginalTaskId was 0.");

					var task = s.Get<ScheduledTask>(taskId);

					if (task.Executed != null)
						throw new PermissionsException("Task was already executed.");
					if (task.DeleteTime != null)
						throw new PermissionsException("Task was deleted.");

					if (task.OriginalTaskId == 0)
						throw new PermissionsException("ScheduledTask OriginalTaskId was 0.");
					if (plan.Task.OriginalTaskId != task.OriginalTaskId)
						throw new PermissionsException("ScheduledTask and PaymentPlan do not have the same task.");
					if (task.Started == null)
						throw new PermissionsException("Task was not started.");


					executeTime = executeTime ?? DateTime.UtcNow.Date;
					try {
						var itemized = CalculateCharge(s, org, plan, executeTime.Value);
						invoice = CreateInvoice(s, org, plan, executeTime.Value, itemized);
#pragma warning disable CS0618 // Type or member is obsolete
						result = await ExecuteInvoice(s, invoice, forceUseTest);
#pragma warning restore CS0618 // Type or member is obsolete
					} finally {

						tx.Commit();
						s.Flush();
					}
				}
			}
			if (sendReceipt) {
#pragma warning disable CS0618 // Type or member is obsolete
				await SendReceipt(result, invoice);
#pragma warning restore CS0618 // Type or member is obsolete
			}

			return result;
		}


		public static InvoiceModel CreateInvoice(ISession s, OrganizationModel org, PaymentPlanModel paymentPlan, DateTime executeTime, IEnumerable<Itemized> items) {
			var invoice = new InvoiceModel() {
				Organization = org,
				InvoiceDueDate = executeTime.Add(TimespanExtensions.OneMonth()).Date
			};

			if (NHibernateUtil.GetClass(paymentPlan) == typeof(PaymentPlan_Monthly)) {
				invoice.ServiceStart = executeTime;
				invoice.ServiceEnd = executeTime.Add(TimespanExtensions.OneMonth()).Date;
			} else {
				throw new PermissionsException("Unhandled Payment Plan");
			}

			s.Save(invoice);

			var invoiceItems = items.Select(x => new InvoiceItemModel() {
				AmountDue = x.Total(),
				Currency = Currency.USD,
				PricePerItem = x.Price,
				Quantity = x.Quantity,
				Name = x.Name,
				Description = x.Description,
				ForInvoice = invoice,
			}).ToList();

			foreach (var i in invoiceItems)
				s.Save(i);

			invoice.InvoiceItems = invoiceItems;
			invoice.AmountDue = invoice.InvoiceItems.Sum(x => x.AmountDue);
			s.Update(invoice);
			return invoice;
		}

		[Obsolete("Unsafe")]
		public static async Task<PaymentResult> ExecuteInvoice(ISession s, InvoiceModel invoice, bool useTest = false) {
			//invoice = s.Get<InvoiceModel>(invoice.Id);
			if (invoice.PaidTime != null)
				throw new PermissionsException("Invoice was already paid");

			var result = await ChargeOrganizationAmount(s, invoice.Organization.Id, invoice.AmountDue, useTest);

			invoice.TransactionId = result.id;
			invoice.PaidTime = DateTime.UtcNow;
			s.Update(invoice);

			return result;
		}

		[Obsolete("Unsafe")]
		public static async Task<bool> SendReceipt(PaymentResult result, InvoiceModel invoice) {
			if (invoice.PaidTime != null) {
				var ProductName = Config.ProductName(invoice.Organization);
				var SupportEmail = ProductStrings.SupportEmail;
				var OrgName = invoice.Organization.GetName();
				var Charged = invoice.AmountDue;
				var CardLast4 = result.card_number ?? "NA";
				var TransactionId = result.id ?? "NA";
				var ChargeTime = invoice.PaidTime;
				var ServiceThroughDate = invoice.ServiceEnd.ToString("yyyy-MM-dd");
				var Address = ProductStrings.Address;

				var localChargeTime = invoice.Organization.ConvertFromUTC(ChargeTime.Value);
				var lctStr = localChargeTime.ToString("dd MMM yyyy hh:mmtt") + " " + invoice.Organization.GetTimeZoneId(localChargeTime);

				var email = Mail.Bcc(EmailTypes.Receipt, ProductStrings.PaymentReceiptEmail);
				if (result.email != null) {
					email = email.AddBcc(result.email);
				}
				var toSend = email.SubjectPlainText("[" + ProductName + "] Payment Receipt for " + invoice.Organization.GetName())
					.Body(EmailStrings.PaymentReceipt_Body, ProductName, SupportEmail, OrgName, String.Format("{0:C}", Charged), CardLast4, TransactionId, lctStr, ServiceThroughDate, Address);
				await Emailer.SendEmail(toSend);
				return true;
			}
			return false;
		}


		public static List<Itemized> CalculateCharge(ISession s, OrganizationModel org, PaymentPlanModel paymentPlan, DateTime executeTime) {
			var itemized = new List<Itemized>();

			if (NHibernateUtil.GetClass(paymentPlan) == typeof(PaymentPlan_Monthly)) {
				var plan = (PaymentPlan_Monthly)s.GetSessionImplementation().PersistenceContext.Unproxy(paymentPlan);
				var rangeStart = executeTime.Subtract(TimespanExtensions.OneMonth());
				var rangeEnd = executeTime;

				UserModel u = null;
				UserOrganizationModel uo = null;
				var allPeopleList = s.QueryOver<UserOrganizationModel>(() => uo)
					.Left.JoinAlias(() => uo.User, () => u)
					//.Where(Restrictions.Or(
					//    Restrictions.On(()=>uo.User).IsNull,
					//    Restrictions.Eq(Projections.Property(()=>uo.User.IsRadialAdmin),false)
					//))
					.Where(() => uo.Organization.Id == org.Id && uo.CreateTime < rangeEnd && (uo.DeleteTime == null || uo.DeleteTime > rangeStart) && !uo.IsRadialAdmin)
					.Select(x => x.Id, x => u.IsRadialAdmin, x => x.IsClient, x => x.User.Id, x => x.EvalOnly)
					.List<object[]>()
					.Select(x => new {
						UserOrgId = (long)x[0],
						IsRadialAdmin = (bool?)x[1],
						IsClient = (bool)x[2],
						UserId = (string)x[3],
						IsRegistered = x[3] != null,
						EvalOnly = (bool?)x[4] ?? false
					})
					.Where(x => x.IsRadialAdmin == null || (bool)x.IsRadialAdmin == false)
					.ToList();

				if (plan.NoChargeForClients) {
					allPeopleList = allPeopleList.Where(x => x.IsClient == false).ToList();
				}
				if (plan.NoChargeForUnregisteredUsers) {
					allPeopleList = allPeopleList.Where(x => x.IsRegistered).ToList();
				}

				var l10Users = allPeopleList.Where(x => !x.EvalOnly);
				var l10UserCount = l10Users.Count();

				var people = Math.Max(0, l10UserCount - plan.FirstN_Users_Free);
				var allRevisions = s.AuditReader().GetRevisionsBetween<OrganizationModel>(s, org.Id, rangeStart, rangeEnd).ToList();

				//s.Auditer().GetRevisionNumberForDate(<OrganizationModel>(org.Id,);

				var reviewEnabled = /*org.Settings.EnableReview;//*/allRevisions.Any(x => x.Object.Settings.EnableReview);
				var l10Enabled = /*org.Settings.EnableL10; //*/allRevisions.Any(x => x.Object.Settings.EnableL10);


				//In case clocks are off.
				var executionCalculationDate = executeTime.AddDays(1).Date;


				if (plan.BaselinePrice > 0) {
					var reviewItem = new Itemized() {
						Name = "Traction® Tools",
						Price = plan.BaselinePrice,
						Quantity = 1,
					};
					itemized.Add(reviewItem);
				}

				if (reviewEnabled) {
					var reviewItem = new Itemized() {
						Name = "Quarterly Conversation",
						Price = plan.ReviewPricePerPerson,
						Quantity = allPeopleList.Where(x => !x.IsClient).Count()
					};
					if (reviewItem.Quantity != 0) {
						itemized.Add(reviewItem);
						if (!(plan.ReviewFreeUntil == null || !(plan.ReviewFreeUntil.Value.Date > executionCalculationDate))) {
							//Discount it since it is free
							itemized.Add(reviewItem.Discount());
						}
					}
				}
				if (l10Enabled) {
					var l10Item = new Itemized() {
						Name = "L10 Meeting Software",
						Price = plan.L10PricePerPerson,
						Quantity = people,
					};
					if (l10Item.Quantity != 0) {
						itemized.Add(l10Item);

						if (!(plan.L10FreeUntil == null || !(plan.L10FreeUntil.Value.Date > executionCalculationDate))) {
							//Discount it since it is free
							itemized.Add(l10Item.Discount());
						}
					}
				}
				if ((plan.FreeUntil.Date > executionCalculationDate)) {
					//Discount it since it is free
					var total = itemized.Sum(x => x.Total());
					itemized.Add(new Itemized() {
						Name = "Discount",
						Price = -1 * total,
						Quantity = 1,
					});
				}
			} else {
				throw new PermissionsException("Unhandled Payment Plan");
			}
			return itemized;
		}


		[Obsolete("Unsafe")]
		public static async Task<PaymentResult> ChargeOrganizationAmount(ISession s, long organizationId, decimal amount, bool forceTest = false) {
			if (amount == 0) {
				EventUtil.Trigger(x => x.Create(s, EventType.PaymentFree, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "No Charge", arg1: 0m));
				return new PaymentResult() {
					amount_settled = 0,
				};
			}
			//var tokens = s.QueryOver<PaymentSpringsToken>()
			//				.Where(x => x.OrganizationId == organizationId && x.Active && x.DeleteTime == null)
			//				.List().ToList();

			var token = PaymentSpringUtil.GetToken(s, organizationId);

			var org2 = s.Get<OrganizationModel>(organizationId);
			if (org2 != null && org2.AccountType == AccountType.Implementer) {
				EventUtil.Trigger(x => x.Create(s, EventType.PaymentFree, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Implementer", arg1: 0));
				throw new FallthroughException("Failed to charge implementer account (" + org2.Id + ") " + org2.GetName());
			}
			if (org2 != null && org2.AccountType == AccountType.Dormant) {
				EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Dormant", arg1: 0));
				throw new FallthroughException("Failed to charge dormant account (" + org2.Id + ") " + org2.GetName());
			}

			if (token == null) {
				EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "MissingToken", arg1: amount));
				throw new PaymentException(s.Get<OrganizationModel>(organizationId), amount, PaymentExceptionType.MissingToken);
			}
			PaymentResult pr = null;
			try {
				pr = await PaymentSpringUtil.ChargeToken(org2, token, amount, forceTest);
				EventUtil.Trigger(x => x.Create(s, EventType.PaymentReceived, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Charged", arg1: amount));
			} catch (PaymentException e) {
				EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "" + e.Type, arg1: amount));
				throw e;
			} catch (Exception e) {
				EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Unhandled:" + e.Message, arg1: amount));
				throw e;
			}

			var org = s.Get<OrganizationModel>(organizationId);
			if (org.AccountType == AccountType.Demo)
				org.AccountType = AccountType.Paying;

			if (org.PaymentPlan != null)
				org.PaymentPlan.LastExecuted = DateTime.UtcNow;

			return pr;
		}


		[Obsolete("Unsafe")]
		public static async Task<PaymentResult> ChargeOrganizationAmount(long organizationId, decimal amount, bool useTest = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var charged = await ChargeOrganizationAmount(s, organizationId, amount, useTest);
					tx.Commit();
					s.Flush();
					return charged;
				}
			}
		}

		public static List<PaymentMethodVM> GetCards(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (organizationId != caller.Organization.Id)
						throw new PermissionsException("Organization Ids do not match");


					PermissionsUtility.Create(s, caller).EditCompanyPayment(organizationId);
					var cards = s.QueryOver<PaymentSpringsToken>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();



					return cards.Select(x => new PaymentMethodVM(x)).ToList();
				}
			}
		}


		public enum TestCardType : long {
			Visa = 4111111111111111L,
			Amex = 345829002709133L,
			Discover = 6011010948700474L,
			Mastercard = 5499740000000057L,

		}

		public static async Task<PaymentTokenVM> GenerateFakeCard(string owner = "John Doe", TestCardType cardType = TestCardType.Visa) {

			var url = "https://api.paymentspring.com/api/v1/tokens";
			//var  reqparm = new NameValueCollection();
			//reqparm.Add("card_number", @"""4111111111111111""");
			//reqparm.Add("card_exp_month", @"""1""");
			//reqparm.Add("card_exp_year", @"""2020""");
			//reqparm.Add("csc", @"""1234""");
			//reqparm.Add("card_owner_name", "\""+owner+"\"");
			//var cc = new CredentialCache();
			//cc.Add(new Uri(url), "NTLM", new NetworkCredential(Config.PaymentSpring_PublicKey(true),""));
			//client.Credentials = cc;
			//byte[] responsebytes = await client.UploadValuesTaskAsync(url, "POST", reqparm);
			//string responsebody = Encoding.UTF8.GetString(responsebytes);
			//PaymentTokenVM r = JsonConvert.DeserializeObject<PaymentTokenVM>(responsebody);

			//if (r.@class != "token")
			//    throw new PermissionsException("Id must be a token");
			//if (String.IsNullOrWhiteSpace(r.id))
			//    throw new PermissionsException("Token was empty");
			//if (r.card_owner_name!=owner)
			//    throw new PermissionsException("Owner incorrect");

			var csc = "999";
			if (cardType == TestCardType.Amex)
				csc = csc + "7";

			var client = new HttpClient();

			var keys = new List<KeyValuePair<string, string>>();
			keys.Add(new KeyValuePair<string, string>("card_number", "" + (long)cardType));
			keys.Add(new KeyValuePair<string, string>("card_exp_month", "08"));
			keys.Add(new KeyValuePair<string, string>("card_exp_year", "2018"));
			keys.Add(new KeyValuePair<string, string>("csc", csc));

			keys.Add(new KeyValuePair<string, string>("card_owner_name", owner));


			// Create the HttpContent for the form to be posted.
			var requestContent = new FormUrlEncodedContent(keys.ToArray());

			var publicApi = Config.PaymentSpring_PublicKey(true);
			var byteArray = new UTF8Encoding().GetBytes(publicApi + ":");
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
			HttpResponseMessage response = await client.PostAsync(url, requestContent);
			HttpContent responseContent = response.Content;
			using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
				var result = await reader.ReadToEndAsync();
				if (Json.Decode(result).errors != null) {
					var builder = new List<string>();
					for (var i = 0; i < Json.Decode(result).errors.Length; i++) {
						builder.Add(Json.Decode(result).errors[i].message + " (" + Json.Decode(result).errors[i].code + ").");
					}
					throw new PermissionsException(String.Join(" ", builder));
				}
				var r = JsonConvert.DeserializeObject<PaymentTokenVM>(result);
				if (r.@class != "token")
					throw new PermissionsException("Id must be a token");
				if (String.IsNullOrWhiteSpace(r.id))
					throw new PermissionsException("Token was empty");
				if (r.card_owner_name != owner)
					throw new PermissionsException("Owner incorrect");
				return r;
			}
		}

		public static async Task<PaymentMethodVM> SetCard(UserOrganizationModel caller, long orgId, PaymentTokenVM token) {
			return await SetCard(caller, orgId, token.id, token.@class, token.card_type, token.card_owner_name, token.last_4, token.card_exp_month, token.card_exp_year, null, null, null, null, null, null, null, null, null, true);
		}

		public static async Task<PaymentMethodVM> SetACH(UserOrganizationModel caller, long organizationId, string tokenId, string @class,
			string token_type, string account_type, string firstName, string lastName, string accountLast4, string routingNumber, String address_1, String address_2,
			String city, String state, string zip, string phone, string website, string country, string email, bool active) {
			if (token_type != "bank_account")
				throw new PermissionsException("ACH requires token_type = 'bank_account'");

			return await SetToken(caller, organizationId, tokenId, @class, null, null, null, 0, 0, address_1, address_2, city, state, zip, phone, website, country, email, active, accountLast4, routingNumber, firstName, lastName, account_type, PaymentSpringTokenType.BankAccount);
		}

		public static async Task<PaymentMethodVM> SetCard(UserOrganizationModel caller, long organizationId, string tokenId, string @class,
			string cardType, string cardOwnerName, string last4, int expireMonth, int expireYear, String address_1, String address_2,
			String city, String state, string zip, string phone, string website, string country, string email, bool active) {
			
			return await SetToken(caller, organizationId, tokenId, @class, cardType, cardOwnerName, last4, expireMonth, expireYear, address_1, address_2, city, state, zip, phone, website, country, email, active, null, null, null, null, null, PaymentSpringTokenType.CreditCard);

		}


		private static async Task<PaymentMethodVM> SetToken(UserOrganizationModel caller, long organizationId, string tokenId, string @class,
			string cardType, string cardOwnerName, string cardLast4, int cardExpireMonth, int cardExpireYear, String address_1, String address_2,
			String city, String state, string zip, string phone, string website, string country, string email, bool active,
			string bankLast4, string bankRouting, string bankFirstName, string bankLastName, string bankAccountType, PaymentSpringTokenType tokenType) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (@class != "token")
						throw new PermissionsException("Id must be a token");
					if (String.IsNullOrWhiteSpace(tokenId))
						throw new PermissionsException("Token was empty");
					if (organizationId != caller.Organization.Id)
						throw new PermissionsException("Organization Ids do not match");


					PermissionsUtility.Create(s, caller).EditCompanyPayment(organizationId);
					if (active) {
						var previous = s.QueryOver<PaymentSpringsToken>().Where(x => x.OrganizationId == organizationId && x.Active == true && x.DeleteTime == null).List().ToList();
						foreach (var p in previous) {
							p.Active = false;
							s.Update(p);
						}
					}

					//CURL
					var client = new HttpClient();

					var keys = new List<KeyValuePair<string, string>>();
					keys.Add(new KeyValuePair<string, string>("token", tokenId));
					keys.Add(new KeyValuePair<string, string>("first_name", caller.GetFirstName()));
					keys.Add(new KeyValuePair<string, string>("last_name", caller.GetLastName()));
					keys.Add(new KeyValuePair<string, string>("company", caller.Organization.GetName()));
					if (address_1 != null)
						keys.Add(new KeyValuePair<string, string>("address_1", address_1));
					if (address_2 != null)
						keys.Add(new KeyValuePair<string, string>("address_2", address_2));
					if (city != null)
						keys.Add(new KeyValuePair<string, string>("city", city));
					if (state != null)
						keys.Add(new KeyValuePair<string, string>("state", state));
					if (zip != null)
						keys.Add(new KeyValuePair<string, string>("zip", zip));
					if (phone != null)
						keys.Add(new KeyValuePair<string, string>("phone", phone));
					//if (fax != null)
					//    keys.Add(new KeyValuePair<string, string>("fax", fax));
					if (website != null)
						keys.Add(new KeyValuePair<string, string>("website", website));
					if (country != null)
						keys.Add(new KeyValuePair<string, string>("country", country));
					if (email != null)
						keys.Add(new KeyValuePair<string, string>("email", email));


					// Create the HttpContent for the form to be posted.
					var requestContent = new FormUrlEncodedContent(keys.ToArray());


					//Do not supress
					var privateApi = Config.PaymentSpring_PrivateKey();


					var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
					client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
					HttpResponseMessage response = await client.PostAsync("https://api.paymentspring.com/api/v1/customers", requestContent);
					HttpContent responseContent = response.Content;
					using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
						var result = await reader.ReadToEndAsync();
						if (Json.Decode(result).errors != null) {
							var builder = new List<string>();
							for (var i = 0; i < Json.Decode(result).errors.Length; i++) {
								builder.Add(Json.Decode(result).errors[i].message + " (" + Json.Decode(result).errors[i].code + ").");
							}
							throw new PermissionsException(String.Join(" ", builder));
						}
						if (Json.Decode(result).@class != "customer")
							throw new PermissionsException("Expected class: 'Customer'");


						var token = new PaymentSpringsToken() {
							CustomerToken = Json.Decode(result).id,
							CardLast4 = cardLast4,
							CardOwner = cardOwnerName,
							CardType = cardType,
							MonthExpire = cardExpireMonth,
							YearExpire = cardExpireYear,
							OrganizationId = organizationId,
							Active = active,
							ReceiptEmail = email,
							CreatedBy = caller.Id,

							TokenType = tokenType,
							BankAccountLast4 = bankLast4,
							BankRouting = bankRouting,
							BankFirstName = bankFirstName,
							BankLastName = bankLastName,
							BankAccountType = bankAccountType

						};
						s.Save(token);
						tx.Commit();
						s.Flush();
																	
						return new PaymentMethodVM(token);
					}

				}
			}
		}

		public static PaymentPlanModel AttachPlan(ISession s, OrganizationModel organization, PaymentPlanModel plan) {
			var task = new ScheduledTask() {
				MaxException = 1,
				Url = "/Scheduler/ChargeAccount/" + organization.Id,
				NextSchedule = plan.SchedulerPeriod(),
				Fire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date.AddHours(3)),
				FirstFire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date.AddHours(3)),
				TaskName = plan.TaskName(),
				EmailOnException = true,
			};
			s.Save(task);
			task.OriginalTaskId = task.Id;
			s.Update(task);
			if (plan is PaymentPlan_Monthly) {
				var ppm = (PaymentPlan_Monthly)plan;
				ppm.OrgId = organization.Id;
			}

			plan.Task = task;
			s.Save(plan);
			return plan;
		}

		public static PaymentPlanModel GetPlan(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);
					var org = s.Get<OrganizationModel>(organizationId);

					var plan = s.Get<PaymentPlanModel>(org.PaymentPlan.Id);

					if (plan != null && plan.Task != null) {
						plan._CurrentTask = s.QueryOver<ScheduledTask>()
							.Where(x => x.OriginalTaskId == plan.Task.OriginalTaskId && x.Executed == null)
							.List().FirstOrDefault();
					}


					return (PaymentPlanModel)s.GetSessionImplementation().PersistenceContext.Unproxy(plan);

				}
			}
		}

		[Obsolete("Unsafe", false)]
		public static List<long> GetPayingOrganizations(ISession s) {
			var scheduledToPay = s.QueryOver<ScheduledTask>().Where(x => x.TaskName == ScheduledTask.MonthlyPaymentPlan && x.DeleteTime == null && x.Executed == null)
				.List().ToList().Select(x => x.Url.Split('/').Last().ToLong());
			var hasTokens = s.QueryOver<PaymentSpringsToken>().Where(x => x.Active && x.DeleteTime == null).List().ToList();

			var hasTokens_scheduledToPay = hasTokens.Select(x => x.OrganizationId).Intersect(scheduledToPay);
			return hasTokens_scheduledToPay.ToList();
		}


		[Obsolete("Unsafe", false)]
		public static decimal CalculateTotalCharge(ISession s, List<long> orgIds) {
			var orgs = s.QueryOver<OrganizationModel>().WhereRestrictionOn(x => x.Organization.Id).IsIn(orgIds.Distinct().ToList()).List().ToList();

			return orgs.Sum(o =>
					CalculateCharge(s, o, o.PaymentPlan, DateTime.UtcNow)
						.Sum(x => x.Total())
				);

		}




		public static PaymentPlanType GetPlanType(string planType) {
			switch (planType.Replace("-", "").ToLower()) {
				case "professional":
					return PaymentPlanType.Professional_Monthly_March2016;
				case "enterprise":
					return PaymentPlanType.Enterprise_Monthly_March2016;
				case "selfimplementer":
					return PaymentPlanType.SelfImplementer_Monthly_March2016;
				default:
					throw new ArgumentOutOfRangeException("Cannot create Payment Plan (" + planType + ")");
			}
		}


		//public async static Task<string> GenerateToken_Test(string name, TestCardType card) {
		//	var client = new HttpClient();

		//	var csc = "999";
		//	if (card == TestCardType.Amex)
		//		csc = csc + "7";

		//	var keys = new List<KeyValuePair<string, string>>();
		//	keys.Add(new KeyValuePair<string, string>("card_number", "" + (long)card));
		//	keys.Add(new KeyValuePair<string, string>("card_exp_month", "08"));
		//	keys.Add(new KeyValuePair<string, string>("card_exp_year", "18"));
		//	keys.Add(new KeyValuePair<string, string>("csc", csc));
		//	keys.Add(new KeyValuePair<string, string>("card_owner_name", name));

		//	// Create the HttpContent for the form to be posted.
		//	var requestContent = new FormUrlEncodedContent(keys.ToArray());

		//	//Do not supress
		//	var privateApi = Config.PaymentSpring_PrivateKey(true);


		//	var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
		//	client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
		//	HttpResponseMessage response = await client.PostAsync("https://api.paymentspring.com/api/v1/tokens", requestContent);
		//	HttpContent responseContent = response.Content;
		//	using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
		//		var result = await reader.ReadToEndAsync();
		//		if (Json.Decode(result).errors != null) {
		//			var builder = new List<string>();
		//			for (var i = 0; i < Json.Decode(result).errors.Length; i++) {
		//				builder.Add(Json.Decode(result).errors[i].message + " (" + Json.Decode(result).errors[i].code + ").");
		//			}
		//			throw new PermissionsException(String.Join(" ", builder));
		//		}
		//		if (Json.Decode(result).@class != "token")
		//			throw new PermissionsException("Expected class: 'token'");

		//		return Json.Decode(result).id;
		//	}
		//}


		[Obsolete("Dont forget to attach to send this through AttachPlan")]
		public static PaymentPlan_Monthly GeneratePlan(PaymentPlanType type, DateTime? now = null) {
			var now1 = now ?? DateTime.UtcNow;
			var day30 = now1.AddDays(30);
			var day90 = now1.AddDays(90);
			var basePlan = new PaymentPlan_Monthly() {
				FreeUntil = day30,
				L10FreeUntil = day30,
				ReviewFreeUntil = day90,
				PlanCreated = now1,
				NoChargeForUnregisteredUsers = true,
			};
			switch (type) {
				case PaymentPlanType.Enterprise_Monthly_March2016:
					basePlan.Description = "Traction® Tools for Enterprise";
					basePlan.BaselinePrice = 500;
					basePlan.L10PricePerPerson = 2;
					basePlan.ReviewPricePerPerson = 2;
					basePlan.FirstN_Users_Free = 45;
					break;
				case PaymentPlanType.Professional_Monthly_March2016:
					basePlan.Description = "Traction® Tools Professional";
					basePlan.BaselinePrice = 149;
					basePlan.L10PricePerPerson = 10;
					basePlan.ReviewPricePerPerson = 2;
					basePlan.FirstN_Users_Free = 10;
					break;
				case PaymentPlanType.SelfImplementer_Monthly_March2016:
					basePlan.Description = "Traction® Tools Self-Implementer";
					basePlan.BaselinePrice = 199;
					basePlan.L10PricePerPerson = 12;
					basePlan.ReviewPricePerPerson = 3;
					basePlan.FirstN_Users_Free = 10;
					break;
				default:
					throw new ArgumentOutOfRangeException("type", "PaymentPlanType not implemented " + type);
			}
			return basePlan;
		}
	}


}