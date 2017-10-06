using RadialReview.Api;
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

namespace RadialReview.Areas.CoreProcess.Controllers.Api_V1
{

	[Obsolete("Not ready, needs angularization")]
	[RoutePrefix("api/v1")]
	public class CoreProcess : BaseApiController
    {

        [Route("CoreProcess/{PROCESS_ID}/Start")]
        [HttpPost]
        public async Task<ProcessDef_Camunda> StartProcess(long PROCESS_ID)
        {
            ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
            return await processDefAccessor.ProcessStart(GetUser(), PROCESS_ID);  
        }

        [Route("CoreProcess/User/{USER_ID}/Tasks")]
        [HttpGet]
        public async Task<IEnumerable<TaskViewModel>> GetUserTaskList(long USER_ID)
        {
            ProcessDefAccessor processDefAccessor = new ProcessDefAccessor();
            //return await processDefAccessor.GetAllTaskByTeamId(GetUser(), teamId);  
            return await processDefAccessor.GetTaskListByUserId(GetUser(), USER_ID);  

        }
    }
}
