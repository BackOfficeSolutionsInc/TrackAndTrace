using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models;
using RadialReview.Exceptions;
using RadialReview.Properties;

namespace RadialReview.Accessors
{
    public class UrlAccessor : BaseAccessor
    {/*
        public String GetUrl(String shortened,String ipAddress)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    var urlModel=db.Get<UrlModel>(ShortenerUtility.Expand(shortened));
                    if(urlModel==null){
                        throw new PermissionsException();
                    }
                    urlModel.Hits.Add(new UrlHitModel() { IP = ipAddress, Time = DateTime.UtcNow });
                    db.SaveOrUpdate(urlModel);
                    return urlModel.Url;
                }
            }
        }

        public String RecordUrl(String relativeUrl,String emailAddress)
        {
            using (var db = HibernateSession.GetCurrentSession())
            {
                using (var tx = db.BeginTransaction())
                {
                    var urlModel = new UrlModel() { Url = relativeUrl, Email = emailAddress };
                    var map=ShortenerUtility.Shorten(urlModel.Id);
                    urlModel.Map=map;
                    db.Save(urlModel);
                    return ProductStrings.BaseUrl+"u/"+map;
                }
            }
        }*/
    }
}