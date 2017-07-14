(function () {
	'use strict';

	var app = angular.module('people').component('peopleAnalyzer', {
		templateUrl: function () { return '/Content/AngularTemplates/People/PeopleAnalyzer/peopleAnalyzerTable.html'; },
		bindings: {
			//	"surveyContainerId": "<?",
			//	surveyId: "<?",
		},
		controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", "$timeout", "$window", function ($scope, $element, $attrs, $http, radial, $log, $timeout, $window) {
			var ctrl = this;
			$scope.functions = {};
			//$log.log("survComp:", $scope);
			var r = radial($scope, {
				hubName: "PeopleHub",
				hubJoinMethod: "Join",
				hubJoinArgs: [null, null],//ctrl.surveyContainerId, ctrl.surveyId],
				sendUpdateUrl: function (self) { return "/People/PeopleAnalyzer/Update" + self.Type; },
				loadDataUrl: "/People/PeopleAnalyzer/Data",
				loadDataOptions: {
					success: function (data) {
						//$log.log("survComp2:", $scope);
						r.updater.clearAndApply(data);
						angular.element(window).triggerHandler('data-loaded');

						//var dates = [];
						$scope.surveyContainerLookup = {};

						var dates = $scope.model.SurveyContainers.map(function (x) { return x.IssueDate; });

						for (var i = 0; i < $scope.model.SurveyContainers.length; i++) {
							var a = $scope.model.SurveyContainers[i];
							$scope.surveyContainerLookup[+a.IssueDate] = a;
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

			$scope.functions.printRow = function (r) {
				var scid = $scope.functions.selectedSurveyContainer().Id;
				$window.open('/people/quarterlyconversation/print?surveyContainerId=' + scid + '&nodeId=' + r.About.ModelId + '', '_blank');
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

			$scope.functions.inactive = function (row, question) {
				var response = $scope.functions.lookup(row, question);
				if (response) {
					var scid = $scope.functions.selectedSurveyContainer().Id;
					return response.SurveyContainerId != scid;
				}
				return true;
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
					var ordered= avail.groupBy(function (x) {
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