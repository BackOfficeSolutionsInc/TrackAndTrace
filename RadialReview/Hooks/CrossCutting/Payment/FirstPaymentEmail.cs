using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Payments;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Models.Application;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Variables;

namespace RadialReview.Hooks.CrossCutting.Payment {
    public class FirstPaymentEmail : IPaymentHook {
        public bool CanRunRemotely() {
            return false;
        }

        public async Task FirstSuccessfulCharge(ISession s, PaymentSpringsToken token) {
            var creator = s.Get<UserOrganizationModel>(token.CreatedBy);
            if (creator != null) {
                var mail = Mail
                    .To("FirstCharge",creator.GetEmail())
                    .SubjectPlainText("Welcome to Traction® Tools")
                    .Body(
@"
<p>
Welcome to the <b>Traction<span style=""line-height: 200%;font-size: 50%;vertical-align: top;"">®</span> Tools</b> family!
</p><p>
Please save this email which has some important next-steps and links.
</p><p>
<b>What's next?</b>
</p><p>
Schedule a quick <a href=""https://www.mytractiontools.com/schedule/optimize/""><b>15-20 minute Optimizer Call</b></a> to ensure that you are taking advantage of all of the features that Traction Tools has to offer. 
</p><p>
<b>Get Assistance When you Need it</b>
 </p><p>
Helping you is a core value that our Client Success team takes very seriously! 
<ul>
<li>Check our new, searchable <a href=""http://www.mytractiontools.com/success/""><b>knowledgebase</b></a> for quick answers to common questions</li>
<li>Schedule a video chat with the first available member of our Client Success team </li>
<li><a href=""http://www.mytractiontools.com/success/new/""><b>Submit a request</b></a> via our new client success portal</li>
<li>Forward / email your request to <a href=""mailto:client-success@mytractiontools.com""><b>client-success@mytractiontools.com</b></a></li>
<li>Call us at <a href=""tel:1-402-437-0098""><b>1-402-437-0098</b></a></li>
</ul>
</p><p>
<b>Looking for Receipts?</b>
</p><p>
For any purchases or subscription payments, you will receive an emailed receipt after each payment has processed. Our billing cycle is every 30 days.
</p><p>
<b>Connect With Us</b>
</p><p>
Like and Share us on <a href=""https://www.facebook.com/TractionTools/""><b>Facebook</b></a>.
</p><p>
Follow us on <a href=""https://www.youtube.com/channel/UCfM_bMyqs66VZo3Hx8uiepQ""><b>YouTube</b></a>, where we have how-to's and ways to get the most out of Traction Tools.
</p><p>
<b>Send Us Your Testimonial or Review</b>
</p><p>
Traction® Tools is made better by hearing from you. Please send your thoughts to <a href=""client-success@mytractiontools.com""><b>client-success@mytractiontools.com</b></a> and we may reach out to feature you on our website!
</p><p>
From all of us at Traction Tools, <b>thank you</b>.
</p>
", new string[] { });
                mail.ReplyToAddress = s.GetSettingOrDefault("SupportEmail", "client-success@mytractiontools.com");
                mail.ReplyToName = "Traction Tools Support";
               
                await Emailer.SendEmail(mail);
            }
        }

        public HookPriority GetHookPriority() {
            return HookPriority.Low;
        }

        public async Task UpdateCard(ISession s, PaymentSpringsToken token) {
            //noop
        }
    }
}