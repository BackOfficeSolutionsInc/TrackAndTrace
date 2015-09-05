using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using FluentNHibernate.Mapping;
using NHibernate.Envers;
using NHibernate.Envers.Configuration.Attributes;

namespace RadialReview.Models.Audit
{
	/*[RevisionEntity(typeof(EnversRevisionListener))]
	public class EnversRevisionEntity : DefaultRevisionEntity
	{
		public virtual string UserName { get; set; }

		public EnversRevisionEntity()
		{
			UserName = Thread.CurrentPrincipal.Identity.Name;
		}

		public class EreMap : ClassMap<EnversRevisionEntity>
		{
			public EreMap()
			{
				Id(x => x.Id);
				Map(x => x.RevisionDate);
				Map(x => x.UserName);
				Table("REVINFO");
			}
		}
	}

	public class EnversRevisionListener : IRevisionListener
	{
		private string _userName = "unknown";
		public void NewRevision(object revisionEntity)
		{
			var found = (revisionEntity as EnversRevisionEntity);
			if (found != null)
				found.UserName = Thread.CurrentPrincipal.Identity.Name;
		}
	}*/
}