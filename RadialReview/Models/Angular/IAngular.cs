using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Models.Angular;

namespace RadialReview.Models.Angular
{
	public interface IAngular{
		
	}
	public interface IAngularItem : IAngular
	{
		long Id { get; }
		string Type { get; }
	}

	public interface IAngularUpdate : IAngular
	{
	}
}

namespace RadialReview
{
	public static class IAngularExtensions
	{
		public static string GetKey(this IAngularItem self){	return self.Type + "_" + self.Id;	}
	}

}
