using RadialReview.Models.Interfaces;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Microsoft.AspNet.Identity;
using FluentNHibernate.Mapping;
using RadialReview.Models;

namespace RadialReview.Controllers
{
    public class ConsoleController : BaseController
    {
        public class ConsoleLog :ILongIdentifiable
        {
            public virtual long Id { get; set; }
            public virtual string Type { get; set; }
            [AllowHtml]
            public virtual string Message { get; set; }
            public virtual DateTime CreateTime { get; set; }
            public virtual string UserId { get; set; }
            public ConsoleLog()
            {
                CreateTime = DateTime.UtcNow;
            }
            public class ConsoleLogMap : ClassMap<ConsoleLog>
            {
                public ConsoleLogMap()
                {
                    Id(x => x.Id);
                    Map(x => x.Type);
                    Map(x => x.Message);
                    Map(x => x.CreateTime);
                    Map(x => x.UserId);
                }
            }
        }
           // GET: /Console/
        [Access(AccessLevel.Radial)]
        public string SetLogging(long id,bool? enabled=null)
        {

            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var user = s.Get<UserOrganizationModel>(id);
                    user.User.ConsoleLog=enabled??(!user.User.ConsoleLog);
                    s.Update(user.User);

                    tx.Commit();
                    s.Flush();
                    return "Set ConsoleLogging="+user.User.ConsoleLog+" for :"+user.User.Id+" "+user.GetName();
                }
            }
        }
        //
        // GET: /Console/
        [Access(AccessLevel.Any)]
        [HttpPost, ValidateInput(false)]
        public JsonResult Log(ConsoleLog model)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    model.UserId = User.Identity.GetUserId();
                    s.Save(model);

                    tx.Commit();
                    s.Flush();
                }
            }
            return Json(ResultObject.SilentSuccess());
        }
	}
}