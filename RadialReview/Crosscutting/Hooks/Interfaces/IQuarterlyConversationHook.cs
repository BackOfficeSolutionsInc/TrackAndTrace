using NHibernate;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Interfaces {
	public enum QuarterlyConversationErrorType {
		CreationFailed = 1,
		EmailsFailed =2,
	}

	public interface IQuarterlyConversationHook : IHook {


		Task QuarterlyConversationCreated(ISession s, long qcId);
		Task QuarterlyConversationEmailsSent(ISession s, long qcId);
		Task QuarterlyConversationError(ISession s, IForModel creator, QuarterlyConversationErrorType failureType, List<string> errors);

	}
}
