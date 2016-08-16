//using FluentNHibernate.Mapping;
//using RadialReview.Models.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;

//namespace RadialReview.Models
//{
//    public class DeepSubordinateModel : IDeletable
//    {
//        public virtual long Id { get; set; }
//        //public virtual UserOrganizationModel Manager { get; set; }
//        //public virtual UserOrganizationModel Subordinate { get; set; }
//        public virtual long ManagerId { get; set; }
//        public virtual long SubordinateId { get; set; }
//		//public virtual bool SubordinateIsNode { get; set; }
//		//public virtual bool ManagerIsNode { get; set; }
//		public virtual int Links { get; set; }
//        public virtual DateTime CreateTime { get; set; }
//        public virtual DateTime? DeleteTime {get;set;}

//        public virtual long OrganizationId { get; set; }

//        public DeepSubordinateModel()
//        {
//            CreateTime = DateTime.UtcNow;
//        }
//    }

//    public class DeepSubordinateModelMap : ClassMap<DeepSubordinateModel>
//    {
//        public DeepSubordinateModelMap()
//        {
//            Id(x => x.Id);
//            Map(x => x.CreateTime);
//            Map(x => x.DeleteTime);
//            Map(x => x.OrganizationId);
//			Map(x => x.Links);

//			//Map(x => x.ManagerIsNode);
//			//Map(x => x.SubordinateIsNode);

//			Map(x => x.SubordinateId).Column("SubordinateId");
//            //References(x => x.Subordinate).Column("SubordinateId").LazyLoad().ReadOnly();

//            Map(x => x.ManagerId).Column("ManagerId");
//            //References(x => x.Manager).Column("ManagerId").LazyLoad().ReadOnly();
//        }
//    }
//}