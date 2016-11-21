using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.DataTypes
{
	public static class CoworkerRelationships {
		
		public static TheReviewers_CoworkerRelationships Create(Reviewer reviewer) {
			return new TheReviewers_CoworkerRelationships(reviewer);
		}
		public static TheReviewees_CoworkerRelationships Create(Reviewee reviewer) {
			return new TheReviewees_CoworkerRelationships(reviewer);
		}
		public class CoworkerRelationships_Coworkers<COWORKER> : IEnumerable<KeyValuePair<COWORKER, List<AboutType>>> {
			protected Multimap<COWORKER, AboutType> Relationships { get; set; }
			public CoworkerRelationships_Coworkers() {
				Relationships = new Multimap<COWORKER, AboutType>();
			}
			public void Add(COWORKER coworker, AboutType relationship) {
				Relationships.Add(coworker, relationship);
			}
			public IEnumerator<KeyValuePair<COWORKER, List<AboutType>>> GetEnumerator() {
				return Relationships.GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator() {
				return Relationships.GetEnumerator();
			}
		}
	}

	//public class CoworkerRelationshipLookup {
	//	protected Dictionary<Reviewer, Reviewers_CoworkerRelationships> Reviewer_Reviewee { get; set; }
	//	public CoworkerRelationshipLookup(IEnumerable<Reviewers_CoworkerRelationships> reviewer_reviewee) {
	//		Reviewer_Reviewee = reviewer_reviewee.ToList();
	//		Reviewee_Reviewer = reviewer_reviewee.
	//	}
	//}

	/// <summary>
	/// Who are the Reviewers for this Reviewee?
	/// </summary>
	public class TheReviewees_CoworkerRelationships : CoworkerRelationships.CoworkerRelationships_Coworkers<Reviewer> {
		public Reviewee Reviewee { get; set; }
		public TheReviewees_CoworkerRelationships(Reviewee reviewee) {
			Reviewee = reviewee;
		}
	}
	/// <summary>
	/// Who are the Reviewees for this Reviewer?
	/// </summary>
	public class TheReviewers_CoworkerRelationships : CoworkerRelationships.CoworkerRelationships_Coworkers<Reviewee> {
		public Reviewer Reviewer { get; set; }
		public TheReviewers_CoworkerRelationships(Reviewer reviewer) {
			Reviewer = reviewer;
		}

	}


	
	
}