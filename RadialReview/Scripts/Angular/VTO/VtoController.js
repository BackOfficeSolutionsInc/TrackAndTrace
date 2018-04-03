angular.module('VtoApp').controller('VtoController', ['$scope', '$http', '$timeout', 'radial', 'signalR', 'vtoDataUrlBase', 'vtoId', "vtoCallback",
	function ($scope, $http, $timeout, radial, signalR, vtoDataUrlBase, vtoId, vtoCallback) {
		if (vtoId == null)
			throw Error("VtoId was empty");
		$scope.disconnected = false;
		$scope.vtoId = vtoId;

		function rejoin(connection, proxy, callback) {
			try {
				if (proxy) {
					proxy.invoke("join", $scope.vtoId, connection.id).done(function () {
						console.log("rejoin");
						$(".rt").prop("disabled", false);
						if (callback) {
							callback();
						}
						if ($scope.disconnected) {
							clearAlerts();
							showAlert("Reconnected.", "alert-success", "Success", 1000);
						}
						$scope.disconnected = false;
					});
				}
			} catch (e) {
				console.error(e);
			}
		}



		var r = radial($scope, 'vtoHub', rejoin);

		$http({ method: 'get', url: vtoDataUrlBase + $scope.vtoId })
			.success(function (data, status) {
				r.updater.clearAndApply(data);

				if (vtoCallback) {
					setTimeout(function () {
						vtoCallback();
					}, 1);
				}
				$(".target [textarea-resize]").autoResize();
				$(".target-market [textarea-resize]").autoResize();
				$(".purpose [textarea-resize]").autoResize();
				$(".niche [textarea-resize]").autoResize();

				//setTimeout(function () {
				//	$("[textarea-resize]").each(function () {
				//		$(this).autoResize();
				//	});
				//}, 1);
			}).error(showAngularError);
		$scope.functions = {};
		$scope.filters = {};

		$scope.functions.subtractDays = function (date, days) {
			var d = new Date(date);
			d.setDate(d.getDate() - days);
			return d;
		};

		$scope.proxyLookup = {};
		//var tzoffset = r.updater.tzoffset;

		$scope.functions.sendUpdate = function (self) {
			Time.eachDate(self, function (d, o, k) {
				if (k == "FutureDate") { return Time.adjustToMidnight(d); }
			});
			var dat = angular.copy(self);
			if (false) {
				r.updater.convertDatesForServer(dat, Time.tzoffset());
			} else {
				console.warn("Dates were not converted for server, please confirm");
			}
			console.log(self);

			var url = Time.addTimestamp("/VTO/Update" + self.Type + "?connectionId=" + $scope.connectionId);

		$http.post(url, dat).then(function () { }, showAngularError);
		};

		$scope.functions.AddRow = function (url, self) {
			$scope.functions.Get(url);
		};

		$scope.functions.tabClick = function (element) {
			console.log(element);
			$('#exTab2 .nav nav-tabs li').each(function () {
				$(this).removeClass('active');
			})

			$(this).parent().addClass('active');

			$('.tab-content .tab-pane').each(function () {
				$(this).removeClass('active');
			});

			$(element).addClass('active');

			$('#exTab2 .nav nav-tabs li').each(function () {
				$(this).removeClass('active');
			})

			$(this).parent().addClass('active');
		}

	$scope.functions.shouldCreateRow = function ($event, self, url, collection) {
		if ($event.keyCode == 9 && self.$last) {
			$scope.functions.AddRow(url, self);
			$event.preventDefault();
			var that = $event.currentTarget;
			var watcher = $scope.$watchCollection(
                    collection,
                    function (newValue, oldValue) {
                    	if (newValue.length > oldValue.length) {
                    		angular.element(that)
                                .closest("[ng-repeat]")
                                .siblings()
                                .filter(function () {
                                	return $(this).find("textarea").length;
                                }).last()
                                .find("textarea").last()
                                .focus()
                    		watcher();
                    	}

                    }
                );
			}
		}

		$scope.functions.Get = function (url, dat) {
			//var _clientTimestamp = new Date().getTime();
			var u = Time.addTimestamp(url) + "&connectionId=" + $scope.connectionId;
			$http.get(u).then(function () { }, showAngularError)
		};


		$scope.functions.AddNewStrategy = function (url, dat) {
			//var _clientTimestamp = new Date().getTime();
			var u = Time.addTimestamp(url) + "&connectionId=" + $scope.connectionId;
			$http.get(u).then(function () {

				$http({ method: 'get', url: vtoDataUrlBase + $scope.vtoId })
					.success(function (data, status) {
						r.updater.clearAndApply(data);

						if (vtoCallback) {
							setTimeout(function () {
								vtoCallback();
							}, 1);
						}
						$(".target [textarea-resize]").autoResize();
						$(".target-market [textarea-resize]").autoResize();
						$(".purpose [textarea-resize]").autoResize();
						$(".niche [textarea-resize]").autoResize();

						//setTimeout(function () {
						//	$("[textarea-resize]").each(function () {
						//		$(this).autoResize();
						//	});
						//}, 1);
					}).error(showAngularError);
				//window.location.href = '/vto/edit/' + $scope.vtoId;
			}, showAngularError)
		};



		$scope.functions.DeleteStrategy = function (url, title) {
			//var _clientTimestamp = new Date().getTime();
			var u = Time.addTimestamp(url) + "&connectionId=" + $scope.connectionId;

			if (!title) {
				title = "this marketing strategy";
			} else {
				title = "<b>"+title+"</b>"
			}

			showModal({
				title: "Are you sure you want to delete "+title+"?",
				icon: "warning",
				success: function () {
					//call delete code

					$http.get(u).then(function () {
						setTimeout(function(){
							$(".strategy-tabs li").first().click().addClass("active-li");
						},1);
						//$http({ method: 'get', url: vtoDataUrlBase + $scope.vtoId });
							//.success(function (data, status) {
							//	r.updater.clearAndApply(data);

							//	if (vtoCallback) {
							//		setTimeout(function () {
							//			vtoCallback();
							//		}, 1);
							//	}
							//	$(".target [textarea-resize]").autoResize();
							//	$(".target-market [textarea-resize]").autoResize();
							//	$(".purpose [textarea-resize]").autoResize();
							//	$(".niche [textarea-resize]").autoResize();
							//}).error(showAngularError);
						//window.location.href = '/vto/edit/' + $scope.vtoId;
					}, showAngularError)

				},
				cancel: function () {
					//optional cancel code
				}
			});
		};



		$scope.functions.showStrategyTab = function (id) {
			var targets1 = angular.element(document).find('.tab-pane');
			var tabs = angular.element(document).find('.nav-tabs li');

			tabs.each(function () {
				$(this).removeClass('active');
				$(this).removeClass('active-li');
			});

			$('#tab' + id).addClass('active-li');

			targets1.each(function () {
				$(this).removeClass('active');
				$(this).removeClass('in');
			});

			$('#' + id).addClass('active');
			$('#' + id).addClass('in');
		}


	}]).directive('blurToCurrency', function ($filter) {
		return {
			scope: {
				amount: '='
			},
			link: function (scope, el, attrs) {
				el.val($filter('currency')(scope.amount));

				el.bind('focus', function () {
					el.val(scope.amount);
				});

				el.bind('input', function () {
					scope.amount = el.val();
					scope.$apply();
				});

				el.bind('blur', function () {
					el.val($filter('currency')(scope.amount));
				});
			}
		};
	}).directive('textareaResize', function () {
		return {
			link: function (scope, elem) {
				if (elem.attr('isResized') !== "true") {
					scope.resize = $(elem).autoResize();
					elem.attr('isResized', 'true');
				}

				scope.$on(
					"$destroy",
					function handleDestroyEvent() {
						scope.resize.destroy();
					}
				);
			}
		};
	});
