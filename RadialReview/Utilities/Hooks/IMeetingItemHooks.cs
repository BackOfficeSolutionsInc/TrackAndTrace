using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	
	public interface IMeetingRockHook : IHook {
		Task AttachRock(ISession s,UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock);
		Task DetachRock(ISession s, RockModel rock,long recurrenceId);
		Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock);
	}

	public interface IMeetingMeasurableHook : IHook {
		Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable);
		Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId);
	}

    public interface IMeetingTodoHook : IHook {
        Task AttachTodo(ISession s, UserOrganizationModel caller, TodoModel todo);
        Task DetachTodo(ISession s, UserOrganizationModel caller, TodoModel todo);
    }
}
