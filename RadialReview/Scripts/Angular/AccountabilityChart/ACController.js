var acapp = angular.module('ACApp', ['helpers', 'panzoom', 'tree']);


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
        	$rootScope.$emit("CenterNode", $scope.model.center || 0);

        }).error(function (data, status) {
        	console.log("Error");
        	console.error(data);
        });
	}
	$scope.functions.reload(true);

	var self = this;

	$scope.functions.selectedItemChange = function (id) {
		$rootScope.$emit("SelectNode", id);
	};

	$scope.search = {};
	$scope.search.querySearch = function (query) {
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

	$scope.search.searchTerms = {};


	$scope.nodeWatch = function (node) {
		var uname = null;
		var roles = null;
		if (node.User && node.User.Name)
			uname = node.User.Name;
		if (node.Group && node.Group.Roles)
			roles = node.Group.Roles;
		return {
			name: uname,
			roles: roles
		}
	}

	function fixNodeRecurse(self) {
		if (self.Type == "AngularAccountabilityNode") {
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
		if (!d3.select(d3.event.srcElement).classed("minimize-icon")) {
			throw "Incorrect selector";
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

		var buttons = node.append("xhtml:div")
            .classed("acc-buttons move-icon", true);

		buttons.append("xhtml:span").classed("button minimize", true).append("xhtml:span")
		//buttons.append("xhtml:span").classed("button move", true).append("xhtml:span").classed("move-icon glyphicon glyphicon-move", true);
		buttons.append("xhtml:span").classed("button add", true).append("xhtml:span").attr("title", "Add direct report").classed("glyphicon glyphicon-plus", true).on("click", function (d) {
			if (d.Id) {
				addNode(d.Id);
			} else {
				throw "Add node requires Id"
			}
		});

		var position = node.append("xhtml:div")
            .classed("acc-position", true);

		var posAutoComplete = position.append("md-autocomplete")
			.attr("placeholder", "Function")
            .attr("md-selected-item", function (d) {
            	return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position";
            }).attr("md-item-text", function (d) {
            	return "pitem.Name";
            }).attr("md-items", function (d) { return "pitem in search.queryPositions(search.searchPos_" + d.Id + ")"; })
            .attr("md-search-text", function (d) { return "search.searchPos_" + d.Id; })
            .attr("md-selected-item-change", function (d) {
            	return "functions.sendUpdate(model.Lookup['AngularAccountabilityNode_" + d.Id + "'])";
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
            .attr("md-selected-item", function (d) {
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
           });//.attr("");

		node.append("xhtml:div").classed("acc-line", true);

		var rows = node.append("xhtml:div").classed("acc-roles", true);


		//node.append("input").attr("ng-model", function (d) {
		//    return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User.Name";
		//});

		var roles = rows.selectAll(".role-row").data(function (d) {
			if (d.Group && d.Group.Roles)
				return d.Group.Roles || [];
			return [];
		});

		roles.enter().append("input").attr("ng-model", function (d) {
			return "model.Lookup['AngularRole_" + d.Id + "'].Name";
		});

		nodeEnter.call(function (d3Selection) {
			d3Selection.each(function (d, i) {
				// this is the actual DOM element
				$compile(this)($scope);
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

		nodeUpdate.select(".acc-buttons .button.minimize span").classed("minimize-icon glyphicon", function (d) {
			return d.children || d._children;
		}).classed("glyphicon-chevron-right", function (d) {
			if (d._children)
				return true;
			return false;
		}).classed("glyphicon-chevron-down", function (d) {
			if (d.children)
				return true;
			return false;
		}).attr("title", function (d) {
			if (d._children)
				return "Expand direct reports";
			return "Collapse direct reports";
		});

		nodeUpdate.select("foreignObject")
			.attr("width", function (d) { return d.width; })
			.attr("height", function (d) { return d.height; })
	};
	$scope.nodeExit = function (nodeExit) {
		nodeExit.select(".acc-rect").attr("height", 1e-6);
	};

	var addNode = function (parentId) {
		var _clientTimestamp = new Date().getTime();
		$http.post("/Accountability/AddNode/" + parentId + "?&_clientTimestamp=" + _clientTimestamp, {})
			.error(function (data) {
				showJsonAlert(data, true, true);
			});
	}

}]);