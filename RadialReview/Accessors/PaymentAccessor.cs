using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Payments;
using RadialReview.Models.Tasks;
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
        }
        public async Task<PaymentResult> ChargeOrganization(long organizationId, decimal amount, bool useTest = false)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var tokens = s.QueryOver<PaymentSpringsToken>()
                                    .Where(x => x.OrganizationId == organizationId && x.Active && x.DeleteTime == null)
                                    .List().ToList();

                    var token = tokens.OrderByDescending(x => x.CreateTime).FirstOrDefault();

                    if (token == null)
                    {
                        throw new PaymentException(organizationId, amount, PaymentExceptionType.MissingToken);
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
                    HttpResponseMessage response = await client.PostAsync("https://api.paymentspring.com/api/v1/charge", requestContent);

                    // Get the response content.
                    HttpContent responseContent = response.Content;

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
                            throw new PaymentException(organizationId, amount, PaymentExceptionType.ResponseError, String.Join(" ", builder));
                        }

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
                        };
                    }
                }
            }
        }

        public List<CreditCardVM> GetCards(UserOrganizationModel caller, long organizationId)
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

        public async Task<CreditCardVM> SetCard(UserOrganizationModel caller,
            long organizationId,
            string tokenId,
            string @class,
            string cardType,
            string cardOwnerName,
            string last4,
            int expireMonth,
            int expireYear,
            String address_1,
            String address_2,
            String city,
            String state,
            string zip,
            string phone,
            string website,
            string country,
            bool active)
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

        public static PaymentPlanModel CreatePlan(ISession s, OrganizationModel organization,PaymentPlanModel plan)
        {
            var task = new ScheduledTask(){
                MaxException = 1,
                Url = "/Scheduler/ChargeAccount/" + organization.Id,
                NextSchedule = plan.SchedulerPeriod(),
                Fire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date),
                FirstFire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date),
                TaskName = ScheduledTask.MonthlyPaymentPlan,
            };
            s.Save(task);
            plan.Task = task;
            s.Save(plan);
            return plan;
        }
    }
}