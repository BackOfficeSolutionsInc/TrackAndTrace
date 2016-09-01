var acapp = angular.module('ACApp', ['helpers', 'panzoom', 'tree']);

acapp.directive('mdBlur', ["$timeout","$rootScope", function ($timeout,$rootScope) {
	var directive = {
		restrict: 'A',
		link: function (scope, element, attributes) {
			$timeout(function () {
				scope.oldValue = scope.$eval(attributes.mdSelectedItem);
				scope.focused = false;
				angular.element(element[0].querySelector("input")).bind("focus", function () {
				    //console.log("focus:",this,$(this).is(":focus"));
					scope.focused = true;
					var resolved = scope.$eval(attributes.mdSelectedItem);
					if (resolved) {
						scope.oldValue = resolved.Key;
					} else {
						scope.oldValue = null;
					}
				});
				angular.element(element[0].querySelector("input")).bind("blur", function () {
					var that = this;
					scope.focused = false;

					var e = element;
					var that = this;
					$timeout(function () {
						////console.log(element);
						//var a = attributes;
						//var resolved = scope.$eval(attributes.mdSelectedItem);
						//var newValue = null;
						//if (resolved) {
						//	newValue = resolved.Key;
						//}

						////var index = $(that).val().indexOf(" (Create)");
						////if (index != -1) {
						////	var a = attributes;
						////	$(that).val(name.substring(0, index));
						////}

						//if (scope.oldValue != newValue || newValue=="AngularPosition_-2") {
						//	//console.log("triggered");
						//	console.log(scope.oldValue, newValue);
						//	//scope.$eval(attributes.mdBlur);
						//} else {
						//	//if (newValue == null) {
						//	//	$(that).val(null);
						//	//}
						//	//console.log("skip blur");
						//}
					}, 30);
				});
			}, 0);
		}
	};

	return directive;
}]);


acapp.directive('rolegroups', function () {
	var directive = {
		restrict: 'A',
		scope: {
			groups: '=groups',
			onUpdate: '&onUpdate',
			onDeleteRole: '&onDeleteRole',
		},
		controller: ["$scope", "$http", "$timeout", "$element","$rootScope", function ($scope, $http, $timeout, $element,$rootScope) {
			$scope.addRoleToNode = function (group) {
				var attachType = group.AttachType;
				var attachId = group.AttachId;
				var _clientTimestamp = new Date().getTime();
				$http.post("/Accountability/AddRole/?aid=" + attachId + "&atype=" + attachType + "&_clientTimestamp=" + _clientTimestamp, {})
					.error(function (data) {
						showJsonAlert(data, true, true);
					});

			};

			$scope.newRoleButton = function (group) {

				var origLength = group.Roles.length;
				$scope.addRoleToNode(group);
				depth = 0;
				var setFocus = function () {
					try {
						if (depth == 100)
							return;
						if (origLength < group.Roles.length) {
							console.log("focusing");
							var res = $($($element).find("[data-group=" + group.Id + "] input").last()).focus();
						} else {
							depth += 1;
							$timeout(setFocus, 20);
						}
					} catch (e) {
						console.error(e);
					}
				}
				$timeout(setFocus, 20);
			}


			$scope.updating = function (r) {
				$scope.onUpdate()(r);
			};
			$scope.deleting = function (r) {
				$scope.onDeleteRole()(r);
			};

			$scope.focusing = function () {
			    $rootScope.$emit("RoleFocused");
			}
			$scope.blurring = function () {
			    $rootScope.$emit("RoleBlurred");
			}

			//$($element).on("keydown","input", function () {
			//    console.log("faster");
			//});

			$scope.checkCreateRole = function (evt, r, group, index) {
			    $timeout(function () {
			        var origLength = group.Roles.length;
			        var depth = 0;
			        if (evt.which === 13) {
			            //var isLast = index === origLength - 1
			            if (/*isLast &&*/ group.Editable != false) {

			                $scope.addRoleToNode(group);

			                depth = 0;
			                var setFocus = function () {
			                    try {
			                        if (depth == 100)
			                            return;
			                        if (origLength < group.Roles.length) {
			                            console.log("focusing");
			                            $($($element).find("[data-group=" + group.Id + "] input")[index + 1]).focus();
			                        } else {
			                            depth += 1;
			                            $timeout(setFocus, 20);
			                        }
			                    } catch (e) {
			                        console.error(e);
			                    }
			                }
			                $timeout(setFocus, 20);

			            }
			        } else if (evt.which == 8) {
			            if (r.Name == "" || typeof (r.Name) === "undefined" || r.Name == null) {
			                $scope.deleting(r);
			                if (origLength != 1) {
			                    var setFocus = function () {
			                        try {
			                            if (depth == 100)
			                                return;
			                            if (origLength > group.Roles.length) {
			                                console.log("focusing");

			                                $($($element).find("[data-group=" + group.Id + "] input")[index - 1]).focus();
			                                //$("[data-group=" + group.Id + "]").find("input:nth-child(" + (index) + ")").focus();
			                            } else {
			                                depth += 1;
			                                $timeout(setFocus, 20);
			                            }
			                        } catch (e) {
			                            console.error(e);
			                        }
			                    }
			                    $timeout(setFocus, 20);
			                } else {
			                    $($element).find("[data-group=" + group.Id + "] .add-role-row").focus();
			                }
			            }
			        } else if (evt.which == 38) {
			            if (index > 0) {
			                var that = $($($element).find("[data-group=" + group.Id + "] input")[index - 1]);
			                $(that).focus();
			                $timeout(function () {
			                    var len = $(that).val().length * 2;
			                    $(that)[0].setSelectionRange(len, len);
			                }, 0);
			            }
			        } else if (evt.which == 40) {
			            if (index < origLength - 1) {
			                var that = $($($element).find("[data-group=" + group.Id + "] input")[index + 1]);
			                $(that).focus();
			                $timeout(function () {
			                    var len = $(that).val().length * 2;
			                    $(that)[0].setSelectionRange(len, len);
			                }, 0);
			            } else {
			                var id = $($($element).find("[data-group=" + group.Id + "] input")).closest("g.node").attr("data-id");
			                $scope.$emit("ExpandNode", id);
			            }
			        }
			    }, 1);
			}
		}],

		template: "<div class='role-groups'>" +
						"<div ng-repeat='group in groups' class='role-group' ng-if='::group.Editable!=false || group.Roles.length>0' data-group='{{group.Id}}'>" +
							"<div class='role-group-title'><span ng-if='!(groups.length==1 && group.AttachType==\"Position\")'>{{::group.AttachName}} Roles </span>" +
								"<div ng-if='::group.Editable!=false' class='add-role-row' ng-class='{tinyRow:(groups.length==1 && group.AttachType==\"Position\")}' ng-click='newRoleButton(group)' style='opacity:0;'> <div class='circle'>+</div> </div>" +
							"</div>" +
							"<ul>" +
								"<li ng-repeat='role in group.Roles'  class='role-row' >" +
									"<input ng-model-options='{debounce:200}'  ng-focus='focusing()' ng-blur='blurring()'"+" ng-keydown='checkCreateRole($event,role,group,$index)'"+ " title='{{role.Name}}' class='role' ng-if='::group.Editable!=false' ng-model=\"role.Name\" ng-change=\"updating(role)\">" +
									"<div title='{{role.Name}}' class='role' ng-if='::group.Editable==false'>{{role.Name}}</div>" +
									"<span ng-if='::group.Editable!=false' class='delete-role-row' ng-click=\"deleting(role)\" tabindex='-1'></span>" +
								"</li>" +
							"</ul>" +
							"<div ng-if='group.Roles.length==0' class='gray no-roles-placeholder'>" +
								"No roles. <span ng-if='::group.Editable!=false'>Use the <span class='glyphicon glyphicon-plus-sign'></span> button to add some.</span>" +
							"</div>" +
						"</div>" +
				  "</div>"
	};
	return directive;
});

acapp.controller('ACController', ['$scope', '$http', '$timeout', '$location', 'radial', 'orgId', 'chartId', 'dataUrl', "$compile", "$sce", "$q", "$window", "$rootScope",
function ($scope, $http, $timeout, $location, radial, orgId, chartId, dataUrl, $compile, $sce, $q, $window, $rootScope) {
	$scope.orgId = orgId;
	$scope.chartId = chartId;
	$scope.model = $scope.model || {};
	$scope.model.height = $scope.model.height || 10000;
	$scope.model.width = $scope.model.width || 10000;

	$scope.suspendUpdate = false;

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

	//AJAX
	function fixNodeRecurse(self) {
		if (self && self.Type == "AngularAccountabilityNode") {
			delete self.children;
			delete self._children;
			delete self.parent;
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
	$scope.functions.removeRow = function (self) {
		var _clientTimestamp = new Date().getTime();
		$http.post("/Accountability/Remove" + self.Type + "/" + self.Id + "?connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, null)
			.error(function (data) {
				showJsonAlert(data, true, true);
			});
	};

	//SEARCH
	var self = this;
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

	//SELECT AUTOCOMPLETE
	//ULTRA HACK
	$rootScope.$on("BeginCallbackSignalR", function (event, count) {
		$scope.suspendUpdate = true;
		//console.log("suspend update (" + count + ")...");
	});
	$rootScope.$on("EndCallbackSignalR", function (event, count) {
		if (count <= 0) {
			$scope.suspendUpdate = false;
			//console.log("...resume update");
		} else {
			//console.log("...(" + count + ")...");
		}
		//console.log("------------------");
	});

	$scope.roleFocused = false;
	$rootScope.$on("RoleFocused", function (event) {
	    $scope.roleFocused = true;
	});
	$rootScope.$on("RoleBlurred", function (event) {
	    $scope.roleFocused = false;
	});
	//END ULTRA HACK
	$scope.functions.selectedItemChange = function (node) {
		d3.select(".selected").classed("selected", false).attr("filter", null);
		if (node) {
			$rootScope.$emit("SelectNode", node.Id);
		}
	};
	$scope.functions.selectedItemChange_UpdateNode = function (id) {
		console.log("selectedItemChange_UpdateNode");
		if (!$scope.suspendUpdate) {
			console.log("..sending update");
			$scope.functions.sendUpdate($scope.model.Lookup['AngularAccountabilityNode_' + id + '']);
		} else {
			console.log("..applying update");
			try {
				$scope.$eval("search.searchPos_" + id + "=$scope.model.Lookup['AngularAccountabilityNode_" + id + "'].Group.Position.Name");
			} catch (e) {
				console.error("Silly position update " + e)
			}
		}
	};
	$scope.functions.selectedItemChange_UpdateUser = function (id) {
		console.log("selectedItemChange_UpdateUser");
		if (!$scope.suspendUpdate) {
			console.log("..sending update");
			$scope.functions.sendUpdate($scope.model.Lookup['AngularAccountabilityNode_' + id + '']);
		} else {
			console.log("..applying update");
			try {
				$scope.$eval("search.searchText_" + id + "=$scope.model.Lookup['AngularAccountabilityNode_" + id + "'].User.Name");
			} catch (e) {
				console.error("Silly user update " + e)
			}
		}
	};

	//TREE EVENT FUNCTIONS
	$scope.nodeWatch = function (node) {
		var uname = null;
		var roles = null;
		var pos = false;
		if (node.User && node.User.Name)
			uname = node.User.Name;
		if (node.Group && node.Group.RoleGroups) {
		    if ($scope.roleFocused){
		        var copy = node.Group.RoleGroups.map(function (rg) {
		            var cp = angular.copy(rg);
		            cp.Roles = cp.Roles.map(function () { return null; });
		            return cp;
		        });
		    //    var rolesGroups = [];
		    //    for(var c in copy){
		    //        var rg = copy[c];
		    //        rg.Roles = null;
		    //        rolesGroups.push(rg);
		    //    }
		        roles = copy;
		    }
		    else
		        roles = node.Group.RoleGroups;
		}
		if (node.Group)
			pos = node.Group.Position;
		return {
			name: uname,
			roles: roles,
			pos: pos
		};
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

	$scope.dragStart = function (d) {
		if (!d3.select(d3.event.sourceEvent.srcElement).classed("move-icon")) {
			throw "Incorrect selector";
		}
		if (d.Editable == false) {
			throw "Cannot edit";
		}
	}
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
		if ($(d3.event.srcElement).closest(".minimize-icon").length != 1) {
			throw "Incorrect selector";
		}
	}

	$scope.clearIfNull = function (item, searchText, update) {
		if (!item) {
			$scope.$eval(searchText + "=null");
		}

		if (update) {
			var result = $scope.$eval(update);
			if (result) {
				$scope.functions.sendUpdate(result);
			}
		}
	}

	$scope.functions.fixName = function (id, nameVar, item, self) {
		if (!self.focused) {
			if (item == null) {
				$scope.$eval(nameVar + "=null");
			} else {
			}
		} else {
			$scope.functions.selectedItemChange_UpdateNode(id);
		}
	}

	$scope.nodeEnter = function (nodeEnter) {
		var rect = nodeEnter.append("rect").attr("class", "acc-rect")
            .attr("width", 0).attr("height", 0).attr("x", 0).attr("rx", 2).attr("ry", 2);

		var node = nodeEnter.append("foreignObject")
            .append("xhtml:div").classed("acc-node", true)
            .style("font", "14px 'Helvetica Neue'");

		var buttonsTop = node.append("xhtml:div")
            .classed("acc-buttons move-icon top-bar", true)
			.classed("move-icon", function (d) {
				return d.Editable != false;
			}).text(function (d) {
				return d.Name;
			});

		var contents = node.append("xhtml:div").classed("acc-contents", true);

		var position = contents.append("xhtml:div").classed("acc-position", true);

		var posAutoComplete = position.append("md-autocomplete")
			.attr("placeholder", "Function")
            .attr("md-blur", function (d) {
            	return "clearIfNull(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position,\"search.searchPos_" + d.Id + "\",\"model.Lookup['AngularAccountabilityNode_" + d.Id + "']\")";
            }).attr("md-selected-item", function (d) {
            	return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position";
            }).attr("md-item-text", function (d) {
            	return "pitem.Name";
            }).attr("md-items", function (d) { return "pitem in search.queryPositions(search.searchPos_" + d.Id + ")"; })
            .attr("md-search-text", function (d) { return "search.searchPos_" + d.Id; })
            /*.attr("md-selected-item-change", function (d) {
            	return "functions.selectedItemChange_UpdateNode(" + d.Id + ")";
            })*/
			.attr("md-selected-item-change", function (d) {
				return "functions.fixName(" + d.Id + ",'search.searchPos_" + d.Id + "',model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position,this)";
			}).attr("md-no-cache", "true");//.attr("md-delay", "300");

		//Is this even used? vvv
		posAutoComplete.append("md-item-template").append("span").attr("md-highlight-text", function (d) { return "search.searchPos_" + d.Id; }).attr("md-highlight-flags", "^i").text("{{pitem.Name}}{{pitem.Create}}");
		posAutoComplete.append("md-not-found").text(function (d) { return "No matches were found."; });


		var owner = contents.append("xhtml:div").classed("acc-owner", true);
		var autoComplete = owner.append("md-autocomplete")
			.attr("md-blur", function (d) {
				return "clearIfNull(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User,\"search.searchText_" + d.Id + "\",\"model.Lookup['AngularAccountabilityNode_" + d.Id + "']\")";
			}).attr("md-selected-item", function (d) {
				return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User";
			}).attr("md-item-text", function (d) {
				return "uitem.Name";
			}).attr("md-items", function (d) { return "uitem in search.querySearch(search.searchText_" + d.Id + ")"; })
            .attr("md-search-text", function (d) { return "search.searchText_" + d.Id; })
			.attr("placeholder", "Employee")
			.attr("md-selected-item-change", function (d) {
				return "functions.fixName(" + d.Id + ",'search.searchText_" + d.Id + "',model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User,this)";
			})
		/*.attr("md-selected-item-change", function (d) {
			return "functions.selectedItemChange_UpdateUser(" + d.Id + ")";
		})*/;
		autoComplete.append("md-item-template")
            .append("span")
            .attr("md-highlight-text", function (d) { return "search.searchText_" + d.Id; })
            .attr("md-highlight-flags", "^i")
            .text("{{uitem.Name||uitem.User.Name}}");
		autoComplete.append("md-not-found")
           .text(function (d) {
           	return "No matches were found.";
           });

		//ROLES

		contents.append("div").attr("rolegroups", "").attr("groups", function (d) {
			return "model.Lookup['" + d.Group.Key + "'].RoleGroups";
		}).attr("on-update", "functions.sendUpdate").attr("on-delete-role", "functions.removeRow");


		nodeEnter.on('mouseover', function (d) {
			d3.select(this).selectAll(".add-role-row")/*.transition()*/.style("opacity", 1);
			d3.select(this).selectAll(".node-button").transition().style("opacity", 1);
		}).on('mouseout', function (d) {
			d3.select(this).selectAll(".add-role-row")/*.transition()*/.style("opacity", 0);
			d3.select(this).selectAll(".node-button").transition().style("opacity", 0);
		});

		var buttonsBottom = node.append("xhtml:div")
            .classed("acc-buttons bottom-bar", true);
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
						title: "Cannot delete accountability node that has direct reports.",
						noCancel: true,
						icon: "warning",
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
				d3.event.stopPropagation();
			} else {
				throw "Add node requires Id"
			}
		};

		function onClickKey(func) {
			debugger;
			if (d3.event && (d3.event.keyCode == 13 || d3.event.keyCode == 32))
				return func;
			return function (d) { };
		}


		var expandNode = function (d) {
		};

		nodeEnter.append("rect").classed("bounding-box", true);
		var minimizeNodeBtn = nodeEnter.append("g").classed("button minimize minimize-icon node-button", true).style("opacity", 0).attr("tabindex", 0)
			.on("click", expandNode)
			.on("keydown", function (d) {
				if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
					expandNode(d);
			});
		minimizeNodeBtn.append("circle").attr("r", 10).attr("title", "Collapse node").on("click", expandNode);//.on("keydown", onClickKey(expandNode));
		minimizeNodeBtn.append("text").classed("glyphicon", true).attr("title", "Remove node").on("click", expandNode);//.on("keydown", onClickKey(expandNode));

		var addNodeBtn = nodeEnter.append("g").classed("button add node-button", true).attr("tabindex", 0).style("opacity", 0)
			.on("click", clickAddNode)
			.on("keydown", function (d) {
				if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
					clickAddNode(d);
			});
		addNodeBtn.append("circle").attr("r", 10).attr("title", "Add direct report").on("click", clickAddNode);
		addNodeBtn.append("text").text("+").attr("title", "Add direct report").on("click", clickAddNode);


		var deleteNodeBtn = nodeEnter.append("g").classed("button remove node-button", true).attr("tabindex", 0).style("opacity", 0).on("click", clickRemoveNode).on("keyup", function (d) {
			if (d3.event.keyCode == 13 || d3.event.keyCode == 32)
				clickRemoveNode(d);
		});
		deleteNodeBtn.append("circle").attr("r", 10).attr("title", "Remove node").on("click", clickRemoveNode);
		deleteNodeBtn.append("text").classed("glyphicon glyphicon-trash", true).attr("title", "Remove node").text("").on("click", clickRemoveNode);


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
	    nodeUpdate.select(".acc-rect").attr("width", function (d) {
	        console.log("update called");
			return d.width;
		}).attr("height", function (d) {
			return (d.height || 20);
		});

		nodeUpdate.select(".button.add").attr("transform", function (d) {
			return "translate(" + (d.width / 2 - 30) + "," + (d.height + 15.5) + ")";
		}).attr("tabindex", function (d) {
			return d.Editable == false ? "-1" : "0";
		}).style("display", function (d) {
			return d.Editable == false ? "none" : null;
		});

		nodeUpdate.select(".button.remove").attr("transform", function (d) {
			return "translate(" + (d.width / 2 + 30) + "," + (d.height + 15.5) + ")";
		}).attr("tabindex", function (d) {
			return d.Editable == false ? "-1" : "0";
		}).style("display", function (d) {
			return d.Editable == false ? "none" : null;
		});

		nodeUpdate.select(".button.minimize").attr("transform", function (d) {
			return "translate(" + (d.width / 2 - .25) + "," + (d.height + 15.5) + ")";
		});

		nodeUpdate.select(".button.minimize")
			.classed("minimize-icon", function (d) {
				return (d.children && d.children.length) || (d._children && d._children.length);
			}).attr("tabindex", function (d) {
				return !((d.children && d.children.length) || (d._children && d._children.length)) ? "-1" : "0";
			}).style("display", function (d) {
				return !((d.children && d.children.length) || (d._children && d._children.length)) ? "none" : null;
			})
			.attr("title", function (d) {
				if (d._children && d._children.length)
					return "Expand direct reports";
				if (d.children && d.children.length)
					return "Collapse direct reports";
				return null;
			})
			.select("text")
			.text(function (d) {
				if (d.children && d.children.length)
					return "";//
				if (d._children && d._children.length)
					return "";
				return "";
			});

		nodeUpdate.select(".bounding-box").attr("transform", function (d) {
			return "translate(" + (d.width / 2 - 12) + "," + (d.height) + ")";
		});

		nodeUpdate.select(".add-role-row").attr("transform", function (d) {
			return "translate(" + d.width + "," + (d.height - 11) + ")";
		});

		nodeUpdate.select("foreignObject").attr("width", function (d) { return d.width; });
	};
	$scope.nodeExit = function (nodeExit) {
		nodeExit.each(function (d, i) {
			var s = angular.element(this).scope();
			if (s) {
				console.log("ac-node: destroy scope");
				s.$destroy();
			}
		});
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

