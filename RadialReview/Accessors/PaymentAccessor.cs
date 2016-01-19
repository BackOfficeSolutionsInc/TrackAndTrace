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

namespace RadialReview.Accessors
{
	public class PaymentAccessor : BaseAccessor
	{
		public PaymentPlanModel BasicPaymentPlan()
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PaymentPlanModel basicPlan = null;
					try
					{
						basicPlan = s.QueryOver<PaymentPlanModel>().Where(x => x.IsDefault).SingleOrDefault();
					}
					catch (Exception e)
					{
						log.Error(e);
					}
					if (basicPlan == null)
					{
						basicPlan = new PaymentPlanModel()
						{
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
		public class PaymentResult
		{
			private string _cardNumber;
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

		public static async Task<PaymentResult> ChargeOrganization(long organizationId, long taskId, bool forceUseTest = false)
		{
			PaymentResult result = null;
			InvoiceModel invoice = null;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var org = s.Get<OrganizationModel>(organizationId);

					if (org == null)
						throw new NullReferenceException("Organization does not exist");

					if(org.DeleteTime!=null)
						throw new PermissionsException("Organization was deleted.");

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

					var executeTime = DateTime.UtcNow.Date;
					try
					{
						var itemized = CalculateCharge(s, org, plan, executeTime);
						invoice = CreateInvoice(s, org,plan, executeTime, itemized);
						result = await ExecuteInvoice(s, invoice, forceUseTest);
					}
					finally
					{

						tx.Commit();
						s.Flush();
					}
				}
			}

			await SendReceipt(result, invoice);
			
			return result;
		}


		public static InvoiceModel CreateInvoice(ISession s, OrganizationModel org, PaymentPlanModel paymentPlan, DateTime executeTime, IEnumerable<Itemized> items)
		{
			var invoice = new InvoiceModel()
			{
				Organization = org,
				InvoiceDueDate = executeTime.Add(TimespanExtensions.OneMonth()).Date
			};

			if (NHibernateUtil.GetClass(paymentPlan) == typeof (PaymentPlan_Monthly)){
				invoice.ServiceStart = executeTime;
				invoice.ServiceEnd = executeTime.Add(TimespanExtensions.OneMonth()).Date;
			}else{
				throw new PermissionsException("Unhandled Payment Plan");
			}
			
			s.Save(invoice);
			
			var invoiceItems = items.Select(x => new InvoiceItemModel()
			{
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
		public static async Task<PaymentResult> ExecuteInvoice(ISession s, InvoiceModel invoice, bool useTest = false)
		{
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
		public static async Task<bool> SendReceipt(PaymentResult result, InvoiceModel invoice)
		{
			if (invoice.PaidTime!=null){
				var ProductName=Config.ProductName(invoice.Organization);
				var SupportEmail=ProductStrings.SupportEmail;
				var OrgName=invoice.Organization.GetName();
				var Charged = invoice.AmountDue;
				var CardLast4=result.card_number??"NA";
				var TransactionId=result.id??"NA";
				var ChargeTime = invoice.PaidTime;
				var ServiceThroughDate=invoice.ServiceEnd.ToString("yyyy-MM-dd");
				var Address = ProductStrings.Address;

				var localChargeTime = invoice.Organization.ConvertFromUTC(ChargeTime.Value);
				var lctStr = localChargeTime.ToString("dd MMM yyyy hh:mmtt")+" "+invoice.Organization.GetTimeZoneId(localChargeTime);

				var email = Mail.Bcc(EmailTypes.Receipt, ProductStrings.PaymentReceiptEmail);
				if (result.email != null){
					email = email.AddBcc(result.email);
				}
				var toSend = email.SubjectPlainText("[" + ProductName + "] Payment Receipt for " + invoice.Organization.GetName())
					.Body(EmailStrings.PaymentReceipt_Body, ProductName, SupportEmail, OrgName, String.Format("{0:C}",Charged), CardLast4, TransactionId, lctStr, ServiceThroughDate, Address);
				await Emailer.SendEmail(toSend);
				return true;
			}
			return false;
		}


		public static List<Itemized> CalculateCharge(ISession s, OrganizationModel org, PaymentPlanModel paymentPlan, DateTime executeTime)
		{
			var itemized = new List<Itemized>();

			if (NHibernateUtil.GetClass(paymentPlan) == typeof(PaymentPlan_Monthly))
			{
				var plan = (PaymentPlan_Monthly)s.GetSessionImplementation().PersistenceContext.Unproxy(paymentPlan);
				var rangeStart = executeTime.Subtract(TimespanExtensions.OneMonth());
				var rangeEnd = executeTime;

				var people = s.QueryOver<UserOrganizationModel>()
					.Where(x => x.Organization.Id == org.Id && x.CreateTime < rangeEnd && (x.DeleteTime == null || x.DeleteTime > rangeStart) && !x.IsRadialAdmin)
					.Select(x => x.Id)
					.List<long>().Count();

				people = Math.Max(0, people - plan.FirstN_Users_Free);
				var allRevisions = s.AuditReader().GetRevisionsBetween<OrganizationModel>(org.Id, rangeStart, rangeEnd).ToList();

				var reviewEnabled = allRevisions.Any(x => x.Object.Settings.EnableReview);
				var l10Enabled = allRevisions.Any(x => x.Object.Settings.EnableL10);

				if (reviewEnabled)
				{
					var reviewItem = new Itemized()
					{
						Name = "Review Software",
						Price = plan.ReviewPricePerPerson,
						Quantity = people,
					};

					itemized.Add(reviewItem);
					if (!(plan.ReviewFreeUntil == null || plan.ReviewFreeUntil.Value.Date > executeTime.Date))
					{
						//Discount it since it is free
						itemized.Add(reviewItem.Discount());
					}
				}
				if (l10Enabled)
				{
					var l10Item = new Itemized()
					{
						Name = "L10 Meeting Software",
						Price = plan.L10PricePerPerson,
						Quantity = people,
					};
					itemized.Add(l10Item);

					if (!(plan.L10FreeUntil == null || plan.L10FreeUntil.Value.Date > executeTime.Date))
					{
						//Discount it since it is free
						itemized.Add(l10Item.Discount());
					}
				}
				if ((plan.FreeUntil.Date > executeTime.Date))
				{
					//Discount it since it is free
					var total = itemized.Sum(x => x.Total());
					itemized.Add(new Itemized()
					{
						Name = "Discount",
						Price = -1 * total,
						Quantity = 1,
					});
				}
			}
			else
			{
				throw new PermissionsException("Unhandled Payment Plan");
			}
			return itemized;
		}


		[Obsolete("Unsafe")]
		public static async Task<PaymentResult> ChargeOrganizationAmount(ISession s, long organizationId, decimal amount, bool useTest = false)
		{
			if (amount == 0){
				return new PaymentResult(){
					amount_settled = 0,
					
				};
			}

			var tokens = s.QueryOver<PaymentSpringsToken>()
							.Where(x => x.OrganizationId == organizationId && x.Active && x.DeleteTime == null)
							.List().ToList();

			var token = tokens.OrderByDescending(x => x.CreateTime).FirstOrDefault();

			if (token == null)
			{
				throw new PaymentException(s.Get<OrganizationModel>(organizationId), amount, PaymentExceptionType.MissingToken);
			}
			//CURL 
			var client = new HttpClient();

			// Create the HttpContent for the form to be posted.
			var requestContent = new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("customer_id", token.CustomerToken),
                        new KeyValuePair<string, string>("amount", ""+((int)(amount*100))),
                    });

			var privateApi = Config.PaymentSpring_PrivateKey(useTest);
			var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
			// Get the response.
			var response = await client.PostAsync("https://api.paymentspring.com/api/v1/charge", requestContent);

			// Get the response content.
			var responseContent = response.Content;

			// Get the stream of the content.
			using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
			{
				// Write the output.
				var result = await reader.ReadToEndAsync();
				log.Info("Charged Card: " + result);
				if (Json.Decode(result).errors != null)
				{
					var builder = new List<string>();
					for (var i = 0; i < Json.Decode(result).errors.Length; i++)
					{
						builder.Add(Json.Decode(result).errors[i].message + " (" + Json.Decode(result).errors[i].code + ").");
					}
					throw new PaymentException(s.Get<OrganizationModel>(organizationId), amount, PaymentExceptionType.ResponseError, String.Join(" ", builder));
				}

				if (Json.Decode(result).@class != "transaction")
					throw new PermissionsException("Response must be of type 'transaction'.");

				var org = s.Get<OrganizationModel>(organizationId);
				if (org.AccountType == AccountType.Demo)
					org.AccountType = AccountType.Paying;

				return new PaymentResult
				{
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

		[Obsolete("Unsafe")]
		public static async Task<PaymentResult> ChargeOrganizationAmount(long organizationId, decimal amount, bool useTest = false)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var charged = await ChargeOrganizationAmount(s, organizationId, amount, useTest);
					tx.Commit();
					s.Flush();
					return charged;
				}
			}
		}

		public static List<CreditCardVM> GetCards(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (organizationId != caller.Organization.Id)
						throw new PermissionsException("Organization Ids do not match");


					PermissionsUtility.Create(s, caller).EditCompanyPayment(organizationId);
					var cards = s.QueryOver<PaymentSpringsToken>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();

					return cards.Select(x => new CreditCardVM()
					{
						Active = x.Active,
						CardId = x.Id,
						Created = x.CreateTime,
						Last4 = x.CardLast4,
						Owner = x.CardOwner
					}).ToList();
				}
			}
		}

		public static async Task<CreditCardVM> SetCard(UserOrganizationModel caller,long organizationId,string tokenId,string @class,
			string cardType,string cardOwnerName,string last4,int expireMonth,int expireYear,String address_1,String address_2,
			String city,String state,string zip,string phone,string website,string country,string email,bool active)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (@class != "token")
						throw new PermissionsException("Id must be a token");
					if (String.IsNullOrWhiteSpace(tokenId))
						throw new PermissionsException("Token was empty");
					if (organizationId != caller.Organization.Id)
						throw new PermissionsException("Organization Ids do not match");


					PermissionsUtility.Create(s, caller).EditCompanyPayment(organizationId);
					if (active)
					{
						var previous = s.QueryOver<PaymentSpringsToken>().Where(x => x.OrganizationId == organizationId && x.Active == true && x.DeleteTime == null).List().ToList();
						foreach (var p in previous)
						{
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

					var privateApi = Config.PaymentSpring_PrivateKey();
					var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
					client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
					HttpResponseMessage response = await client.PostAsync("https://api.paymentspring.com/api/v1/customers", requestContent);
					HttpContent responseContent = response.Content;
					using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
					{
						var result = await reader.ReadToEndAsync();
						if (Json.Decode(result).errors != null)
						{
							var builder = new List<string>();
							for (var i = 0; i < Json.Decode(result).errors.Length; i++)
							{
								builder.Add(Json.Decode(result).errors[i].message + " (" + Json.Decode(result).errors[i].code + ").");
							}
							throw new PermissionsException(String.Join(" ", builder));
						}
						if (Json.Decode(result).@class != "customer")
							throw new PermissionsException("Expected class: 'Customer'");


						var token = new PaymentSpringsToken()
						{
							CustomerToken = Json.Decode(result).id,
							CardLast4 = last4,
							CardOwner = cardOwnerName,
							CardType = cardType,
							MonthExpire = expireMonth,
							YearExpire = expireYear,
							OrganizationId = organizationId,
							Active = active,
							ReceiptEmail = email,
							CreatedBy = caller.Id,
						};
						s.Save(token);
						tx.Commit();
						s.Flush();

						return new CreditCardVM()
						{
							Active = active,
							CardId = token.Id,
							Created = token.CreateTime,
							Last4 = token.CardLast4,
							Owner = token.CardOwner
						};
					}

				}
			}
		}

		public static PaymentPlanModel CreatePlan(ISession s, OrganizationModel organization, PaymentPlanModel plan)
		{
			var task = new ScheduledTask()
			{
				MaxException = 1,
				Url = "/Scheduler/ChargeAccount/" + organization.Id,
				NextSchedule = plan.SchedulerPeriod(),
				Fire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date),
				FirstFire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date),
				TaskName = ScheduledTask.MonthlyPaymentPlan,
			};
			s.Save(task);
			task.OriginalTaskId = task.Id;
			s.Update(task);
			if (plan is PaymentPlan_Monthly)
			{
				var ppm = (PaymentPlan_Monthly)plan;
				ppm.OrgId = organization.Id;
			}

			plan.Task = task;
			s.Save(plan);
			return plan;
		}

		public static PaymentPlanModel GetPlan(UserOrganizationModel caller, long organizationId)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);
					var org = s.Get<OrganizationModel>(organizationId);

					var plan = s.Get<PaymentPlanModel>(org.PaymentPlan.Id);

					if (plan != null && plan.Task != null)
					{
						plan._CurrentTask = s.QueryOver<ScheduledTask>()
							.Where(x => x.OriginalTaskId == plan.Task.OriginalTaskId && x.Executed == null)
							.List().FirstOrDefault();
					}


					return (PaymentPlanModel)s.GetSessionImplementation().PersistenceContext.Unproxy(plan);

				}
			}
		}

		[Obsolete("Unsafe",false)]
		public static List<long> GetPayingOrganizations(ISession s)
		{
			var scheduledToPay = s.QueryOver<ScheduledTask>().Where(x => x.TaskName == ScheduledTask.MonthlyPaymentPlan && x.DeleteTime == null && x.Executed == null)
				.List().ToList().Select(x=>x.Url.Split('/').Last().ToLong());
			var hasTokens = s.QueryOver<PaymentSpringsToken>().Where(x => x.Active && x.DeleteTime == null).List().ToList();

			var hasTokens_scheduledToPay = hasTokens.Select(x => x.OrganizationId).Intersect(scheduledToPay);
			return hasTokens_scheduledToPay.ToList();
		}


		[Obsolete("Unsafe", false)]
		public static decimal CalculateTotalCharge(ISession s,List<long> orgIds)
		{
			var orgs = s.QueryOver<OrganizationModel>().WhereRestrictionOn(x => x.Organization.Id).IsIn(orgIds.Distinct().ToList()).List().ToList();

			return orgs.Sum(o => 
					CalculateCharge(s, o, o.PaymentPlan, DateTime.UtcNow)
						.Sum(x => x.Total())
				);

		}
	}

	
}