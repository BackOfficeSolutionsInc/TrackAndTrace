using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Models.Angular;

namespace RadialReview.Models.Angular.Base
{
	public interface IAngular{
		
	}
	public interface IAngularItem : IAngular
	{
        long Id { get; set; }
		string Type { get; }
		bool Hide { get; }
	}

	public interface IAngularUpdate : IAngular
	{
	}

	public static class IAngularExtensions
	{
		public static string GetKey(this IAngularItem self){	return self.Type + "_" + self.Id;	}
	}
}