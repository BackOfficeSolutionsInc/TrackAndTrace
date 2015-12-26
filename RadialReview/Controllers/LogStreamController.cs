using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using RadialReview.Utilities;
using System.Reflection;
using System.Web.Routing;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Controllers
{
    public class LogStreamController : BaseController
    {
        public enum NavType
        {
            Invalid,
            SignalR,
            Content,
            JSON,
            Partial,
            Action
        }
        public class LogLine
        {
            public DateTime Date { get; set; }
            public string sIp           { get; set; }
            public string csMethod      { get; set; }
            public string csUriStem     { get; set; }
            public string csUriQuery    { get; set; }
            public string sPort         { get; set; }
            public string csUsername    { get; set; }
            public string cIp           { get; set; }
            public string csUserAgent   { get; set; }
            public string csReferer     { get; set; }
            public string scStatus      { get; set; }
            public string scSubstatus   { get; set; }
            public string scWin32Status { get; set; }
            public int    timeTaken     { get; set; }

            public NavType NavType { get; set; }

            public static LogLine Parse(string line)
            {
                var s=line.Split(' ');
                return new LogLine
                {
                    Date = DateTime.Parse(s[0] + " " + s[1]),
                    sIp = s[2],
                    csMethod = s[3],
                    csUriStem = s[4],
                    csUriQuery = s[5],
                    sPort = s[6],
                    csUsername = s[7],
                    cIp = s[8],
                    csUserAgent = s[9],
                    csReferer = s[10],
                    scStatus = s[11],
                    scSubstatus = s[12],
                    scWin32Status = s[13],
                    timeTaken = int.Parse(s[14])
                };
            }
        }

        private static Dictionary<Tuple<string, string>, MethodInfo> PathLookup = new Dictionary<Tuple<string, string>, MethodInfo>();
        private static Dictionary<string, Tuple<ControllerDescriptor, ControllerContext>> ControllerContextLookup = new Dictionary<string, Tuple<ControllerDescriptor, ControllerContext>>();

        private Tuple<ControllerDescriptor, ControllerContext> LookupCC(string controller)
        {
            var key = controller;
            if (!ControllerContextLookup.ContainsKey(key))
            {
                ControllerContextLookup[key] = null;
                try
                {
                    var tempRequestContext = new RequestContext(Request.RequestContext.HttpContext, new RouteData());
                    var c = ControllerBuilder.Current.GetControllerFactory().CreateController(tempRequestContext, controller);
                    var controllerType = c.GetType();
                    ControllerContext cc = new ControllerContext(tempRequestContext, Activator.CreateInstance(controllerType) as ControllerBase);
                    ControllerDescriptor cd = new ReflectedControllerDescriptor(controllerType);
                    ControllerContextLookup[key] = Tuple.Create(cd, cc);
                }
                catch (Exception e)
                {
                }
            }
            return ControllerContextLookup[key];
        }

        protected MethodInfo Lookup(string controller, string action)
        {
            if (String.IsNullOrWhiteSpace(action))
                action = "Index";

            var key = Tuple.Create(controller, action);
            if (!PathLookup.ContainsKey(key)){
                 PathLookup[key]=null;
                var cc = LookupCC(controller);
                if (cc!=null){
                    var  controllerDescriptor = cc.Item1;
                    var controllerContext = cc.Item2;
                    ActionDescriptor actionDescriptor = controllerDescriptor.FindAction(controllerContext, action);
                    if (actionDescriptor != null){
                        PathLookup[key] = (actionDescriptor as ReflectedActionDescriptor).MethodInfo;
                    }
                }
               
            }
            return PathLookup[key];
        }
        private NavType GetNavType(LogLine ll)
        {

                try
                {
                    var method = ll.csUriStem.Replace("RadialReview_deploy/", "").TrimStart('/').Split(new[] { '/' });

                    var controller = method[0];
                    if (String.IsNullOrWhiteSpace(method[0])){
                        controller = "Home";
                    }
                    if (controller == "signalr")
                        return NavType.SignalR;
                    if (controller == "Scripts")
                        return NavType.Content;
                    if (controller == "Bundles")
                        return NavType.Content;

                    var action = "Index";
                    if (method.Length > 1)
                        action = method[1];
                    if (action.IndexOf('.') > -1)
                        return NavType.Content;
                    var lu = Lookup(controller, action);
                    if (lu == null || lu.ReturnType==null)
                        return NavType.Invalid;
                    var returnType = lu.ReturnType;

                    if (typeof(JsonResult).IsAssignableFrom(returnType))
                        return NavType.JSON;
                    if (typeof(PartialViewResult).IsAssignableFrom(returnType))
                        return NavType.Partial;
                    if (typeof(ActionResult).IsAssignableFrom(returnType))
                        return NavType.Action;
                }
                catch (Exception e)
                {
                    var o = false;
                }
                return NavType.Invalid;
            
        }
        public class LogLineActionGroup
        {
            public List<LogLine> Lines { get; set; }
            public LogLine Parent { get; set; }
            public LogLineActionGroup()
            {
                Lines = new List<LogLine>();
            }
        }

        public class LogLineUserGroups {
            public List<LogLineActionGroup> Actions {get;set;}
            public LogLineUserGroups()
            {
                Actions = new List<LogLineActionGroup>();
            }
        }

        

        private List<LogLine> GetLines()
        {
            var directory = Config.GetAccessLogDir();
            var file = Directory.EnumerateFiles(directory).Where(x => x.EndsWith("log")).OrderBy(x => x).LastOrDefault();
            if (file == null)
                throw new Exception("No log exists");

            var text = FileUtilities.WriteSafeReadAllLines(file).Skip(4);
            var o = new List<LogLine>();
            foreach (var line in text)
            {
                try { 
                    var ll = LogLine.Parse(line);
                    ll.NavType = GetNavType(ll);
                    o.Add(ll);
                }
                catch (Exception e)
                {
                    var f = false;
                }
            }
            return o;
        }


        private List<LogLineUserGroups> GetGroups(int minutes)
        {

            var lines = GetLines().Where(x=>x.Date>DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(minutes)));
            var userGroupLines = lines.OrderByDescending(x=>x.Date).GroupBy(x => x.csUsername);
            var o = new List<LogLineUserGroups>();
            foreach (var ugl in userGroupLines)
            {
                var llug = new LogLineUserGroups();
                var lg = new LogLineActionGroup();
                foreach (var line in ugl)
                {
                    if (line.NavType == NavType.Action)
                    {
                        lg.Parent = line;
                        llug.Actions.Add(lg);
                        lg = new LogLineActionGroup();
                    }
                    else
                    {
                        lg.Lines.Add(line);
                    }
                }
                llug.Actions.Add(lg);
                o.Add(llug);
            }

            return o;
        }

        //
        // GET: /LogStream/
        [Access(AccessLevel.Radial)]
        public ActionResult Index(int minutes=120)
        {

            return View(GetGroups(minutes));
        }
	}
}