﻿using NHibernate;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Interfaces
{
    public interface IProcessDefAccessor
    {
        Task<bool> Deploy(UserOrganizationModel caller, long localId);
        List<ProcessInstanceViewModel> GetProcessInstanceList(UserOrganizationModel caller, long localId);
        Task<long> Create(UserOrganizationModel caller, string processName);
        Task<bool> EditProcess(UserOrganizationModel caller, long localId, string processName);
        bool DeleteProcess(UserOrganizationModel caller, long processId);
   
        IEnumerable<ProcessDef_Camunda> GetProcessDefinitionList(UserOrganizationModel caller, long orgId);
        ProcessDef_Camunda GetProcessDefById(UserOrganizationModel caller, long processId);
        Task<TaskViewModel> CreateProcessDefTask(UserOrganizationModel caller, long localId, TaskViewModel model);
        Task<TaskViewModel> UpdateTask(UserOrganizationModel caller, long localId, TaskViewModel model);
        Task<bool> DeleteProcessDefTask(UserOrganizationModel caller, string taskId, long localId);
        Task<List<TaskViewModel>> GetAllTaskForProcessDefinition(UserOrganizationModel caller, long localId);
        Task<List<TaskViewModel>> GetTaskListByCandidateGroups(UserOrganizationModel caller, long[] candidateGroupIds, bool unassigned = false);
        Task<List<TaskViewModel>> GetAllTaskByRGM(UserOrganizationModel caller, long teamId);
        Task<List<TaskViewModel>> GetTaskListByUserId(UserOrganizationModel caller, string userId);
        Task<List<TaskViewModel>> GetTaskListByProcessInstanceId(UserOrganizationModel caller, string processInstanceId);
        Task<TaskViewModel> GetTaskById(UserOrganizationModel caller, string taskId);
        Task<long[]> GetCandidateGroupIdsForTask_UnSafe(ISession s, string taskId);
        Task<bool> TaskAssignee(UserOrganizationModel caller, string taskId, long userId);
        Task<bool> TaskClaimOrUnclaim(UserOrganizationModel caller, string taskId, long userId,bool claim);
        //Task<bool> TaskUnClaim(UserOrganizationModel caller, string taskId, long userId);
        Task<bool> TaskComplete(UserOrganizationModel caller, string taskId, long userId);
        Task<bool> ModifiyBpmnFile(UserOrganizationModel caller, long localId, int oldOrder, int newOrder);
        Task<bool> SuspendProcess(UserOrganizationModel caller, long localId, bool shouldSuspend);
        Task<ProcessDef_Camunda> ProcessStart(UserOrganizationModel caller, long processId);        
    }
}
