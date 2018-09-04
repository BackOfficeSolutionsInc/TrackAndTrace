using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Event;

namespace RadialReview.Models.Interfaces
{
	public interface IHistorical : IDeletable
	{
		DateTime CreateTime { get; set; }
	}

	//public class IHistoricalImpl : IHistorical,ILongIdentifiable {
	//	public long Id { get; set; }
	//	public DateTime CreateTime { get; set; }
	//	public DateTime? DeleteTime { get; set; }
	//	public string Name { get; set; }

	//	public static IHistoricalImpl From<T>(T from,string name=null) where T : ILongIdentifiable, IHistorical {
	//		return new IHistoricalImpl {
	//			Id = from.Id,
	//			CreateTime = from.CreateTime,
	//			DeleteTime = from.DeleteTime,
	//			Name = name,
	//		};
	//	}
	//}
}
