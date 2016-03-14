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

        protected Dictionary<string, AngularUpdate> _updaters = new Dictionary<string, AngularUpdate>();
        protected Dictionary<string, dynamic> _groups = new Dictionary<string, dynamic>();

        protected List<Action> _actions = new List<Action>();
        protected bool Executed = false;
        private RealTimeUtility(){}

        public RealTimeUtility Create(){
            return new RealTimeUtility();
        }

        public bool Execute()
        {
            if (Executed)
                throw new PermissionsException("Cannot execute again.");
            Executed = true;
            _actions.ForEach(f => f());
            foreach (var b in _updaters) {
                var group = _groups[b.Key];
                var angularUpdate = b.Value;
                group.update(angularUpdate);
            }
            return true;
        }
        protected AngularUpdate GetUpdater<HUB>(string name) where HUB : IHub
        {
            if (_updaters.ContainsKey(name))
                return _updaters[name];

            var hub = GlobalHost.ConnectionManager.GetHubContext<HUB>();
            var group = hub.Clients.Group(name);
            _groups[name]=group;
            var updater = new AngularUpdate();
            _updaters[name] = updater;
            return updater;

        }

        public RTRecurrenceUpdater UpdateRecurrences(params IEnumerable<long> recurrences){
            return new RTRecurrenceUpdater(recurrences, this);
        }

        protected void AddAction(Action a)
        {
            _actions.Add(a);
        }
      

        public void Dispose()
        {
            if (!Executed)
                Execute();
        }
    }
}