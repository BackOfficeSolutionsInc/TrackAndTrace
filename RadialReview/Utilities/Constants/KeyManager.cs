using System;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using RadialReview.Utilities.DataTypes;
using Amazon;
using Newtonsoft.Json;

namespace RadialReview.Utilities.Constants {
	public class KeyManager {

        public class DatabaseCredentials {
            public string Username { get; set; }
            public string Password { get; set; }
			public string Host { get; set; }
			public string Port { get; set; }
			public string Database { get; set; }
			public string DatabaseIdentifier { get; set; }
		}

        public static DatabaseCredentials ProductionDatabaseCredentials {
            get {
                var s=GetSecret("prod/db/radial-enc");
                return new DatabaseCredentials {
                    Username = s.GetJsonValue("username"),
					Password = s.GetJsonValue("password"),
					Host = s.GetJsonValue("host"),
					Port = s.GetJsonValue("port"),
					Database = s.GetJsonValue("dbname"),
					DatabaseIdentifier = s.GetJsonValue("dbInstanceIdentifier")
				};
            }
        }


        /*
         *	Use this code snippet in your app.
         *	If you need more information about configurations or implementing the sample code, visit the AWS docs:
         *	https://aws.amazon.com/developers/getting-started/net/
         */

        public class Key {
            public Key(string secretName, RegionEndpoint region, string secret) {
                Name = secretName;
                Region = region;
                SecretPlainText = secret;
            }

            public string Name { get; set; }
            public RegionEndpoint Region { get; set; }
            public string SecretPlainText { get; set; }

            public string GetJsonValue(string key) {
                dynamic o =JsonConvert.DeserializeObject(SecretPlainText);
                return o[key];
            }

        }


        public static Key GetSecret(string secretName) {
            return KeysLookup[secretName];
        }

        private static DefaultDictionary<string, Key> KeysLookup = new DefaultDictionary<string, Key>(x => {
            return GetSecret_CacheMiss(x);
        });
        private static Key GetSecret_CacheMiss(string secretName) {
            var region = Amazon.RegionEndpoint.USWest2;
            string secret = "";

			IAmazonSecretsManager client = new AmazonSecretsManagerClient(region);
            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;

            GetSecretValueResponse response = null;
            try {
                response = client.GetSecretValue(request);
            } catch (ResourceNotFoundException) {
                Console.WriteLine("The requested secret {0} was not found", secretName);
                throw;
            } catch (InvalidRequestException e) {
                Console.WriteLine("The request was invalid due to: {0}", e.Message);
                throw;
            } catch (InvalidParameterException e) {
                Console.WriteLine("Request had invalid params: {0}", e.Message);
                throw;
            } catch (InternalServiceErrorException e) {
                Console.WriteLine("An error occurred on the server side.", e.Message);
                throw;
            } catch (DecryptionFailureException e) {
                Console.WriteLine("Secrets Manager can't decrypt the protected secret text using the provided KMS key.", e.Message);
                throw;
            }

            if (response != null) {
                if (response.SecretString != null) {
                    secret = response.SecretString;
                } else {
                    using (var memoryStream = response.SecretBinary) {
                        secret = memoryStream.ReadToEnd();
                    }
                }
            }
            return new Key(secretName, region, secret);
            // Your code goes here...
        }
    }
}