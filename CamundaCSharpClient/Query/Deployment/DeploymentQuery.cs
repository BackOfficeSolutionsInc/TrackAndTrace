using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using CamundaCSharpClient.Model.Deployment;

namespace CamundaCSharpClient.Query.Deployment
{
    public class DeploymentQuery:QueryBase
    {
        public static string DEFAULT_URL = "http://localhost:8080/engine-rest/engine/default/";
        public static string COCKPIT_URL = "http://localhost:8080/camunda/app/cockpit/default/";
        public static string Username = "demo";
        public static string Password = "demo";

        public DeploymentQuery(CamundaRestClient client):base(client)
        {

        }
        public string Deploy(string deploymentName, List<object> files)
        {
            Dictionary<string, object> postParameters = new Dictionary<string, object>();
            postParameters.Add("deployment-name", deploymentName);
            postParameters.Add("deployment-source", "C# Process Application");
            postParameters.Add("enable-duplicate-filtering", "true");
            postParameters.Add("data", files);

            // Create request and receive response
            // string postURL = helper.RestUrl + "deployment/create";
            string postURL = DEFAULT_URL + "deployment/create";
            HttpWebResponse webResponse = FormUpload.MultipartFormDataPost(postURL, null, null, postParameters);

            using (var reader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
            {
                var deployment = JsonConvert.DeserializeObject<CamundaCSharpClient.Model.Deployment.Deployment>(reader.ReadToEnd());
                return deployment.Id;
            }
        }

        public void AutoDeploy()
        {
            Assembly thisExe = Assembly.GetEntryAssembly();
            string[] resources = thisExe.GetManifestResourceNames();

            if (resources.Length == 0)
            {
                return;
            }

            List<object> files = new List<object>();
            foreach (string resource in resources)
            {
                // TODO Check if Camunda relevant (BPMN, DMN, HTML Forms)

                // Read and add to Form for Deployment                
                files.Add(FileParameter.FromManifestResource(thisExe, resource));

                //Console.WriteLine("Adding resource to deployment: " + resource);
            }

            Deploy(thisExe.GetName().Name, files);

            //Console.WriteLine("Deployment to Camunda BPM succeeded.");

        }
    }
}
