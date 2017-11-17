
using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Models;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.CoreProcess.Models.MapModel {

	public class ProcessDef_Camunda : IHistorical, ILongIdentifiable, IProcessId {
		public ProcessDef_Camunda() {
			CreateTime = DateTime.UtcNow;
		}
		public virtual long Id { get; set; }
		public virtual string ProcessDefKey { get; set; }
		public virtual long LocalId {
			get {
				return Id;
			}
			set {
				Id = value;
			}
		}
		public virtual long OrgId { get; set; }
		public virtual ForModel Creator { get; set; }
		public virtual string CamundaId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
	}

	public class ProcessDef_CamundaMap : ClassMap<ProcessDef_Camunda> {
		public ProcessDef_CamundaMap() {
			Id(x => x.Id);
			Map(x => x.ProcessDefKey).Length(256);
			//Map(x => x.LocalId).Length(256);
			Map(x => x.OrgId);
			Component(x => x.Creator).ColumnPrefix("Creator_");
			Map(x => x.CamundaId).Length(256);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
		}
	}


	public class ProcessInstance_Camunda : IHistorical, ILongIdentifiable {
		public ProcessInstance_Camunda() {
			CreateTime = DateTime.UtcNow;
		}
		public virtual long Id { get; set; }
		//public virtual string ProcessDefId { get; set; }
		public virtual string CamundaProcessInstanceId { get; set; }
		public virtual long LocalProcessInstanceId { get; set; }
		public virtual bool Suspended { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime? CompleteTime { get; set; }
	}

	public class ProcessInstance_CamundaMap : ClassMap<ProcessInstance_Camunda> {
		public ProcessInstance_CamundaMap() {
			Id(x => x.Id);
			//Map(x => x.ProcessDefId).Length(256);
			Map(x => x.CamundaProcessInstanceId).Length(256);
			Map(x => x.LocalProcessInstanceId).Length(256);  // reference with PRocessDef_Camunda
			Map(x => x.Suspended);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
			Map(x => x.CompleteTime);
		}
	}

	public class ProcessDef_CamundaFile : IHistorical, ILongIdentifiable {
		public ProcessDef_CamundaFile() {
			CreateTime = DateTime.UtcNow;
		}
		public virtual long Id { get; set; }
		public virtual long LocalProcessDefId { get; set; } // reference with PRocessDef_Camunda
		public virtual string DeploymentId { get; set; }
		public virtual string Version { get; set; }
		public virtual string FileKey { get; set; }
		public virtual BPMN_FileType File { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
	}

	public class ProcessDef_CamundaFileMap : ClassMap<ProcessDef_CamundaFile> {
		public ProcessDef_CamundaFileMap() {
			Id(x => x.Id);
			Map(x => x.LocalProcessDefId).Index("idx_ProcessDef_CamundaFile_LocalProcessDefId");
			Map(x => x.DeploymentId);
			Map(x => x.Version);
			Map(x => x.FileKey);
			Map(x => x.File); // db will save integer for enum value
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
		}
	}
	public enum BPMN_FileType {
		INVALID = 0,
		BPMN = 1,
	}

	public class Task_Camunda : IHistorical, ILongIdentifiable {
		public Task_Camunda() {
			CreateTime = DateTime.UtcNow;
		}
		public virtual long Id { get; set; }
		public virtual string Name { get; set; }
		public virtual string DueDate { get; set; }
		public virtual long OrgId { get; set; }
		public virtual long OwnerId { get; set; }
		public virtual ForModel Owner { get; set; }
		public virtual string ProcessDefId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
	}

	public class Task_CamundaMap : ClassMap<Task_Camunda> {
		public Task_CamundaMap() {
			Id(x => x.Id);
			Map(x => x.Name);
			Map(x => x.DueDate);
			Map(x => x.OrgId);
			Component(x => x.Owner).ColumnPrefix("Owner_");
			Map(x => x.ProcessDefId);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
		}
	}
}