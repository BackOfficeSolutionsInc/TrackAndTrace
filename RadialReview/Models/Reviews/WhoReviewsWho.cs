using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;

namespace RadialReview.Models.Reviews {

	public class Reviewee {
		public long RGMId { get; set; }
		public long? ACNodeId { get; set; }
		public string _Name { get; set; }

		public OriginType Type { get; set; }

		public Reviewee(long rgmId, long? acNodeId) {
			RGMId = rgmId;
			ACNodeId = acNodeId;
			Type = OriginType.User;
		}

		public Reviewee(ResponsibilityGroupModel rgm) : this(rgm.Id,null){
			Type = rgm is OrganizationModel ? OriginType.Organization : OriginType.User;
			_Name = rgm.GetName();
		}

		public Reviewee(AccountabilityNode node) : this(new AngularAccountabilityNode(node)) { }
		public Reviewee(AngularAccountabilityNode node) {
			RGMId = node.User.Id;
			ACNodeId = node.Id;
			Type = OriginType.User;

			_Name = node.User.Name;
			if (node.Group != null && node.Group.Position != null) {
				_Name += " (" + node.Group.Position.Name+")";
			}
		}

		public Reviewee(long rgmId, long? acNodeId,string name) :this(rgmId,acNodeId){
			_Name = name;
		}

		public override bool Equals(object obj) {
			if (obj is Reviewee) {
				var o = (Reviewee)obj;
				return o.RGMId == RGMId && o.ACNodeId == ACNodeId;
			}
			return false;
		}

		public override int GetHashCode() {
			return HashUtil.Merge(RGMId.GetHashCode(), (ACNodeId ?? -2).GetHashCode());
		}

		public Reviewer ConvertToReviewer() {
			return new Reviewer(RGMId, _Name) { __ACNodeId = ACNodeId};
		}

		public static Reviewee FromId(string id) {
			//Code face is watching you code
			var split = id.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries).Select(x=>x.TryParseLong()).ToList();
			if (split.Count == 1)
				return new Reviewee(split[0].Value, null);
			return new Reviewee(split[0].Value, split[1]);
		}
		
		public string ToId() {
			if (ACNodeId == null)
				return "" + RGMId;
			return RGMId + "_" + ACNodeId;
		}

		public static bool operator ==(Reviewee x, Reviewee y) {
			if ((object)x == null)
				return (object)y == null;
			return x.Equals(y);
		}
		public static bool operator !=(Reviewee x, Reviewee y) {
			return !(x == y);
		}

		public override string ToString() {
			var builder = "";
			if (_Name != null)
				builder+= "\"" + _Name + "\"=";
			builder+= "("+ Type +":"+ RGMId + "["+ ACNodeId + "])";
			return builder;
		}
	}

	public class Reviewer {
		internal long? __ACNodeId;

		public long RGMId { get; set; }
		public string _Name { get; set; }


		public Reviewer(long rgmId) {
			RGMId = rgmId;
		}
		public Reviewer(long rgmId,string name) {
			RGMId = rgmId;
			_Name = name;
		}
		public Reviewer(ResponsibilityGroupModel rgm) {
			RGMId = rgm.Id;
			_Name = rgm.GetName();
		}

		public Reviewer(AccountabilityNode reviewer) : this(new AngularAccountabilityNode(reviewer)) {}
		public Reviewer(AngularAccountabilityNode reviewer) : this(reviewer.User.Id) {
			_Name = reviewer.User.Name;
			__ACNodeId = reviewer.Id;
		}

		public Reviewee ConvertToReviewee() {
			return new Reviewee(RGMId, __ACNodeId, _Name);
		}

		public override bool Equals(object obj) {
			if (obj is Reviewer) {
				var o = (Reviewer)obj;
				return o.RGMId == RGMId;
			}
			return false;
		}

		public override int GetHashCode() {
			return HashUtil.Merge(RGMId.GetHashCode());
		}

		public static bool operator ==(Reviewer x, Reviewer y) {
			if (System.Object.ReferenceEquals(x, y)) {
				return true;
			}
			// If one is null, but not both, return false.
			if (((object)x == null) || ((object)y == null)) {
				return false;
			}
			return x.Equals(y);
		}
		public static bool operator !=(Reviewer x, Reviewer y) {
			return !(x == y);
		}

		public override string ToString() {
			var builder = "";
			if (_Name != null)
				builder += "\""+_Name + "\"=";
			builder += "(User:" + RGMId +")";
			return builder;
		}
	}

	public class WhoReviewsWho {
		public Reviewer Reviewer { get; set; }
		public Reviewee Reviewee { get; set;}

		public DateTime SetTime { get; set; }

		public WhoReviewsWho() {
			SetTime = DateTime.UtcNow;
		}

		public WhoReviewsWho(AccRelationship acc) {
			SetTime = DateTime.UtcNow;
			Reviewee = acc.Reviewee;
			Reviewer = acc.Reviewer;
		}

		public WhoReviewsWho(Reviewer reviewer, Reviewee reviewee) {
			Reviewer = reviewer;
			Reviewee = reviewee;
		}

		public override bool Equals(object obj) {
			if (obj is WhoReviewsWho) {
				var o = (WhoReviewsWho)obj;
				return Reviewer.Equals(o.Reviewer) && Reviewee.Equals(o.Reviewee);
			}
			return false;
		}

		public override int GetHashCode() {
			return HashUtil.Merge(Reviewer.GetHashCode(), Reviewee.GetHashCode());
		}


		public static bool operator ==(WhoReviewsWho x, WhoReviewsWho y) {
			if ((object)x == null)
				return (object)y == null;
			return x.Equals(y);
		}
		public static bool operator !=(WhoReviewsWho x, WhoReviewsWho y) {
			return !(x == y);
		}

	}

	public class AccRelationship {
		public Reviewer Reviewer { get; set; }
		public Reviewee Reviewee { get; set; }
		public AboutType _RevieweeIsThe {get; set; }

		public AboutType RevieweeIsThe { get { return _RevieweeIsThe; } set { _RevieweeIsThe = value; } }
		public AboutType ReviewerIsThe { get { return _RevieweeIsThe.Invert(); } set { _RevieweeIsThe = value.Invert(); } }

		public AccRelationship() { }
		public AccRelationship(Reviewer reviewer, Reviewee reviewee,AboutType revieweeIsThe) {
			Reviewer = reviewer;
			Reviewee = reviewee;
			RevieweeIsThe = revieweeIsThe;
		}

		public void Invert() {
			_RevieweeIsThe = _RevieweeIsThe.Invert();
			var tempReviewee = Reviewee;
			Reviewee = Reviewer.ConvertToReviewee();
			Reviewer = tempReviewee.ConvertToReviewer();
		}

		public override bool Equals(object obj) {
			if (obj is AccRelationship) {
				var o = (AccRelationship)obj;
				return Reviewer.Equals(o.Reviewer) && Reviewee.Equals(o.Reviewee) && o._RevieweeIsThe==_RevieweeIsThe;
			}
			return false;
		}

		public override int GetHashCode() {
			return HashUtil.Merge(Reviewer.GetHashCode(), Reviewee.GetHashCode(), _RevieweeIsThe.GetHashCode());
		}

		public override string ToString() {
			return "Reviewer:" + Reviewer + " - Reviewee("+_RevieweeIsThe+"):" + Reviewee;
		}


		public static bool operator ==(AccRelationship x, AccRelationship y) {
			if ((object)x == null)
				return (object)y == null;
			return x.Equals(y);
		}
		public static bool operator !=(AccRelationship x, AccRelationship y) {
			return !(x == y);
		}

	}

	public class RelationshipCollection : IEnumerable<AccRelationship> {
		private List<AccRelationship> _relationships { get; set; }

		public RelationshipCollection(IEnumerable<AccRelationship> existing) {
			_relationships = (existing ?? new List<AccRelationship>()).ToList();
		}
		public RelationshipCollection() :this(new List<AccRelationship>()){}

		public void Add(AccRelationship relationship) {
			_relationships.Add(relationship);
		}
		public void AddRange(IEnumerable<AccRelationship> relationships) {
			_relationships.AddRange(relationships);
		}

		public void AddRelationship(Reviewer reviewer, Reviewee reviewee, AboutType revieweeIsThe) {
			Add(new AccRelationship() {
				Reviewee=reviewee,
				Reviewer=reviewer,
				RevieweeIsThe = revieweeIsThe
			});
		}

		public List<WhoReviewsWho> ToWhoReviewsWho(AboutType revieweeIsThe) {
			return GetAll().Where(x => x.RevieweeIsThe == revieweeIsThe).Select(x => new WhoReviewsWho(x)).ToList();
		}

		public List<WhoReviewsWho> ToAllWhoReviewsWho() {
			return GetAll().Select(x => new WhoReviewsWho(x)).Distinct().ToList();
		}


		public List<AccRelationship> GetAll() {
			return _relationships.Distinct(x => Tuple.Create(x.Reviewee, x.Reviewer, x.RevieweeIsThe)).ToList();
		}

		public IEnumerator<AccRelationship> GetEnumerator() {
			return GetAll().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetAll().GetEnumerator();
		}

	}
}