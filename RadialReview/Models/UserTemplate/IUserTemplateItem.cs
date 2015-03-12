using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;

namespace RadialReview.Models.UserTemplate
{
	public interface IUserTemplateItem
	{
		long TemplateId { get; }
	}
}
