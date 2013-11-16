using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class ImageModel
    {
        public virtual Guid Id { get; set; }
        public virtual String OriginalName { get;set;}
        public virtual UserModel UploadedBy { get; set; }
        public virtual UploadType UploadType { get; set; }

    }

    public class ImageModelMap : ClassMap<ImageModel>
    {
        public ImageModelMap()
        {
            Id(x => x.Id).GeneratedBy.GuidComb();
            Map(x => x.UploadType);
            Map(x => x.OriginalName);
            References(x => x.UploadedBy);
        }
    }
}