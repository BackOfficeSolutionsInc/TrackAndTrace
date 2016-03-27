using FluentNHibernate.Mapping;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Application
{
    public class UploadModel
    {
        public virtual long Id { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual long CreatedBy { get; set; }
        public virtual long OrganizationId { get; set; }
        public virtual UploadType UploadType { get; set; }
        public virtual string OriginalName { get; set; }
        public virtual Guid Identifier { get; set; }
        public virtual string MimeType { get; set; }
        public virtual ForModel ForModel { get; set; }
        public virtual byte[] _Data { get; set; }
        //public virtual Stream _Stream { get; set; }

        public UploadModel()
        {
            CreateTime = DateTime.UtcNow;
            Identifier = Guid.NewGuid();
        }


        public virtual string GetPath(bool includeBucket=true)
        {
            var path = UploadType + "/" + Identifier;
            if (includeBucket)
                return "Radial/"+path ;
            return path;
        }

        public class Map : ClassMap<UploadModel>
        {
            public Map()
            {
                Id(x => x.Id);
                Map(x => x.CreatedBy);
                Map(x => x.OrganizationId);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.UploadType);
                Map(x => x.OriginalName);
                Map(x => x.Identifier);
                Map(x => x.MimeType);
                Component(x => x.ForModel).ColumnPrefix("ForModel_");
            }
        }

    }
}