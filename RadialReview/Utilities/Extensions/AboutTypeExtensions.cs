using RadialReview.Models.Enums;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class AboutTypeExtensions
	{

	    public static string GetBestTitle(this AboutType self)
	    {
			switch (self.GetBestAboutType())
			{
				case AboutType.NoRelationship:
					return "No Relationship";
				case AboutType.Self:
					return "Self";
				case AboutType.Subordinate:
					return ""+Config.DirectReportName()+"s";
				case AboutType.Teammate:
					goto case AboutType.NoRelationship;
					//return "Teammate";
				case AboutType.Peer:
					return "Peers";
				case AboutType.Manager:
					return ""+Config.ManagerName()+"s";
				default:
					throw new ArgumentOutOfRangeException();
			}
	    }

	    public static string GetBestShape(this AboutType self)
	    {
		    switch(self.GetBestAboutType()){
			    case AboutType.NoRelationship:
				    return "shape-circle";
			    case AboutType.Self:
				    return "shape-x";
			    case AboutType.Subordinate:
				    return "shape-diamond";
			    case AboutType.Teammate:
					goto case AboutType.NoRelationship;
				    //return "shape-plus";
				case AboutType.Peer:
					return "shape-triangle";
				case AboutType.Manager:
					return "shape-square";
			    default:
				    throw new ArgumentOutOfRangeException();
		    }
	    }
	    public static AboutType GetBestAboutType(this AboutType self)
	    {
			if (self.HasFlag(AboutType.Self))
				return AboutType.Self; 
			if (self.HasFlag(AboutType.Manager))
				return AboutType.Manager; 
			if (self.HasFlag(AboutType.Subordinate))
				return AboutType.Subordinate; 
			if (self.HasFlag(AboutType.Peer))
				return AboutType.Peer; 
			/*if (self.HasFlag(AboutType.Teammate))
				return AboutType.Teammate; */
			if (self.HasFlag(AboutType.NoRelationship))
				return AboutType.NoRelationship; 
			throw new ArgumentException("Unknown about type (" + self + ")");
	    }

		public static AboutType Invert(this AboutType self)
		{
			var build = AboutType.NoRelationship;
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
					case AboutType.Organization: build = build | AboutType.Organization; break; // The Organization Selector in GetCustomizeModel() depends on this inversion
					default:
						throw new ArgumentException("Unknown about type (" + self + ")");
				}
			}
			return build;
		}
		public static decimal Score(this PositiveNegativeNeutral self)
		{
			switch (self)
			{
				case PositiveNegativeNeutral.Indeterminate:
					return 0;
				case PositiveNegativeNeutral.Negative:
					return 0;
				case PositiveNegativeNeutral.Neutral:
					return .5m;
				case PositiveNegativeNeutral.Positive:
					return 1;
				default:
					throw new ArgumentOutOfRangeException("self");
			}
		}
		public static decimal Score2(this PositiveNegativeNeutral self)
		{
			switch (self)
			{
				case PositiveNegativeNeutral.Indeterminate:
					return 0;
				case PositiveNegativeNeutral.Negative:
					return 1;
				case PositiveNegativeNeutral.Neutral:
					return 2;
				case PositiveNegativeNeutral.Positive:
					return 3;
				default:
					throw new ArgumentOutOfRangeException("self");
			}
		}
    }
}