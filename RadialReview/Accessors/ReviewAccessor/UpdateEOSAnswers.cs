using System;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities;

namespace RadialReview.Accessors
{
	public partial class ReviewAccessor
	{
		#region Update EOS Answers

		public Boolean UpdateCompanyValueAnswer(UserOrganizationModel caller, long questionId, PositiveNegativeNeutral value, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<CompanyValueAnswer>(questionId);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

					edited = false;

					if (value != answer.Exhibits)
					{
						edited = true;
						answer.Complete = (value != PositiveNegativeNeutral.Indeterminate);
						answer.Exhibits = value;
					}

					if (edited)
					{
						UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
						s.Update(answer);
						tx.Commit();
						s.Flush();
					}

					return answer.Complete || !answer.Required;
				}
			}
		}

		public bool UpdateCompanyValueReasonAnswer(UserOrganizationModel caller, long questionId, string reason, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<CompanyValueAnswer>(questionId);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

					edited = false;
					if (reason.Trim() != answer.Reason)
					{
						edited = true;
						answer.Reason = reason.Trim();
						//UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
						s.Update(answer);
						tx.Commit();
						s.Flush();
					}
					return true; // Because "Reasons" are not required
				}
			}
		}

		public Boolean UpdateGWCReasonAnswer(UserOrganizationModel caller, long questionId,string gwcType, string newReason, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<GetWantCapacityAnswer>(questionId);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

					edited = false;

					switch (gwcType)
					{
						case "GetItReason":
							if (newReason.Trim() != answer.GetItReason){
								edited = true;
								answer.GetItReason = newReason.Trim();
							}
							break;
						case "WantItReason":
							if (newReason.Trim() != answer.WantItReason){
								edited = true;
								answer.WantItReason = newReason.Trim();
							}
							break;
						case "HasCapacityReason":
							if (newReason.Trim() != answer.HasCapacityReason){
								edited = true;
								answer.HasCapacityReason = newReason.Trim();
							}
							break;
						default:
							throw new Exception("GWC Reason type unknown. (" + gwcType + ")");
					}
					if(edited){
						//DO NOT CALL.. only for required answers
						//UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
						s.Update(answer);
						tx.Commit();
						s.Flush();
					}
					return true; // Because "Reasons" are not required
				}
			}
		}

		public Boolean UpdateGWCAnswer(UserOrganizationModel caller, long questionId, string type, FiveState fivestate, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<GetWantCapacityAnswer>(questionId);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

					edited = false;
					switch (type)
					{
						case "GetIt":
							if (fivestate != answer.GetIt)
							{
								edited = true;
								answer.Complete = (fivestate != FiveState.Indeterminate);
								answer.GetIt = fivestate;
							}
							break;
						case "WantIt":
							if (fivestate != answer.WantIt)
							{
								edited = true;
								answer.Complete = (fivestate != FiveState.Indeterminate);
								answer.WantIt = fivestate;
							}
							break;
						case "HasCapacity":
							if (fivestate != answer.HasCapacity)
							{
								edited = true;
								answer.Complete = (fivestate != FiveState.Indeterminate);
								answer.HasCapacity = fivestate;
							}
							break;
						default:
							throw new Exception("GWC tristate type unknown. (" + type + ")");
					}

					if (edited)
					{
						UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
						s.Update(answer);
						tx.Commit();
						s.Flush();
					}

					return answer.Complete || !answer.Required;
				}
			}
		}

		public bool UpdateRockAnswer(UserOrganizationModel caller, long questionId, RockState finished, DateTime now, out bool edited, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<RockAnswer>(questionId);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

					edited = false;

					if (finished != answer.Completion)
					{
						edited = true;
						answer.Complete = (finished != RockState.Indeterminate);
						answer.Completion = finished;
						UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
						s.Update(answer);
						tx.Commit();
						s.Flush();
					}

					return answer.Complete || !answer.Required;
				}
			}
		}

		public bool UpdateRockReasonAnswer(UserOrganizationModel caller, long questionId, string reason, DateTime now, out bool edited, ref int qA, ref int oA)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var answer = s.Get<RockAnswer>(questionId);
					PermissionsUtility.Create(s, caller).EditReview(answer.ForReviewId);

					edited = false;

					if (reason.Trim() != answer.Reason)
					{
						edited = true;
						answer.Reason = reason.Trim();
						//DO NOT CALL.. only for required answers
						//UpdateCompletion(answer, now, ref questionsAnsweredDelta, ref optionalAnsweredDelta);
						s.Update(answer);
						tx.Commit();
						s.Flush();
					}
					return true; // Because "Reasons" are not required
				}
			}
		}

		#endregion
	}
}