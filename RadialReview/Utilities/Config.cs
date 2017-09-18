﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models;
using RadialReview.Models.Enums;
using System.IO;
using System.Threading;

namespace RadialReview.Utilities {
    public class Config {

        public static void DbUpdateSuccessful() {
            var env = GetEnv();
            if (env == Env.production)
                return;

            var version = GetAppSetting("dbVersion", "0");
            var dir = Path.Combine(Path.GetTempPath(), "TractionTools");

            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            var file = Path.Combine(dir, "dbversion" + env + ".txt");
            if (!File.Exists(file))
                File.CreateText(file).Close();
            while (FileUtilities.IsFileLocked(new FileInfo(file))) {
                Thread.Sleep(100);
            }
            File.WriteAllText(file, version);
        }

        public static bool RunChromeExt() {
            switch (GetEnv()) {
                case Env.local_test_sqlite:
                    return true;
                case Env.local_sqlite:
                    return false;
                case Env.local_mysql:
                    return GetAppSetting("RunExt", "false").ToBooleanJS();
                case Env.production:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static int EnterpriseAboveUserCount() {
            return 45;
        }

        public class ActiveCampaignConfig {
            public string BaseUrl { get; set; }
            public string EventUrl { get; set; }
            public string ApiKey { get; set; }

            /// <summary>
            /// https://tractiontools.activehosted.com/admin/main.php?action=settings#tab_track  >> click "Event Tracking API"
            /// </summary>
            public string ActId { get; set; }
            /// <summary>
            /// "Event Key" https://tractiontools.activehosted.com/admin/main.php?action=settings#tab_track
            /// </summary>
            public string TrackKey { get; set; }

            public bool TestMode { get; set; }
            public ConfigFields Fields { get; set; }
            public ConfigLists Lists { get; set; }

            public class ConfigFields {
                public long Autogenerated { get; internal set; }
                public long OrgId { get; internal set; }

                /// <summary>
                /// Business Coach",
                ///	Certified of Professional EOS Implementer",
                ///	Other",
                ///	Unknown",
                ///	EOS Implementer (Basecamp only; no certs)",
                /// </summary>
                public long CoachType { get; internal set; }
                public long CoachName { get; internal set; }
                public long AssignedTo { get; internal set; }
                public long AccountType { get; internal set; }
                public long HasEosImplementer { get; internal set; }
                public long UserId { get; internal set; }
                public long IsTest { get; internal set; }
                public long ReferralSource { get; internal set; }
                public long ReferralQuarter { get; internal set; }
                public int ReferralYear { get; internal set; }
                public long Title { get; internal set; }
                public long CoachLastReferral { get; internal set; }
                public long CoachHasReferral { get; internal set; }
                public long TrialEnd { get; internal set; }
                public long TrialStart { get; internal set; }
            }
            public class ConfigLists {
                public long PrimaryContact { get; internal set; }
                public long Implementer { get; internal set; }
                public long ContactList { get; internal set; }
                public long CoachThatReferred { get; internal set; }
            }

        }

        [Obsolete("Must remove when ready for production")]
        public static void ThrowNotImplementedOnProduction() {
            if (!IsLocal())
                throw new NotImplementedException();
        }

        public static long TextInNumber() {
            return 13217665599;
        }

        public static string ModifyEmail(string email) {
            if (IsLocal()) {
                return "clay.upton+" + (email ?? "").Replace("@", "_") + "@mytractiontools.com";
            }
            return email;
        }

        public static ActiveCampaignConfig GetActiveCampaignConfig() {
            var shouldRun = !Config.IsLocal();
            var forceRun = GetAppSetting("ActiveCampaign_ForceRun", "false").ToBooleanJS();
            if (!shouldRun) {
                shouldRun = shouldRun || forceRun;
            }
            var testMode = !shouldRun;

            return new ActiveCampaignConfig() {
                BaseUrl = GetAppSetting("ActiveCampaign_BaseUrl", ""),
                EventUrl = GetAppSetting("ActiveCampaign_EventUrl", ""),
                ApiKey = GetAppSetting("ActiveCampaign_ApiKey", ""),
                ActId = GetAppSetting("ActiveCampaign_ActId", ""),
                TrackKey = GetAppSetting("ActiveCampaign_TrackKey", ""),
                TestMode = testMode,
                Fields = new ActiveCampaignConfig.ConfigFields() {
                    HasEosImplementer = 1,
                    AccountType = 2,
                    AssignedTo = 3,
                    UserId = 11,
                    Title = 13,
                    CoachName = 16,
                    CoachType = 17,
                    ReferralSource = 20,
                    ReferralYear = 22,
                    ReferralQuarter = 23,
                    OrgId = 30,
                    Autogenerated = 32,
                    IsTest = 37,
                    CoachLastReferral = 42,
                    CoachHasReferral = 43,
                    TrialEnd = 44,
                    TrialStart = 45,
                },
                Lists = new ActiveCampaignConfig.ConfigLists() {
                    ContactList = 3,
                    Implementer = 4,
                    PrimaryContact = 5,
                    CoachThatReferred = 6,
                }
            };
        }

        public static string SchedulerSecretKey() {
            var found = GetAppSetting("Scheduler_SecretKey", null);
            if (string.IsNullOrWhiteSpace(found))
                throw new Exception("Scheduler Key not found in file.");
            return found;
        }

        //public static string PeopleAreaName() {
        //    return "People";
        //}


        public static bool ShouldUpdateDB() {
            var version = GetAppSetting("dbVersion", "0");
            if (version == "0")
                return true;

            var env = GetEnv();

            switch (env) {
                case Env.local_test_sqlite:
                    goto case Env.local_sqlite;
                case Env.local_mysql:
                    goto case Env.local_sqlite;
                case Env.local_sqlite: {
                        var dir = Path.Combine(Path.GetTempPath(), "TractionTools");
                        var file = Path.Combine(dir, "dbversion" + env + ".txt");
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        if (!File.Exists(file)) {
                            File.Create(file);
                            while (!File.Exists(file)) {
                                Thread.Sleep(100);
                            }
                        }
                        if (version == File.ReadAllText(file))
                            return false;
                        return true;
                    }
                case Env.production:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string BaseUrl(OrganizationModel organization, string append = null) {
            var baseUrl = new Func<string>(() => {
                if (HttpContext.Current != null) {
                    try {
                        var strPathAndQuery = HttpContext.Current.Request.Url.PathAndQuery;
                        return HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "/");
                    } catch (Exception) {
                        //Skip
                    }
                }
                switch (GetEnv()) {
                    case Env.local_sqlite:
                        return "https://localhost:44300/";
                    case Env.local_mysql:
                        return "https://localhost:44300/";
                    case Env.production:
                        if (organization == null)
                            return "https://traction.tools/";

                        switch (organization.Settings.Branding) {
                            case BrandingType.RadialReview:
                                return "https://traction.tools/";
                            case BrandingType.RoundTable:
                                return "https://traction.tools/";
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    case Env.local_test_sqlite:
                        return "https://localhost:44300/";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
            return baseUrl() + (append ?? "").TrimStart('/');
        }

        public static string ProductName(OrganizationModel organization = null) {
            var org = organization.NotNull(x => x);
            if (org != null) {
                switch (org.Settings.Branding) {
                    case BrandingType.RadialReview:
                        return GetAppSetting("ProductName_Review", "Traction® Tools");
                    case BrandingType.RoundTable:
                        return GetAppSetting("ProductName_Roundtable", "Traction® Tools");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            try {
                if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialreview"))
                    return GetAppSetting("ProductName_Review", "Traction Tools");
                else if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialroundtable"))
                    return GetAppSetting("ProductName_Roundtable", "Traction Tools");
            } catch (Exception) {
                //Fall back...
            }
            return GetAppSetting("ProductName_Review", "Traction Tools");
        }

        public static string DirectReportName() {
            return "Direct Report";
        }

        public static string ReviewName(OrganizationModel organization = null) {
            if (organization != null) {
                switch (organization.Settings.Branding) {
                    case BrandingType.RadialReview:
                        return GetAppSetting("ReviewName_Review", "Eval");
                    case BrandingType.RoundTable:
                        return GetAppSetting("ReviewName_Roundtable", "Eval");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            try {
                if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialreview"))
                    return GetAppSetting("ReviewName_Review", "Eval");
                else if (HttpContext.Current.Request.Url.Authority.ToLower().Contains("radialroundtable"))
                    return GetAppSetting("ReviewName_Roundtable", "Eval");
            } catch (Exception) {
                //Fall back...
            }
            return GetAppSetting("ReviewName_Review", "Eval");
        }

        public static string ManagerName() {
            return "Supervisor";
        }

		public class CamundaCredentials {
            public string Url { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public bool IsLocal { get; set; }
        }

		public static CamundaCredentials GetCamundaServer() {
			CamundaCredentials credentials = new CamundaCredentials();
            credentials.Username = "demo";
            credentials.Password = "demo";

            switch (GetEnv()) {
                case Env.local_test_sqlite:
                    credentials.Url = "http://localhost:8080/engine-rest";
                    credentials.IsLocal = true;
                    return credentials;
                case Env.local_mysql:
                    credentials.Url = "http://localhost:8080/engine-rest";
                    credentials.IsLocal = true;
                    return credentials;
                case Env.production:
                    credentials.Url = GetAppSetting("Camunda_Url");
                    credentials.IsLocal = false;
                    credentials.Username = GetAppSetting("Camunda_Username");
                    credentials.Password = GetAppSetting("Camunda_Password");
                    return credentials;
                default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static bool IsLocal() {
			switch (GetEnv()) {
				case Env.local_test_sqlite:
					return true;
				case Env.local_sqlite:
					return true;
				case Env.local_mysql:
					return true;
				case Env.production:
					return false;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

        public static bool ShouldDeploy()
        {
            return !IsLocal();
        }

        public static bool IsSchedulerAction() {
            if (!IsLocal()) {
                if (GetAppSetting("SchedulerAction").ToBooleanJS()) {
                    return true;
                }
            }
            return false;            
        }

        public static string GetAppSetting(string key, string deflt = null) {
            var config = System.Configuration.ConfigurationManager.AppSettings;
            return config[key] ?? deflt;
        }

        public static string FixEmail(string email) {
            return Config.IsLocal() ? "clay.upton+test_" + email.Replace("@", "_at_") + "@mytractiontools.com" : email;
        }

        public static Env GetEnv() {

            Env result;
            if (Enum.TryParse(GetAppSetting("Env").ToLower(), out result)) {
                return result;
            }
            throw new Exception("Invalid Environment");
        }

        public static string GetSecret() {
            return GetAppSetting("sha_secret");
        }

        public static bool OptimizationEnabled() {
            switch (GetEnv()) {
                case Env.local_sqlite:
                    return GetAppSetting("OptimizeBundles", "False").ToBoolean();
                case Env.local_mysql:
                    return GetAppSetting("OptimizeBundles", "False").ToBoolean();
                case Env.production:
                    return true;
                case Env.local_test_sqlite:
                    return GetAppSetting("OptimizeBundles", "True").ToBoolean();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string GetTrelloKey() {
            return GetAppSetting("TrelloKey");
        }

        public static class Basecamp {
            /*public static string GetUrl()
            {
                return GetAppSetting("BasecampUrl");
            }*/

            public static string GetUserAgent() {
                switch (GetEnv()) {
                    case Env.local_mysql:
                        goto case Env.local_sqlite;
                    case Env.local_sqlite:
                        return GetAppSetting("BasecampTestApp");
                    case Env.production:
                        return GetAppSetting("BasecampApp");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public static BCXAPI.Service GetService(OrganizationModel organization) {
                string key, secret;//, app;
                var redirect = BaseUrl(organization) + "Callback/Basecamp";
                switch (GetEnv()) {
                    case Env.local_mysql:
                        goto case Env.local_sqlite;
                    case Env.local_sqlite: {
                            key = GetAppSetting("BasecampTestKey");
                            secret = GetAppSetting("BasecampTestSecret");
                            break;
                        }
                    case Env.production: {
                            key = GetAppSetting("BasecampKey");
                            secret = GetAppSetting("BasecampSecret");
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return new BCXAPI.Service(key, secret, redirect, GetUserAgent());
            }
        }

        public static bool SendEmails() {
            switch (GetEnv()) {
                case Env.local_mysql:
                    return GetAppSetting("SendEmail_Debug", "false").ToBooleanJS();
                case Env.local_sqlite:
                    return GetAppSetting("SendEmail_Debug", "false").ToBooleanJS();
                case Env.production:
                    return true;
                case Env.local_test_sqlite:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string PaymentSpring_PublicKey(bool forceUseTest = false) {
            if (forceUseTest)
                return GetAppSetting("PaymentSpring_PublicKey_Test");

            switch (GetEnv()) {
                case Env.local_test_sqlite:
                    return GetAppSetting("PaymentSpring_PublicKey_Test");
                case Env.local_mysql:
                    return GetAppSetting("PaymentSpring_PublicKey_Test");
                case Env.local_sqlite:
                    return GetAppSetting("PaymentSpring_PublicKey_Test");
                case Env.production:
                    return GetAppSetting("PaymentSpring_PublicKey");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        [Obsolete("Be careful with private keys")]
        public static string PaymentSpring_PrivateKey(bool forceUseTest = false) {
            if (forceUseTest)
                return GetAppSetting("PaymentSpring_PrivateKey_Test");

            switch (GetEnv()) {
                case Env.local_test_sqlite:
                    return GetAppSetting("PaymentSpring_PrivateKey_Test");
                case Env.local_mysql:
                    return GetAppSetting("PaymentSpring_PrivateKey_Test");
                case Env.local_sqlite:
                    return GetAppSetting("PaymentSpring_PrivateKey_Test");
                case Env.production:
                    return GetAppSetting("PaymentSpring_PrivateKey");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string VideoConferenceUrl(string resource = null) {
            var server = GetAppSetting("VideoConferenceServer").TrimEnd('/');
            if (resource != null) {
                server = server + "/" + resource.TrimStart('/');
            }
            return server;
            /*
            switch (GetEnv())
            {
                case Env.local_mysql:   return server;
                case Env.local_sqlite:	return GetAppSetting("VideoConferenceServer");
                case Env.production:	return GetAppSetting("VideoConferenceServer");
                default: throw new ArgumentOutOfRangeException();
            }*/
        }

        public static string GetMandrillGoogleAnalyticsDomain() {
            return GetAppSetting("Mandrill_GoogleAnalyticsDomain", null);
        }

        /*internal static string PaymentEmail()
        {
            throw new NotImplementedException();
        } */
        public static string GetAccessLogDir() {
            switch (GetEnv()) {
                case Env.local_mysql:
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\IISExpress\Logs\RadialReview\";
                case Env.local_sqlite:
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\IISExpress\Logs\RadialReview\";
                case Env.production:
                    return @"C:\inetpub\logs\LogFiles\W3SVC1\";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string NotesUrl(string append = "") {
            var server = GetAppSetting("NotesServer", "https://notes.traction.tools");
            if (!string.IsNullOrWhiteSpace(append)) {
                server = server.TrimEnd('/') + "/" + append;
            }
            return server;
        }

        internal static string NoteApiKey() {
            return GetAppSetting("NotesServer_ApiKey");
        }

        public static class Office365 {
            public static string AppId() {
                return GetAppSetting("ida:AppId");
            }
            public static string Password() {
                return GetAppSetting("ida:AppPassword");
            }

            public static string RedirectUrl() {
                return BaseUrl(null);
            }
            public static string[] Scopes() {
                return GetAppSetting("ida:AppScopes").Replace(' ', ',').Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }


        public class RedisConfig {
            public string Server { get; set; }
            public int Port { get; set; }
            public string Password { get; set; }
            public string ChannelName { get; set; }
        }

        public static RedisConfig Redis(string channel) {
            string server;
            switch (GetEnv()) {
                case Env.local_mysql:
                    server = "127.0.0.1";
                    break;
                case Env.local_sqlite:
                    server = "127.0.0.1";
                    break;
                case Env.production:
                    server = GetAppSetting("RedisSignalR_Server", null);
                    break;
                case Env.local_test_sqlite:
                    server = "127.0.0.1";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new RedisConfig() {
                Server = server,
                ChannelName = channel,
                Password = GetAppSetting("RedisSignalR_Password", null),
                Port = GetAppSetting("RedisSignalR_Port", "6379").ToInt()
            };
            /*
            switch (GetEnv())
            {
                case Env.local_mysql: return GetAppSetting("RedisSignalR-server",null);
                case Env.local_sqlite: return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\IISExpress\Logs\RadialReview\";
                case Env.production: return @"C:\inetpub\logs\LogFiles\W3SVC1\";
                default: throw new ArgumentOutOfRangeException();
            }*/
        }

        public class TwilioData {
            public string Sid { get; set; }
            public string AuthToken { get; set; }
            public bool ShouldSendText { get; set; }
        }

        public static TwilioData Twilio() {
            return new TwilioData() {
                Sid = Config.GetAppSetting("TwilioSid"),
                AuthToken = Config.GetAppSetting("TwilioToken"),
                ShouldSendText = !Config.IsLocal()
            };

        }
    }
}
