angular.module("tree", []).directive("tree", ["$timeout", function ($timeout) {
	return {
		templateNamespace: 'svg',
		restrict: "E",
		scope: {
			graph: "=graph", ttEnter: "=?ttEnter", ttUpdate: "=?ttUpdate", ttExit: "=?ttExit", ttWatch: "=?ttWatch", ttPostExit: "=?ttPostExit",
			ttDragStart: "=?ttDragStart", ttDragEnd: "=?ttDragEnd",
			ttCollapse: "=?ttCollapse", ttExpand: "=?ttExpand",
		},
		transclude: true,
		replace: true,
		template: '<g class="tt-tree"></g>',
		controller: ["$element", "$scope", "$rootScope", "$timeout", "$q", function ($element, $scope, $rootScope, $timeout, $q) {

			var svg = $element.closest("svg");
			try {
				var filter = d3.select(svg[0]).append("filter");

				filter.attr("id", "glow")
                    .attr("width", "10px")
                    .attr("height", "10px")
                    .attr("x", -.75)
                    .attr("y", -.75);
				filter.append("feFlood")
                    .attr("result", "flood")
                    .attr("flood-color", "gray")
                    .attr("flood-opacity", "1");

				filter.append("feComposite")
                    .attr("in", "flood")
                    .attr("result", "mask")
		            .attr("in2", "SourceGraphic")
		            .attr("operator", "in");
				filter.append("feMorphology")
                    .attr("in", "mask")
                    .attr("result", "dilated")
                    .attr("radius", "2")
                    .attr("operator", "dilate");
				filter.append("feGaussianBlur")
                    .attr("in", "dilated")
                    .attr("result", "blurred")
                    .attr("stdDeviation", "5");

				//filter.append("feColorMatrix")
				//    .attr("result","bluralpha")
				//    .attr("type","matrix")
				//    .attr("values", "1 0 0 0   0 0 1 0 0   0		        0 0 1 0   0		        0 0 0 0.4 0 ");

				//filter.append("feOffset")
				//    .attr("in","bluralpha")
				//    .attr("dx","3")
				//    .attr("dy","3")
				//    .attr("result","offsetBlur");

				var femerge = filter.append("feMerge");
				femerge.append("feMergeNode").attr("in", "blurred");
				femerge.append("feMergeNode").attr("in", "SourceGraphic");


			} catch (e) {
				console.error(e);
			}

			$scope.duration = 250;

			function getNode(source) {
				if (typeof (source) === "string") {
					source = +source;
				}
				if (typeof (source) === "number") {
					return d3.select("[data-id='" + source + "']").datum();
				}
				if (typeof (source) === "object") {
					return source;
				}
				return false;
			}
			function getNodeId(source) {
				if (typeof (source) === "string") {
					return source = +source;
				}
				if (typeof (source) === "number") {
					return source
				}
				if (typeof (source) === "object") {
					return source.Id;
				}
				return false;
			}
			$scope.expandAll = function () {
				var dive = function (d) {
					if (d && d._children) {
						d.children = d._children;
						d._children = null;
					}
					if (d.children) {
						for (var i in d.children)
							dive(d.children[i]);
					}
				}
				var d = $scope.root;
				dive(d);
			};
			$scope.collapseAll = function () {
				var dive = function (d) {
					if (d.children) {
						for (var i in d.children)
							dive(d.children[i]);
					}
					if (d && d.children) {
						d._children = d.children;
						d.children = null;
					}
				}
				var d = $scope.root;
				dive(d);
			};

			$scope.expand = function (d) {
				d = getNode(d);
				if (d && d._children) {
					d.children = d._children;
					//d.children.forEach($scope.expand);
					d._children = null;
				}
			};
			$scope.collapse = function (d) {
				d = getNode(d);
				if (d && d.children) {
					d._children = d.children;
					//d._children.forEach($scope.collapse);
					d.children = null;
				}
			};
			$scope.toggle = function (d) {
				d = getNode(d);
				if (d && d._children) {
					$scope.expand(d);
				} else if (d && d.children) {
					$scope.collapse(d);
				}
			};
			$scope.centerNode = function (source, ms) {
				if (typeof (ms) === "undefined")
					ms = 1;

				$scope.showNode(source);
				return $q(function (resolve, reject) {
					$timeout(function () {
						try {
							var scale = 1;
							var pz3;
							var pz = $element.closest('.pz-pan');
							if (pz) {
								var panzoom = pz.scope().panzoom;
								if (panzoom)
									scale = panzoom.scale();
							}
							source = getNode(source);
							//if (typeof (source) === "number") {
							//	source = d3.select("[data-id='" + source + "']").datum();
							//}
							if (source) {
								console.log("CenterNode called " + source.Id);
								x = -source.x0;
								y = -source.y0;
								x = x * scale + svg.width() / 2;
								y = y * scale + svg.height() / 3;
								scale = Math.max(1, scale);
								d3.select(pz[0]).transition()
                                    .duration($scope.duration)
                                    .ease("quad-in-out")
                                    .attr("transform", "translate(" + x + "," + y + ")scale(" + scale + ")");
								if (panzoom) {
									panzoom.scale(scale);
									panzoom.translate([x, y]);
									$timeout(function () {
										resolve();
									}, $scope.duration);
								} else {
									reject("No panzoom");
								}
							} else {
								reject("Element not found");
							}
						} catch (e) {
							console.info("Could not center:", e);
						}
					}, ms);
				});

			}

			$scope.showNode = function (source) {
				var id = getNodeId(source);
				console.log("ShowNode called " + id);
				//http://bl.ocks.org/jjzieve/a743242f46321491a950
				function searchTree(obj, id, path) {
					if (obj.Id === id) { //if search is found return, add the object to the path and return it
						//path.push(obj);
						return path;
					}
					else if (obj.children || obj._children) { //if children are collapsed d3 object will have them instantiated as _children
						var children = (obj.children) ? obj.children : obj._children;
						for (var i = 0; i < children.length; i++) {
							path.push(obj);// we assume this path is the right one
							var found = searchTree(children[i], id, path);
							if (found) {// we were right, this should return the bubbled-up path from the first if statement
								return found;
							}
							else {//we were wrong, remove this parent from the path and continue iterating
								path.pop();
							}
						}
					}
					else {//not the right object, return false so it will continue to iterate in the loop
						return false;
					}
				}
				function openPaths(paths) {
					for (var i = 0; i < paths.length; i++) {
						if (paths[i].Id !== "1") {//i.e. not root
							paths[i].class = 'found';
							if (paths[i]._children) { //if children are hidden: open them, otherwise: don't do anything
								paths[i].children = paths[i]._children;
								paths[i]._children = null;
							}
							$scope.updater(paths[i]);
						}
					}
				}


				var paths = searchTree($scope.root, id, []);
				if (typeof (paths) !== "undefined") {
					openPaths(paths);
				}
			}

			$scope.selectNode = function (id) {
				console.log("SelectNode called " + id);
				$scope.showNode(id)
				setTimeout(function () {
					d3.select(".selected").classed("selected", false).attr("filter", null);
					var all = d3.selectAll("[data-id='" + id + "']").classed("selected", true).attr("filter", "url(#glow)");
					setTimeout(function () {
						$scope.centerNode(id).then(function () {
							setTimeout(function () {
								all.attr("filter", null);
							}, 500);
						}, console.error);
					}, $scope.duration || 250);

				}, 1);
			}

			$scope.swap = function (nodeId, parentId) {
				console.log("Swap called on" + nodeId + "=>" + parentId);
				var draggingNode = null;
				var selectedNode = null;
				var nodes = $scope.tree.nodes($scope.root);
				for (var nid in nodes) {
					var n = nodes[nid];
					if (n.Id == nodeId)
						draggingNode = n;
					if (n.Id == parentId)
						selectedNode = n;
				}
				if (draggingNode == null)
					throw "Node not found";
				if (selectedNode == null)
					throw "Parent not found";

				var index = draggingNode.parent.children.indexOf(draggingNode);
				if (index > -1) {
					draggingNode.parent.children.splice(index, 1);
				}
				if (Array.isArray(selectedNode.children) || Array.isArray(selectedNode._children)) {
					if (Array.isArray(selectedNode.children)) {
						selectedNode.children.push(draggingNode);
					} else {
						selectedNode._children.push(draggingNode);
					}
				} else {
					selectedNode.children = [];
					selectedNode.children.push(draggingNode);
				}
			}

			$rootScope.$on("CenterNode", function (event, source, ms) {
				//setTimeout(function () {
				return $scope.centerNode(source, ms);
				//}, 1);
			});
			$rootScope.$on("ShowNode", function (event, id) {
				$scope.showNode(id);
			});
			$rootScope.$on("SelectNode", function (event, id) {
				$scope.selectNode(id);
			});
			$rootScope.$on("SwapNode", function (event, id, parent) {
				$scope.swap(id, parent);
			});

			$rootScope.$on("ExpandNode", function (event, id) {
				$scope.expand(id);
			});
			$rootScope.$on("CollapseNode", function (event, id) {
				$scope.collapse(id);
			});
			$rootScope.$on("ExpandAllNodes", function (event) {
				$scope.expandAll();
			});
			$rootScope.$on("CollapseAllNodes", function (event) {
				$scope.collapseAll();
			});
			$rootScope.$on("ToggleNode", function (event, id) {
				$scope.toggle(id);
			});
			$rootScope.$on("RefreshTree", function (event, id) {
				$scope.updater($scope.root);
			});




		}],
		link: function (scope, element, attr, ctrl, transclude) {

			var minHeight = 20;
			var minWidth = 20;

			var i = 0,
            duration = scope.duration;
			scope.root = scope.root || {};

			var vSeparation = 40;
			var hSeparation = 25;
			var height = scope.graph.height;
			var width = scope.graph.width;

			var baseWidth = 250;
			var baseHeight = 50;

			var panBoundary = 1;

			var tree = d3.layout.tree()
                .nodeSize([baseWidth, baseHeight]);

			scope.tree = tree;

			var diagonal = function (d) {
				var sx = d.source.x, sy = d.source.y;
				var tx = d.target.x, ty = d.target.y;
				return "M" + sx + "," + sy
                 + "L" + (sx) + "," + (ty - vSeparation / 4)
                 + " " + (tx) + "," + (ty - vSeparation / 4)
                 + " " + (tx) + "," + ty;
				+ " " + (tx) + "," + ty;
			};

			var svg = element.closest("svg");
			var self = d3.select(element[0]);
			var selfSvg = d3.select(element[0]);

			var expandSource = scope.root;

			scope.updater = function (source, first) {

				if (typeof (first) === "undefined")
					first = true;
				// Compute the new tree layout.
				var nodes = tree.nodes(scope.root),//.reverse(),
                    links = tree.links(nodes);

				// Normalize for fixed-depth.
				var maxHeightRow = {};
				maxHeightRow[-1] = 0;

				var maxWidth = baseWidth;

				var rowWidth = {};

				var maxDepth = 0;
				nodes.forEach(function (d) {
					maxHeightRow[d.depth] = Math.max((d.height || minHeight), maxHeightRow[d.depth] || 0);
					maxDepth = Math.max(maxDepth, d.depth);
					rowWidth[d.depth] = rowWidth[d.depth] || [];
					rowWidth[d.depth].push({
						ox: d.x,
						w: d.width
					});
					maxWidth = Math.max(maxWidth, d.width);
				});
				for (var i = 1; i <= maxDepth; i++) {
					maxHeightRow[i] = maxHeightRow[i] + maxHeightRow[i - 1];
					rowWidth[i].sort(function (a, b) {
						return a.ox - b.ox;
					});
				}
				nodes.forEach(function (d) {
					d.y = d.depth * vSeparation + maxHeightRow[d.depth - 1];
					var shift = 0;
					for (var i = 0; i < rowWidth[d.depth].length; i++) {
						var ww = (rowWidth[d.depth][i].w - baseWidth + hSeparation);
						// shift+=ww;
						if (rowWidth[d.depth][i].ox == d.x) {
							break;
						}
						shift += ww;
					}
					d.ddx = shift;
				});

				//

				// Update the nodes�
				var node = self.selectAll("g.node")
                    .data(nodes, function (d) {
                    	return (d.Id);//|| (d.Id = ++i));
                    });


				// Enter any new nodes at the parent's previous position.
				var nodeEnter = node.enter().append("g")
                    .call(scope.dragListener || function () { })
                    .call(dragListener)
                    .attr("class", "node")
                    .style("opacity", -10)
                    .attr("transform", function (d) {
                    	var es = expandSource;
                    	if (!es || !es.x0 || !es.y0)
                    		es = source;

                    	return "translate(" + (es.x0) + "," + es.y0 + ")";
                    })
					.classed("root-node", function (d) {
						return d.Id == scope.root.Id;
					})
					.classed("no-edit", function (d) {
						return d.Editable == false;
					})
					.classed("is-me", function (d) {
						return d.Me == true;
					}).on("click", expandCollapse).on("keydown", function (d) {
						if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
							expandCollapse(d);
					});


				if (scope.ttEnter) {
					scope.ttEnter(nodeEnter);
				} else {
					nodeEnter.append("rect").attr("width", 20).attr("height", 20).attr("x", -10).attr("y", 0);
				}

				var ghost = nodeEnter.append("g").attr('class', 'ghost');
				ghost.append("circle").on("mouseover", overCircle).on("mouseout", outCircle);
				ghost.append("text").text("").on("mouseover", overCircle).on("mouseout", outCircle);

				// Transition nodes to their new position.
				var nodeUpdate = node
                    .attr("data-id", function (d) { return d.Id })
                    .attr("data-height", function (d) {
                    	var maxH = 0; var maxW = 0;
                    	var dive = function (parent) {
                    		if ($(parent).css("display") == "none")
                    			return;

                    		var oh = $(parent).outerHeight();
                    		var ow = $(parent).outerWidth();
                    		if (ow > 0)
                    			maxH = Math.max(maxH, oh);
                    		if (oh > 0)
                    			maxW = Math.max(maxW, ow);
                    		$(parent).children().each(function () {
                    			dive(this);
                    		});
                    	}
                    	dive(this);
                    	d.height = maxH;
                    	d.width = maxW;
                    	return d.height;
                    })
                    .attr("data-width", function (d) {
                    	return d.width;
                    })
                    .classed("collapsed", function (d) {
                    	return d._children && d._children.length;
                    })
                    .transition()
                    .duration(duration)
                    .style("opacity", 1)
                    .attr("transform", function (d) { return "translate(" + (d.x - d.width / 2) + "," + d.y + ")"; });

				nodeUpdate.select(".ghost circle")
                    .attr("r", 10)
                    //.attr("height", function (d) { return d.height + 16; })
                    .attr("transform", function (d) { return "translate(" + (d.width / 2 - .25) + "," + (d.height + 15.5) + ")"; });

				if (scope.ttUpdate)
					scope.ttUpdate(node);



				// Transition exiting nodes to the parent's new position.
				var nodeExit = node.exit();


				//nodeExit.transition().duration(duration);


				var afterExit = nodeExit.attr("transform", function (d) { return "translate(" + source.x + "," + source.y + ")"; })
                    .style("opacity", -10);

				if (scope.ttExit)
					scope.ttExit(afterExit);

				afterExit.remove();

				//if (scope.ttPostExit)
				//	scope.ttPostExit(afterExit);

				//.call(function () {
				//	var a = this[0];
				//	for (var i in a) {
				//		if (typeof (a[i]) !== "undefined")
				//			angular.element(a[i]).scope()//.$destroy();
				//	}
				//})



				// Update the links�
				var link = self.selectAll("path.link")
                    .data(links, function (d) { return d.target.Id; });

				// Enter any new links at the parent's previous position.
				link.enter().insert("path", "g")
                    .attr("class", "link")
                    .style("opacity", 0)
					.classed("root-node-link", function (d) {
						if (d.source && scope.root)
							return d.source.Id == scope.root.Id;
						return false;
					});

				// Transition links to their new position.
				link.attr("d", function (d) {
					var s = d.source;
					var t = d.target;
					var o = { source: { x: s.x, y: s.y + (s.height || minHeight) }, target: { x: t.x, y: t.y } };
					return diagonal(o);
				})
					.transition()
                    .duration(duration * 2)
                    .style("opacity", 1);

				// Transition exiting nodes to the parent's new position.
				link.exit().transition()
                    .duration(duration)
                    .attr("d", function (d) {
                    	var o = { x: source.x, y: source.y };
                    	return diagonal({ source: o, target: o });
                    })
                    .style("opacity", 0)
					.remove();

				// Stash the old positions for transition.
				nodes.forEach(function (d) {
					d.x0 = d.x;
					d.y0 = d.y;
				});

				var maxWidth = baseWidth;
				nodes.forEach(function (d) {
					maxWidth = Math.max(maxWidth, d.width);
				});

				tree.nodeSize([maxWidth + hSeparation, baseHeight]);

				if (first)
					$timeout(function () { scope.updater(scope.root, false); }, 1);
			}

			var nestWatch = function (node) {
				if (node) {
					var children = null;
					if (node.children || node._children) {
						var flatChilds = (node.children || node._children);
						var dict = {};
						for (var i in flatChilds) {
							dict[flatChilds[i].Key] = nestWatch(flatChilds[i]);
						}
						children = dict;//flatChilds.map(function (x) { return nestWatch(x) });
					}
					var nr = {};
					if (scope.ttWatch)
						nr = scope.ttWatch(node);
					nr.children = children;
					if (node._children || !node.children)
						nr.collapsed = true;
					else
						nr.collapsed = false;
					return nr;

				} else {
					return undefined;
				}
			};

			var newValUndefined = false;
			scope.$watch(function () {
				if (scope.graph) {
					var r = null;
					var cn = null;
					var sn = null;
					var en = null;
					if (scope.graph.data) {
						r = scope.graph.data.Root;
						cn = scope.graph.data.CenterNode;
						en = scope.graph.data.ExpandNode;
						sn = scope.graph.data.ShowNode;
					}
					var n = nestWatch(r);
					return {
						nest: n,
						center: cn,
						expand: en,
						show: sn
					};
				}
				return undefined;
			}, function (newVal, oldVal) {
				console.log("Update called");
				if (typeof (newVal) === "undefined")
					newValUndefined = true;
				else if (newValUndefined)
					$timeout(function () { scope.updater(scope.root); }, 1);
				if (scope.graph && scope.graph.data && scope.graph.data.Root) {
					scope.root = scope.graph.data.Root;
					scope.root.x = svg.width() / 2;
					scope.root.y = 0;
					scope.root.x0 = svg.width() / 2;
					scope.root.y0 = 0;

					$timeout(function () {
						//console.log("timeout1")
						scope.updater(scope.root);
						//console.log("timeout2")
					}, 1);

					if (newVal && (!oldVal || newVal.center !== oldVal.center))
						scope.centerNode(newVal.center, 200);

					if (newVal && newVal.expand && (!oldVal || newVal.expand !== oldVal.expand))
						scope.expand(newVal.expand, 200);

					if (newVal && newVal.show && (!oldVal || newVal.show !== oldVal.show))
						scope.showNode(newVal.show);


					//var dive 


				}
			}, true);
			// Toggle children on click.
			function expandCollapse(d) {
				expandSource = d;
				if (d.children) {
					if (typeof (scope.ttCollapse) === "function") {
						try {
							scope.ttCollapse(d);
						} catch (e) {
							return;
						}
					}
					d._children = d.children;
					d.children = null;
				} else {
					if (typeof (scope.ttExpand) === "function") {
						try {
							scope.ttExpand(d);
						} catch (e) {
							return;
						}
					}
					d.children = d._children;
					d._children = null;
				}
				scope.updater(d);
				scope.centerNode(d);
			}

			var svgGroup = self;
			var root = scope.root;
			var selectedNode = null;
			var panTimer = true;
			var dragStarted = false;
			var domNode = null;
			var panSpeed = 200;
			var panBoundary = 20; // Within 20px from edges will pan when dragging.
			var draggingNode = null;

			var updateTempConnector = function () {
				var data = [];
				if (draggingNode !== null && selectedNode !== null) {
					// have to flip the source coordinates since we did this for the existing connectors on the original tree
					data = [{
						source: {
							x: selectedNode.x0,
							y: selectedNode.y0 + selectedNode.height
						},
						target: {
							x: draggingNode.x0,
							y: draggingNode.y0 + 50
						}
					}];
				}
				var link = svgGroup.selectAll(".templink").data(data);

				link.enter().append("path")
                    .attr("class", "templink")
                    .attr("d", d3.svg.diagonal())
                    .attr('pointer-events', 'none');

				link.attr("d", d3.svg.diagonal());

				link.exit().remove();
			};
			function pan(domNode, direction) {
				var speed = panSpeed;
				var pz = element.closest('.pz-pan');
				var svgGroup = d3.select(pz[0]);
				var zoomListener = pz.scope().panzoom;
				if (panTimer) {
					clearTimeout(panTimer);
					console.log("clear2");
					translateCoords = d3.transform(svgGroup.attr("transform"));
					if (direction == 'left' || direction == 'right') {
						translateX = direction == 'left' ? translateCoords.translate[0] + speed : translateCoords.translate[0] - speed;
						translateY = translateCoords.translate[1];
					} else if (direction == 'up' || direction == 'down') {
						translateX = translateCoords.translate[0];
						translateY = direction == 'up' ? translateCoords.translate[1] + speed : translateCoords.translate[1] - speed;
					}
					scaleX = translateCoords.scale[0];
					scaleY = translateCoords.scale[1];
					scale = zoomListener.scale();
					svgGroup.transition().attr("transform", "translate(" + translateX + "," + translateY + ")scale(" + scale + ")");
					d3.select(domNode).select('g.node').attr("transform", "translate(" + translateX + "," + translateY + ")");
					zoomListener.scale(zoomListener.scale());
					zoomListener.translate([translateX, translateY]);
					panTimer = setTimeout(function () {
						pan(domNode,/* speed*10, */direction);
						console.log("shift " + direction);
					}, 50);
				}
			}

			//	function applyPan(domNode,direction)

			var overCircle = function (d) {
				selectedNode = d;
				updateTempConnector();
			};
			var outCircle = function (d) {
				selectedNode = null;
				updateTempConnector();
			};


			function initiateDrag(d, domNode) {
				expandSource = d;
				console.log("init drag");
				draggingNode = d;
				d3.select(domNode).select('.ghost').attr('pointer-events', 'none');
				d3.selectAll('.ghost').attr('class', 'ghost showing');
				d3.select(domNode).classed('activeDrag', true).attr("filter", "url(#glow)");


				svgGroup.selectAll("g.node").sort(function (a, b) { // select the parent and sort the path's
					if (a.Id != draggingNode.Id) return -1; // a is not the hovered element, send "a" to the back
					else return 1; // a is the hovered element, bring "a" to the front
				});
				// if nodes has children, remove the links and nodes
				if (nodes.length > 1) {
					// remove link paths
					links = tree.links(nodes);
					nodePaths = svgGroup.selectAll("path.link")
                        .data(links, function (d) {
                        	return d.target.Id;
                        }).remove();
					// remove child nodes
					nodesExit = svgGroup.selectAll("g.node")
                        .data(nodes, function (d) {
                        	return d.Id;
                        }).filter(function (d, i) {
                        	if (d.Id == draggingNode.Id) {
                        		return false;
                        	}
                        	return true;
                        }).remove();
				}

				// remove parent link
				parentLink = tree.links(tree.nodes(draggingNode.parent));
				svgGroup.selectAll('path.link').filter(function (d, i) {
					if (d.target.Id == draggingNode.Id) {
						return true;
					}
					return false;
				}).remove();

				dragStarted = null;
			};
			// Define the drag listeners for drag/drop behaviour of nodes.

			var ox, oy;
			var skipDrag = false;
			dragListener = d3.behavior.drag()
                .on("dragstart", function (d) {
                	skipDrag = false;
                	if (d == scope.root)
                		skipDrag = true;
                	//if ($("input:focus").length > 0) {
                	//	skipDrag = true;
                	//}

                	if (skipDrag)
                		return;
                	if (typeof (scope.ttDragStart) === "function") {
                		try {
                			scope.ttDragStart(d);
                		} catch (e) {
                			//console.log("skipping drag");
                			skipDrag = true;
                			return;
                		}
                	}
                	if (skipDrag)
                		return;
                	selfSvg.classed("is-dragging", true);

                	//var pz = element.closest('.pz-pan');
                	dragStarted = true;
                	nodes = tree.nodes(d);
                	ox = d.width / 2;
                	oy = -50;

                	console.log(ox, oy);
                	d3.event.sourceEvent.stopPropagation();
                	// it's important that we suppress the mouseover event on the node being dragged. Otherwise it will absorb the mouseover event and the underlying node will not detect it d3.select(this).attr('pointer-events', 'none');
                })
                .on("drag", function (d) {
                	if (skipDrag)
                		return;
                	if (dragStarted) {
                		domNode = this;
                		initiateDrag(d, domNode);
                	}
                	var pz = element.closest('.pz-pan');

                	// get coords of mouseEvent relative to svg container to allow for panning
                	relCoords = d3.mouse(svg.get(0));
                	console.log(relCoords);

                	if (relCoords[0] < panBoundary) {
                		panTimer = true;
                		pan(this, 'left');
                	} else if (relCoords[0] > (svg.width() - panBoundary)) {
                		panTimer = true;
                		pan(this, 'right');
                	} else if (relCoords[1] < panBoundary) {
                		panTimer = true;
                		pan(this, 'up');
                	} else if (relCoords[1] > (svg.height() - panBoundary)) {
                		panTimer = true;
                		pan(this, 'down');
                	} else {
                		try {
                			clearTimeout(panTimer);
                			console.log("clear1");
                		} catch (e) {

                		}
                	}

                	d.x0 += d3.event.dx;
                	d.y0 += d3.event.dy;
                	var node = d3.select(this);
                	node.attr("transform", "translate(" + (d.x0 - ox) + "," + (d.y0 - oy) + ")");
                	updateTempConnector();
                })
                .on("dragend", function (d) {
                	selfSvg.classed("is-dragging", false);
                	panTimer = null;
                	if (skipDrag) {
                		return;
                	}
                	domNode = this;
                	var dnid = null;
                	if (draggingNode)
                		dnid = draggingNode.Id;

                	var dat = {
                		oldParentId: null,
                		newParentId: null,
                		id: dnid,
                		swap: false
                	};
                	if (selectedNode) {
                		dat.swap = true;
                		// now remove the element from the parent, and insert it into the new elements children
                		dat.oldParentId = draggingNode.parent.Id;

                		scope.swap(draggingNode.Id, selectedNode.Id);

                		dat.newParentId = selectedNode.Id;
                		// Make sure that the node being added to is expanded so user can see added node is correctly moved
                		scope.expand(selectedNode);
                		//sortTree();
                		endDrag();
                	} else {
                		endDrag();
                	}
                	if (typeof (scope.ttDragEnd) === "function") {
                		scope.ttDragEnd(d, dat);
                	}
                });

			function endDrag() {
				selectedNode = null;
				d3.selectAll('.ghost').attr('class', 'ghost');
				d3.select(domNode).classed('activeDrag', false).attr("filter", null);
				// now restore the mouseover event or we won't be able to drag a 2nd time
				d3.select(domNode).select('.ghost').attr('pointer-events', '');
				updateTempConnector();
				if (draggingNode !== null) {
					scope.updater(scope.root);
					var dn = draggingNode;
					draggingNode = null;
				}
			}
		}
	}
}]);