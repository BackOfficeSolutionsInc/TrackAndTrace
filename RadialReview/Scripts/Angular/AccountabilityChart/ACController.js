var acapp = angular.module('ACApp', ['helpers', 'panzoom', 'tree']);


acapp.controller('ACController', ['$scope', '$http', '$timeout', '$location', 'radial','orgId', 'chartId', 'dataUrl', "$compile", "$sce", "$q", "$window", "$rootScope",
function ($scope, $http, $timeout, $location, radial,orgId, chartId, dataUrl, $compile, $sce, $q, $window, $rootScope) {
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
            url: '/dropdown/angularpositions?create=true&q='+query
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
    

    $scope.nodeEnter = function (nodeEnter) {
        var rect = nodeEnter.append("rect")
            .attr("class", "acc-rect")
            .attr("width", 0)
            .attr("height", 0)
            .attr("x", 0);


        var node = nodeEnter.append("foreignObject")
            .append("xhtml:div")
            .classed("acc-node", true)
            .style("font", "14px 'Helvetica Neue'");

        var buttons = node.append("xhtml:div")
            .classed("acc-buttons", true);
        
        var position = node.append("xhtml:div")
            .classed("acc-position", true);

        var posAutoComplete = position.append("md-autocomplete")
            .attr("md-selected-item", function (d) {
                return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position";
            }).attr("md-item-text", function (d) {
                return "pitem.Name";
            }).attr("md-items", function (d) { return "pitem in search.queryPositions(search.searchPos_" + d.Id + ")"; })
            .attr("md-search-text", function (d) { return "search.searchPos_" + d.Id; })
            .attr("md-selected-item-change", function (d) {
                return "functions.sendUpdate(model.Lookup['AngularAccountabilityNode_" + d.Id + "'])";
            }).attr("md-no-cache","true").attr("md-delay","300");
        posAutoComplete.append("md-item-template")
            .append("span")
            .attr("md-highlight-text", function (d) { return "search.searchPos_" + d.Id; })
            .attr("md-highlight-flags", "^i")
            .text("{{pitem.Name}}");//.attr("");
        posAutoComplete.append("md-not-found")
           .text(function (d) {
               return "No matches were found.";
           });//.attr("");

        //var roles = position.append("input").attr("ng-model", function (d) {
        //    return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group.Position.Name";
        //}).attr("ng-change", function (d) {
        //    return "functions.sendUpdate(model.Lookup['AngularAccountabilityNode_" + d.Id + "'].Group)";
        //});

        var owner = node.append("xhtml:div")
            .classed("acc-owner", true);

        //nodeEnter.append("g")
        //    .classed("acc-owner-delete", true)
        //    .append("text")
        //    .text("x");

        //owner.append("input")
        //    .classed("name",true)
        //    .attr("ng-model", function (d) {
        //        return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User.Name";
        //    });
        var autoComplete = owner.append("md-autocomplete")
            .attr("md-selected-item", function (d) {
                return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User";
            }).attr("md-item-text", function (d) {
                return "uitem.Name";
            }).attr("md-items",function (d) { return "uitem in search.querySearch(search.searchText_" + d.Id+")"; } )
            .attr("md-search-text", function (d) { return "search.searchText_" + d.Id; });
        autoComplete.append("md-item-template")
            .append("span")
            .attr("md-highlight-text", function (d) { return "search.searchText_" + d.Id; })
            .attr("md-highlight-flags", "^i")
            .text("{{uitem.Name||uitem.User.Name}}");//.attr("");
        autoComplete.append("md-not-found")          
           .text(function (d) {
               return "No matches were found.";
           });//.attr("");

        var rows = node.append("xhtml:div").classed("acc-roles", true);


        //node.append("input").attr("ng-model", function (d) {
        //    return "model.Lookup['AngularAccountabilityNode_" + d.Id + "'].User.Name";
        //});

        var roles = rows.selectAll(".role-row").data(function (d) {
            return d.Group.Roles;
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

        // If it fails, show our other labels instead.

        //newSwitches.append("text")
        //    .attr("class", "label")
        //    .attr("x", function (d, i) { return ((cDim.barWidth + cDim.barMargin) * i) + (cDim.barWidth / 2); })
        //    .attr("y", function (d, i) { return cDim.height - barHeight(d.casualties) + 24; })
        //    .text(function (d, i) { return d.display_division });


        //var node = rect.append("foreignObject").attr("width", 480).attr("height", 500)
        //    .append("div").attr("xmlns", "http://www.w3.org/1999/xhtml")
        //    .text("asdf")

        //node.append("input")
        //    .attr("ng-model", function (d) {
        //        return "model.Lookup['AccountabilityTree_" + d.Id + "'].user.Name";
        //    });
        //.attr("y", function (d) { return 13; })
        //.attr("text-anchor", "middle");
        //.text(function (d) { return d.name || d.user.Name; });



    }
    $scope.nodeUpdate = function (nodeUpdate) {
        nodeUpdate.select(".acc-rect")
            .attr("width", function (d) {
                return d.width;
            })
            //.attr("x", function (d) {
            //    return -d.width/2;
            //})
            .attr("height", function (d) {
                return (d.height || 20);
            });

        nodeUpdate.select("text");
    };
    $scope.nodeExit = function (nodeExit) {
        nodeExit.select(".acc-rect").attr("height", 1e-6);
        //nodeExit.select("text");
    };

}]);