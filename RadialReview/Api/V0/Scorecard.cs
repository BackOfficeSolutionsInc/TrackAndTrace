using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using RadialReview.Accessors;
using RadialReview.Models.Application;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Api.V0
{
	#region DO NOT EDIT, V0
	[RoutePrefix("api/v0")]
	public class WeekController : BaseApiController
	{
		/// <summary>
		/// Get the current week
		/// </summary>
		/// <returns></returns>
		// GET: api/Scores/5
		[Route("week/current")]
		public L10MeetingVM.WeekVM Get()
		{
			var org = GetUser().Organization;
			var now = DateTime.UtcNow;
			var periods = TimingUtility.GetPeriods(org, now, now, /*null,*/ true, true);


			return periods.FirstOrDefault(x => x.IsCurrentWeek);
		}
	}


	[RoutePrefix("api/v0")]
    public class ScoresController : BaseApiController
    {

		/// <summary>
		/// Get a particular score
		/// </summary>
		/// <param name="SCORE_ID"></param>
		/// <returns></returns>
		// GET: api/Scores/5
		[Route("scores/{SCORE_ID}")]
		[HttpGet]
        public ScoreModel.DataContract Get(long SCORE_ID){
	        return new ScoreModel.DataContract(ScorecardAccessor.GetScore(GetUser(), SCORE_ID));
        }

		/// <summary>
		/// Update a score for a particular measureable
		/// </summary>
		/// <param name="MEASURABLE_ID">Measurable ID</param>
		/// <param name="WEEK_ID">Week ID</param>
		/// <param name="value">Value for the score</param>
		// PUT: api/Scores/5
		[HttpPut]
		[Route("scores/{MEASURABLE_ID}/{WEEK_ID}")]
		public void Put_OLD(long MEASURABLE_ID, long WEEK_ID, [FromBody]decimal? value)
		{
			L10Accessor.UpdateScore(GetUser(), MEASURABLE_ID, WEEK_ID, value, null,true);
        }

		/// <summary>
		/// Update a score for a particular measureable
		/// </summary>
		/// <param name="MEASURABLE_ID">Measurable ID</param>
		/// <param name="WEEK_ID">Week ID</param>
		/// <param name="value">Value for the score</param>
		// PUT: api/Scores/5
		[HttpPut]
		[Route("scores/{MEASURABLE_ID}/week/{WEEK_ID}")]
		public void Put(long MEASURABLE_ID, long WEEK_ID, [FromBody]decimal? value) {
			L10Accessor.UpdateScore(GetUser(), MEASURABLE_ID, WEEK_ID, value, null, true);
		}

		/// <summary>
		/// Update a score
		/// </summary>
		/// <param name="SCORE_ID">Score ID</param>
		/// <param name="value">Value for the score</param>
		[HttpPut]
		[Route("scores/{SCORE_ID:long}")]
		public void Put(long SCORE_ID, [FromBody]decimal? value)
		{
			L10Accessor.UpdateScore(GetUser(), SCORE_ID, value, null, true);
		}

		//// GET: api/Scores
		//public IEnumerable<ScoreModel> Get()
		//{
		//	return new ScoreModel[] { new ScoreModel(){
		//		Id = 1,
		//		AccountableUserId = 100,
		//		MeasurableId = 200
		//	}, new ScoreModel(){
		//		Id = 2,
		//		AccountableUserId = 100,
		//		MeasurableId = 201
		//	},  };
		//}
        /*// POST: api/Scores
        public void Post([FromBody]string value){
        }*/    /*// DELETE: api/Scores/5
        public void Delete(int id)
        {
        }*/
    }
	[RoutePrefix("api/v0")]
	public class MeasurablesController : BaseApiController
	{
		
		[Route("measurables/mine")]
		public IEnumerable<MeasurableModel> GetMineMeasureables(){
			return ScorecardAccessor.GetUserMeasurables(GetUser(), GetUser().Id, true, true);
		}

		
		[Route("measurables/organization")]
		public IEnumerable<MeasurableModel> GetOrganizationMeasureables(){
			return ScorecardAccessor.GetOrganizationMeasurables(GetUser(), GetUser().Organization.Id, true);
		}
		
		[Route("measurables/owner/{id}")]
		public IEnumerable<MeasurableModel> GetOwnerMeasureables(long id) {
			return ScorecardAccessor.GetUserMeasurables(GetUser(), id, true, true);
		}

		[Route("measurables/user/{id}")]
		public IEnumerable<MeasurableModel> GetUserMeasureables(long id) {
			return ScorecardAccessor.GetUserMeasurables(GetUser(), id, true, true);
		}
		
		[Route("measurables/{id}/scores")]
		public IEnumerable<ScoreModel> GetMeasurableScores(long id){
			return ScorecardAccessor.GetMeasurableScores(GetUser(), id).OrderBy(x=>x.DataContract_ForWeek);
		}
		
		[Route("measurables/{id:long}")]
		public object Get(long id)
		{
			var found = ScorecardAccessor.GetMeasurable(GetUser(), id);
			if (found==null)
				throw new HttpResponseException(HttpStatusCode.BadRequest);
			return found;
		}



		/*public void Put(int id, [FromBody]decimal value){			

		}*/


	}

	#endregion
}
