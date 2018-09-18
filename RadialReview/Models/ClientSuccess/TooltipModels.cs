using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Models.ClientSuccess {


    public class TooltipTemplate {
        public virtual long Id { get; set; }
        public virtual string Note { get; set; }
        public virtual string Title { get; set; }
        [AllowHtml]
        public virtual string HtmlBody { get; set; }
        public virtual string UrlSelector { get; set; }
        public virtual string Selector { get; set; }
        public virtual DateTime CreateTime { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual bool IsEnabled { get; set; }

        public TooltipTemplate() {
            CreateTime = DateTime.UtcNow;
            DeleteTime = DateTime.UtcNow.AddDays(14);
            IsEnabled = true;
            Selector = "#main";

        }

        public class Map : ClassMap<TooltipTemplate> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.CreateTime);
                Map(x => x.DeleteTime);
                Map(x => x.Note);
                Map(x => x.Title);
                Map(x => x.HtmlBody);
                Map(x => x.Selector);
                Map(x => x.UrlSelector);
                Map(x => x.IsEnabled);
            }
        }
    }

    public class TooltipViewModel {
        public TooltipViewModel() {
        }
        public TooltipViewModel(TooltipTemplate x) {
            TooltipId = x.Id;
            Title = x.Title;
            HtmlBody = x.HtmlBody;
            Selector = x.Selector;
        }
        public long TooltipId { get; set; }
        public string Title { get; set; }
        public string HtmlBody { get; set; }
        public string Selector { get; set; }
    }

    public class TooltipSeen {
        public virtual long Id { get; set; }
        public virtual long TipId { get; set; }
        public virtual DateTime SeenTime { get; set; }
        public virtual string UserId { get; set; }
        public TooltipSeen() {
            SeenTime = DateTime.UtcNow;
        }
        public class Map : ClassMap<TooltipSeen> {
            public Map() {
                Id(x => x.Id);
                Map(x => x.TipId);
                Map(x => x.SeenTime);
                Map(x => x.UserId).Index("idx_TooltipSeen_UserId").Length(256);
            }
        }
    }
}