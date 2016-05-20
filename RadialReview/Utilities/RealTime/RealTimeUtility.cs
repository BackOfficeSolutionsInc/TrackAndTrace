using log4net;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.RealTime {
    public partial class RealTimeUtility : IDisposable{

        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected Dictionary<string, AngularUpdate> _updaters = new Dictionary<string, AngularUpdate>();
        protected Dictionary<string, dynamic> _groups = new Dictionary<string, dynamic>();

        protected List<Action> _actions = new List<Action>();
        protected bool Executed = false;
        protected bool SkipExecution = false;
        protected string SkipUser = null;
        private RealTimeUtility(){}

        private RealTimeUtility(string skipUser,bool shouldExecute)
        {
            // TODO: Complete member initialization
            SkipExecution = !shouldExecute;
            SkipUser = skipUser;
        }

        public static RealTimeUtility Create()
        {
            return new RealTimeUtility(null, true);
        }
        public static RealTimeUtility Create(bool shouldExecute = true)
        {
            return new RealTimeUtility(null, shouldExecute);
        }
        public static RealTimeUtility Create(string skipUser=null,bool shouldExecute=true){
            return new RealTimeUtility(skipUser,shouldExecute);
        }

        public void DoNotExecute()
        {
            if (Executed)
                throw new PermissionsException("Already executed.");
            SkipExecution = true;
        }

        public bool Execute()
        {
            if (SkipExecution)
                return false;

            if (Executed)
                throw new PermissionsException("Cannot execute again.");
            Executed = true;
            _actions.ForEach(f => {
                try {
                    f();
                } catch (Exception e) {
                    log.Error("RealTime exception", e);
                }
            });
            foreach (var b in _updaters) {
                try {
                    var group = _groups[b.Key];
                    var angularUpdate = b.Value;
                    group.update(angularUpdate);
                } catch (Exception e) {
                    log.Error("SignalR exception", e);
                }
            }
            return true;
        }
        protected AngularUpdate GetUpdater<HUB>(string name) where HUB : IHub
        {
            if (_updaters.ContainsKey(name))
                return _updaters[name];

            GetGroup<HUB>(name);
            var updater = new AngularUpdate();
            _updaters[name] = updater;
            return updater;

        }
        public RTRecurrenceUpdater UpdateRecurrences(IEnumerable<long> recurrences){
            return UpdateRecurrences(recurrences.ToArray());
        }

        public RTRecurrenceUpdater UpdateRecurrences(params long[] recurrences){
            return new RTRecurrenceUpdater(recurrences, this);
        }

        protected void AddAction(Action a)
        {
            _actions.Add(a);
        }

        protected dynamic GetGroup<HUB>(string name) where HUB : IHub
        {
            if (_groups.ContainsKey(name))
                return _groups[name];
            var hub = GlobalHost.ConnectionManager.GetHubContext<HUB>();
            var group = hub.Clients.Group(name,SkipUser);
            _groups[name] = group;
            return group;
        }
        
        public void Dispose()
        {
            if (!Executed)
                Execute();
        }
    }
}