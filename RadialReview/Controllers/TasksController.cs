using RadialReview.Models.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class TaskVM
    {
        public List<TaskModel> Tasks { get; set; }

        public String GetUrl(TaskModel task)
        {
            switch (task.Type)
            {
                case RadialReview.Models.Enums.TaskType.Review:     return "/Review/Take/" + task.Id;
                case RadialReview.Models.Enums.TaskType.Prereview:  return "/Prereview/Customize/" + task.Id;
                //case RadialReview.Models.Enums.TaskType.Profile:   <Not implemented>
                default: throw new ArgumentOutOfRangeException("TaskType is unknown (" + task.Type + ")");
            }
        }
    }

    public class TasksController : BaseController
    {
        //
        // GET: /Tasks/
        [Access(AccessLevel.Any)]
        public ActionResult Index()
        {
            var tasks=_TaskAccessor.GetTasksForUser(GetUser(), GetUser().Id);
            var model = new TaskVM()
            {
                Tasks=tasks
            };
            return View(model);
        }
	}
}