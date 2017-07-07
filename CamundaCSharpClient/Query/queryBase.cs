using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CamundaCSharpClient.Model;
using RestSharp;
using System.Threading.Tasks;

namespace CamundaCSharpClient.Query
{
    public class QueryBase
    {
        protected CamundaRestClient client;

        public QueryBase(CamundaRestClient client)
        {
            this.client = client;
        }

        protected async Task<List<T>> List<T>(IRestRequest request)
        {
            return await this.client.Execute<List<T>>(request); 
        }

        protected async Task<T> SingleResult<T>(IRestRequest request) where T : new()
        {
            return await this.client.Execute<T>(request);
        }

        protected async Task<Count> Count(IRestRequest request)
        {
            return await this.client.Execute<Count>(request);
        }
    }
}
