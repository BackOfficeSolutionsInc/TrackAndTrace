using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CamundaCSharpClient
{
    public class CamundaConfig
    {
        public class UrlCredentials
        {
            public string Url { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public static UrlCredentials GetCamundaServer()
        {
            UrlCredentials credentials = new UrlCredentials();
            credentials.Username = "demo";
            credentials.Password = "demo";

            switch (GetEnv())
            {
                case Env.local_test_sqlite:
                    credentials.Url = "http://localhost:8080/engine-rest";
                    return credentials;
                case Env.local_mysql:
                    credentials.Url = "http://localhost:8080/engine-rest";
                    return credentials;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public static Env GetEnv()
        {
            Env result;
            if (Enum.TryParse(GetAppSetting("Env").ToLower(), out result))
            {
                return result;
            }
            throw new Exception("Invalid Environment");
        }
        public static string GetAppSetting(string key, string deflt = null)
        {
            var config = System.Configuration.ConfigurationManager.AppSettings;
            return config[key] ?? deflt;
        }
    }
    public enum Env
    {
        invalid,
        local_sqlite,
        local_mysql,
        production,
        local_test_sqlite
    }
}
