//SurveyContainer Controller
angular.module('people').component('surveyContainer', {
	templateUrl: function () { return '/Content/AngularTemplates/People/Survey/surveyContainer.html'; },
	bindings: {
		"surveyContainerId": "<?",
		"surveyId": "<?",
	},
	controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", "$timeout", "$window", function ($scope, $element, $attrs, $http, radial, $log, $timeout, $window) {
		var ctrl = this;
		$scope.functions = {};
		$log.log("survComp:", $scope);
		var r = radial($scope, {
			hubName: "PeopleHub",
			hubJoinMethod: "Join",
			hubJoinArgs: [ctrl.surveyContainerId, ctrl.surveyId],
			sendUpdateUrl: function (self) { return "/People/Survey/Update" + self.Type; },
			loadDataUrl: "/People/Survey/Data?surveyId=" + ctrl.surveyId + "&surveyContainerId=" + ctrl.surveyContainerId,
			loadDataOptions: {
				success: function (data) {
					$log.log("survComp2:", $scope);
					r.updater.clearAndApply(data);
				}
			}
		});
		$scope.functions.sendUpdate = function (a) {
			$log.log(a);
			r.sendUpdate(a);
		};

		$scope.showSurvey = function (survey) {
			for (var i = 0; i < survey.Sections.length; i++) {
				if (survey.Sections[i].Items.length > 0)
					return true;
			}
			return false;
		}

		var sectionNames = {};

		$scope.getSections = function (survey) {
			var builder = "";
			for (var c in survey.Sections) {
				if (arrayHasOwnIndex(survey.Sections, c)) {
					builder += "|section-" + survey.Sections[c].Id;
				}
			}
			sectionNames["survey-" + survey.Id] = survey.Name;
			return "survey-" + survey.Id + builder;
		}
		$scope.anchorShift = function (titlebar) {
			var s = -90;
			if (titlebar)
				s = -48;
			$timeout(function () {
				$window.scrollTo($window.scrollX, $window.scrollY + s);
			}, 1);
		};
		$scope.$on('spied', function (evt, spies) {

			//debugger;
			for (var i = 0; i < spies.length; i++) {
				var spy = spies[i];
				if (spy in sectionNames)
					$scope.CurrentSection = sectionNames[spy];
			}
		});
	}]
}).component('survey', {
	templateUrl: function () { return '/Content/AngularTemplates/People/Survey/survey.html'; },
	bindings: {
		survey: "<",
		onChangeResponse: "&?",
	},
	controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", function ($scope, $element, $attrs, $http, radial, $log) {
		$scope.model = this.survey;
		$scope.onChangeResponse = this.onChangeResponse;
		$scope.changeResponse = function (response) {
			$scope.onChangeResponse()(response);
		};

		$scope.showSurvey = function () {
			for (var i = 0; i < $scope.model.Sections.length; i++) {
				if ($scope.model.Sections[i].Items.length > 0)
					return true;
			}
			return false;
		}

	}]
}).component('surveySection', {
	templateUrl: function () { return '/Content/AngularTemplates/People/Survey/surveySection.html'; },
	bindings: {
		section: "<",
		onChangeResponse: "&?",
	},
	controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", function ($scope, $element, $attrs, $http, radial, $log) {
		$scope.model = this.section;
		$scope.onChangeResponse = this.onChangeResponse;

		$scope.changeResponse = function (response) {
			$scope.onChangeResponse()(response);
		};
	}]
}).component('surveyItemContainer', {
	templateUrl: function () { return '/Content/AngularTemplates/People/Survey/surveyItemContainer.html'; },
	bindings: {
		itemContainer: "<",
		onChangeResponse: "&?",
	},
	controller: ["$scope", "$element", "$scope", "$http", 'radial', "$log", function ($scope, $element, $attrs, $http, radial, $log) {
		$scope.model = this.itemContainer;
		$scope.onChangeResponse = this.onChangeResponse;
		$scope.changeResponse = function (response) {
			$scope.onChangeResponse()(response);
		};
	}]
}).component('surveyItem', {
	template: '<div ng-include="getActualTemplateContent()"></div>',
	bindings: {
		item: "<",
		format: "<",
		type: "<",
		response: "<?",
		onChangeResponse: "&?",
	},
	controller: ["$scope", "$element", "$attrs", "$http", 'radial', "$log", function ($scope, $element, $attrs, $http, radial, $log) {
		$scope.item = this.item;
		$scope.type = this.type;
		$scope.format = this.format;
		$scope.response = this.response;
		$scope.onChangeResponse = this.onChangeResponse;
		$scope.classes = this.format.classes || {
			responses: "btn-group",
			response: "",
			responselabel: "btn btn-primary"
		};
		$scope.changeResponse = function (response) {
			$scope.onChangeResponse()(response);
		};
		$scope.getActualTemplateContent = function () {
			return '/Content/AngularTemplates/People/Survey/Items/' + $scope.format.TemplateModifier + "/" + $scope.format.ItemType + '-item.html';
		};
	}]
});
/* ng-ScrollSpy.js v3.2.2
 * https://github.com/patrickmarabeas/ng-ScrollSpy.js
 *
 * Copyright 2014, Patrick Marabeas http://marabeas.io
 * Released under the MIT license
 * http://opensource.org/licenses/mit-license.php
 *
 * Date: 09/02/2016
 */

; (function (window, document, angular, undefined) {
	'use strict';
	angular.module('ngScrollSpy', [])
		.value('scrollSpyDefaultConfig', {
			'offset': 1,
			'delay': 100
		})
		.run(['PositionFactory', function (PositionFactory) {
			PositionFactory.refreshPositions();
			angular.element(window).bind('scroll', function () {
				PositionFactory.refreshPositions();
			});
		}])
		.factory('PositionFactory', ['$rootScope', function ($rootScope) {
			return {
				'position': [],
				'refreshPositions': function () {
					this.position.documentHeight = Math.max(document.body.scrollHeight, document.body.offsetHeight, document.documentElement.clientHeight, document.documentElement.scrollHeight, document.documentElement.offsetHeight)
					this.position.windowTop = (window.pageYOffset !== undefined) ? window.pageYOffset : (document.documentElement || document.body.parentNode || document.body).scrollTop
					this.position.windowBottom = this.position.windowTop + window.innerHeight
				}
			}
		}])
		.factory('SpyFactory', ['$rootScope', function ($rootScope) {
			return {
				'spies': [],
				'addSpy': function (id) {
					var index = this.spies.map(function (e) { return e }).indexOf(id);
					if (index == -1) {
						this.spies.push(id);
						this.broadcast(this.spies);
					}
				},
				'removeSpy': function (id) {
					var index = this.spies.map(function (e) { return e }).indexOf(id);
					if (index != -1) {
						this.spies.splice(index, 1);
						//if (index > 0 && index - 1 < this.spies.length - 1) {
						//	debugger;
						//	this.broadcast(this.spies[index - 1], "add");
						//}
						this.broadcast(this.spies);
					}
				},
				'broadcast': function (id, type) {
					$rootScope.$broadcast('spied', id, type);
				}
			}
		}])
		.directive('scrollspyBroadcast', [
			'scrollSpyDefaultConfig',
			'scrollspyConfig',
			'SpyFactory',
			'PositionFactory',
			function (config, scrollspyConfig, SpyFactory, PositionFactory) {
				return {
					restrict: 'A',
					scope: true,
					link: function (scope, element, attrs) {
						angular.extend(config, scrollspyConfig.config);
						var offset = parseInt(attrs.scrollspyOffset || config.offset);
						scope.checkActive = function () {
							scope.elementTop = element[0].offsetTop;
							scope.elementBottom = scope.elementTop + Math.max(element[0].scrollHeight, element[0].offsetHeight);
							if ((scope.elementTop - offset) < (PositionFactory.position.documentHeight - window.innerHeight)) {
								if (scope.elementTop <= (PositionFactory.position.windowTop + offset)) {
									SpyFactory.addSpy(attrs.id);
								} else {
									SpyFactory.removeSpy(attrs.id);
								}
							} /*else {
								if (PositionFactory.position.windowBottom > (scope.elementBottom - offset)) {
									SpyFactory.addSpy(attrs.id);
								} else {
									SpyFactory.removeSpy(attrs.id);
								}
							}*/
						};

						config.throttle
						  ? angular.element(window).bind('scroll', config.throttle(function () { scope.checkActive() }, config.delay))
						  : angular.element(window).bind('scroll', function () { scope.checkActive() });

						angular.element(document).ready(function () { scope.checkActive() });
						angular.element(window).bind('resize', function () { scope.checkActive() });
					}
				}
			}
		])

	  .directive('scrollspyListen', ['$timeout', 'SpyFactory', function ($timeout, SpyFactory) {
	  	return {
	  		restrict: 'A',
	  		scope: {
	  			scrollspyListen: '@',
	  			enabled: '@'
	  		},
	  		replace: true,
	  		transclude: true,
	  		template: function (element) {
	  			var tag = element[0].nodeName;
	  			return '<' + tag + ' data-ng-transclude data-ng-class="{active: enabled}"></' + tag + '>';
	  		},
	  		link: function (scope) {
	  			scope.$on('spied', function () {
	  				$timeout(function () {
	  					var spies = scope.scrollspyListen.split("|");
	  					for (var i = 0; i < spies.length; i++)
	  						if (scope.enabled = spies[i] === SpyFactory.spies[SpyFactory.spies.length - 1])
	  							break;
	  				});
	  			});
	  		}
	  	}
	  }])

	  .provider('scrollspyConfig', function () {
	  	var self = this;
	  	this.config = {};
	  	this.$get = function () {
	  		var extend = {};
	  		extend.config = self.config;
	  		return extend;
	  	};
	  	return this;
	  });

})(window, document, angular);







//.directive('scrollSpy', function ($window) {
//	return {
//		restrict: 'A',
//		controller: function ($scope) {
//			$scope.spies = [];
//			this.addSpy = function (spyObj) {
//				$scope.spies.push(spyObj);
//			};
//		},
//		link: function (scope, elem, attrs) {
//			var spyElems;
//			spyElems = [];
//			scope.$watch('spies', function (spies) {
//				var spy, _i, _len, _results;
//				_results = [];
//				for (_i = 0, _len = spies.length; _i < _len; _i++) {
//					spy = spies[_i];
//					if (spyElems[spy.id] == null) {
//						_results.push(spyElems[spy.id] = elem.find('#' + spy.id));
//					}
//				}
//				return _results;
//			});
//			$($window).scroll(function () {
//				var highlightSpy, pos, spy, _i, _len, _ref;
//				highlightSpy = null;
//				_ref = scope.spies;
//				// cycle through `spy` elements to find which to highlight
//				for (_i = 0, _len = _ref.length; _i < _len; _i++) {
//					spy = _ref[_i];
//					spy.out();
//					// catch case where a `spy` does not have an associated `id` anchor
//					if (spyElems[spy.id].offset() === undefined) {
//						continue;
//					}
//					if ((pos = spyElems[spy.id].offset().top) - $window.scrollY <= 0) {
//						// the window has been scrolled past the top of a spy element
//						spy.pos = pos;
//						if (highlightSpy == null) {
//							highlightSpy = spy;
//						}
//						if (highlightSpy.pos < spy.pos) {
//							highlightSpy = spy;
//						}
//					}
//				}
//				// select the last `spy` if the scrollbar is at the bottom of the page
//				if ($(window).scrollTop() + $(window).height() >= $(document).height()) {
//					spy.pos = pos;
//					highlightSpy = spy;
//				}
//				return highlightSpy != null ? highlightSpy["in"]() : void 0;
//			});
//		}
//	};
//}).directive('spy', function ($location, $anchorScroll) {
//	return {
//		restrict: "A",
//		require: "^scrollSpy",
//		link: function (scope, elem, attrs, affix) {
//			elem.click(function () {
//				$location.hash(attrs.spy);
//				$anchorScroll();
//			});
//			affix.addSpy({
//				id: attrs.spy,
//				in: function () {
//					elem.addClass('active');
//				},
//				out: function () {
//					elem.removeClass('active');
//				}
//			});
//		}
//	};
//});

///**
//  * x is a value between 0 and 1, indicating where in the animation you are.
//  */
//var duScrollDefaultEasing = function (x) {
//	'use strict';

//	if (x < 0.5) {
//		return Math.pow(x * 2, 2) / 2;
//	}
//	return 1 - Math.pow((1 - x) * 2, 2) / 2;
//};

//var duScroll = angular.module('duScroll', [
//  'duScroll.scrollspy',
//  'duScroll.smoothScroll',
//  'duScroll.scrollContainer',
//  'duScroll.spyContext',
//  'duScroll.scrollHelpers'
//])
//  //Default animation duration for smoothScroll directive
//  .value('duScrollDuration', 350)
//  //Scrollspy debounce interval, set to 0 to disable
//  .value('duScrollSpyWait', 100)
//  //Scrollspy forced refresh interval, use if your content changes or reflows without scrolling.
//  //0 to disable
//  .value('duScrollSpyRefreshInterval', 0)
//  //Wether or not multiple scrollspies can be active at once
//  .value('duScrollGreedy', false)
//  //Default offset for smoothScroll directive
//  .value('duScrollOffset', 0)
//  //Default easing function for scroll animation
//  .value('duScrollEasing', duScrollDefaultEasing)
//  //Which events on the container (such as body) should cancel scroll animations
//  .value('duScrollCancelOnEvents', 'scroll mousedown mousewheel touchmove keydown')
//  //Whether or not to activate the last scrollspy, when page/container bottom is reached
//  .value('duScrollBottomSpy', false)
//  //Active class name
//  .value('duScrollActiveClass', 'active');

//if (typeof module !== 'undefined' && module && module.exports) {
//	module.exports = duScroll;
//}


//angular.module('duScroll.scrollHelpers', ['duScroll.requestAnimation'])
//.run(["$window", "$q", "cancelAnimation", "requestAnimation", "duScrollEasing", "duScrollDuration", "duScrollOffset", "duScrollCancelOnEvents", function ($window, $q, cancelAnimation, requestAnimation, duScrollEasing, duScrollDuration, duScrollOffset, duScrollCancelOnEvents) {
//	'use strict';

//	var proto = {};

//	var isDocument = function (el) {
//		return (typeof HTMLDocument !== 'undefined' && el instanceof HTMLDocument) || (el.nodeType && el.nodeType === el.DOCUMENT_NODE);
//	};

//	var isElement = function (el) {
//		return (typeof HTMLElement !== 'undefined' && el instanceof HTMLElement) || (el.nodeType && el.nodeType === el.ELEMENT_NODE);
//	};

//	var unwrap = function (el) {
//		return isElement(el) || isDocument(el) ? el : el[0];
//	};

//	proto.duScrollTo = function (left, top, duration, easing) {
//		var aliasFn;
//		if (angular.isElement(left)) {
//			aliasFn = this.duScrollToElement;
//		} else if (angular.isDefined(duration)) {
//			aliasFn = this.duScrollToAnimated;
//		}
//		if (aliasFn) {
//			return aliasFn.apply(this, arguments);
//		}
//		var el = unwrap(this);
//		if (isDocument(el)) {
//			return $window.scrollTo(left, top);
//		}
//		el.scrollLeft = left;
//		el.scrollTop = top;
//	};

//	var scrollAnimation, deferred;
//	proto.duScrollToAnimated = function (left, top, duration, easing) {
//		if (duration && !easing) {
//			easing = duScrollEasing;
//		}
//		var startLeft = this.duScrollLeft(),
//			startTop = this.duScrollTop(),
//			deltaLeft = Math.round(left - startLeft),
//			deltaTop = Math.round(top - startTop);

//		var startTime = null, progress = 0;
//		var el = this;

//		var cancelScrollAnimation = function ($event) {
//			if (!$event || (progress && $event.which > 0)) {
//				if (duScrollCancelOnEvents) {
//					el.unbind(duScrollCancelOnEvents, cancelScrollAnimation);
//				}
//				cancelAnimation(scrollAnimation);
//				deferred.reject();
//				scrollAnimation = null;
//			}
//		};

//		if (scrollAnimation) {
//			cancelScrollAnimation();
//		}
//		deferred = $q.defer();

//		if (duration === 0 || (!deltaLeft && !deltaTop)) {
//			if (duration === 0) {
//				el.duScrollTo(left, top);
//			}
//			deferred.resolve();
//			return deferred.promise;
//		}

//		var animationStep = function (timestamp) {
//			if (startTime === null) {
//				startTime = timestamp;
//			}

//			progress = timestamp - startTime;
//			var percent = (progress >= duration ? 1 : easing(progress / duration));

//			el.scrollTo(
//			  startLeft + Math.ceil(deltaLeft * percent),
//			  startTop + Math.ceil(deltaTop * percent)
//			);
//			if (percent < 1) {
//				scrollAnimation = requestAnimation(animationStep);
//			} else {
//				if (duScrollCancelOnEvents) {
//					el.unbind(duScrollCancelOnEvents, cancelScrollAnimation);
//				}
//				scrollAnimation = null;
//				deferred.resolve();
//			}
//		};

//		//Fix random mobile safari bug when scrolling to top by hitting status bar
//		el.duScrollTo(startLeft, startTop);

//		if (duScrollCancelOnEvents) {
//			el.bind(duScrollCancelOnEvents, cancelScrollAnimation);
//		}

//		scrollAnimation = requestAnimation(animationStep);
//		return deferred.promise;
//	};

//	proto.duScrollToElement = function (target, offset, duration, easing) {
//		var el = unwrap(this);
//		if (!angular.isNumber(offset) || isNaN(offset)) {
//			offset = duScrollOffset;
//		}
//		var top = this.duScrollTop() + unwrap(target).getBoundingClientRect().top - offset;
//		if (isElement(el)) {
//			top -= el.getBoundingClientRect().top;
//		}
//		return this.duScrollTo(0, top, duration, easing);
//	};

//	proto.duScrollLeft = function (value, duration, easing) {
//		if (angular.isNumber(value)) {
//			return this.duScrollTo(value, this.duScrollTop(), duration, easing);
//		}
//		var el = unwrap(this);
//		if (isDocument(el)) {
//			return $window.scrollX || document.documentElement.scrollLeft || document.body.scrollLeft;
//		}
//		return el.scrollLeft;
//	};
//	proto.duScrollTop = function (value, duration, easing) {
//		if (angular.isNumber(value)) {
//			return this.duScrollTo(this.duScrollLeft(), value, duration, easing);
//		}
//		var el = unwrap(this);
//		if (isDocument(el)) {
//			return $window.scrollY || document.documentElement.scrollTop || document.body.scrollTop;
//		}
//		return el.scrollTop;
//	};

//	proto.duScrollToElementAnimated = function (target, offset, duration, easing) {
//		return this.duScrollToElement(target, offset, duration || duScrollDuration, easing);
//	};

//	proto.duScrollTopAnimated = function (top, duration, easing) {
//		return this.duScrollTop(top, duration || duScrollDuration, easing);
//	};

//	proto.duScrollLeftAnimated = function (left, duration, easing) {
//		return this.duScrollLeft(left, duration || duScrollDuration, easing);
//	};

//	angular.forEach(proto, function (fn, key) {
//		angular.element.prototype[key] = fn;

//		//Remove prefix if not already claimed by jQuery / ui.utils
//		var unprefixed = key.replace(/^duScroll/, 'scroll');
//		if (angular.isUndefined(angular.element.prototype[unprefixed])) {
//			angular.element.prototype[unprefixed] = fn;
//		}
//	});

//}]);


////Adapted from https://gist.github.com/paulirish/1579671
//angular.module('duScroll.polyfill', [])
//.factory('polyfill', ["$window", function ($window) {
//	'use strict';

//	var vendors = ['webkit', 'moz', 'o', 'ms'];

//	return function (fnName, fallback) {
//		if ($window[fnName]) {
//			return $window[fnName];
//		}
//		var suffix = fnName.substr(0, 1).toUpperCase() + fnName.substr(1);
//		for (var key, i = 0; i < vendors.length; i++) {
//			key = vendors[i] + suffix;
//			if ($window[key]) {
//				return $window[key];
//			}
//		}
//		return fallback;
//	};
//}]);

//angular.module('duScroll.requestAnimation', ['duScroll.polyfill'])
//.factory('requestAnimation', ["polyfill", "$timeout", function (polyfill, $timeout) {
//	'use strict';

//	var lastTime = 0;
//	var fallback = function (callback, element) {
//		var currTime = new Date().getTime();
//		var timeToCall = Math.max(0, 16 - (currTime - lastTime));
//		var id = $timeout(function () { callback(currTime + timeToCall); },
//		  timeToCall);
//		lastTime = currTime + timeToCall;
//		return id;
//	};

//	return polyfill('requestAnimationFrame', fallback);
//}])
//.factory('cancelAnimation', ["polyfill", "$timeout", function (polyfill, $timeout) {
//	'use strict';

//	var fallback = function (promise) {
//		$timeout.cancel(promise);
//	};

//	return polyfill('cancelAnimationFrame', fallback);
//}]);


//angular.module('duScroll.spyAPI', ['duScroll.scrollContainerAPI'])
//.factory('spyAPI', ["$rootScope", "$timeout", "$interval", "$window", "$document", "scrollContainerAPI", "duScrollGreedy", "duScrollSpyWait", "duScrollSpyRefreshInterval", "duScrollBottomSpy", "duScrollActiveClass", function ($rootScope, $timeout, $interval, $window, $document, scrollContainerAPI, duScrollGreedy, duScrollSpyWait, duScrollSpyRefreshInterval, duScrollBottomSpy, duScrollActiveClass) {
//	'use strict';

//	var createScrollHandler = function (context) {
//		var timer = false, queued = false;
//		var handler = function () {
//			queued = false;
//			var container = context.container,
//				containerEl = container[0],
//				containerOffset = 0,
//				bottomReached;

//			if (typeof HTMLElement !== 'undefined' && containerEl instanceof HTMLElement || containerEl.nodeType && containerEl.nodeType === containerEl.ELEMENT_NODE) {
//				containerOffset = containerEl.getBoundingClientRect().top;
//				bottomReached = Math.round(containerEl.scrollTop + containerEl.clientHeight) >= containerEl.scrollHeight;
//			} else {
//				var documentScrollHeight = $document[0].body.scrollHeight || $document[0].documentElement.scrollHeight; // documentElement for IE11
//				bottomReached = Math.round($window.pageYOffset + $window.innerHeight) >= documentScrollHeight;
//			}
//			var compareProperty = (duScrollBottomSpy && bottomReached ? 'bottom' : 'top');

//			var i, currentlyActive, toBeActive, spies, spy, pos;
//			spies = context.spies;
//			currentlyActive = context.currentlyActive;
//			toBeActive = undefined;

//			for (i = 0; i < spies.length; i++) {
//				spy = spies[i];
//				pos = spy.getTargetPosition();
//				if (!pos || !spy.$element) continue;

//				if ((duScrollBottomSpy && bottomReached) || (pos.top + spy.offset - containerOffset < 20 && (duScrollGreedy || pos.top * -1 + containerOffset) < pos.height)) {
//					//Find the one closest the viewport top or the page bottom if it's reached
//					if (!toBeActive || toBeActive[compareProperty] < pos[compareProperty]) {
//						toBeActive = {
//							spy: spy
//						};
//						toBeActive[compareProperty] = pos[compareProperty];
//					}
//				}
//			}

//			if (toBeActive) {
//				toBeActive = toBeActive.spy;
//			}
//			if (currentlyActive === toBeActive || (duScrollGreedy && !toBeActive)) return;
//			if (currentlyActive && currentlyActive.$element) {
//				currentlyActive.$element.removeClass(duScrollActiveClass);
//				$rootScope.$broadcast(
//				  'duScrollspy:becameInactive',
//				  currentlyActive.$element,
//				  angular.element(currentlyActive.getTargetElement())
//				);
//			}
//			if (toBeActive) {
//				toBeActive.$element.addClass(duScrollActiveClass);
//				$rootScope.$broadcast(
//				  'duScrollspy:becameActive',
//				  toBeActive.$element,
//				  angular.element(toBeActive.getTargetElement())
//				);
//			}
//			context.currentlyActive = toBeActive;
//		};

//		if (!duScrollSpyWait) {
//			return handler;
//		}

//		//Debounce for potential performance savings
//		return function () {
//			if (!timer) {
//				handler();
//				timer = $timeout(function () {
//					timer = false;
//					if (queued) {
//						handler();
//					}
//				}, duScrollSpyWait, false);
//			} else {
//				queued = true;
//			}
//		};
//	};

//	var contexts = {};

//	var createContext = function ($scope) {
//		var id = $scope.$id;
//		var context = {
//			spies: []
//		};

//		context.handler = createScrollHandler(context);
//		contexts[id] = context;

//		$scope.$on('$destroy', function () {
//			destroyContext($scope);
//		});

//		return id;
//	};

//	var destroyContext = function ($scope) {
//		var id = $scope.$id;
//		var context = contexts[id], container = context.container;
//		if (context.intervalPromise) {
//			$interval.cancel(context.intervalPromise);
//		}
//		if (container) {
//			container.off('scroll', context.handler);
//		}
//		delete contexts[id];
//	};

//	var defaultContextId = createContext($rootScope);

//	var getContextForScope = function (scope) {
//		if (contexts[scope.$id]) {
//			return contexts[scope.$id];
//		}
//		if (scope.$parent) {
//			return getContextForScope(scope.$parent);
//		}
//		return contexts[defaultContextId];
//	};

//	var getContextForSpy = function (spy) {
//		var context, contextId, scope = spy.$scope;
//		if (scope) {
//			return getContextForScope(scope);
//		}
//		//No scope, most likely destroyed
//		for (contextId in contexts) {
//			context = contexts[contextId];
//			if (context.spies.indexOf(spy) !== -1) {
//				return context;
//			}
//		}
//	};

//	var isElementInDocument = function (element) {
//		while (element.parentNode) {
//			element = element.parentNode;
//			if (element === document) {
//				return true;
//			}
//		}
//		return false;
//	};

//	var addSpy = function (spy) {
//		var context = getContextForSpy(spy);
//		if (!context) return;
//		context.spies.push(spy);
//		if (!context.container || !isElementInDocument(context.container)) {
//			if (context.container) {
//				context.container.off('scroll', context.handler);
//			}
//			context.container = scrollContainerAPI.getContainer(spy.$scope);
//			if (duScrollSpyRefreshInterval && !context.intervalPromise) {
//				context.intervalPromise = $interval(context.handler, duScrollSpyRefreshInterval, 0, false);
//			}
//			context.container.on('scroll', context.handler).triggerHandler('scroll');
//		}
//	};

//	var removeSpy = function (spy) {
//		var context = getContextForSpy(spy);
//		if (spy === context.currentlyActive) {
//			$rootScope.$broadcast('duScrollspy:becameInactive', context.currentlyActive.$element);
//			context.currentlyActive = null;
//		}
//		var i = context.spies.indexOf(spy);
//		if (i !== -1) {
//			context.spies.splice(i, 1);
//		}
//		spy.$element = null;
//	};

//	return {
//		addSpy: addSpy,
//		removeSpy: removeSpy,
//		createContext: createContext,
//		destroyContext: destroyContext,
//		getContextForScope: getContextForScope
//	};
//}]);


//angular.module('duScroll.scrollContainerAPI', [])
//.factory('scrollContainerAPI', ["$document", function ($document) {
//	'use strict';

//	var containers = {};

//	var setContainer = function (scope, element) {
//		var id = scope.$id;
//		containers[id] = element;
//		return id;
//	};

//	var getContainerId = function (scope) {
//		if (containers[scope.$id]) {
//			return scope.$id;
//		}
//		if (scope.$parent) {
//			return getContainerId(scope.$parent);
//		}
//		return;
//	};

//	var getContainer = function (scope) {
//		var id = getContainerId(scope);
//		return id ? containers[id] : $document;
//	};

//	var removeContainer = function (scope) {
//		var id = getContainerId(scope);
//		if (id) {
//			delete containers[id];
//		}
//	};

//	return {
//		getContainerId: getContainerId,
//		getContainer: getContainer,
//		setContainer: setContainer,
//		removeContainer: removeContainer
//	};
//}]);


//angular.module('duScroll.smoothScroll', ['duScroll.scrollHelpers', 'duScroll.scrollContainerAPI'])
//.directive('duSmoothScroll', ["duScrollDuration", "duScrollOffset", "scrollContainerAPI", function (duScrollDuration, duScrollOffset, scrollContainerAPI) {
//	'use strict';

//	return {
//		link: function ($scope, $element, $attr) {
//			$element.on('click', function (e) {
//				if ((!$attr.href || $attr.href.indexOf('#') === -1) && $attr.duSmoothScroll === '') return;

//				var id = $attr.href ? $attr.href.replace(/.*(?=#[^\s]+$)/, '').substring(1) : $attr.duSmoothScroll;

//				var target = document.getElementById(id) || document.getElementsByName(id)[0];
//				if (!target || !target.getBoundingClientRect) return;

//				if (e.stopPropagation) e.stopPropagation();
//				if (e.preventDefault) e.preventDefault();

//				var offset = $attr.offset ? parseInt($attr.offset, 10) : duScrollOffset;
//				var duration = $attr.duration ? parseInt($attr.duration, 10) : duScrollDuration;
//				var container = scrollContainerAPI.getContainer($scope);

//				container.duScrollToElement(
//				  angular.element(target),
//				  isNaN(offset) ? 0 : offset,
//				  isNaN(duration) ? 0 : duration
//				);
//			});
//		}
//	};
//}]);


//angular.module('duScroll.spyContext', ['duScroll.spyAPI'])
//.directive('duSpyContext', ["spyAPI", function (spyAPI) {
//	'use strict';

//	return {
//		restrict: 'A',
//		scope: true,
//		compile: function compile(tElement, tAttrs, transclude) {
//			return {
//				pre: function preLink($scope, iElement, iAttrs, controller) {
//					spyAPI.createContext($scope);
//				}
//			};
//		}
//	};
//}]);


//angular.module('duScroll.scrollContainer', ['duScroll.scrollContainerAPI'])
//.directive('duScrollContainer', ["scrollContainerAPI", function (scrollContainerAPI) {
//	'use strict';

//	return {
//		restrict: 'A',
//		scope: true,
//		compile: function compile(tElement, tAttrs, transclude) {
//			return {
//				pre: function preLink($scope, iElement, iAttrs, controller) {
//					iAttrs.$observe('duScrollContainer', function (element) {
//						if (angular.isString(element)) {
//							element = document.getElementById(element);
//						}

//						element = (angular.isElement(element) ? angular.element(element) : iElement);
//						scrollContainerAPI.setContainer($scope, element);
//						$scope.$on('$destroy', function () {
//							scrollContainerAPI.removeContainer($scope);
//						});
//					});
//				}
//			};
//		}
//	};
//}]);


//angular.module('duScroll.scrollspy', ['duScroll.spyAPI'])
//.directive('duScrollspy', ["spyAPI", "duScrollOffset", "$timeout", "$rootScope", function (spyAPI, duScrollOffset, $timeout, $rootScope) {
//	'use strict';

//	var Spy = function (targetElementOrId, $scope, $element, offset) {
//		if (angular.isElement(targetElementOrId)) {
//			this.target = targetElementOrId;
//		} else if (angular.isString(targetElementOrId)) {
//			this.targetId = targetElementOrId;
//		}
//		this.$scope = $scope;
//		this.$element = $element;
//		this.offset = offset;
//	};

//	Spy.prototype.getTargetElement = function () {
//		if (!this.target && this.targetId) {
//			this.target = document.getElementById(this.targetId) || document.getElementsByName(this.targetId)[0];
//		}
//		return this.target;
//	};

//	Spy.prototype.getTargetPosition = function () {
//		var target = this.getTargetElement();
//		if (target) {
//			return target.getBoundingClientRect();
//		}
//	};

//	Spy.prototype.flushTargetCache = function () {
//		if (this.targetId) {
//			this.target = undefined;
//		}
//	};

//	return {
//		link: function ($scope, $element, $attr) {
//			var href = $attr.ngHref || $attr.href;
//			var targetId;

//			if (href && href.indexOf('#') !== -1) {
//				targetId = href.replace(/.*(?=#[^\s]+$)/, '').substring(1);
//			} else if ($attr.duScrollspy) {
//				targetId = $attr.duScrollspy;
//			} else if ($attr.duSmoothScroll) {
//				targetId = $attr.duSmoothScroll;
//			}
//			if (!targetId) return;

//			// Run this in the next execution loop so that the scroll context has a chance
//			// to initialize
//			var timeoutPromise = $timeout(function () {
//				var spy = new Spy(targetId, $scope, $element, -($attr.offset ? parseInt($attr.offset, 10) : duScrollOffset));
//				spyAPI.addSpy(spy);

//				$scope.$on('$locationChangeSuccess', spy.flushTargetCache.bind(spy));
//				var deregisterOnStateChange = $rootScope.$on('$stateChangeSuccess', spy.flushTargetCache.bind(spy));
//				$scope.$on('$destroy', function () {
//					spyAPI.removeSpy(spy);
//					deregisterOnStateChange();
//				});
//			}, 0, false);
//			$scope.$on('$destroy', function () { $timeout.cancel(timeoutPromise); });
//		}
//	};
//}]);