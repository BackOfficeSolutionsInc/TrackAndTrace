using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CamundaCSharpClient.Model;
using RestSharp;
using Newtonsoft.Json;
using CamundaCSharpClient.Query;
using CamundaCSharpClient.Query.Task;
using CamundaCSharpClient.Query.Deployment;

namespace CamundaCSharpClient
{
    public partial class CamundaRestClient
    {        
        public DeploymentQuery Deployment()
        {
            return new DeploymentQuery(this);
        }
    }
}
