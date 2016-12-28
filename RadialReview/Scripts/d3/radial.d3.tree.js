!function () {
	d3.layout.compactTree = function () {
		var hierarchy = d3.layout.hierarchy().sort(null).value(null),
			separation = d3_layout_treeSeparation,
			size = [1, 1],
			nodeSize = null,
			compactify = false;


		function debugTree(node, d) {
			d = d || 0;
			if (d == 0)
				console.log("-------------");
			var b = "";
			for (var i = 0; i < d; i++)
				b += "-";

			console.log(b + node.Key);
			for (var c in node.children) {
				debugTree(node.children[c], d + 1);
			}
		}

		function tree(d, i) {
			//console.log("===================\ndebugTree 1");debugTree(d);
			decompactify(d);
			//console.log("===================\ndebugTree 2");debugTree(d);
			if (compactify) {
				compactifyTree(d);
				//console.log("===================\ndebugTree 3");debugTree(d);
			}
			var nodes = hierarchy.call(this, d, i),
			  root0 = nodes[0];
			var root1 = wrapTree(root0);



			// Compute the layout using Buchheim et al.'s algorithm.
			d3_layout_hierarchyVisitAfter(root1, firstWalk),
			root1.parent.m = -root1.z;
			d3_layout_hierarchyVisitBefore(root1, secondWalk);
			
			if (nodeSize) // If a fixed node size is specified, scale x and y.
				d3_layout_hierarchyVisitBefore(root0, sizeNode);
			else {
				// If a fixed tree size is specified, scale x and y based on the extent.
				// Compute the left-most, right-most, and depth-most nodes for extents.
				var left = root0,
					right = root0,
					bottom = root0;
				d3_layout_hierarchyVisitBefore(root0, function (node) {
					if (node.x < left.x) left = node;
					if (node.x > right.x) right = node;
					if (node.depth > bottom.depth) bottom = node;
				});
				var tx = separation(left, right) / 2 - left.x,
					kx = size[0] / (right.x + separation(right, left) / 2 + tx),
					ky = size[1] / (bottom.depth || 1);

				d3_layout_hierarchyVisitBefore(root0, function (node) {
					node.x = (node.x + tx) * kx;
					node.y = node.depth * ky;
				});
			}

			return nodes;
		}
		function wrapTree(root0) {
			var root1 = {
				A: null,
				children: [root0]
			}, queue = [root1], node1;

			while ((node1 = queue.pop()) != null) {
				for (var children = node1.children, child, i = 0, n = children.length; i < n; ++i) {
					queue.push((children[i] = child = {
						_: children[i],
						parent: node1,
						children: (child = children[i].children) && child.slice() || [],
						A: null,
						a: null,
						z: 0,
						m: 0,
						c: 0,
						s: 0,
						t: null,
						i: i
					}).a = child);
				}
			}
			return root1.children[0];
		}

		function compactifyTree(node) {
			if (node.children && node.children.length) {

				var leafs = []
				var newChildren = [];

				if (!node._compact) {
					node._compact = {};
				}

				node._compact.originalChildren = (child = node.children) && child.slice() || [];
				
				while (child = node.children.pop()) {
					if (!child.children || child.children.length == 0) {
						//add to leafs
						leafs.push(child);
					} else {
						newChildren.push(child);
					}
				}

				//only compactify if leafs are more than 3
				if (leafs.length <= 3) {
					//for(var leaf in leafs)
					//	newChildren.push(leafs[leaf]);
					//leafs = [];
					newChildren = node._compact.originalChildren;
					node._compact.originalChildren = undefined;
					leafs = [];
				}


				node.children = newChildren;

				for (var c in node.children) {
					if (node.children[c].children && node.children[c].children.length) {
						compactifyTree(node.children[c]);
					}
				}
				//calculate how to divide up leafs
				//# Rows and Columns
				var n = leafs.length;
				var sqrtn = Math.sqrt(n);
				var rows = Math.floor(sqrtn);
				var cols = Math.ceil(sqrtn);
				if (rows * cols < n)
					cols += 1;

				//# branches
				var branches = cols;
				if (n > 4)
					numBranches = Math.ceil(cols / 2);
				var branches = [];


				var currentColumnHeads = [];
				for (var i = 0; i < cols; i++) {
					currentColumnHeads.push(node);
				}


				//Create faux column
				var leafNum = 0;
				var i = 0;

				var oddEven = 1;
				if (cols % 2 == 0)
					oddEven = 0;


				while (leafNum < leafs.length) {

					var child = leafs[leafNum];
					var colHead = currentColumnHeads[i];

					if (!colHead.children)
						colHead.children = [];

					colHead.children.push(child);

					if (!child._compact) {
						child._compact = {};
					}

					child._compact.originalParent = child.parent;
					child.parent = colHead;
					child._compact.isLeaf = true;
					if (i % 2 == oddEven) {
						child._compact.side = "left";
					}else{
						child._compact.side = "right";
					}


					currentColumnHeads[i] = child;
					child._compact.originalChildren = [];
					child.children = [];
					leafNum++;
					i++;
					i = i % cols;
				}
			}
		}

		function decompactify(node) {
			if (node._compact && node._compact.originalChildren) {
				node.children = node._compact.originalChildren;
				node._compact.originalChildren = undefined;
			}
			if (node.children) {
				for (var c in node.children) {
					decompactify(node.children[c]);
				}
			}
			if (node._compact && node._compact.originalParent) {
				node.parent = node._compact.originalParent;
				node._compact.originalParent = undefined;
			}
			if (node._compact) {
				delete (node._compact);
			}

		}

		// FIRST WALK
		// Computes a preliminary x-coordinate for v. Before that, FIRST WALK is
		// applied recursively to the children of v, as well as the function
		// APPORTION. After spacing out the children by calling EXECUTE SHIFTS, the
		// node v is placed to the midpoint of its outermost children.
		function firstWalk(v) {
			var children = v.children,
				siblings = v.parent.children,
				w = v.i ? siblings[v.i - 1] : null;
			if (children.length) {
				d3_layout_treeShift(v);
				var midpoint = (children[0].z + children[children.length - 1].z) / 2;
				if (w) {
					v.z = w.z + separation(v/*._*/, w/*._*/);

					//if (compactify) {
					//	v.m = v.z - midpoint/2;
					//} else {
						v.m = v.z - midpoint;
					//}
				} else {
					v.z = midpoint;
				}
			} else if (w) {
				v.z = w.z + separation(v/*._*/, w/*._*/);
			}
			v.parent.A = apportion(v, w, v.parent.A || siblings[0]);
		}

		// SECOND WALK
		// Computes all real x-coordinates by summing up the modifiers recursively.
		function secondWalk(v) {
			var adj = 1;
			if (compactify && v._._compact /*&& v._._compact.isLeaf*/) {
				adj = 2;

				//if (v._._compact.side == "left") {
				//	adj = -.25;
				//} else if (v._._compact.side == "right") {
				//	adj = .25;
				//}
			}


			v._.x = v.z + v.parent.m;
			v.m += v.parent.m ;
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
		function apportion(v, w, ancestor) {
			if (w) {
				var vip = v,
					vop = v,
					vim = w,
					vom = vip.parent.children[0],
					sip = vip.m,
					sop = vop.m,
					sim = vim.m,
					som = vom.m,
					shift;
				while (vim = d3_layout_treeRight(vim), vip = d3_layout_treeLeft(vip), vim && vip) {
					vom = d3_layout_treeLeft(vom);
					vop = d3_layout_treeRight(vop);
					vop.a = v;
					shift = vim.z + sim - vip.z - sip + separation(vim/*._*/, vip/*._*/);
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
				if (vim && !d3_layout_treeRight(vop)) {
					vop.t = vim;
					vop.m += sim - sop;
				}
				if (vip && !d3_layout_treeLeft(vom)) {
					vom.t = vip;
					vom.m += sip - som;
					ancestor = v;
				}
			}
			return ancestor;
		}
		function sizeNode(node) {
			node.x *= size[0];
			node.y = (node.depth) * size[1];
		}
		tree.separation = function (x) {
			if (!arguments.length) return separation;
			separation = x;
			return tree;
		};
		tree.size = function (x) {
			if (!arguments.length) return nodeSize ? null : size;
			nodeSize = (size = x) == null ? sizeNode : null;
			return tree;
		};
		tree.nodeSize = function (x) {
			if (!arguments.length) return nodeSize ? size : null;
			nodeSize = (size = x) == null ? null : sizeNode;
			return tree;
		};
		tree.compactify = function (x) {
			if (!arguments.length) return compactify;
			compactify = (x);
			return tree;
		};
		return d3_layout_hierarchyRebind(tree, hierarchy);
	};
	function d3_layout_treeSeparation(a, b) {
		if (a._ && b._ && a._._compact && b._._compact && a._._compact.isLeaf && b._._compact.isLeaf){
			if (a._._compact.side == "right" && b._._compact.side == "left") {
				return a.parent == b.parent ? .5 : 1;
			} else if (a._._compact.side == "left" && b._._compact.side == "right") {
				var mult = 1; // .6
				return a.parent == b.parent ? mult : 2 * mult;
			}
		}
		return a.parent == b.parent ? 1 : 2;
	}
	// NEXT LEFT
	// This function is used to traverse the left contour of a subtree (or
	// subforest). It returns the successor of v on this contour. This successor is
	// either given by the leftmost child of v or by the thread of v. The function
	// returns null if and only if v is on the highest level of its subtree.
	function d3_layout_treeLeft(v) {
		var children = v.children;
		return children.length ? children[0] : v.t;
	}

	// NEXT RIGHT
	// This function works analogously to NEXT LEFT.
	function d3_layout_treeRight(v) {
		var children = v.children, n;
		return (n = children.length) ? children[n - 1] : v.t;
	}

	// MOVE SUBTREE
	// Shifts the current subtree rooted at w+. This is done by increasing
	// prelim(w+) and mod(w+) by shift.
	function d3_layout_treeMove(wm, wp, shift) {
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
	function d3_layout_treeShift(v) {
		var shift = 0, change = 0, children = v.children, i = children.length, w;
		while (--i >= 0) {
			w = children[i];
			w.z += shift;
			w.m += shift;
			shift += w.s + (change += w.c);
		}
	}
	// ANCESTOR
	// If vi-’s ancestor is a sibling of v, returns vi-’s ancestor. Otherwise,
	// returns the specified (default) ancestor.
	function d3_layout_treeAncestor(vim, v, ancestor) {
		return vim.a.parent === v.parent ? vim.a : ancestor;
	}

	//Verbetum Copy	
	function d3_layout_hierarchyRebind(object, hierarchy) {
		d3.rebind(object, hierarchy, "sort", "children", "value");
		object.nodes = object;
		object.links = d3_layout_hierarchyLinks;
		return object;
	}
	function d3_layout_hierarchyLinks(nodes) {
		return d3.merge(nodes.map(function (parent) {
			return (parent.children || []).map(function (child) {
				return {
					source: parent,
					target: child
				};
			});
		}));
	}
	function d3_layout_hierarchyVisitBefore(node, callback) {
		var nodes = [node];
		while ((node = nodes.pop()) != null) {
			callback(node);
			if ((children = node.children) && (n = children.length)) {
				var n, children;
				while (--n >= 0) nodes.push(children[n]);
			}
		}
	}
	function d3_layout_hierarchyVisitAfter(node, callback) {
		var nodes = [node], nodes2 = [];
		while ((node = nodes.pop()) != null) {
			nodes2.push(node);
			if ((children = node.children) && (n = children.length)) {
				var i = -1, n, children;
				while (++i < n) nodes.push(children[i]);
			}
		}
		while ((node = nodes2.pop()) != null) {
			callback(node);
		}
	}

}();