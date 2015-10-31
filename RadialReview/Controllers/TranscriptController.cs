using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Json;

namespace RadialReview.Controllers
{
    public class TranscriptController : BaseController
    {
        // GET: Transcript
		[Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
            return View();
        }

	    public class TranscriptVM
	    {
		    public string Text { get; set; }
			public long? MeetingId { get; set; }
			public long? RecurrenceId { get; set; }
			public string ConnectionId { get; set; }
	    }

	    [Access(AccessLevel.UserOrganization)]
	    [HttpPost]
	    public JsonResult Add(TranscriptVM model)
	    {
		    var transcript = TranscriptAccessor.AddTranscript(GetUser(),model.Text,model.RecurrenceId,model.MeetingId,model.ConnectionId);
			return Json(ResultObject.SilentSuccess(new {date=transcript.CreateTime,id=transcript.Id}));
	    }


    }
}