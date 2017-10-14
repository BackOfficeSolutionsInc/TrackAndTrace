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
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using System.Threading.Tasks;

namespace RadialReview.Api.V1 {

	[RoutePrefix("api/v1")]
	[SwaggerName(Name ="Week")]
	public class Week_Controller : BaseApiController {
		/// <summary>
		/// Get the current week
		/// </summary>
		/// <returns></returns>
		// GET: api/Scores/5
		[Route("weeks/current")]
		[HttpGet]
		public AngularWeek Get() {
			var org = GetUser().Organization;
			var now = DateTime.UtcNow;
			var periods = TimingUtility.GetPeriods(org, now, now, /*null,*/ true, true);

			return new AngularWeek(periods.FirstOrDefault(x => x.IsCurrentWeek));
		}
	}


	[RoutePrefix("api/v1")]
	public class Scores_Controller : BaseApiController {
		/// <summary>
		/// Get a particular score
		/// </summary>
		/// <param name="SCORE_ID"></param>
		/// <returns></returns>
		// GET: api/Scores/5
		[Route("scores/{SCORE_ID}")]
		[HttpGet]
		public AngularScore Get(long SCORE_ID) {
			return new AngularScore(ScorecardAccessor.GetScore(GetUser(), SCORE_ID));
		}

		public class UpdateScoreModel {
			/// <summary>
			/// The score's new value. If null, score is deleted.
			/// </summary>
			public decimal? value { get; set; }
		}
		/// <summary>
		/// Update a score
		/// </summary>
		/// <param name="SCORE_ID">Score ID</param>
		[HttpPut]
		[Route("scores/{SCORE_ID:long}")]
		[Untested("Test Me")]
		public async Task Put(long SCORE_ID, [FromBody]UpdateScoreModel body) {
			await ScorecardAccessor.UpdateScore(GetUser(), SCORE_ID, body.value);
			///L10Accessor.UpdateScore(GetUser(), SCORE_ID, body.value, null, true);
		}
	}

	[RoutePrefix("api/v1")]
	public class Measurables_Controller : BaseApiController {
		/// <summary>
		/// Get measurables that you own
		/// </summary>
		/// <returns></returns>
		[Route("measurables/user/mine")]
		[HttpGet]
		public IEnumerable<AngularMeasurable> GetMineMeasureables() {
			return ScorecardAccessor.GetUserMeasurables(GetUser(), GetUser().Id, true, true)
				.Select(x => new AngularMeasurable(x));
		}

		/// <summary>
		/// Get measurables for a particular user
		/// </summary>
		/// <param name="USER_ID">User ID</param>
		/// <returns></returns>
		[Route("measurables/user/{USER_ID}")]
		[HttpGet]
		public IEnumerable<AngularMeasurable> GetUserMeasureables(long USER_ID) {
			return ScorecardAccessor.GetUserMeasurables(GetUser(), USER_ID, true, true)
				.Select(x => new AngularMeasurable(x));
		}

		/// <summary>
		/// Get scores for a particular measurable
		/// </summary>
		/// <param name="MEASURABLE_ID">Measurable ID</param>
		/// <returns></returns>
		[Route("measurables/{MEASURABLE_ID}/scores")]
		[HttpGet]
		public IEnumerable<AngularScore> GetMeasurableScores(long MEASURABLE_ID) {
			return ScorecardAccessor.GetMeasurableScores(GetUser(), MEASURABLE_ID)
									.OrderBy(x => x.DataContract_ForWeek)
									.Select(x => new AngularScore(x));
		}

		/// <summary>
		/// Get a measurable
		/// </summary>
		/// <param name="MEASURABLE_ID">Measurable ID</param>
		/// <returns></returns>
		[Route("measurables/{MEASURABLE_ID:long}")]
		[HttpGet]
		public AngularMeasurable Get(long MEASURABLE_ID) {
			var found = ScorecardAccessor.GetMeasurable(GetUser(), MEASURABLE_ID);
			if (found == null)
				throw new HttpResponseException(HttpStatusCode.BadRequest);
			return new AngularMeasurable(found);
		}

		///// <summary>
		///// Update a score for a particular measureable
		///// </summary>
		///// <param name="MEASURABLE_ID">Measurable ID</param>
		///// <param name="WEEK_ID">Week ID</param>
		//// PUT: api/Scores/5
		//[HttpPut]
		//[Obsolete("Use other")]
		//[Route("measurables/{MEASURABLE_ID}/week/{WEEK_ID}/score")]
		//public void Put_OLD(long MEASURABLE_ID, long WEEK_ID, [FromBody]Scores_Controller.UpdateScoreModel body) {
		//	L10Accessor.UpdateScore(GetUser(), MEASURABLE_ID, WEEK_ID, body.value, null, true);
		//}

		/// <summary>
		/// Update a score for a particular measureable
		/// </summary>
		/// <param name="MEASURABLE_ID">Measurable ID</param>
		/// <param name="WEEK_ID">Week ID</param>
		// PUT: api/Scores/5
		[HttpPut]
		[Route("measurables/{MEASURABLE_ID}/week/{WEEK_ID}")]
		[Untested("Test UpdateScore")]
		public async Task UpdateScore(long MEASURABLE_ID, long WEEK_ID, [FromBody]Scores_Controller.UpdateScoreModel body) {
			//await L10Accessor.UpdateScore(GetUser(), MEASURABLE_ID, WEEK_ID, body.value, null, true);
			await ScorecardAccessor.UpdateScore(GetUser(), MEASURABLE_ID, TimingUtility.GetDateSinceEpoch(WEEK_ID), body.value);

		}
	}
}
