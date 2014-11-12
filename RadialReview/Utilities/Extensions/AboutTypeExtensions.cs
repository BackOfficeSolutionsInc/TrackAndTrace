using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class AboutTypeExtensions
	{
		public static AboutType Invert(this AboutType self)
		{
			AboutType build = AboutType.NoRelationship;
			foreach (AboutType flag in self.GetFlags())
			{
				switch (flag)
				{
					case AboutType.Manager: build = build | AboutType.Subordinate; break;
					case AboutType.NoRelationship: build = build | AboutType.NoRelationship; break;
					case AboutType.Peer: build = build | AboutType.Peer; break;
					case AboutType.Self: build = build | AboutType.Self; break;
					case AboutType.Subordinate: build = build | AboutType.Manager; break;
					case AboutType.Teammate: build = build | AboutType.Teammate; break;
					default:
						throw new ArgumentException("Unknown about type (" + self + ")");
				}
			}
			return build;
		}
		public static decimal Score(this PositiveNegativeNeutral self)
		{
			switch(self){
				case PositiveNegativeNeutral.Indeterminate:
					return 0;
					break;
				case PositiveNegativeNeutral.Negative:
					return 0;
					break;
				case PositiveNegativeNeutral.Neutral:
					return .5m;
					break;
				case PositiveNegativeNeutral.Positive:
					return 1;
					break;
				default:
					throw new ArgumentOutOfRangeException("self");
			}
		}
    }
}