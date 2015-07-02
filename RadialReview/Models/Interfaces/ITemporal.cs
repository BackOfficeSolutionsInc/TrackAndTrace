using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Interfaces
{
	public interface ITemporal : ILongIdentifiable, IHistorical
	{
		new long Id { get; set; }
		long? CopiedFrom { get; set; }
		bool Locked { get; set; }
	}

}

namespace RadialReview
{
	public static class TemporalExtensions
	{
		public static void TemporalSaveOrUpdate<T>(this ISession s,T o) where T : ITemporal
		{
			if (o.Id == 0){
				s.Save(o);
			}else if (o.Locked){
				o.CopiedFrom = o.Id;
				o.Id = 0;
				o.Locked = false;
				s.Save(o);
			}else{
				s.SaveOrUpdate(o);
			}
		}
	}

}
	
