using RadialReview.Api.V0;
using RadialReview.Areas.CoreProcess.Accessors;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Areas.CoreProcess.Models.Process;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace RadialReview.Areas.CoreProcess.Controllers.Api_V0
{
    public class ProcessDef : BaseApiController
    {

        [Route("ProcessDef/StartProcess")]
        [HttpPut]
        public async Task<ProcessDef_Camunda> StartProcess([FromBody]long processId)
        {
            ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
            return await processDefAccessor.ProcessStart(GetUser(), processId);  
        }

        [Route("ProcessDef/UserTask")]
        [HttpPut]
        public async Task<IEnumerable<TaskViewModel>> GetUserTaskList([FromBody]long teamId)
        {
            ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
            return await processDefAccessor.GetAllTaskByTeamId(GetUser(), teamId);  
        }
    }
}
