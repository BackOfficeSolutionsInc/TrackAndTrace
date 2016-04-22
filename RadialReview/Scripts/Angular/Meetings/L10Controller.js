﻿angular.module('L10App').controller('L10Controller', ['$scope', '$http', '$timeout', 'radial', 'meetingDataUrlBase', 'meetingId', "meetingCallback", "$compile", "$sce", "$q","$window",
function ($scope, $http, $timeout, radial, meetingDataUrlBase, meetingId, meetingCallback, $compile, $sce, $q,$window) {

    $scope.trustAsResourceUrl = $sce.trustAsResourceUrl;
    if (meetingId == null)
        throw Error("MeetingId was empty");
    $scope.disconnected = false;
    $scope.meetingId = meetingId;

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
        if (data.Scorecard != null && data.Scorecard.Scores != null) {

            var lu = data.Scorecard.Scores;
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
    $scope.functions.reload = function (reload) {
        if (typeof (reload) == "undefined") {
            reload = true;
        }
        if (reload) {

            if (!window.tzoffset) {
                var jan = new Date(new Date().getYear() + 1900, 0, 1, 2, 0, 0), jul = new Date(new Date().getYear() + 1900, 6, 1, 2, 0, 0);
                window.tzoffset = (jan.getTime() % 24 * 60 * 60 * 1000) >
                             (jul.getTime() % 24 * 60 * 60 * 1000)
                             ? jan.getTimezoneOffset() : jul.getTimezoneOffset();
            }


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

            $http({ method: 'get', url: url })
            .success(function (data, status) {
                // r.updater.clearAndApply(data, status);
                r.updater.convertDates(data);
                r.updater.clearAndApply(data);
                //$scope.model = data;

                if (meetingCallback) {
                    setTimeout(function () {
                        meetingCallback();
                    }, 1);
                }
            }).error(function (data, status) {
                console.log("Error");
                console.error(data);
            });
        }
    }

    $scope.functions.reload(true);

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
        var goal = s.Measurable.Target;
        var dir = s.Measurable.Direction;
        if (!$.trim(v)) {
            return "";
        } else if ($.isNumeric(v)) {
            if (dir == "GreaterThan" || dir == 1) {
                if (+v >= +goal)
                    return "success";
                else
                    return ("danger");
            } else {
                if (+v < +goal)
                    return ("success");
                else
                    return ("danger");
            }

        } else {
            return ("error");
        }
    };

    $scope.proxyLookup = {};
    $scope.ScoreIdLookup = null;


    $scope.functions.getFcsa = function (measurable) {
        if (measurable.Modifiers == "Dollar") {
            return { prepend: "$" };
        } else if (measurable.Modifiers == "Percent") {
            return { append: "%" };
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
        if (!(measurableId in $scope.proxyLookup[wKey]))
            $scope.proxyLookup[wKey][measurableId] = { Id: -1, Type: "AngularScore", Measurable: { Id: measurableId }, ForWeek: week, Measured: null };

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

    $scope.rockstates = [{ name: 'Off Track', value: 'AtRisk' }, { name: 'On Track', value: 'OnTrack' }, { name: 'Complete', value: 'Complete' }];

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

    $scope.options = {};
    $scope.options.l10teamtypes = $scope.loadSelectOptions('/dropdown/type/l10teamtype');

    $scope.functions.sendUpdate = function (self) {
        var dat = angular.copy(self);
        var _clientTimestamp = new Date().getTime();

        $http.post("/L10/Update" + self.Type + "?connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp, dat).error(function (data) {
            showJsonAlert(data, true, true);
        });
    };


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

            $http.get("/L10/Add" + type + "/" + $scope.meetingId + "?connectionId=" + $scope.connectionId + "&_clientTimestamp=" + _clientTimestamp + args).error(function (data) {
                showJsonAlert(data, true, true);
            }).finally(function () {
                controller.removeClass("loading");
                $(event.target).removeClass("disabled");
            });
        }
    };
    $scope.ShowSearch = false;
    $scope.functions.showUserSearch = function (event) {
        $scope.ShowSearch = true;
        setTimeout(function(){
            $(".user-list-container .livesearch-container input").focus();
        },1);
    };

    $scope.functions.addAttendee = function (selected) {
        var event = { target: $(".user-list-container") };
        $scope.functions.addRow(event, "AngularUser", "&userid=" + selected.item.id);
    }

    $scope.functions.createUser = function () {
        $scope.functions.showModal('Add managed user', '/User/AddModal', '/nexus/AddManagedUserToOrganization?meeting=' + $scope.meetingId+"&refresh=false");
    }

    $scope.functions.goto = function (url) {
        $window.location.href = url;
    }

    $scope.functions.blurSearch = function (self) {
        $timeout(function () {
            $scope.model.Search = '';
            self.visible = false;
            $scope.ShowSearch = false;
        },1);
    }

    $scope.userSearchCallback = function (params) {
        var defer = $q.defer();

        var ids = $.map($scope.model.Attendees, function (item) {
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