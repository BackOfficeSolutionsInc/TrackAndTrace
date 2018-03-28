using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors.PDF {

	public partial class D3 {
		//public static object rebind(object target, object source,params object[] arguments) {
		//	var i = 1;
		//	var n = arguments.Length;
		//	var method;
		//	while (++i < n) {
		//		method = arguments[i];
		//		target[method] = d3_rebind(target, source, source[method]);
		//	}
		//	return target;
		//}
		//public static object d3_rebind(object target,object source,object method) {
		//	return ()=> {
		//		var value = method.apply(source, arguments);
		//		return value === source ? target : value;
		//	};
		//}
	}

	public partial class D3 {
		public partial class Layout {

			public class link<N> where N : node<N> {
				public N source { get; set; }
				public N target { get; set; }
			}

			public interface IChildren<T> {
				List<T> children { get; set; }
			}

			public class node : node<node> {

			}

			public class node<N> : IChildren<N> where N : node<N> {

				public double value { get; set; }
				public double x { get; set; }
				public double y { get; set; }
				public int depth { get; set; }
				public int order { get; set; }

				public long Id { get; set; }

				public double width { get; set; }
				public double height { get; set; }

				public double ddx { get; set; }
				public double ddy { get; set; }

				public N parent { get; set; }
				public List<N> children { get; set; }

				public compact _compact { get; set; }
				public string side { get; set; }

				private string DebugNotes { get; set; }

				public string GetDebugNotes() {
					return DebugNotes ?? "";
				}
				public void SetDebugNotes(string notes) {
					if (DebugNotes != null)
						DebugNotes += ",";
					DebugNotes += notes;
				}

				public node() {
					children = new List<N>();
				}

				public class compact {

					public N originalParent { get; set; }
					public List<N> originalChildren { get; set; }
					public bool isLeaf { get; set; }
					public string side { get; set; }
					public List<N> columnHeads { get; set; }

					protected N me { get; set; }

					public compact(N me) {
						originalChildren = new List<N>();
						this.me = me;
					}

					public int? WhichColumn() {
						var hs = originalParent.children;
						for (var i = 0; i < hs.Count; i++) {
							var column = dive(hs[i]);
							if (column.Any(y => y == me))
								return i;
						}
						return null;
					}

					protected List<N> dive(N a) {
						var items = new List<N>();
						items.Add(a);
						if (a.children != null) {
							foreach (var c in a.children) {
								items.AddRange(dive(c));
							}
						}
						return items;

					}


					public int ColumnCount() {
						return originalParent._compact.columnHeads.Count;
					}

				}
			}

			public class nodeWrapper<N> : IChildren<nodeWrapper<N>> where N : node<N> {
				public N _self { get; set; }

				public nodeWrapper<N> parent { get; set; }
				public List<nodeWrapper<N>> children { get; set; }
				public nodeWrapper<N> A { get; set; }
				public nodeWrapper<N> a { get; set; }
				public double z { get; set; }
				public double m { get; set; }
				public double c { get; set; }
				public double s { get; set; }
				public nodeWrapper<N> t { get; set; }
				public int i { get; set; }
				public void SetDebugNotes(string notes) {
					_self.SetDebugNotes(notes);
				}

				public int? ColumnCount() {
					if (_self == null || _self._compact == null)
						return null;
					return _self._compact.ColumnCount();
				}
				public int? WhichColumn() {
					if (_self == null || _self._compact == null)
						return null;
					return _self._compact.WhichColumn();
				}
			}

			public class Hierarchy<T, N> where T : Hierarchy<T, N> where N : node<N> {

				//public delegate int HierarchySort(node x, node y);
				public delegate List<N> HierarchyChildren(N x);
				public delegate double HierarchyValue(N x);

				protected Comparison<N> _sort { get; set; }
				protected HierarchyChildren _children { get; set; }
				protected HierarchyValue _value { get; set; }


				public Hierarchy() {
					_sort = d3_layout_hierarchySort;
					_children = d3_layout_hierarchyChildren;
					_value = d3_layout_hierarchyValue;
				}

				private void recurse(object node, object depth, object nodes) {
					throw new NotImplementedException();
					//var childs = children.call(hierarchy, node, depth);
					//node.depth = depth;
					//nodes.push(node);
					//if (childs && (n = childs.length)) {
					//	var i = -1, n, c = node.children = new Array(n), v = 0, j = depth + 1, d;
					//	while (++i < n) {
					//		d = c[i] = recurse(childs[i], j, nodes);
					//		d.parent = node;
					//		v += d.value;
					//	}
					//	if (sort)
					//		c.sort(sort);
					//	if (value)
					//		node.value = v;
					//} else {
					//	delete node.children;
					//	if (value) {
					//		node.value = +value.call(hierarchy, node, depth) || 0;
					//	}
					//}
					//return node;
				}

				private double revalue(N node, int depth) {
					var children = node.children;
					var v = 0.0;
					if (children != null && children.Count > 0) {
						var n = children.Count;
						var i = -1;
						var j = depth + 1;
						while (++i < n)
							v += revalue(children[i], j);
					} else if (_value != null) {
						v = _value(node);
					}
					if (_value != null)
						node.value = v;
					return v;
				}

				public List<N> callHierarchy(N root) {
					var stack = new Stack<N>();
					stack.Push(root);
					var nodes = new List<N>();
					root.depth = 0;
					while (stack.Count > 0) {
						var node = stack.Pop();
						nodes.Add(node);
						var childs = _children(node);//, node.depth)
						if (childs != null && childs.Count > 0) {
							var n = childs.Count;
							N child;
							while (--n >= 0) {
								child = childs[n];
								stack.Push(child);
								child.parent = node;
								child.depth = node.depth + 1;
							}
							if (_value != null)
								node.value = 0;
							node.children = childs;
						} else {
							if (_value != null)
								node.value = _value(node);
							node.children = null;
						}
					}
					d3_layout_hierarchyVisitAfter(root, n => {
						var childs = n.children;
						if (_sort != null && childs != null) {
							Comparison<N> compare = _sort;
							childs.Sort(_sort);
						}
						var parent = n.parent;
						if (_value != null && parent != null)
							parent.value += n.value;
					});
					return nodes;
				}

				public Comparison<N> sort() {
					return _sort;
				}
				public void sort(Comparison<N> x) {
					_sort = x;
				}

				public HierarchyChildren children() {
					return _children;
				}
				public void children(HierarchyChildren x) {
					_children = x;
					//return this;
				}

				public HierarchyValue value() {
					return _value;
				}
				public void value(HierarchyValue x) {
					_value = x;
					//return this;
				}

				public HierarchyValue revalue() {
					return _value;
				}

				public object revalue(N root) {
					revalue(root, 0);
					return root;
				}

				protected int d3_layout_hierarchySort(N a, N b) {
					return Math.Sign(b.value - a.value);
				}
				protected List<N> d3_layout_hierarchyChildren(N d) {
					return d.children;
				}

				protected double d3_layout_hierarchyValue(N d) {
					return d.value;
				}


#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
				protected static void d3_layout_hierarchyVisitBefore<T>(T node, Action<T> callback) where T : IChildren<T> {
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
					var nodes = new Stack<T>();
					nodes.Push(node);
					while (nodes.Any()) {
						node = nodes.Pop();
						callback(node);
						var children = node.children;
						if (children != null && children.Count > 0) {
							var n = children.Count;
							while (--n >= 0)
								nodes.Push(children[n]);
						}
					}
				}
#pragma warning disable CS0693 // Type parameter has the same name as the type parameter from outer type
				protected static void d3_layout_hierarchyVisitAfter<T>(T node, Action<T> callback) where T : IChildren<T> {
#pragma warning restore CS0693 // Type parameter has the same name as the type parameter from outer type
					var nodes = new Stack<T>();
					nodes.Push(node);
					var nodes2 = new Stack<T>();
					while (nodes.Any()) {
						node = nodes.Pop();
						nodes2.Push(node);
						var children = node.children;
						if (children != null && children.Count > 0) {
							var n = children.Count;
							var i = -1;
							while (++i < n)
								nodes.Push(children[i]);
						}
					}
					while (nodes2.Any()) {
						node = nodes2.Pop();
						callback(node);
					}
				}
			}
		}
	}

	public partial class D3 {
		public partial class Layout {
			public class CompactTree<N> where N : node<N>, new() {
				public delegate double TreeSeparation(nodeWrapper<N> x, nodeWrapper<N> y);
				public delegate void SizeNode(node<N> node);
				public delegate List<link<N>> Links(List<N> nodes);

				public tree hierarchy { get; set; }
				public TreeSeparation separation { get; set; }
				public double[] size { get; set; }
				public SizeNode nodeSize;
				public bool compactify { get; set; }

				public static tree call() {
					var ct = new CompactTree<N>();
					ct.hierarchy = new tree(ct);
					ct.d3_layout_hierarchyRebind(ct.hierarchy);
					return ct.hierarchy;
				}

				private CompactTree() {
					var hierarchy = new Hierarchy<tree, N>();
					hierarchy.sort(null);
					hierarchy.value(null);

					separation = d3_layout_treeSeparation;
					size = new[] { 1.0, 1.0 };
					nodeSize = null;
					compactify = false;
				}

				private void debugTree(object node, object d) {
					throw new NotImplementedException();
					//d = d || 0;
					//if (d == 0)
					//	console.log("-------------");
					//var b = "";
					//for (var i = 0; i < d; i++)
					//	b += "-";

					//console.log(b + node.Key);
					//for (var c in node.children) {
					//	debugTree(node.children[c], d + 1);
					//}
				}


				public class tree : Hierarchy<tree, N> {

					private CompactTree<N> _ct { get; set; }
					public Links links { get; set; }

					public tree(CompactTree<N> ct) {
						_ct = ct;
					}

					public List<N> nodes(N d, object i = null) {
						return call(d, i);
					}

					public List<N> call(N d, object i = null) {

						_ct.decompactify(d);
						if (_ct.compactify) {
							_ct.compactifyTree(d, null);
						}
						var nodes = _ct.hierarchy.callHierarchy(d);
						var root0 = nodes[0];
						var root1 = _ct.wrapTree(root0);

						// Compute the layout using Buchheim et al.'s algorithm.
						d3_layout_hierarchyVisitAfter(root1, _ct.firstWalk);
						root1.parent.m = -root1.z;
						d3_layout_hierarchyVisitBefore(root1, _ct.secondWalk);

						if (_ct.nodeSize != null) // If a fixed node size is specified, scale x and y.
							d3_layout_hierarchyVisitBefore(root0, _ct.sizeNode);
						else {
							throw new NotImplementedException("separation only handles nodeWrapper, not node");
							// If a fixed tree size is specified, scale x and y based on the extent.
							// Compute the left-most, right-most, and depth-most nodes for extents.
							//var left = root0;
							//var right = root0;
							//var bottom = root0;

							//d3_layout_hierarchyVisitBefore(root0, node=> {
							//	if (node.x < left.x)
							//		left = node;
							//	if (node.x > right.x)
							//		right = node;
							//	if (node.depth > bottom.depth)
							//		bottom = node;
							//});
							//var tx = _ct.separation(left, right) / 2 - left.x;
							//var kx = size[0] / (right.x + _ct.separation(right, left) / 2 + tx);
							//var ky = size[1] / (bottom.depth || 1);

							//d3_layout_hierarchyVisitBefore(root0, node=> {
							//	node.x = (node.x + tx) * kx;
							//	node.y = node.depth * ky;
							//});
						}

						return nodes;
					}

					public TreeSeparation separation() {
						return _ct.separation;
					}
					public tree separation(TreeSeparation x) {
						_ct.separation = x;
						return this;
					}

					public double[] size() {
						return _ct.nodeSize != null ? null : _ct.size;
					}
					public tree size(double[] x) {
						_ct.size = x;
						_ct.nodeSize = _ct.size == null ? _ct.sizeNode : (SizeNode)null;
						return this;
					}

					public double[] nodeSize() {
						return _ct.nodeSize != null ? _ct.size : (double[])null;
					}

					public tree nodeSize(double[] x) {
						_ct.size = x;
						_ct.nodeSize = (_ct.size) == null ? (SizeNode)null : _ct.sizeNode;
						return this;
					}

					public bool compactify() {
						return _ct.compactify;
					}

					public tree compactify(bool x) {
						_ct.compactify = (x);
						return this;
					}

				}




				//		private nodeWrapper wrapTree(node root0) {
				//			var root1 = new nodeWrapper {
				//				A = null,
				//				children = new List<nodeWrapper>()
				//			};
				//			root1.children.Add(root0);

				//			var queue = [root1];

				//			while ((node1 = queue.pop()) != null) {
				//				for (var children = node1.children, child, i = 0, n = children.length; i < n; ++i) {
				//					queue.push((children[i] = child = {
				//						_:
				//						children[i],
				//				parent:
				//						node1,
				//				children:
				//						(child = children[i].children) && child.slice() || [],
				//				A:
				//						null,
				//				a:
				//						null,
				//				z:
				//						0,
				//				m:
				//						0,
				//				c:
				//						0,
				//				s:
				//						0,
				//				t:
				//						null,
				//				i:
				//						i
				//					}).a = child);
				//			}
				//		}
				//	return root1.children[0];
				//}
				//}

				private nodeWrapper<N> wrapTree(N root0) {
					var extra = new N() {
						children = new List<N>() { root0 }
					};
					var wt = wrapTree(extra, null, 0);
					return wt.children[0];
				}
				private nodeWrapper<N> wrapTree(N root0, nodeWrapper<N> node1, int i) {

					var nw = new nodeWrapper<N> {
						_self = root0,
						parent = node1,
						//children= ,
						A = null,
						a = null,
						z = 0,
						m = 0,
						c = 0,
						s = 0,
						t = null,
						i = i
					};

					nw.children = root0.children.NotNull(x => x.Select((child, itr) => wrapTree(child, nw, itr)).ToList()) ?? new List<nodeWrapper<N>>();
					nw.a = nw;
					//var root1 = new nodeWrapper{
					//	A = null,
					//	children = new List<node> { root0 }
					//};
					//var queue = new Stack<nodeWrapper>();
					//queue.Push(root1);
					//nodeWrapper node1;

					//while ((node1 = queue.Pop()) != null) {
					//	var children = node1.children;

					//	var n = children.Count;

					//	for (var i = 0; i < n; ++i) {
					//		var cc = children[i].children.NotNull(x=>x.Select(y=>y).ToList()) ?? new List<node>();
					//		children[i] = new nodeWrapper{
					//					_self= children[i],
					//					parent= node1,
					//					children= cc,
					//					A= null,a= null,
					//					z= 0,m= 0,c= 0,s= 0,
					//					t= null,
					//					i= i
					//		};
					//		var child = children[i];
					//		child.a = child;

					//		queue.Push(child);
					//	}
					//}
					return nw;
				}

				private void compactifyTree(N node, N parent) {
					if (node.children != null && node.children.Count != 0) {

						var leafs = new List<N>();
						var newChildren = new List<N>();

						if (node._compact == null) {
							node._compact = new node<N>.compact(node);
							node._compact.originalParent = parent;
						}
						var childs = node.children;
						node._compact.originalChildren = childs.Select(x => x).ToList();

						foreach (var child in node.children) {
							//var child = node.children.pop();
							if (child.children == null || child.children.Count == 0) {
								//add to leafs
								leafs.Add(child);
							} else {
								newChildren.Add(child);
							}
						}
						node.children = new List<N>();

						//only compactify if leafs are more than 3
						if (leafs.Count <= 3) {
							//for(var leaf in leafs)
							//	newChildren.push(leafs[leaf]);
							//leafs = [];
							newChildren = node._compact.originalChildren;
							node._compact.originalChildren = null;
							leafs = new List<N>();
						}


						node.children = newChildren;

						foreach (var c in node.children) {
							if (c.children != null && c.children.Count != 0) {
								compactifyTree(c, node);
							}
						}
						//calculate how to divide up leafs
						//# Rows and Columns
						var n = leafs.Count;
						var sqrtn = Math.Sqrt(n);
						//
						//var cols = (int)Math.Ceiling(sqrtn);
						/*var cols = (int)Math.Floor(sqrtn);
						var rows = (int)Math.Ceiling(sqrtn);
						if (rows * cols < n)
							cols += 1;
							*/
						int cols;
						int rows;
						if (n <= 3) {
							cols = n;
							rows = 1;
						} else if (n > 3 && n < 11) {
							cols = 2;
							rows = (int)Math.Ceiling(n / 2.0);
						} else {
							cols = (int)Math.Ceiling(sqrtn);
							rows = (int)Math.Floor(sqrtn);
						}

						if (rows * cols < n) {
							rows += 1;
						}






						//# branches
						//var branches = cols;
						//if (n > 4)
						//	var numBranches = Math.Ceiling(cols / 2);
						//var branches = [];


						var currentColumnHeads = new List<N>();
						for (var ii = 0; ii < cols; ii++) {
							currentColumnHeads.Add(node);
						}

						node._compact.columnHeads = currentColumnHeads;


						//Create faux column
						var leafNum = 0;
						var i = 0;

						var oddEven = 1;
						if (cols % 2 == 0)
							oddEven = 0;
						while (leafNum < leafs.Count) {
							var child = leafs[leafNum];
							var colHead = currentColumnHeads[i];
							if (colHead.children == null)
								colHead.children = new List<N>();
							colHead.children.Add(child);
							if (child._compact == null) {
								child._compact = new node<N>.compact(child);
								child._compact.originalParent = node;
							}
							//child._compact.originalParent = child.parent;
							child.parent = colHead;
							child._compact.isLeaf = true;
							if (i % 2 == oddEven) {
								child._compact.side = "left";
								child.side = "left";//added
							} else {
								child._compact.side = "right";
								child.side = "right";//added
							}
							currentColumnHeads[i] = child;
							//child._compact.originalChildren = new List<N>();
							child.children = new List<N>();
							leafNum++;
							i++;
							i = i % cols;
						}
					}
				}

				private void decompactify(N node) {
					if (node._compact != null && node._compact.originalChildren != null) {
						node.children = node._compact.originalChildren;
						node._compact.originalChildren = null;
					}
					if (node.children != null) {
						foreach (var c in node.children) {
							decompactify(c);
						}
					}
					if (node._compact != null && node._compact.originalParent != null) {
						node.parent = node._compact.originalParent;
						node._compact.originalParent = null;
					}
					if (node._compact != null) {
						node._compact = null;
					}

				}


				// FIRST WALK
				// Computes a preliminary x-coordinate for v. Before that, FIRST WALK is
				// applied recursively to the children of v, as well as the function
				// APPORTION. After spacing out the children by calling EXECUTE SHIFTS, the
				// node v is placed to the midpoint of its outermost children.
				private void firstWalk(nodeWrapper<N> v) {
					var children = v.children;
					var siblings = v.parent.children;
					var w = v.i != 0 ? siblings[v.i - 1] : null;

					if (children.Count != 0) {
						d3_layout_treeShift(v);
						var midpoint = (children[0].z + children[children.Count - 1].z) / 2;
						if (w != null) {
							v.z = w.z + separation(v, w) * scaleSeparation(v, w);
							v.m = v.z - midpoint;
						} else {
							v.z = midpoint;
						}
					} else if (w != null) {
						v.z = w.z + separation(v, w) * scaleSeparation(v, w);
					}
					v.parent.A = apportion(v, w, v.parent.A ?? siblings[0]);
				}

				// SECOND WALK
				// Computes all real x-coordinates by summing up the modifiers recursively.
				private void secondWalk(nodeWrapper<N> v) {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
					var adj = 1;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
					if (compactify && v._self._compact != null /*&& v._._compact.isLeaf*/) {
						adj = 2;
						//if (v._._compact.side == "left") {
						//	adj = -.25;
						//} else if (v._._compact.side == "right") {
						//	adj = .25;
						//}
					}
					v._self.x = v.z + v.parent.m;
					v.m += v.parent.m;
				}

				// APPORTION
				// The core of the algorithm. Here, a new subtree is combined with the
				// previous subtrees. Threads are used to traverse the inside and outside
				// contours of the left and right subtree up to the highest common level. The
				// vertices used for the traversals are vi+, vi-, vo-, and vo+, where the
				// superscript o means outside and i means inside, the subscript - means left
				// subtree and + means right subtree. For summing up the modifiers along the
				// contour, we use respective variables si+, si-, so-, and so+. Whenever two
				// nodes of the inside contours conflict, we compute the left one of the
				// greatest uncommon ancestors using the function ANCESTOR and call MOVE
				// SUBTREE to shift the subtree and prepare the shifts of smaller subtrees.
				// Finally, we add a new thread (if necessary).
				private nodeWrapper<N> apportion(nodeWrapper<N> v, nodeWrapper<N> w, nodeWrapper<N> ancestor) {

					if (w != null) {
						var vip = v;
						var vop = v;
						var vim = w;
						var vom = vip.parent.children[0];
						var sip = vip.m;
						var sop = vop.m;
						var sim = vim.m;
						var som = vom.m;
						var shift = 0.0;
						while (true) {
							vim = d3_layout_treeRight(vim);
							vip = d3_layout_treeLeft(vip);
							if (!(vim != null && vip != null)) {
								break;
							}
							vom = d3_layout_treeLeft(vom);
							vop = d3_layout_treeRight(vop);
							vop.a = v;
							shift = (vim.z + sim - vip.z - sip + separation(vim, vip)) * scaleSeparation(vim, vip);
							if (shift > 0) {
								d3_layout_treeMove(d3_layout_treeAncestor(vim, v, ancestor), v, shift);
								sip += shift;
								sop += shift;
							}
							sim += vim.m;
							sip += vip.m;
							som += vom.m;
							sop += vop.m;
						}
						if (vim != null && d3_layout_treeRight(vop) == null) {
							vop.t = vim;
							vop.m += sim - sop;
						}
						if (vip != null && d3_layout_treeLeft(vom) == null) {
							vom.t = vip;
							vom.m += sip - som;
							ancestor = v;
						}
					}
					return ancestor;
				}

				// NEXT LEFT
				// This function is used to traverse the left contour of a subtree (or
				// subforest). It returns the successor of v on this contour. This successor is
				// either given by the leftmost child of v or by the thread of v. The function
				// returns null if and only if v is on the highest level of its subtree.
				private nodeWrapper<N> d3_layout_treeLeft(nodeWrapper<N> v) {
					var children = v.children;
					return children.Count != 0 ? children[0] : v.t;
				}

				// NEXT RIGHT
				// This function works analogously to NEXT LEFT.
				private nodeWrapper<N> d3_layout_treeRight(nodeWrapper<N> v) {
					var children = v.children;
					var n = children.Count;
					return n != 0 ? children[n - 1] : v.t;
				}

				// MOVE SUBTREE
				// Shifts the current subtree rooted at w+. This is done by increasing
				// prelim(w+) and mod(w+) by shift.
				private void d3_layout_treeMove(nodeWrapper<N> wm, nodeWrapper<N> wp, double shift) {
					var change = shift / (wp.i - wm.i);
					wp.c -= change;
					wp.s += shift;
					wm.c += change;
					wp.z += shift;
					wp.m += shift;
				}
				// EXECUTE SHIFTS
				// All other shifts, applied to the smaller subtrees between w- and w+, are
				// performed by this function. To prepare the shifts, we have to adjust
				// change(w+), shift(w+), and change(w-).
				private void d3_layout_treeShift(nodeWrapper<N> v) {
					var shift = 0.0;
					var change = 0.0;
					var children = v.children;
					var i = children.Count;

					while (--i >= 0) {
						var w = children[i];
						w.z += shift;
						w.m += shift;
						change += w.c;
						shift += w.s + change;
					}
				}
				// ANCESTOR
				// If vi-’s ancestor is a sibling of v, returns vi-’s ancestor. Otherwise,
				// returns the specified (default) ancestor.
				private nodeWrapper<N> d3_layout_treeAncestor(nodeWrapper<N> vim, nodeWrapper<N> v, nodeWrapper<N> ancestor) {
					return vim.a.parent == v.parent ? vim.a : ancestor;
				}

				//Verbetum Copy	
				private tree d3_layout_hierarchyRebind(tree obj) {
					//D3.rebind(obj, hierarchy, "sort", "children", "value");  //Done with extension instead
					//obj._nodes = obj;
					obj.links = d3_layout_hierarchyLinks;
					return obj;
				}
				private List<link<N>> d3_layout_hierarchyLinks(List<N> nodes) {
					return nodes.SelectMany(parent => {
						var children = parent.children ?? new List<N>();
						return children.Select(child => new link<N> { source = parent, target = child });
					}).ToList();
				}

				private double scaleSeparation(nodeWrapper<N> a, nodeWrapper<N> b) {

					var aCompact = a.children.Any(x => x._self.NotNull(y => y._compact.side == "left" || y._compact.side == "right"));
					var bCompact = b.children.Any(x => x._self.NotNull(y => y._compact.side == "left" || y._compact.side == "right"));


					var aCompactLeft = a.children.Any(x => x._self.NotNull(y => y._compact.side == "left"));
					var aCompactRight = a.children.Any(x => x._self.NotNull(y => y._compact.side == "right"));
					var bCompactLeft = a.children.Any(x => x._self.NotNull(y => y._compact.side == "left"));
					var bCompactRight = a.children.Any(x => x._self.NotNull(y => y._compact.side == "right"));

					var ab = new[] { a, b };

					//foreach (var i in ab) {
					//	if (i.NotNull(x => x._self._compact.isLeaf)) {
					//		i.SetDebugNotes("L");
					//	}
					//	if (i == null || i._self == null || i._self._compact == null) {
					//		i.SetDebugNotes("n");
					//	}
					//}

					//if (a.parent == b.parent) {
					//	if (b.NotNull(x=>x._self._compact.side=="right") && a.NotNull(x => x._self._compact.side == "left")) {
					//		b.SetDebugNotes(">");
					//		a.SetDebugNotes("<");
					//		return 1;
					//	}
					//}




					//if (!aCompactLeft && aCompactRight && !bCompact) {
					//	a.SetDebugNotes("A|");
					//	b.SetDebugNotes("|A");
					//	return a.parent == b.parent ? 1 : 0.8;
					//}
					//if (!aCompact && !bCompactLeft && bCompactRight) {
					//	a.SetDebugNotes("B|");
					//	b.SetDebugNotes("|B");
					//	return a.parent == b.parent ? 1 : 0.8;
					//}


					//if (a.NotNull(x => x._self._compact.isLeaf) && b.NotNull(x => x._self._compact.isLeaf) && a.parent == b.parent) {
					//	//if (b.NotNull(x => x._self._compact.side == "right") && a.NotNull(x => x._self._compact.side == "left")) {
					//	if (a.parent.children.Last() != a) {
					//		a.SetDebugNotes("x0");
					//		b.SetDebugNotes("(x0)");
					//		return 0;
					//	}
					//}

					//a.SetDebugNotes("Z|");
					//b.SetDebugNotes("|Z");
					return 1;
				}

				private double d3_layout_treeSeparation(nodeWrapper<N> a, nodeWrapper<N> b) {


					//if (a.children.Any() && b.children.Any() && a.children.Any(x => x.NotNull(y => y._self._compact.isLeaf)) && b.children.Any(x => x.NotNull(y => y._self._compact.isLeaf))) {
					//	return 2;
					//}
					var p1 = a.NotNull(x => x._self._compact.originalParent);
					var p2 = b.NotNull(x => x._self._compact.originalParent);
					var ppnn = !(p1 == null || p2 == null);

					//if (a.parent.parent == b.parent.parent && a.parent != b.parent && a.NotNull(x => x._self._compact.isLeaf) && b.NotNull(x => x._self._compact.isLeaf)) {
					//	b.SetDebugNotes("2");
					//	a.SetDebugNotes("2");
					//	return 2;
					//}

					//if (!a.NotNull(x => x._self._compact.isLeaf) && !b.NotNull(x => x._self._compact.isLeaf) && a.NotNull(x => x.children.LastOrDefault()._self._compact.isLeaf) && b.NotNull(x => x.children.FirstOrDefault()._self._compact.isLeaf) && p1 == p2) {
					//	a.SetDebugNotes("0");
					//	b.SetDebugNotes("(0)");
					//	return 0;
					//}

					//if (a.parent.parent == b.parent.parent && p1 != p2 && ppnn) {
					//	a.SetDebugNotes("0");
					//	b.SetDebugNotes("(0)");
					//	return 0;
					//}


					var isBLeft = b.NotNull(x => x._self._compact.side == "left");
					var isBRight = b.NotNull(x => x._self._compact.side == "right");

					var isALeft = a.NotNull(x => x._self._compact.side == "left");
					var isARight = a.NotNull(x => x._self._compact.side == "right");

					var colCount = a.ColumnCount();
					var curCol = a.WhichColumn();
					var isFirstCol = new Func<int?, bool>(x => colCount != null && x == 0);
					var isLastCol = new Func<int?, bool>(x => colCount != null && x == colCount - 1);

					if ((b.NotNull(x => x._self._compact.isLeaf) && b.NotNull(x => x._self._compact.side == "left") && a.NotNull(x => x._self._compact) == null) ||
						(a.NotNull(x => x._self._compact.isLeaf) && a.NotNull(x => x._self._compact.side == "right") && b.NotNull(x => x._self._compact) == null)) {
						a.SetDebugNotes("b");
						//b.SetDebugNotes("(b)");
						return 1;
					}

					if (colCount == 3) {
						if (curCol == 0) {
							a.SetDebugNotes("h");
							return 1;
						}
						if (curCol == 1) {
							a.SetDebugNotes("g");
							return 1;
						}
						if (curCol == 2) {
							a.SetDebugNotes("i");
							return 2;
						}
					}




					if (a.NotNull(x => x._self._compact.isLeaf) && b.NotNull(x => x._self._compact.isLeaf) && p1 == p2 && ppnn) {
						//if (b.NotNull(x => x._self._compact.side == "right") && a.NotNull(x => x._self._compact.side == "left")) {

						if (colCount == 4) {
							if (curCol == 0) {
								a.SetDebugNotes("q");
								return 2;//2
							}
							if (curCol == 1) {
								a.SetDebugNotes("d");
								return .15;//2
							}
							if (curCol == 2) {
								a.SetDebugNotes("m");
								return .15;//1
							}
							if (curCol == 3) {
								a.SetDebugNotes("p");
								return 2;//1
							}
						}

						if (a.children.Any()) {
							if (a.children.Last() == a) {
								a.SetDebugNotes("a");
								return 1;
							} else {
								a.SetDebugNotes("c");
								return 2;
							}
						}

					}

					if (isFirstCol(curCol)) {
						a.SetDebugNotes("[");
						//b.SetDebugNotes("([)");
						return 1;
					}
					if (isLastCol(curCol)) {

						if (isBLeft && isARight && p1.parent == p2.parent) {

							/*
							
									|
							 o______o_______o
							 |		|		|
							o+o    o+a	   b+o
							 |		|		|
							o+o    o+o	   o+o

							*/

							a.SetDebugNotes("j" + a._self.Id);
							b.SetDebugNotes("(j" + a._self.Id + ")");
							return .6;
						}

						if (isBRight && isARight) {
							/*
							
							 |
							 o______o
							 |		|
							o+a		|-b
							 |
							o+o

							*/
							a.SetDebugNotes("k" + a._self.Id);
							b.SetDebugNotes("(k" + a._self.Id + ")");
							return 1.5;
						}

						if (isBLeft && isALeft) {

							/*
							
									|
							  o_____o
							  |		|
							a-|    b+o
							  |		|
							o-|    o+o

							*/
							a.SetDebugNotes("k" + a._self.Id);
							b.SetDebugNotes("(k" + a._self.Id + ")");
							return 1.5;
						}

						a.SetDebugNotes("]" + a._self.Id);
						b.SetDebugNotes("(]" + a._self.Id + ")");
						return 2;
					}

					//if (a.children.Any() && a.children[0].children.Any() && b.children.Any() && b.children[0].children.Any()) {
					//	//b.SetDebugNotes(".25");
					//	//a.SetDebugNotes(".25");
					//	//return .25;
					//}


					//if (a.children.Any() && b.children.Any() && a.children.Any(x => x.NotNull(y => y._self._compact.isLeaf ))&& b.children.Any(x => x.NotNull(y => y._self._compact.isLeaf)) {
					//	return 0;
					//}

					//if (a.parent.parent == b.parent.parent && a.parent != b.parent) {
					//	return 2;
					//}



					//if (a.parent == b.parent && a.children.Any() && b.children.Any()) {
					//	return 3;
					//}
					//return 1;
					//	if (a.parent == b.parent) {
					//		if (b.NotNull(x => x._self._compact.side == "right") && a.NotNull(x => x._self._compact.side == "left")) {
					//			b.SetDebugNotes(">>");
					//			a.SetDebugNotes("<<");
					//			return .5;
					//		}
					//	}
					//}

					if (a.parent == b.parent || (p1 == p2 && ppnn)) {
						a.SetDebugNotes("1");
						//b.SetDebugNotes("(1)");
						return 1;
					}
					a.SetDebugNotes("2");
					//b.SetDebugNotes("(2)");
					return 2;

					//return a.parent == b.parent ? 1 : 2 /*2*/;


					//if (a.children.Count() != 0 && b.children.Count() != 0) {
					//	return a.parent == b.parent ? 1 : 1;
					//}

					////
					//var aCompact = a.children.Any(x => x._self.NotNull(y => y._compact.side == "left" || y._compact.side == "right"));
					//var bCompact = b.children.Any(x => x._self.NotNull(y => y._compact.side == "left" || y._compact.side == "right"));


					//var aCompactLeft = a.children.Any(x => x._self.NotNull(y => y._compact.side == "left"));
					//var aCompactRight = a.children.Any(x => x._self.NotNull(y => y._compact.side == "right"));
					//var bCompactLeft = a.children.Any(x => x._self.NotNull(y => y._compact.side == "left"));
					//var bCompactRight = a.children.Any(x => x._self.NotNull(y => y._compact.side == "right"));


					//if (!aCompactLeft && aCompactRight && !bCompact) {
					//	return a.parent == b.parent ? 1 : 1.5;
					//}
					//if (!aCompact && !bCompactLeft && bCompactRight) {
					//	return a.parent == b.parent ? 1 : 1.5;
					//}
					////if (aCompactRight && bCompactLeft || bCompactRight && aCompactLeft) {
					////	return a.parent == b.parent ? 1 : 1;
					////}
					//if (aCompact && bCompact) {
					//	return 1;//a.parent == b.parent ? .5 : 0.5;
					//}

					//if (aCompact || bCompact) {
					//	return a.parent == b.parent ? 1 : 1.5;
					//}



					//if (a._self != null && b._self != null &&
					//		a._self._compact != null && b._self._compact != null &&
					//		a._self._compact.isLeaf && b._self._compact.isLeaf) {
					//	if (a._self._compact.side == "right" && b._self._compact.side == "left") {
					//		return a.parent == b.parent ? .5 : 1.0;
					//	} else if (a._self._compact.side == "left" && b._self._compact.side == "right") {
					//		var mult = 1.0; // .6
					//		return a.parent == b.parent ? mult :2 * mult;
					//	}
					//}


					return a.parent == b.parent ? 1 : 1.5 /*2*/;
				}

				private void sizeNode(node<N> node) {
					node.x *= size[0];
					node.y = (node.depth) * size[1];
				}
			}
		}
	}
}