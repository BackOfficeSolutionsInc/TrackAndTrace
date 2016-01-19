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

	[RoutePrefix("api/v0")]
	public class WeekController : BaseApiController
	{

		// GET: api/Scores/5
		[Route("week/current")]
		public L10MeetingVM.WeekVM Get()
		{
			var org = GetUser().Organization;
			var now = DateTime.UtcNow;
			var periods = TimingUtility.GetPeriods(org.Settings.WeekStart, org.GetTimezoneOffset(), now, now.AddDays(7), null, true, org.Settings.ScorecardPeriod, new YearStart(org),true);


			return periods.FirstOrDefault(x => x.IsCurrentWeek);
		}
	}


	[RoutePrefix("api/v0")]
    public class ScoresController : BaseApiController
    {

		// GET: api/Scores/5
		[Route("scores/{id}")]
		[HttpGet]
        public ScoreModel.DataContract Get(long id){
	        return new ScoreModel.DataContract(ScorecardAccessor.GetScore(GetUser(), id));
        }


		// PUT: api/Scores/5
		//[Route("api/{namespace}/{controller}/{id}")]
		[HttpPut]
		[Route("scores/{measurable}/{forweek}")]
		public void Put(long measurable,long forWeek, [FromBody]decimal? value)
		{
			L10Accessor.UpdateScore(GetUser(), measurable, forWeek, value, null,true);
        }

		[HttpPut]
		[Route("scores/{id:long}")]
		public void Put(long id, [FromBody]decimal? value)
		{
			L10Accessor.UpdateScore(GetUser(), id, value, null, true);
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
		public IEnumerable<MeasurableModel> GetOwnerMeasureables(long id)
		{
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
}
