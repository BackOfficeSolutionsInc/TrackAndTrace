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
}
