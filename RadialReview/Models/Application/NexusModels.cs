using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class NexusModel :IDeletable, IStringIdentifiable
    {
        public virtual string Id { get; protected set; }
        public virtual string Message { get; set; }
        public virtual long ForUserId { get; set; }
        public virtual long ByUserId { get; set; }
        public virtual NexusActions ActionCode { get; set; }
        public virtual string[] GetArgs(){
			if (pArgs == null)
				return new string[0];

            return pArgs.Split('\a');
        }
        public virtual void SetArgs(params string[] args)
        {
            pArgs = String.Join("\a", args);
        }
        
        public virtual string pArgs { get; set; }

        public virtual DateTime DateCreated { get; set; }
        public virtual DateTime? DateExecuted { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public NexusModel(Guid guid)
        {
            Id = guid.ToString();
            DateCreated = DateTime.UtcNow;
        }

        protected NexusModel()
        {

        }
    }

    public class NexusModelMap : ClassMap<NexusModel>
    {
        public NexusModelMap()
        {
            Id(x => x.Id).GeneratedBy.Assigned();
            Map(x => x.Message);
            Map(x => x.ForUserId);
            Map(x => x.ByUserId);
            Map(x => x.ActionCode);
            Map(x => x.pArgs);
            Map(x => x.DateCreated);
            Map(x => x.DateExecuted);
            Map(x => x.DeleteTime);
            
        }
    }
}