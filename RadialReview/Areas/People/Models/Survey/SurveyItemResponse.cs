using RadialReview.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Models.Survey {
    public class SurveyItemContainer : IItemContainer {
        public long Id { get; set; }

        public IItem Item { get; set; }
        public IResponse Response { get; set; }
        public IItemFormat Format { get; set; }
        

        [Obsolete("Use other constructor")]
        private SurveyItemContainer() {
        }
        public SurveyItemContainer(IItem item, IResponse response, IItemFormat format) : this() {
            Item = item;
            Response = response;
            Format = format;
        }

        public IItem GetItem() {
            return Item;
        }

        public IResponse GetResponse() {
            return Response;
        }

        public bool HasResponse() {
            return Response != null;
        }

        public string ToPrettyString() {
            return "ItemResponse: \n\t"+Item.ToPrettyString() +  Response.NotNull(x=> " \n\t" + x.ToPrettyString()) + " \n\t" + Format.ToPrettyString();
        }

        public IItemFormat GetFormat() {
            return Format;
        }

        public virtual string GetName() {
           return ToPrettyString(); 
        }
        public virtual string GetHelp() {
            return null;
        }
        public virtual int GetOrdering() {
            return Item.GetOrdering();
        }
    }
}