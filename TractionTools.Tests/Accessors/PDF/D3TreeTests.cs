using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static RadialReview.Accessors.PDF.D3.Layout;
using RadialReview.Accessors.PDF.JS;

namespace TractionTools.Tests.Accessors.PDF {
	[TestClass]
	public class D3TreeTests {
		[TestMethod]
		public void TreePositions() {

			var a = new node();
			var b = new node();
			var c = new node();
			var d = new node();
			var e = new node();
			var f = new node();
			var g = new node();
			var h = new node();

			a.children.Add(b);
			a.children.Add(c);
			a.children.Add(d);

			c.children.Add(e);
			c.children.Add(f);

			d.children.Add(g);
			d.children.Add(h);
			

			var tree = Tree.Update(a, null);

			Assert.IsTrue(b.x < c.x);
			Assert.IsTrue(c.x < d.x);

			Assert.IsTrue(e.x < f.x);
			Assert.IsTrue(f.x < g.x);
			Assert.IsTrue(g.x < h.x);

			Assert.IsTrue(b.y == c.y);
			Assert.IsTrue(b.y == d.y);

			Assert.IsTrue(e.y == g.y);
			Assert.IsTrue(e.y == f.y);
			Assert.IsTrue(e.y == g.y);
			Assert.IsTrue(e.y == h.y);

		}
	}
}
