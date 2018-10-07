(function () {
	'use strict';

	var app = angular.module('people').component('peopleAnalyzer', {
		templateUrl: function () { return '/Content/AngularTemplates/People/PeopleAnalyzer/peopleAnalyzerTable.html'; },
		bindings: {
			//	"surveyContainerId": "<?",
			//	surveyId: "<?",
			"recurrence": "<?",
			"showQcActions":"<?"
		},
		controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", "$timeout", "$window", function ($scope, $element, $attrs, $http, radial, $log, $timeout, $window) {
			var ctrl = this;
			$scope.functions = {};
			//$log.log("survComp:", $scope);
			var query = "";
			if (this.recurrence) {
				query = "?recurrenceId=" + this.recurrence;
			}


			$scope.showQuarterlyConversationActions=this.showQcActions;


			var r = radial($scope, {  });

			var r = radial($scope, {
				hubs: { surveyContainerId:null,surveyId:null},
				//hubName: "RealTimeHub",
				//hubJoinMethod: "Join",
				//hubJoinArgs: [{
				//	surveys: [{surveyContainerId:null,surveyId:null}]
				//}],//null, null],//ctrl.surveyContainerId, ctrl.surveyId],
				sendUpdateUrl: function (self) { return "/People/PeopleAnalyzer/Update" + self.Type; },
				loadDataUrl: "/People/PeopleAnalyzer/Data" + query,
				loadDataOptions: {
					success: function (data) {
						r.updater.clearAndApply(data);
						angular.element(window).triggerHandler('data-loaded');

						$scope.surveyContainerLookup = {};
						$scope.surveyContainerLookupById = {};

						var dates = $scope.model.SurveyContainers.map(function (x) { return x.IssueDate; });

						for (var i = 0; i < $scope.model.SurveyContainers.length; i++) {
							var a = $scope.model.SurveyContainers[i];
							$scope.surveyContainerLookup[+a.IssueDate] = a;
							$scope.surveyContainerLookupById[+a.Id] = a;
						}

						$scope.slider = {
							value: dates[dates.length - 1], // or new Date(2016, 7, 10) is you want to use different instances
							options: {
								stepsArray: dates,
								translate: function (date) {
									if (date != null)
										return date.toDateString();
									return '';
								}
							}
						};

						$scope.refreshSlider();

					}
				}
			});

			$scope.functions.selectedSurveyContainer = function () {
				if ($scope.slider && $scope.slider.value) {
					return $scope.surveyContainerLookup[+$scope.slider.value];
				}
			}

			//$scope.functions.selectedSurveyContainerId = function () {
			//	return $scope.surveyContainerLookup[+$scope.slider.value];
			//};

			$scope.functions.getSunId = function (row) {
				return Enumerable.from($scope.model.Responses)
					.where(function (x) {
						return x.About.ModelId == row.About.ModelId &&
								x.About.ModelType == row.About.ModelType &&
								(x.IssueDate - $scope.slider.value == 0)
					}).select(function (x) {
						return x.SunId;
					}).firstOrDefault();
			};

			$scope.functions.printRow = function (r) {
				var scid = $scope.functions.selectedSurveyContainer().Id;
				var pid = $scope.functions.getSunId(r);
				$window.open('/people/quarterlyconversation/print?surveyContainerId=' + scid + '&sunId=' + pid + '', '_blank');
			};

			$scope.functions.sendUpdate = function (a) {
				$log.log(a);
				r.sendUpdate(a);
			};

			$scope.refreshSlider = function () {
				$timeout(function () {
					$scope.$broadcast('rzSliderForceRender');
				});
			};
			$scope.functions.personallyOwning = function (row) {
				var id = $scope.functions.getSunId(row);
				return typeof (id) !== "undefined" && id != null;
			};
			$scope.functions.isLockedIn = function (row) {
				var found = Enumerable.from($scope.model.LockedIn)
					.where(function (x) {
						return x.By.ModelId == row.About.ModelId &&
								x.By.ModelType == row.About.ModelType &&
								(x.IssueDate - $scope.slider.value == 0)
					}).firstOrDefault();

				if (typeof(found)!=="undefined" && found!==null){
					return found.LockedIn;
				}
				return false;				
			};
			$scope.functions.sendReminder = function (r) {
				var btn = $("[data-name='row_" + r.Id+"']").find(".btn-remind");
				btn.attr("disabled", true);
				var scid = $scope.functions.selectedSurveyContainer().Id;
				var pid = $scope.functions.getSunId(r);
				var send = true;
				btn.find(".msg").text("Sending");
				btn[0].blur();
				debugger;
				if (send) {
					$http.get('/people/quarterlyconversation/remind?surveyContainerId=' + scid + '&sunId=' + pid).then(function () {
						showAlert("Reminder sent.");
						btn.find(".msg").text("Sent.")
					}, function () {
						showAlert("Something went wrong.");						
						btn.find(".msg").text("Remind");
						btn.attr("disabled",null);
					});
				}
				//var scid = $scope.functions.selectedSurveyContainer().Id;
				//var pid = $scope.functions.getSunId(r);
				//$window.open('/people/quarterlyconversation/print?surveyContainerId=' + scid + '&sunId=' + pid + '', '_blank');
			};
			
			$scope.functions.inactive = function (row, question) {
				var response = $scope.functions.lookup(row, question);
				if (response) {
					var scid = $scope.functions.selectedSurveyContainer().Id;
					return response.SurveyContainerId != scid;
				}
				return true;
			};

			$scope.functions.lookupWhen = function (row, question) {
				var response = $scope.functions.lookup(row, question);
				
				if (response) {
					var surveyContainerId = response.SurveyContainerId;
					var sc = $scope.surveyContainerLookupById[surveyContainerId];
					try {
						return sc.Name + "  (" + getFormattedDate(sc.IssueDate)+")";
					} catch (e) {
						console.error(e);
					}
				}
				return "";
			};

			$scope.functions.lookup = function (row, question) {
				if (typeof (question) === "string") {
					question = { Source: { ModelId: -1, ModelType: question } };
				}

				if (typeof (question) === "undefined") {
					return;
				}

				var avail = Enumerable.from($scope.model.Responses)
					.where(function (x) {
						return x.Source.ModelId == question.Source.ModelId &&
								x.Source.ModelType == question.Source.ModelType &&
								x.About.ModelId == row.About.ModelId &&
								x.About.ModelType == row.About.ModelType &&
								x.IssueDate <= $scope.slider.value
					});

				if (avail.any()) {
					var ordered = avail.groupBy(function (x) {
						return +x.IssueDate;
					}).orderByDescending(function (x) {
						return x.key();//.IssueDate.getTime();
					}).first();
					var selected = ordered.orderByDescending(function (x) {
						return x.Override;
					}).first();
					return selected;
				}

				//$log.log("lu:", row, question);
			};
		}]
	});
})();