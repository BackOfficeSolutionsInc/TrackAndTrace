using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Payments
{
	public class Itemized
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public decimal Price { get; set; }
		public decimal Quantity { get; set; }

		public decimal Total()
		{
			return Price*Quantity;
		}

		public Itemized Discount()
		{
			return new Itemized()
			{
				Name = Name + " (Discounted)",
				Price = -1 * Price,
				Quantity = Quantity,
			};
		}

	}

	public static class ItemizedExtensions
	{
		public static decimal TotalCharge(this IEnumerable<Itemized> items)
		{
			return items.Sum(x => x.Total());
		}
	}
}