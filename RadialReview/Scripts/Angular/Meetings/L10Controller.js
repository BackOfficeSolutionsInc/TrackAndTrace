angular.module('L10App').controller('L10Controller', ['$scope', '$http', '$timeout', '$location',
    'radial', 'meetingDataUrlBase'/*, 'dateFormat'*/, 'meetingId', "meetingCallback", "$compile", "$sce", "$q", "$window",
function ($scope, $http, $timeout, $location, radial, meetingDataUrlBase, meetingId, meetingCallback, $compile, $sce, $q, $window) {

    $scope.trustAsResourceUrl = $sce.trustAsResourceUrl;
    if (meetingId == null)
        throw Error("MeetingId was empty");
    $scope.disconnected = false;
    $scope.meetingId = meetingId;

    $scope.dateFormat = window.dateFormat || "MM-dd-yyyy";

    function rejoin(connection, proxy, callback) {
        try {
            if (proxy) {
                proxy.invoke("join", $scope.meetingId, connection.id).done(function () {
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

    function updateScorecard(data) {
        console.log("updateScorecard");
        $scope.ScoreLookup = $scope.ScoreLookup || {};
        var luArr = [];
        if (data.Scorecard != null && data.Scorecard.Scores != null) {

            luArr.push(data.Scorecard.Scores);
        }

        if (typeof (data.L10Scorecards) !== "undefined") {
            for (var sc in data.L10Scorecards) {
                var i = data.L10Scorecards[sc];
                if (typeof (i.Contents) !== "undefined" && typeof (i.Contents.Scores) !== "undefined") {
                    luArr.push(i.Contents.Scores);
                }
            }
        }


        for (var luidx in luArr) {
            var lu = luArr[luidx];
            for (var key in lu) {
                var value = lu[key];
                if (!(value.ForWeek in $scope.ScoreLookup))
                    $scope.ScoreLookup[value.ForWeek] = {};
                if (value.Measurable) {
                    $scope.ScoreLookup[value.ForWeek][value.Measurable.Id] = value.Key;
                }
            }
        }
    };


    var r = radial($scope, 'meetingHub', rejoin);

    r.updater.postResolve = updateScorecard;

    $scope.functions = $scope.functions || {};
    $scope.filters = $scope.filters || {};

    if (typeof (dataDateRange) === "undefined") {
        dataDateRange = {};
    }
    if (typeof (dataDateRange.startDate) === "undefined") {
        dataDateRange.startDate = moment().add('days', 1).toDate();//.subtract('days', 6).toDate();
    } else {
        dataDateRange.startDate = moment(dataDateRange.startDate).toDate();
    }
    if (typeof (dataDateRange.endDate) === "undefined") {
        dataDateRange.endDate = moment().add('days', 1).toDate();
    } else {
        dataDateRange.endDate = moment(dataDateRange.endDate).toDate();
    }
    $scope.model = $scope.model || {};
    $scope.model.dataDateRange = dataDateRange;
    $scope._alreadyLoaded = { startDate: dataDateRange.startDate.getTime(), endDate: dataDateRange.endDate.getTime() };
    $scope.$watch('model.dataDateRange', function (newValue, oldValue) {
        console.log("watch dataDateRange");
        if (newValue.startDate < $scope._alreadyLoaded.startDate) {
            var range1 = {
                startDate: newValue.startDate,
                endDate: Math.min($scope._alreadyLoaded.startDate, newValue.endDate)
            };
            $scope.functions.reload(true, range1);
        }
        if (newValue.endDate > $scope._alreadyLoaded.endDate) {
            var range2 = {
                startDate: Math.max(newValue.startDate, $scope._alreadyLoaded.endDate),
                endDate: newValue.endDate
            };
            $scope.functions.reload(true, range2);
        }
        $scope._alreadyLoaded.startDate = Math.min($scope._alreadyLoaded.startDate, newValue.startDate);
        $scope._alreadyLoaded.endDate = Math.max($scope._alreadyLoaded.endDate, newValue.endDate);
    });
    var dateToNumber = function (date) {
        var type = typeof (date);
        if (type == 'number') {
            return date;
        } else if (typeof (date._d) !== 'undefined') {
            return +date;
        } else if (date.getDate !== undefined) {
            return date.getTime();
        }
        console.error("cant process:" + date);
    }

    $scope.functions.adjustToMidnight = function (date) {
        debugger;
        //tzoffset();
        //adjusts local time to end of day local time
        return new Date(((+date) + (24 * 60 * 60 * 1000 - 1)));// + window.tzoffset * 60 * 1000 - 1)));
    }

    var tzoffset = r.updater.tzoffset;
    //    if (!window.tzoffset) {
    //        var jan = new Date(new Date().getYear() + 1900, 0, 1, 2, 0, 0), jul = new Date(new Date().getYear() + 1900, 6, 1, 2, 0, 0);
    //        window.tzoffset = (jan.getTime() % 24 * 60 * 60 * 1000) >
    //                     (jul.getTime() % 24 * 60 * 60 * 1000)
    //                     ? jan.getTimezoneOffset() : jul.getTimezoneOffset();
    //    }
    //    return window.tzoffset;
    //}

    //$scope.functions.debugger = function () {
    //    debugger;
    //};

    $scope.functions.reload = function (reload, range, first) {
        if (typeof (reload) === "undefined") {
            reload = true;
        }
        if (typeof (first) === "undefined") {
            first = false;
        }
        if (reload) {
            //if (!window.tzoffset) {
            //    var jan = new Date(new Date().getYear() + 1900, 0, 1, 2, 0, 0), jul = new Date(new Date().getYear() + 1900, 6, 1, 2, 0, 0);
            //    window.tzoffset = (jan.getTime() % 24 * 60 * 60 * 1000) >
            //                 (jul.getTime() % 24 * 60 * 60 * 1000)
            //                 ? jan.getTimezoneOffset() : jul.getTimezoneOffset();
            //}
            tzoffset();

            console.log("reloading...");
            var url = meetingDataUrlBase;
            if (meetingDataUrlBase.indexOf("{0}") != -1) {
                url = url.replace("{0}", $scope.meetingId);
            } else {
                url = url + $scope.meetingId;
            }

            var date = ((+new Date()) + (window.tzoffset * 60 * 1000));
            if (meetingDataUrlBase.indexOf("?") != -1) {
                url += "&_clientTimestamp=" + date;
            } else {
                url += "?_clientTimestamp=" + date;
            }

            if (typeof (range) !== "undefined" && typeof (range.startDate) !== "undefined")
                url += "&start=" + dateToNumber(range.startDate);
            if (typeof (range) !== "undefined" && typeof (range.endDate) !== "undefined")
                url += "&end=" + dateToNumber(range.endDate);
            if (first)
                url += "&fullScorecard=true";
            var stD = new Date();
            $http({ method: 'get', url: url })
            .success(function (data, status) {
                // r.updater.clearAndApply(data, status);
                console.log("A dur: " + (+(new Date() - stD)));
                var ddr = undefined;
                if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.dataDateRange) !== "undefined")
                    ddr = $scope.model.dataDateRange;

                r.updater.convertDates(data);

                if (first) {
                    r.updater.clearAndApply(data);
                } else {
                    r.updater.applyUpdate(data);
                }

                if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.dataDateRange) === "undefined")
                    $scope.model.dataDateRange = ddr;

                //if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.Attendees) === "undefined")
                //    $scope.model.dataDateRange = ddr;
                //$scope.model = data;

                if (meetingCallback) {
                    //setTimeout(function () {
                    meetingCallback();
                    //}, 1);
                }
            }).error(showAngularError);
        }
    }

    $scope.functions.reload(true, $scope.model.dataDateRange, true);

    $scope.functions.setHtml = function (element, data) {
        var newstuff = element.html(data);
        $compile(newstuff)($scope); // loads the angular stuff in the new markup
        $scope.$apply();
    };

    $scope.functions.setPage = function (page) {
        $http.get("/meeting/SetPage/" + $scope.model.RecurrenceId + "?page=" + page + "&connection=" + $scope.connectionId);
        if (!$scope.model.FollowLeader || $scope.model.IsLeader) {
            $scope.model.CurrentPage = page;
        }
    };

    $scope.functions.subtractDays = function (date, days) {
        var d = new Date(date);
        d = new Date(moment(d).add(new Date().getTimezoneOffset(), "minutes").valueOf())
        d.setDate(d.getDate() - days);
        return d;
    };
    $scope.functions.scorecardId = function (s, measurableId, weekId) {
        if (!s)
            return "sc_" + measurableId + "_" + weekId;
        return "sc_" + s.Id;
    };
    $scope.functions.scorecardColor = function (s) {
        if (!s)
            return "";

        var v = s.Measured;
        var goal = s.Target;//s.Measurable.Target;
        var altgoal = s.AltTarget;//s.Measurable.Target;
        var dir = s.Direction;//s.Measurable.Direction;
        
        if (typeof (goal) === "undefined")
            goal = s.Measurable.Target;
        if (typeof (altgoal) === "undefined")
            altgoal = s.Measurable.AltTarget;
        if (typeof (dir) === "undefined")
            dir = s.Measurable.Direction;
        if (typeof (goal) === "undefined") {
            var item= $("[data-measurable=" + s.Measurable.Id + "][data-week=" + s.ForWeek + "]");
            goal = item.data("goal");
            if (typeof (altgoal) === "undefined")
                altgoal = item.data("alt-goal");
            console.log("goal not found, trying element. Found: " + goal);
        }

        if (!$.trim(v)) {
            return "";
        } else {
            var met = metGoal(dir, goal, v, altgoal);
            if (met == true)
                return "success";
            else if (met == false)
                return "danger";
            else
                return "error";
        }        
    };

    $scope.proxyLookup = {};
    $scope.ScoreIdLookup = null;

    $scope.functions.setValue = function (keyStr, value) {
        $scope[keyStr] = value;
    }

    $scope.functions.getFcsa = function (measurable) {
        if (measurable.Modifiers == "Dollar") {
            return { prepend: "$" };
        } else if (measurable.Modifiers == "Percent") {
            return { append: "%" };
        } else if (measurable.Modifiers == "Euros") {
            return { prepend: "€" };
        } else if (measurable.Modifiers == "Pound") {
            return { prepend: "£" };
        }
    };

    $scope.functions.lookupScoreFull = function (week, measurableId, scorecardKey) {
        var scorecard = $scope.model.Lookup[scorecardKey];
        var scores = scorecard.Scores;
        for (var s in scores) {
            var score = $scope.model.Lookup[scores[s].Key];
            if (score.ForWeek == week && score.Measurable.Id == measurableId) {
                if (!(week in $scope.ScoreLookup))
                    $scope.ScoreLookup[week] = {};
                $scope.ScoreLookup[week][measurableId] = scores[s].Key;

                return scores[s].Key;
            }
        }
        return null;
    };

    $scope.functions.lookupScore = function (week, measurableId, scorecardKey) {

        if ($scope.ScoreLookup == null) {
            $scope.ScoreLookup = {};
            var scorecard = $scope.model.Lookup[scorecardKey];
            //var scores = scorecard.Scores;
            for (var w in scorecard.Weeks) {
                var wn = scorecard.Weeks[w].ForWeekNumber;
                $scope.ScoreLookup[wn] = {};
                for (var m in scorecard.Measurables) {
                    var mn = scorecard.Measurables[m].Id;
                    $scope.ScoreLookup[wn][mn] = $scope.functions.lookupScoreFull(wn, mn, scorecardKey);
                }
            }
        }

        if (week in $scope.ScoreLookup && measurableId in $scope.ScoreLookup[week]) {
            var lu = $scope.model.Lookup[$scope.ScoreLookup[week][measurableId]];
            if (lu != null)
                return lu;
        }

        var wKey = week;
        if (!(wKey in $scope.proxyLookup))
            $scope.proxyLookup[wKey] = {};
        if (!(measurableId in $scope.proxyLookup[wKey])) {
            var measurable = { Id: measurableId };
            if (("AngularMeasurable_" + measurableId) in $scope.model.Lookup)
                measurable = $scope.model.Lookup["AngularMeasurable_" + measurableId];

            $scope.proxyLookup[wKey][measurableId] = {
                Id: -1, Type: "AngularScore", ForWeek: week, Measured: null,
                Measurable: measurable,
                Target: measurable.Target, // WRONG (scores generated before nearest)
                Direction: measurable.Direction
            };
        }

        return $scope.proxyLookup[wKey][measurableId];
    };

    $scope.functions.updateComplete = function (self) {
        var instance = self.todo;
        if (!instance)
            instance = self.issue;

        if (instance.Complete) {
            instance.CompleteTime = new Date();
        } else {
            instance.CompleteTime = null;
        }
    };


    $scope.possibleOwners = [];
    $scope.loadPossibleOwners = function () {
        if (typeof ($scope.model) !== "undefined" && typeof ($scope.model.Attendees) !== "undefined") {
            $scope.possibleOwners = $scope.model.Attendees;
            $scope.possibleOwners;
        } else {
            return $scope.possibleOwners.length ? null : $http.get('/Dropdown/AngularMeetingMembers/' + $scope.model.Id + '?userId=true').success(function (data) {
                $scope.possibleOwners = data;
            });
        }
    };
    $scope.possibleDirections = [];
    $scope.loadPossibleDirections = function () {
        return $scope.possibleDirections.length ? null : $http.get('/Dropdown/Type/lessgreater').success(function (data) {
            $scope.possibleDirections = data;
        });
    };

    //var sevenMin = moment().subtract('days', 6).toDate();
    //var sevenMax = moment().add('days', 2).toDate();

    $scope.now = moment();

    $scope.rockstates = [{ name: 'Off Track', value: 'AtRisk' }, { name: 'On Track', value: 'OnTrack' }, { name: 'Done', value: 'Complete' }];

    $scope.opts = {
        ranges: {
            'Incomplete': [moment().add('days', 1), moment().add('days', 9)],
            'Today': [moment().subtract('days', 1), moment().add('days', 9)],
            'Last 7 Days': [moment().subtract('days', 6), moment().add('days', 9)],
            'Last 14 Days': [moment().subtract('days', 13), moment().add('days', 9)],
            'Last 30 Days': [moment().subtract('days', 29), moment().add('days', 9)],
            //'Last 60 Days': [moment().subtract('days', 59), moment().add('days',1)],
            'Last 90 Days': [moment().subtract('days', 89), moment().add('days', 9)]// [sevenMin, sevenMax]
        },
        separator: '  to  ',
        showDropdowns: true,
        format: 'MMM DD, YYYY',
        opens: 'left'
    };


    $scope.filters.byRange = function (fieldName, minValue, maxValue, forceMin) {
        if (minValue === undefined) minValue = -Number.MAX_VALUE;
        if (maxValue === undefined) maxValue = Number.MAX_VALUE;
        if (typeof (forceMin) !== "undefined") {
            minValue = Math.min(minValue, maxValue - forceMin * 24 * 60 * 60 * 1000);
        }

        return function predicateFunc(item) {
            var d = item[fieldName];
            if (!d) return true;//d = moment().add('days', 1).toDate();
            if (d instanceof Date) d = d.getTime();
            if (minValue instanceof Date) minValue = minValue.getTime();
            if (maxValue instanceof Date) maxValue = maxValue.getTime();

            if (fieldName == "ForWeek")
                d -= 7 * 24 * 60 * 60 * 1000;

            return minValue <= d && d <= maxValue || moment(d).format("MMDDYYYY") == moment(maxValue).format("MMDDYYYY");
        };
    };

    $scope.selectedTab = $location.url().replace("/", "");

    //$scope.filters.applyFilters = function (value, index, array) {
    //    if (typeof ($scope.filters.filterList) === "undefined")
    //        return true;
    //    for (var i = 0; i < $scope.filters.filterList.length; i++) {
    //        if ($filter('filter')([value], $scope.filters.filterList[i])[0] == false)
    //            return false;
    //    }

    //    return true;        
    //}

    $scope.filters.completionFilterItems = [
        { name: "Incomplete", value: { Completion: true }, short: "Incomplete" },
    ];

    $scope.options = {};
    $scope.options.l10teamtypes = $scope.loadSelectOptions('/dropdown/type/l10teamtype');

    $scope.functions.sendUpdate = function (self, args) {
        var dat = angular.copy(self);
        var _clientTimestamp = new Date().getTime();

        r.updater.convertDatesForServer(dat, tzoffset());
        var builder = "";
        args = args || {};

        if (!("connectionId" in args))
            args["connectionId"]=$scope.connectionId;
        
        for (var i in args) {
            builder += "&" + i + "=" + args[i];
        }

        $http.post("/L10/Update" + self.Type + "?_clientTimestamp=" + _clientTimestamp  + builder, dat)
            .then(function () { }, showAngularError);
    };


    $scope.functions.checkFutureAndSend = function (self) {
        var m = self;
        var icon = {title:"Update historical goals?"};
        var fields = [
            {
                name: "history",
                value: "false",
                type: "yesno"
            }];

        if (self.Direction == "Between" || self.Direction == -3) {
            icon = "info";
            fields.unshift({
                type: "label",
                value: "Update historical goals?"
            });
            fields.push({
                type: "number",
                text: "Lower-Boundary",
                name: "Lower",
                value:self.Target,
            });
            fields.push({
                type: "number",
                text: "Upper-Boundary",
                name: "Upper",
                value:self.AltTarget || self.Target,
            });
        }


        $scope.functions.showModal({
            icon: icon,
            noCancel: true,
            fields: fields,
            success: function (model) {
            	//m.Target = Math.min(model.Lower,model.Upper);
            	var low = Math.min(+model.Lower, +model.Upper);
            	var high = Math.max(+model.Lower, +model.Upper);
            	if (isNaN(low))
            		low = null;
            	if (isNaN(high))
            		high = null;

                $scope.functions.sendUpdate(m, {
                    "historical": model.history,
                    "Lower": low,
                    "Upper": high,
                    "connectionId":null
                });
            },
            cancel: function () {

            }

        });
    }

    $scope.functions.removeRow = function (event, self) {
        var dat = angular.copy(self);
        var _clientTimestamp = new Date().getTime();
        //var row = $(event.target).closest("tr");
        //row.hide();
        //var row =angular.element($(event.target).closest("tr"));
        //row.hide();
        self.Hide = true;

        $http.post("/L10/Remove" + self.Type + "/?recurrenceId=" + $scope.meetingId + "&_clientTimestamp=" + _clientTimestamp, dat).error(function (data) {
            showJsonAlert(data, false, true);
            self.Hide = false;
        }).finally(function () {
            // row.show()
        });
    };

    $scope.functions.addRow = function (event, type, args) {
        if (!$(event.target).hasClass("disabled")) {
            var _clientTimestamp = new Date().getTime();
            var controller = angular.element($("[ng-controller]"));
            controller.addClass("loading");
            $(event.target).addClass("disabled");

            if (typeof (args) === "undefined")
                args = "";

            $http.get("/L10/Add" + type + "/" + $scope.meetingId + "?connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp + args)
                .error(showAngularError)
                .finally(function () {
                    controller.removeClass("loading");
                    $(event.target).removeClass("disabled");
                });
        }
    };
    $scope.ShowSearch = false;
    $scope.functions.showUserSearch = function (event) {
        $scope.ShowSearch = true;
        $timeout(function () {
            $(".user-list-container .livesearch-container input").focus();
        }, 1);
    };

    $scope.functions.addAttendee = function (selected) {
        var event = { target: $(".user-list-container") };
        $scope.functions.addRow(event, "AngularUser", "&userid=" + selected.item.id);
    }

    $scope.functions.createUser = function () {
        $timeout(function () {
            $scope.functions.showModal('Add managed user', '/User/AddModal', '/nexus/AddManagedUserToOrganization?meeting=' + $scope.meetingId + "&refresh=false");
        }, 1);
    }

    $scope.functions.goto = function (url) {
        $window.location.href = url;
    }

    $scope.functions.blurSearch = function (self) {
        $timeout(function () {
            $scope.model.Search = '';
            self.visible = false;
            $scope.ShowSearch = false;
            angular.element(".searchresultspopup").addClass("ng-hide");
        }, 150);
    }

    $scope.userSearchCallback = function (params) {
        var defer = $q.defer();
        var attendees = $scope.model.Attendees || [];
        var ids = $.map(attendees, function (item) {
            return item.Id;
        })
        $http.get("/User/Search?q=" + params + "&exclude=" + ids)
        .then(function (response) {
            if (!response.data || !response.data.Object) {
                defer.resolve([]);
            }
            defer.resolve(response.data.Object);
        })
        .catch(function (e) {
            defer.reject(e);
        });

        return defer.promise;
    };

    $scope.functions.setHash = function (value) {
        $timeout(function () {
            $window.location.hash = value;
        }, 1);
    };

    $scope.functions.uploadUsers = function () {

        $window.location.href = "/upload/l10/Users?recurrence=" + $scope.meetingId;

        //showModal({
        //    title: "Upload users (.csv)",
        //    fields: [{ name:"file",type: "file" }],
        //}, "/Upload/UploadRecurrenceFile?recurrenceId=" + $scope.meetingId+"&type=users&csv=true",
        //function () {

        //});
    };

}]);