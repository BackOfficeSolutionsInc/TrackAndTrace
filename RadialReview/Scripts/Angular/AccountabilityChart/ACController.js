var acapp = angular.module('ACApp', ['helpers', 'panzoom', 'tree']);

acapp.directive('mdBlur', ["$timeout", function ($timeout) {
	var directive = {
		restrict: 'A',
		link: function (scope, element, attributes) {
			$timeout(function () {
				angular.element(element[0].querySelector("input")).bind("blur", function () {
					var that = this;
					$timeout(function () {
						//console.log(that);
						//console.log(element);
						scope.$eval(attributes.mdBlur);
					}, 100);
				});
			}, 0);
		}
	};

	return directive;
}]);



acapp.controller('ACController', ['$scope', '$http', '$timeout', '$location', 'radial', 'orgId', 'chartId', 'dataUrl', "$compile", "$sce", "$q", "$window", "$rootScope",
function ($scope, $http, $timeout, $location, radial, orgId, chartId, dataUrl, $compile, $sce, $q, $window, $rootScope) {
	$scope.orgId = orgId;
	$scope.chartId = chartId;
	$scope.model = $scope.model || {};
	$scope.model.height = $scope.model.height || 10000;
	$scope.model.width = $scope.model.width || 10000;

	function rejoin(connection, proxy, callback) {
		try {
			if (proxy) {
				proxy.invoke("join", $scope.orgId, connection.id).done(function () {
					console.log("rejoin");
					//$(".rt").prop("disabled", false);
					if (callback) {
						callback();
					}
					if ($scope.disconnected) {
						clearAlerts();
						showAlert("Reconnected.", "alert-success", "Success");
					}
					$scope.disconnected = false;
				});
			}
		} catch (e) {
			console.error(e);
		}
	}

	var r = radial($scope, 'organizationHub', rejoin);
	var tzoffset = r.updater.tzoffset;
	$scope.functions.reload = function (clearData) {
		if (typeof (clearData) === "undefined")
			clearData = false;
		tzoffset();
		console.log("reloading...");
		var url = dataUrl;
		if (dataUrl.indexOf("{0}") != -1) {
			url = url.replace("{0}", $scope.chartId);
		} else {
			url = url + $scope.chartId;
		}

		var date = ((+new Date()) + (window.tzoffset * 60 * 1000));
		if (dataUrl.indexOf("?") != -1) {
			url += "&_clientTimestamp=" + date;
		} else {
			url += "?_clientTimestamp=" + date;
		}

		$http({ method: 'get', url: url })
        .success(function (data, status) {
        	var ddr = undefined;

        	console.log("Data Loaded");
        	r.updater.convertDates(data);
        	if (clearData) {
        		r.updater.clearAndApply(data);
        		$scope.search.searchTerms = [];
        	} else {
        		r.updater.applyUpdate(data);
        	}
        	$rootScope.$emit("SelectNode", $scope.model.center || 0);

        }).error(function (data, status) {
        	console.log("Error");
        	console.error(data);
        });
	}
	$scope.functions.reload(true);

	var self = this;

	$scope.functions.selectedItemChange = function (node) {
		d3.select(".selected").classed("selected", false).attr("filter", null);
		if (node) {
			$rootScope.$emit("SelectNode", node.Id);
		}
	};

	$scope.search = {};
	$scope.search.querySearch = function (query) {
		function createFilterFor(query) {
			var lowercaseQuery = angular.lowercase(query);
			return function filterFn(x) {
				var index = (x.Name + "").toLowerCase().indexOf(lowercaseQuery);
				var any = index != -1 && (index === 0 || x.Name[index - 1] == " ");
				if (x.User && x.User.Name) {
					var f = x.User.Name.toLowerCase().indexOf(lowercaseQuery);
					any = any || (f != -1 && f == 0 || x.User.Name[f - 1] == " ");
				}
				return any;
			};
		}
		var possible = $scope.model.data.AllUsers;
		return possible.filter(createFilterFor(query));
	}

	$scope.search.queryPositions = function (query) {
		return $http({
			method: 'GET',
			url: '/dropdown/angularpositions?create=true&q=' + query
		}).then(function (results) {
			return results.data;
		});
	}

	$scope.search.findNode = function (query) {

		function createFilterFor(query) {
			var lowercaseQuery = angular.lowercase(query);
			return function filterFn(x) {
				var any = (x.Name + "").toLowerCase().indexOf(lowercaseQuery) === 0;
				if (x.User && x.User.Name) {
					var f = x.User.Name.toLowerCase().indexOf(lowercaseQuery);
					any = any || (f != -1 && f == 0 || x.User.Name[f - 1] == " ");
				}
				return any;
			};
		}

		var possible = [];
		for (var i in $scope.model.Lookup) {
			var n = $scope.model.Lookup[i];
			if (i.indexOf("AngularAccountabilityNode_") == 0 && n.User) {
				possible.push(n);
			}
		}
		return possible.filter(createFilterFor(query));
	}

	$scope.search.searchTerms = {};

	$scope.nodeWatch = function (node) {
		var uname = null;
		var roles = null;
		if (node.User && node.User.Name)
			uname = node.User.Name;
		if (node.Group && node.Group.RoleGroups)
			roles = node.Group.RoleGroups;
		return {
			name: uname,
			roles: roles
		}
	}

	function fixNodeRecurse(self) {
		if (self && self.Type == "AngularAccountabilityNode") {
			//var parentId = null;
			//if (self.parent) {
			//    self.parent.children = null;
			//    self.parent._children = null;
			////}
			//delete self.Id;
			delete self.children;
			delete self._children;
			delete self.parent;
			//delete self.Group;
			//delete self.User;
			////if (self.children) {
			//    self.children = null;
			//    //for (var i in self.children) {
			//    //    self.children[i] = {
			//    //        Id: self.children[i].Id
			//    //    };
			//    //}
			//}
		}
	}

	$scope.functions.sendUpdate = function (self) {
		var dat = angular.copy(self);
		var _clientTimestamp = new Date().getTime();

		fixNodeRecurse(dat);

		r.updater.convertDatesForServer(dat, tzoffset());

		$http.post("/Accountability/Update" + self.Type + "?connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, dat).error(function (data) {
			showJsonAlert(data, true, true);
		});
	};

	$scope.dragStart = function (d) {
		if (!d3.select(d3.event.sourceEvent.srcElement).classed("move-icon")) {
			throw "Incorrect selector";
		}
	}
	var SwapParent = Undo.Command.extend({
		constructor: function (data) {
			this.id = data.id;
			this.oldParent = data.oldParentId;
			this.newParent = data.newParentId;

			this.change = function (nodeId, newParent, revertId) {
				var _clientTimestamp = new Date().getTime();
				$http.post("/Accountability/Swap/" + nodeId + "?parent=" + newParent + "&connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, {})
					.then(function () { }, function (data) {
						showJsonAlert(data.data, true, true);
						$rootScope.$emit("SwapNode", nodeId, revertId);

					});
			}
		},
		execute: function () {
			$rootScope.$emit("SwapNode", this.id, this.newParent);
			this.change(this.id, this.newParent, this.oldParent);
		},
		undo: function () {
			$rootScope.$emit("SwapNode", this.id, this.oldParent);
			this.change(this.id, this.oldParent, this.newParent);
		}
	});

	$scope.dragEnd = function (d, data) {
		if (data) {
			if (data.swap) {
				var _clientTimestamp = new Date().getTime();
				console.log(data.oldParentId + "->" + data.newParentId);
				undoStack.execute(new SwapParent(data));
			}
		}
	}

	$scope.collapseExpand = function (d) {
		//debugger;
		//if (!d3.select(d3.event.srcElement).classed("minimize-icon")) {
		//	throw "Incorrect selector";
		//}
		if ($(d3.event.srcElement).closest(".minimize-icon").length != 1) {
			throw "Incorrect selector";
		}
	}


	$scope.functions.selectedItemChange_UpdateNode = function (id) {
		$scope.functions.sendUpdate($scope.model.Lookup['AngularAccountabilityNode_' + id + ''])
	};


	$scope.clearIfNull = function (item, searchText) {
		if (!item) {
			$scope.$eval(searchText + "=null");
		}
	}

	$scope.nodeEnter = function (nodeEnter) {
		var rect = nodeEnter.append("rect")
            .attr("class", "acc-rect")
            .attr("width", 0)
            .attr("height", 0)
            .attr("x", 0)
            .attr("rx", 2)
            .attr("ry", 2);


		var node = nodeEnter.append("foreignObject")
            .append("xhtml:div")
            .classed("acc-node", true)
            .style("font", "14px 'Helvetica Neue'");

		var buttonsTop = node.append("xhtml:div")
            .classed("acc-buttons move-icon top-bar", true);

		//buttonsTop.append("xhtml:span").classed("button minimize", true).append("xhtml:span")
		//buttons.append("xhtml:span").classed("button move", true).append("xhtml:span").classed("move-icon glyphicon glyphicon-move", true);


		var position = node.append("xhtml:div")
            .classed("acc-position", true);

		var posAutoComplete = position.append("md-autocomplete")
			.attr("placeholder", "Function")
            .attr("md-blur", function (d) {
            	return "clearIfNull(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position,\"search.searchPos_" + d.Id + "\")";
            }).attr("md-selected-item", function (d) {
            	return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position";
            }).attr("md-item-text", function (d) {
            	return "pitem.Name";
            }).attr("md-items", function (d) { return "pitem in search.queryPositions(search.searchPos_" + d.Id + ")"; })
            .attr("md-search-text", function (d) { return "search.searchPos_" + d.Id; })
            .attr("md-selected-item-change", function (d) {
            	return "functions.selectedItemChange_UpdateNode(" + d.Id + ")";
            }).attr("md-no-cache", "true").attr("md-delay", "300");
		posAutoComplete.append("md-item-template")
            .append("span")
            .attr("md-highlight-text", function (d) { return "search.searchPos_" + d.Id; })
            .attr("md-highlight-flags", "^i")
            .text("{{pitem.Name}}");//.attr("");
		posAutoComplete.append("md-not-found")
           .text(function (d) {
           	return "No matches were found.";
           });//.attr("");


		var owner = node.append("xhtml:div")
            .classed("acc-owner", true);

		var autoComplete = owner.append("md-autocomplete")
			.attr("md-blur", function (d) {
				return "clearIfNull(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User,\"search.searchText_" + d.Id + "\")";
			}).attr("md-selected-item", function (d) {
				return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User";
			}).attr("md-item-text", function (d) {
				return "uitem.Name";
			}).attr("md-items", function (d) { return "uitem in search.querySearch(search.searchText_" + d.Id + ")"; })
            .attr("md-search-text", function (d) { return "search.searchText_" + d.Id; })
			.attr("placeholder", "Employee")
            .attr("md-selected-item-change", function (d) {
            	return "functions.sendUpdate(model.Lookup['AngularAccountabilityNode_" + d.Id + "'])";
            });
		autoComplete.append("md-item-template")
            .append("span")
            .attr("md-highlight-text", function (d) { return "search.searchText_" + d.Id; })
            .attr("md-highlight-flags", "^i")
            .text("{{uitem.Name||uitem.User.Name}}");//.attr("");
		autoComplete.append("md-not-found")
           .text(function (d) {
           	return "No matches were found.";
           });

		//node.append("xhtml:div").classed("acc-line", true);

		//ROLES

		node.append("div").attr("rolegroups", "").attr("groups", function (d) {
			return "model.Lookup['" + d.Group.Key + "'].RoleGroups";
		}).attr("on-update", "functions.sendUpdate");

		//var roleGroup = node.enter().append("xhtml:div").classed("role-group", true);
		//var roleTitle = roleGroup.append("xhtml:div").classed("role-group-title", true);

		//roleGroup.append("xhtml:div").

		//ADD A ROLE LINE
		//var addRole = rows.append("xhtml:div").classed("acc-add-role", true);

		////addRole.append("xhtml:div").classed("acc-role-line", true).style("background-color", "#ffffff");
		//var c = addRole.append("xhtml:div").classed("acc-role-circle", true).style("border-color", "#ffffff");
		//c.append("xhtml:div").classed("acc-plus acc-plus-v1", true);
		//c.append("xhtml:div").classed("acc-plus acc-plus-h", true);
		//c.append("xhtml:div").classed("acc-plus acc-plus-v2", true);

		//node.on('mouseover', function (d) {
		//	//d3.select(this).select(".acc-add-role .acc-role-line").transition().style("background-color", "#e6e6e6");
		//	d3.select(this).selectAll(".acc-add-role .acc-plus").transition().style("background-color", "#e6e6e6");
		//}).on('mouseout', function (d) {
		//	d3.select(this).selectAll(".acc-add-role .acc-plus").transition().style("background-color", "#ffffff");
		//});

		/*var addRole = /*nodeEnter*roleTitle.append("xhtml:div").classed("add-role-row", true).style("opacity", 0);
		var addRoleCircle = addRole.append("xhtml:div").classed("circle", true).style("margin-top",function (d) {
			if (d.AttachName == "" || d.AttachName==null)
				return "-9px";
			return "-16px";
		}).text("+");*/
		//addRole.append("xhtml:div").classed("acc-plus acc-plus-v", true).attr("y", "-4");
		//addRole.append("xhtml:div").classed("acc-plus acc-plus-h", true).attr("x", "-4");
		/*addRoleCircle.on('mouseover', function (d) {
			d3.select(this).select("circle")
				.transition().duration(0).style("fill", "#ffffff")
				.transition().duration(100).style("fill", "#4682b4");
		}).on('mouseout', function (d) {
			d3.select(this).select("circle")
				.transition().duration(0).style("fill", "#4682b4")
				.transition().duration(100).style("fill", "#ffffff");
		}).on("click", function (d) {
			if (d) {
				addRoleToNode(d.AttachId, d.AttachType);
			} else {
				console.error("Could not add role.", d);
			}
		});*/

		nodeEnter.on('mouseover', function (d) {
			d3.select(this).selectAll(".add-role-row")/*.transition()*/.style("opacity", 1);
			d3.select(this).selectAll(".node-button").transition().style("opacity", 1);
			//debugger;
			//d3.select(this).select(".add-role-row .circle").transition().style("width", "16px").style("height", "16px");
		}).on('mouseout', function (d) {
			d3.select(this).selectAll(".add-role-row")/*.transition()*/.style("opacity", 0);
			d3.select(this).selectAll(".node-button").transition().style("opacity", 0);
			//d3.select(this).select(".add-role-row .circle").transition().style("width", 16).style("height", 16);
		});

		var buttonsBottom = node.append("xhtml:div")
            .classed("acc-buttons bottom-bar", true);

		//buttonsTop.append("xhtml:span").classed("button add", true).append("xhtml:span").attr("title", "Add direct report").classed("glyphicon glyphicon-plus", true)
		//	.on("click", function (d) {
		//		if (d.Id) {
		//			addNode(d.Id);
		//		} else {
		//			throw "Add node requires Id"
		//		}
		//	});
		var clickAddNode = function (d) {
			if (d.Id) {
				addNode(d.Id);
				d3.event.stopPropagation();
			} else {
				throw "Add node requires Id"
			}
		};
		var clickRemoveNode = function (d) {
			if (d.Id) {
				if ((d._children && d._children.length) || (d.children && d.children.length)) {
					showModal({
						title: "Accountability node cannot have direct reports.",
						noCancel: true,
						icon: "warning",
						//fields: { type: "h5", value: "To delete this node, you must reassign it's direct reports." }
					});
				} else {
					var fields = [];
					if (d.User)
						fields.push({ type: "h6", value: "*Deleting this node DOES NOT remove this user from the organization." });
					var id = d.Id;
					showModal({
						title: "Are you sure you want to delete this accountability node?",
						icon: "danger",
						fields: fields,
						success: function () {
							$.ajax({ url: "/Accountability/Remove/" + id });
						}
					});

				}

				//addNode(d.Id);
				d3.event.stopPropagation();
			} else {
				throw "Add node requires Id"
			}
		};

		var expandNode = function (d) {
			//$rootScope.$emit("ToggleNode", d);
			//d3.event.stopPropagation();
		};

		nodeEnter.append("rect").classed("bounding-box", true);
		var addNodeBtn = nodeEnter.append("g").classed("button add node-button", true).style("opacity", 0).on("click", clickAddNode);
		addNodeBtn.append("circle").attr("r", 10).attr("title", "Add direct report").on("click", clickAddNode);
		addNodeBtn.append("text").text("+").attr("title", "Add direct report").on("click", clickAddNode);


		var deleteNodeBtn = nodeEnter.append("g").classed("button remove node-button", true).style("opacity", 0).on("click", clickRemoveNode);
		deleteNodeBtn.append("circle").attr("r", 10).attr("title", "Remove node").on("click", clickRemoveNode);
		deleteNodeBtn.append("text").classed("glyphicon glyphicon-trash", true).attr("title", "Remove node").text("").on("click", clickRemoveNode);

		var minimizeNodeBtn = nodeEnter.append("g").classed("button minimize minimize-icon node-button", true).style("opacity", 0).on("click", expandNode);
		minimizeNodeBtn.append("circle").attr("r", 10).attr("title", "Collapse node").on("click", expandNode);
		minimizeNodeBtn.append("text").classed("glyphicon", true).attr("title", "Remove node").on("click", expandNode);


		//.append("xhtml:span").attr("title", "Add direct report").classed("glyphicon glyphicon-plus", true)


		nodeEnter.call(function (d3Selection) {
			d3Selection.each(function (d, i) {
				// this is the actual DOM element
				console.log("ac-node: create scope");
				var newScope = $scope.$new();
				$compile(this)(newScope);
			});
		});



	}
	$scope.nodeUpdate = function (nodeUpdate) {
		nodeUpdate.select(".acc-rect")
            .attr("width", function (d) {
            	return d.width;
            })
            .attr("height", function (d) {
            	return (d.height || 20);
            });

		nodeUpdate.select("text");

		nodeUpdate.select(".button.add").attr("transform", function (d) {
			return "translate(" + (d.width / 2 - 30) + "," + (d.height + 15.5) + ")";
		});
		nodeUpdate.select(".button.remove").attr("transform", function (d) {
			return "translate(" + (d.width / 2 + 30) + "," + (d.height + 15.5) + ")";
		})
		nodeUpdate.select(".button.minimize").attr("transform", function (d) {
			return "translate(" + (d.width / 2 - .25) + "," + (d.height + 15.5) + ")";
		})

		//var roleGroups = nodeUpdate.selectAll(".role-group").data(function (d) {
		//	//var keys = [];
		//	//var groups = [];
		//	//var maxInd = 0;
		//	//var anyPosition = false;
		//	//for (var g in d.Group.Roles) {
		//	//	var role = d.Group.Roles[g];
		//	//	var key = role.AttachType + "_" + role.AttachId;
		//	//	var ind = keys.indexOf(key);
		//	//	if (ind == -1) {
		//	//		keys.push(key);
		//	//		groups.push([]);
		//	//		ind = maxInd;
		//	//		maxInd += 1;
		//	//	}
		//	//	if (role.AttachType == "Position")
		//	//		anyPosition = true;

		//	//	role._NodeId = d.Id;

		//	//	groups[ind].push(role);
		//	//}
		//	////If we have a position, lets at least give it a role
		//	//if (!anyPosition && d.Group.Position && d.Group.Position.Id > 0)
		//	//	groups.push([{
		//	//		AttachId: d.Group.Position.Id,
		//	//		AttachType: "Position",
		//	//		AttachName: "Position",
		//	//		_NodeId:d.Id
		//	//	}]);

		//	////No name if its a position and its the only one
		//	//var ks = Object.keys(groups);
		//	//if (ks.length == 1 && groups[ks[0]][0].AttachType == "Position"){
		//	//	for (var i in groups[ks[0]]) {
		//	//		groups[ks[0]][i].AttachName = null;
		//	//	}
		//	//}
		//	//console.log("updated groups.")
		//	//return groups;
		//	console.log("updated role groups");
		//	return d.Group.RoleGroups;
		//});

		//roleGroups.select(".role-group-title").text(function (d) {
		//	if (d.AttachName)
		//		return d.AttachName + " Roles";
		//	return "";
		//});

		//roleGroups.exit().remove();

		//var roles = roleGroups.selectAll(".role-row").data(function (d) {
		//	return d.Roles;
		//})
		//roles.enter().append("input").classed("role-row", true).attr("ng-model", function (d) {
		//	return "model.Lookup['AngularRole_" + d.Id + "'].Name";
		//});

		//nodeUpdate.select(".acc-buttons .button.minimize span").classed("minimize-icon glyphicon", function (d) {
		//	return (d.children && d.children.length) || (d._children && d._children.length);
		//}).classed("glyphicon-chevron-right", function (d) {
		//	if (d._children && d._children.length)
		//		return true;
		//	return false;
		//}).classed("glyphicon-chevron-down", function (d) {
		//	if (d.children && d.children.length)
		//		return true;
		//	return false;
		//}).attr("title", function (d) {
		//	if (d._children)
		//		return "Expand direct reports";
		//	return "Collapse direct reports";
		//});


		nodeUpdate.select(".button.minimize").classed("minimize-icon", function (d) {
			return (d.children && d.children.length) || (d._children && d._children.length);
		}).classed("hidden", function (d) {
			return !((d.children && d.children.length) || (d._children && d._children.length));
		}).attr("title", function (d) {
			if (d._children && d._children.length)
				return "Expand direct reports";
			if (d.children && d.children.length)
				return "Collapse direct reports";
			return null;
		}).select("text").text(function (d) {
			if (d.children && d.children.length)
				return "";
			if (d._children && d._children.length)
				return "";//
			return "";
		})

		nodeUpdate.select(".bounding-box")
            .attr("transform", function (d) {
            	return "translate(" + (d.width / 2 - 12) + "," + (d.height) + ")";
            });

		nodeUpdate.select(".add-role-row")
            .attr("transform", function (d) {
            	return "translate(" + d.width + "," + (d.height - 11) + ")";
            });

		nodeUpdate.select("foreignObject")
			.attr("width", function (d) { return d.width; })
			.attr("height", function (d) { return d.height; })
	};
	$scope.nodeExit = function (nodeExit) {
		//nodeExit.select(".acc-rect").attr("height", 1e-6);
		//};
		//$scope.nodePostExit = function (nodeExit) {
		//var ns = nodeExit;
		//setTimeout(function(){},$scope.duration)

		nodeExit.each(function (d, i) {
			var s = angular.element(this).scope();
			if (s) {
				console.log("ac-node: destroy scope");
				s.$destroy();
			}
		});
		//nodeExit.exit().remove();
	};

	var addNode = function (parentId) {
		var _clientTimestamp = new Date().getTime();


		$rootScope.$emit("ExpandNode", parentId);

		$http.post("/Accountability/AddNode/" + parentId + "?&_clientTimestamp=" + _clientTimestamp, {})
			.error(function (data) {
				showJsonAlert(data, true, true);
			});
	}

}]);


acapp.directive('rolegroups', function () {
	var directive = {
		restrict: 'A',
		scope: {
			groups: '=groups',
			onUpdate: '&onUpdate',
		},
		controller: ["$scope", "$http", function ($scope, $http) {

			$scope.addRoleToNode = function (attachId, attachType) {
				var _clientTimestamp = new Date().getTime();
				$http.post("/Accountability/AddRole/?aid=" + attachId + "&atype=" + attachType + "&_clientTimestamp=" + _clientTimestamp, {})
					.error(function (data) {
						showJsonAlert(data, true, true);
					});
			}

			$scope.updating = function (r) {
				$scope.onUpdate()(r);

			};
		}],

		template: "<div class='role-groups'>" +
						"<div ng-repeat='group in groups' class='role-group'>" +
							"<div class='role-group-title'>{{group.AttachName}} Roles " +
								"<div class='add-role-row' ng-click='addRoleToNode(group.AttachId,group.AttachType)'> <div class='circle'>+</div> </div>" +
							"</div>" +
							"<input ng-repeat='role in group.Roles' class='role-row' ng-model=\"role.Name\" ng-change=\"updating(role)\">" +
						"</div>" +
				  "</div>"
	};
	return directive;
});