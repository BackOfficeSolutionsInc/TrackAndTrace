using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Conventions.Instances;

namespace RadialReview.App_Start
{

	public class StringColumnLengthConvention : IPropertyConvention, IPropertyConventionAcceptance
	{
		public void Accept(IAcceptanceCriteria<IPropertyInspector> criteria)
		{
			criteria.Expect(x => x.Type == typeof(string)).Expect(x => x.Length == 0);
		}
		public void Apply(IPropertyInstance instance)
		{
			instance.Length(10000);
		}
	}
}