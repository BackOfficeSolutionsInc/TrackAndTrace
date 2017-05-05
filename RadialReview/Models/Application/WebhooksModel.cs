using FluentNHibernate.Mapping;
using Microsoft.AspNet.WebHooks;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models
{


    public class WebhooksDetails
    {

        public virtual string Id { get; set; }
        public virtual string Email { get; set; }

        public virtual string UserId { get; set; }

        public virtual string ProtectedData { get; set; }

        [Timestamp]
        public virtual byte[] RowVer { get; set; }

        public WebhooksDetails()
        {
            Id = Guid.NewGuid().ToString();
        }
        public class Map : ClassMap<WebhooksDetails>
        {
            public Map()
            {
                Id(x => x.Id);
                Map(x => x.Email).Length(256);
                Map(x => x.UserId).Length(64);
                Map(x => x.ProtectedData);
                Map(x => x.RowVer);
            }
        }

    }
}