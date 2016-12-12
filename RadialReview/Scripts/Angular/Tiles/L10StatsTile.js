var app = angular.module("L10App");
app.requires.push("n3-line-chart");
app.directive('l10StatsTile', function() {
		return {
			restrict: "E",
			template: "<linechart data='data' options='options'></linechart>",
			scope: {
				ttUrl: "@ttUrl"
			},
			controller: ['$scope', "$http", 'updater', function ($scope, $http,updater) {

				$http.get($scope.ttUrl).success(function (data) {
					$scope.model = {};
					updater().convertDates(data);

					if ($scope.model && $scope.model.options){				
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
						if (typeof(rows)==="undefined" || rows.length == 0)
							return {};

						var date = rows[0].row.x;
						var day = date.getDate();
						var monthIndex = date.getMonth();
						var year = date.getFullYear();

						for (var r in rows) {
							s = rows[r];
							var suffix = "";
							if (s.series.label.indexOf("To-do") >= 0)
								suffix = "%";

							o.push({
								label: s.series.label+":",
								value: s.row.y1+suffix,
								color: s.series.color,
								id: s.series.id
							});
						}

						return {
							abscissas: monthNames[monthIndex] + " " + day + ", " + year,
							rows: o
						};
					}//.axes.x.tickFormat = "%B %e, %Y";
					$scope.options = $scope.model.options;//tooltipFormatter = function (d) {
					//	"%B %e, %Y";
					//	debugger;
					//	return "";
					//}
				});
			}]
	};
});