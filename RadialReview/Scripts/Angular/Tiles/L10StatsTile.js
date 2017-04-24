var app = angular.module("L10App");
app.requires.push("n3-line-chart");
app.directive('l10StatsTile', function () {
	return {
		restrict: "E",
		template: //"<a href='#' class='series-selector' ng-class='{selected: selected==s.label}'  ng-click='click(s)' ng-repeat='s in model.options.series'>{{s.label}}</a>" +
			"<linechart data='data' options='options'></linechart>",
		scope: {
			ttUrl: "@ttUrl"
			//onClick: "@ttUrl"
		},
		controller: ['$scope', "$http", 'updater', function ($scope, $http, updater) {


			/*$scope.click = function (s) {
				for (var r in $scope.model.options.series) {
					if (arrayHasOwnIndex($scope.model.options.series, r)) {
						var series = $scope.model.options.series[r];
						series.visible = s.label == series.label;
					}
				}
			}*/


			$http.get($scope.ttUrl).success(function (data) {
				$scope.model = {};
				updater().convertDates(data);

				if ($scope.model && $scope.model.options) {
				}

				$.extend($scope.model, data);

				$scope.data = $scope.model.data;
				$scope.model.options = $scope.model.options || {};
				$scope.model.options.tooltipHook = function (rows) {
					var monthNames = [
					  "January", "February", "March",
					  "April", "May", "June", "July",
					  "August", "September", "October",
					  "November", "December"
					];

					var o = [];
					if (typeof (rows) === "undefined" || rows.length == 0)
						return {};

					var date = rows[0].row.x;
					var day = date.getDate();
					var monthIndex = date.getMonth();
					var year = date.getFullYear();

					for (var r in rows) {
						if (arrayHasOwnIndex(rows, r)) {
							s = rows[r];
							var suffix = "";
							if (s.series.label.indexOf("To-do") >= 0)
								suffix = "%";

							o.push({
								label: s.series.label + ":",
								value: s.row.y1 + suffix,
								color: s.series.color,
								id: s.series.id
							});
						}
					}

					return {
						abscissas: monthNames[monthIndex] + " " + day + ", " + year,
						rows: o
					};
				}//.axes.x.tickFormat = "%B %e, %Y";
				$scope.options = $scope.model.options;//tooltipFormatter = function (d) {

				/*var first = true;
				debugger;
				for (var r in $scope.model.options.series) {
					if (arrayHasOwnIndex($scope.model.options.series, r)) {
						if (first) {
							$scope.selected = $scope.model.options.series[r].label;
						}
						$scope.model.options.series[r].visible = first;
						first = false;
					}
				}*/



				//	"%B %e, %Y";
				//	debugger;
				//	return "";
				//}
			});
		}]
	};
});