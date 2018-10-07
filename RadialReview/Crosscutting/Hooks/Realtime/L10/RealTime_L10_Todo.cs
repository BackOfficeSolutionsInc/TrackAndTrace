using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using RadialReview.Hubs;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Dashboard;
using RadialReview.Models;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Dashboard;

namespace RadialReview.Hooks.Realtime.L10 {
    public class RealTime_L10_Todo : ITodoHook, IMeetingTodoHook {
        public bool CanRunRemotely() {
            return false;
        }

        public HookPriority GetHookPriority() {
            return HookPriority.UI;
        }
        //[Untested("var updates = new AngularRecurrence(todo.ForRecurrenceId) ??? recurrentId ???")]
        public async Task CreateTodo(ISession s, TodoModel todo) {

            if (todo.TodoType == TodoType.Personal) {
                var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
                var userMeetingHub = hub.Clients.Group(RealTimeHub.Keys.UserId(todo.AccountableUserId));
                var todoData = TodoData.FromTodo(todo);
                userMeetingHub.appendTodo(".todo-list", todoData);

                RealTimeHelpers.GetUserHubForRecurrence(todo.AccountableUserId).update(new AngularUpdate() {
                    new ListDataVM(todo.AccountableUserId) {
                        Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo))
                    }
                });
            }

            //if (todo.ForRecurrenceId > 0) {
            //    var recurrenceId = todo.ForRecurrenceId.Value;
            //    var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
            //    var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
            //    var todoData = TodoData.FromTodo(todo);

            //    if (todo.CreatedDuringMeetingId != null)
            //        todoData.isNew = true;
            //    meetingHub.appendTodo(".todo-list", todoData);

            //    var message = "Created to-do.";
            //    try {
            //        message = todo.CreatedBy.GetFirstName() + " created a to-do.";
            //    } catch (Exception) {
            //    }

            //    meetingHub.showAlert(message, 1500);

            //    var updates = new AngularRecurrence(recurrenceId);
            //    updates.Todos = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(todo));
            //    meetingHub.update(new AngularUpdate() {
            //        updates
            //    });

            //    if (RealTimeHelpers.GetConnectionString() != null) {
            //        var me = hub.Clients.Client(RealTimeHelpers.GetConnectionString());
            //        me.update(new AngularUpdate() { new AngularRecurrence(recurrenceId) {
            //                Focus = "[data-todo='" + todo.Id + "'] input:visible:first"
            //            }
            //        });
            //    }
            //}
        }

        public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates) {

            var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
            List<dynamic> groups = new List<dynamic>();
            if (todo.TodoType == TodoType.Recurrence) {
                groups.Add(hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(todo.ForRecurrenceId.Value), RealTimeHelpers.GetConnectionString()));
            }// else if (todo.TodoType == TodoType.Personal) {
            groups.Add(hub.Clients.Group(RealTimeHub.Keys.UserId(todo.AccountableUserId), RealTimeHelpers.GetConnectionString()));
            //} else {
            //	throw new NotImplementedException("unhandled TodoType");
            //


            bool IsTodoUpdate = false;
            if (updates.MessageChanged) {
                groups.ForEach(g => g.updateTodoMessage(todo.Id, todo.Message));
            }
            //if (updates.DetailsChanged) {
            //	group.updateTodoDetails(todoId, details);
            //}
            if (updates.DueDateChanged) {
                groups.ForEach(g => g.updateTodoDueDate(todo.Id, todo.DueDate));
            }
            if (updates.AccountableUserChanged) {
                groups.ForEach(g => g.updateTodoAccountableUser(todo.Id, todo.AccountableUserId, todo.AccountableUser.GetName(), todo.AccountableUser.ImageUrl(true, ImageSize._32)));
            }

            if (updates.CompletionChanged) {
                if (todo.CompleteTime != null) {
                    new Cache().InvalidateForUser(todo.AccountableUser, CacheKeys.UNSTARTED_TASKS);
                } else if (todo.CompleteTime == null) {
                    new Cache().InvalidateForUser(todo.AccountableUser, CacheKeys.UNSTARTED_TASKS);
                }
                groups.ForEach(g => g.updateTodoCompletion(todo.Id, todo.CompleteTime != null));

                //Re-add
                if (todo.CompleteTime == null && todo.ForRecurrenceId > 0) {
                    groups.ForEach(g => g.update(new AngularRecurrence(todo.ForRecurrenceId.Value) {
                        Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo) {
                            CompleteTime = Removed.Date()
                        })
                    }));
                    var userGroup = hub.Clients.Group(RealTimeHub.Keys.UserId(todo.AccountableUserId), RealTimeHelpers.GetConnectionString());
                    userGroup.update(new ListDataVM(todo.AccountableUserId) {
                        Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo) {
                            CompleteTime = Removed.Date()
                        })
                    });
                }
            }

            //_ProcessDeleted(s, todo, delete);

            groups.ForEach(g => g.update(new AngularUpdate() { new AngularTodo(todo) }));
        }

        public async Task AttachTodo(ISession s, UserOrganizationModel caller, TodoModel todo) {

            if (todo.TodoType == TodoType.Personal) {
                var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
                var userMeetingHub = hub.Clients.Group(RealTimeHub.Keys.UserId(todo.AccountableUserId));
                var todoData = TodoData.FromTodo(todo);
                userMeetingHub.appendTodo(".todo-list", todoData);

                RealTimeHelpers.GetUserHubForRecurrence(todo.AccountableUserId).update(new AngularUpdate() {
                    new ListDataVM(todo.AccountableUserId) {
                        Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo))
                    }
                });
            } else {
                var recurrenceId = todo.ForRecurrenceId.Value;
                var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();
                var meetingHub = hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));
                var todoData = TodoData.FromTodo(todo);

                if (todo.CreatedDuringMeetingId != null)
                    todoData.isNew = true;
                meetingHub.appendTodo(".todo-list", todoData);

                var message = "Created to-do.";
                try {
                    message = todo.CreatedBy.GetFirstName() + " created a to-do.";
                } catch (Exception) {
                }

                meetingHub.showAlert(message, 1500);

                var updates = new AngularRecurrence(recurrenceId);
                updates.Todos = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularTodo(todo));
                meetingHub.update(new AngularUpdate() {
                    updates
                });

                if (RealTimeHelpers.GetConnectionString() != null) {
                    var me = hub.Clients.Client(RealTimeHelpers.GetConnectionString());
                    me.update(new AngularUpdate() { new AngularRecurrence(recurrenceId) {
                            Focus = "[data-todo='" + todo.Id + "'] input:visible:first"
                        }
                    });
                }
            }
        }

        public async Task DetachTodo(ISession s, UserOrganizationModel caller, TodoModel todo) {

            var hub = GlobalHost.ConnectionManager.GetHubContext<RealTimeHub>();

            List<dynamic> groups = new List<dynamic>();
            if (todo.TodoType == TodoType.Recurrence) {
                groups.Add(hub.Clients.Group(RealTimeHub.Keys.GenerateMeetingGroupId(todo.ForRecurrenceId.Value), RealTimeHelpers.GetConnectionString()));
            }
            groups.Add(hub.Clients.Group(RealTimeHub.Keys.UserId(todo.AccountableUserId), RealTimeHelpers.GetConnectionString()));

            if (todo.ForRecurrenceId != null) {
                groups.ForEach(g => g.update(new AngularRecurrence(todo.ForRecurrenceId.Value) {
                    Todos = AngularList.CreateFrom(AngularListType.Remove, new AngularTodo(todo.Id))
                }));
            } else {
                groups.ForEach(g => g.update(AngularList.CreateFrom(AngularListType.Remove, new AngularTodo(todo.Id))));
            }
        }
    }
}