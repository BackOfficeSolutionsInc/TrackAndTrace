using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.IdentityManagement.Model;
using NHibernate;
using RadialReview.Engines;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Accessors
{
	public class FastReviewQueries
	{


		public class UserReviewRoleValues
		{
			public long UserId { get; set; }
			public Ratio GetIt { get; set; }
			public Ratio WantIt { get; set; }
			public Ratio HasCapacity { get; set; }
			public long ValuePositive { get; set; }
			public long ValueNeutral { get; set; }
			public long ValueNegative { get; set; }

			public UserReviewRoleValues(long userId)
			{
				GetIt = new Ratio();
				WantIt = new Ratio();
				HasCapacity = new Ratio();
				UserId = userId;
			}
		}

		public class ReviewIncomplete
		{
			public long reviewId { get; set; }
			public long numberIncomplete { get; set; }
		}

		public static List<ReviewIncomplete> AnyUnansweredReviewQuestions(ISession s, IEnumerable<long> reviewIds)
		{
			s.Flush();

			var query =
@"select
	a.ForReviewId,
	count(*)
from AnswerModel as a 
Where a.ForReviewId in (:reviewIds) and Required=true and Complete=false and DeleteTime is NULL
group by a.ForReviewId";

			var result = s.CreateSQLQuery(query).SetParameterList("reviewIds", reviewIds).List<object[]>();

			var output = reviewIds.Select(x => new ReviewIncomplete{reviewId = (long) x, numberIncomplete = result.SingleOrDefault(y=>(long)y[0]==x).NotNull(y=>(long)y[1])}).ToList();
			//var o = result.Select(x => new ReviewIncomplete { reviewId = (long)x[0], numberIncomplete = (long)x[1] }).ToList();
			return output;
		}


		public static List<UserReviewRoleValues> GetAllRoleValues(ISession s, long reviewContainerId)
		{
			var roleQuery =
@"select
	a.AboutUserId,
	SUM(if(g.GetIt = 'Always' or g.GetIt = 'True', 1, 0)) as GetAlways,
	SUM(if(g.GetIt = 'Mostly', 1, 0)) as GetMostly,
	SUM(if(g.GetIt = 'Rarely', 1, 0)) as GetRarely,
	SUM(if(g.GetIt = 'Never' or g.GetIt = 'False', 1, 0)) as GetNever,
	SUM(if(g.GetIt <> 'Indeterminate', 1, 0)) as GetTotal,
	SUM(if(g.WantIt = 'Always' or g.WantIt = 'True', 1, 0)) as WantAlways,
	SUM(if(g.WantIt = 'Mostly', 1, 0)) as WantMostly,
	SUM(if(g.WantIt = 'Rarely', 1, 0)) as WantRarely,
	SUM(if(g.WantIt = 'Never' or g.WantIt = 'False', 1, 0)) as WantNever,
	SUM(if(g.WantIt <> 'Indeterminate', 1, 0)) as WantTotal,
	SUM(if(g.HasCapacity = 'Always' or g.HasCapacity = 'True', 1, 0)) as HasCapacityAlways,
	SUM(if(g.HasCapacity = 'Mostly', 1, 0)) as HasCapacityMostly,
	SUM(if(g.HasCapacity = 'Rarely', 1, 0)) as HasCapacityRarely,
	SUM(if(g.HasCapacity = 'Never' or g.HasCapacity = 'False', 1, 0)) as HasCapacityNever,
	SUM(if(g.HasCapacity <> 'Indeterminate', 1, 0)) as HasCapacityTotal,
	count(*)
from GetWantCapacityAnswer as g 
	Inner Join AnswerModel as a On a.Id = g.AnswerModel_id
Where a.ForReviewContainerId = (:reviewsId)
group by a.AboutUserId";
			
			var valueQuery = 
@"select
	a.AboutUserId,
	SUM(if(g.Exhibits = 'Positive', 1, 0)) as Positive,
	SUM(if(g.Exhibits = 'Neutral', 1, 0)) as Neutral,
	SUM(if(g.Exhibits = 'Negative', 1, 0)) as Negative,
	count(*)
from CompanyValueAnswer as g 
	Inner Join AnswerModel as a On a.Id = g.AnswerModel_id
Where a.ForReviewContainerId = (:reviewsId)
group by a.AboutUserId";


			var roleData = s.CreateSQLQuery(roleQuery).SetInt64("reviewsId", reviewContainerId).List<Object[]>();
			var valueData = s.CreateSQLQuery(valueQuery).SetInt64("reviewsId", reviewContainerId).List<Object[]>();
			var allData = new DefaultDictionary<long, UserReviewRoleValues>(x => new UserReviewRoleValues(x));
			foreach (var d in roleData){

				var x = allData[(long)d[0]];

				var getAlways = (decimal)d[1] * FiveState.Always.Score();
				var getMostly = (decimal)d[2] * FiveState.Mostly.Score();
				var getRarely = (decimal)d[3] * FiveState.Rarely.Score();
				var getNever = (decimal)d[4] * FiveState.Never.Score();
				var getCount = (decimal)d[5];
				x.GetIt.Add((decimal)getAlways + getMostly + getRarely + getNever, (decimal)getCount);


				var WantAlways = (decimal)d[6] * FiveState.Always.Score();
				var WantMostly = (decimal)d[7] * FiveState.Mostly.Score();
				var WantRarely = (decimal)d[8] * FiveState.Rarely.Score();
				var WantNever = (decimal)d[9] * FiveState.Never.Score();
				var WantCount = (decimal)d[10];
				x.WantIt.Add((decimal)WantAlways + WantMostly + WantRarely + WantNever, (decimal)WantCount);

				var HasCapacityAlways = (decimal)d[11] * FiveState.Always.Score();
				var HasCapacityMostly = (decimal)d[12] * FiveState.Mostly.Score();
				var HasCapacityRarely = (decimal)d[13] * FiveState.Rarely.Score();
				var HasCapacityNever = (decimal)d[14] * FiveState.Never.Score();
				var HasCapacityCount = (decimal)d[15];
				x.HasCapacity.Add((decimal)HasCapacityAlways + HasCapacityMostly + HasCapacityRarely + HasCapacityNever, (decimal)HasCapacityCount);



				//x.WantIt.Add((decimal)d[3], (decimal)d[4]);
				//x.HasCapacity.Add((decimal)d[5], (decimal)d[6]);
			}

			foreach (var d in valueData)
			{
				var x = allData[(long)d[0]];
				x.ValuePositive += Convert.ToInt64(d[1]);
				x.ValueNeutral += Convert.ToInt64(d[2]);
				x.ValueNegative += Convert.ToInt64(d[3]);
			}

			return allData.Backing.Values.ToList();
			/*
					select
						a.AboutUserId,
						SUM(if(g.GetIt = 'True', 1, 0)) as GetTrue,
						SUM(if(g.GetIt <> 'Indeterminate', 1, 0)) as GetTotal,
						SUM(if(g.WantIt = 'True', 1, 0)) as WantTrue,
						SUM(if(g.WantIt <> 'Indeterminate', 1, 0)) as WantTotal,
						SUM(if(g.HasCapacity = 'True', 1, 0)) as CapacityTrue,
						SUM(if(g.HasCapacity <> 'Indeterminate', 1, 0)) as CapacityTotal,
						count(*) 

					from GetWantCapacityAnswer g 
						Inner Join AnswerModel a On a.Id = g.AnswerModel_id
					Where a.ForReviewContainerId = 94
					group by a.AboutUserId
				 */
		}
	}
}