using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Amazon.IdentityManagement.Model;
using NHibernate;
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
	SUM(if(g.GetIt = 'True', 1, 0)) as GetTrue,
	SUM(if(g.GetIt <> 'Indeterminate', 1, 0)) as GetTotal,
	SUM(if(g.WantIt = 'True', 1, 0)) as WantTrue,
	SUM(if(g.WantIt <> 'Indeterminate', 1, 0)) as WantTotal,
	SUM(if(g.HasCapacity = 'True', 1, 0)) as CapacityTrue,
	SUM(if(g.HasCapacity <> 'Indeterminate', 1, 0)) as CapacityTotal,
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
			foreach (var d in roleData)
			{
				var x = allData[(long)d[0]];
				x.GetIt.Add((decimal)d[1], (decimal)d[2]);
				x.WantIt.Add((decimal)d[3], (decimal)d[4]);
				x.HasCapacity.Add((decimal)d[5], (decimal)d[6]);
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