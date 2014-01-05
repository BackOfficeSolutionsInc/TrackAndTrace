using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class ImageModel : IGuidIdentifiable
    {
        public virtual Guid Id { get; set; }
        public virtual String OriginalName { get;set;}
        public virtual UserModel UploadedBy { get; set; }
        public virtual UploadType UploadType { get; set; }
        public virtual long OrganizationId {get;set;}
        public virtual String Url {get;set;}

    }

    public class ImageModelMap : ClassMap<ImageModel>
    {
        public ImageModelMap()
        {
            Id(x => x.Id).GeneratedBy.GuidComb();
            Map(x => x.UploadType);
            Map(x => x.OriginalName);
            Map(x => x.OrganizationId);
            Map(x => x.Url);
            References(x => x.UploadedBy);
        }
    }
}