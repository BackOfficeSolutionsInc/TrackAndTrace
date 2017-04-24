using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static RadialReview.Accessors.PDF.D3.Layout;

namespace RadialReview.Accessors.PDF.JS {
	public class Tree {

		public class TreeSettings {

			public double baseWidth { get; set; }
			public double baseHeight { get; set; }
			public double[] nodeSize { get; set; }
			public double minWidth = 20;
			public double minHeight = 20;


			public double vSeparation = 40;
			public double hSeparation = 25;

			public bool compact { get; set; }


			public TreeSettings() {

				baseWidth = 225;
				baseHeight = 50;
				nodeSize = new[] { 225, 50.0 };
				minWidth = 20;
				minHeight = 20;


				vSeparation = 40;
				hSeparation = 25;
				compact = false;
			}
		}

		public class rowWidth {
			public double ox { get; set; }
			public double w { get; set; }
		}

		private Tree() {
		}

		//public CompactTree.tree tree { get; set; }
		//public TreeSettings Settings { get; set; }

		//public Tree(TreeSettings settings = null) {

		//}


		public static CompactTree<N>.tree Update<N>(N root, Func<N, double> heightFunc=null, TreeSettings settings = null) where N : node<N>, new() {
			var Settings = settings ?? new TreeSettings();
			var tree = CompactTree<N>.call();
			tree.compactify(Settings.compact);//added
			tree.sort((a, b) => {
				if (b != null && a != null) {
					var diff = a.order - b.order;
					if (diff == 0)
						return Math.Sign(a.Id - b.Id);
					return diff;
				}
				return 0;
			});
			tree.nodeSize(Settings.nodeSize);
			
			var maxDepth = 0;
			var maxWidth = Settings.baseWidth;

			/////////////////////////////////////////////////////////
			////HEY HEAVY LIFTING HERE:

			var nodes = tree.nodes(root);
			var links = tree.links(nodes);

			////DONE WITH HEAVY LIFTING.
			/////////////////////////////////////////////////////////


			// Normalize for fixed-depth.
			var maxHeightRow = new DefaultDictionary<int, double>(x => 0.0);
			maxHeightRow[-1] = 0;


			var rowWidth = new DefaultDictionary<int, List<rowWidth>>(x => new List<rowWidth>());

			//recalculate heights
			if (heightFunc != null) {
				foreach (var d in nodes) {
					d.height = heightFunc(d);
				}
			}


			foreach (var d in nodes) {
				maxHeightRow[d.depth] = Math.Max(Math.Max(d.height, Settings.minHeight), maxHeightRow[d.depth]);
				maxDepth = Math.Max(maxDepth, d.depth);
				//rowWidth[d.depth] = rowWidth[d.depth] || [];
				rowWidth[d.depth].Add(new rowWidth {
					ox = d.x,
					w = d.width
				});
				maxWidth = Math.Max(maxWidth, d.width);
			}

			for (var i = 1; i <= maxDepth; i++) {
				maxHeightRow[i] = maxHeightRow[i] + maxHeightRow[i - 1];
				rowWidth[i].Sort((a, b) => Math.Sign(a.ox - b.ox));
			}

			foreach (var d in nodes) {
				d.y = d.depth * Settings.vSeparation + maxHeightRow[d.depth - 1];
				var shift = 0.0;
				for (var i = 0; i < rowWidth[d.depth].Count; i++) {
					var ww = (rowWidth[d.depth][i].w - Settings.baseWidth + Settings.hSeparation);
					if (rowWidth[d.depth][i].ox == d.x) {
						break;
					}
					shift += ww;
				}
				d.ddx = shift;
			}
			return tree;
		}


		/*public static CompactTree<N>.tree Update<N>(N root, bool compact, Func<N, double> heightFunc = null,TreeSettings settings=null) where N : node<N>,new() {
			var tree = Update(root, heightFunc, settings);
			//tree.compactify(compact);
			//tree = Update(root, heightFunc, settings);
			return tree;
		}*/
	}
}