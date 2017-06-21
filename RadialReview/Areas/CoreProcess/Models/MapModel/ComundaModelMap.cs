
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

namespace RadialReview.Areas.CoreProcess.Models.MapModel
{

    public class ProcessDef_Camunda : IHistorical, ILongIdentifiable, IProcessId
    {
        public virtual long Id { get; set; }
        public virtual string ProcessDefKey { get; set; }
        public virtual string DeploymentId { get; set; }
        public virtual string LocalId { get; set; }
        public virtual long OrgId { get; set; }
        public virtual string CamundaId { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
    }

    public class ProcessDef_CamundaMap : ClassMap<ProcessDef_Camunda>
    {
        public ProcessDef_CamundaMap()
        {
            Id(x => x.Id);
            Map(x => x.ProcessDefKey).Length(256);
            Map(x => x.DeploymentId).Length(256);
            Map(x => x.LocalId).Length(256);
            Map(x => x.OrgId);
            Map(x => x.CamundaId).Length(256);
            Map(x => x.CreateTime);
            Map(x => x.DeleteTime);
        }
    }

    public class Task_Camunda : IHistorical, ILongIdentifiable
    {
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

    public class Task_CamundaMap : ClassMap<Task_Camunda>
    {
        public Task_CamundaMap()
        {
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