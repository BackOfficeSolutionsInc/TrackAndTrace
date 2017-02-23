//Version #
function msieversion() {
	var ua = window.navigator.userAgent;
	var msie = ua.indexOf('MSIE ');
	if (msie > 0) {
		// IE 10 or older => return version number
		return parseInt(ua.substring(msie + 5, ua.indexOf('.', msie)), 10);
	}
	var trident = ua.indexOf('Trident/');
	if (trident > 0) {
		// IE 11 => return version number
		var rv = ua.indexOf('rv:');
		return parseInt(ua.substring(rv + 3, ua.indexOf('.', rv)), 10);
	}
	var edge = ua.indexOf('Edge/');
	if (edge > 0) {
		// Edge (IE 12+) => return version number
		return parseInt(ua.substring(edge + 5, ua.indexOf('.', edge)), 10);
	}

	// other browser
	return false;
}

var awaitJquery = {
	exec: function (func) {
		if (typeof ($) !== "undefined")
			func();
		else
			awaitJquery.functions.push(func);
	},
	functions: [],
	timeout: false,
	go: function () {
		if (typeof ($) !== "undefined") {
			clearTimeout(awaitJquery.timeout);
			for (var f in awaitJquery.functions) {
				awaitJquery.functions[f]();
			}
			awaitJquery.functions = [];
		} else {
			var nextGo = awaitJquery.go;
			awaitJquery.timeout = setTimeout(function () {
				nextGo();
			}, 30);
		}
	}
};
awaitJquery.go();


//IE console bug (keep first)
(function () {
	var method;
	var noop = function () { };
	var methods = [
		'assert', 'clear', 'count', 'debug', 'dir', 'dirxml', 'error',
		'exception', 'group', 'groupCollapsed', 'groupEnd', 'info', 'log',
		'markTimeline', 'profile', 'profileEnd', 'table', 'time', 'timeEnd',
		'timeStamp', 'trace', 'warn'
	];
	var length = methods.length;
	var console = (window.console = window.console || {});

	while (length--) {
		method = methods[length];

		// Only stub undefined methods.
		if (!console[method]) {
			console[method] = noop;
		}
	}
}());

////Fixed bar scrolling
//if (msieversion() != false) { // if IE
//	awaitJquery.exec(function () {
//		try {
//			$('body').on("mousewheel", function (event) {
//				// remove default behavior
//				event.preventDefault();

//				//scroll without smoothing
//				var wheelDelta = event.originalEvent.wheelDelta;
//				var currentScrollPosition = window.pageYOffset;
//				window.scrollTo(0, currentScrollPosition - wheelDelta);
//			});
//		} catch (e) {
//			alert(e);
//		}
//	});
//}


///Capture stack polyfill
Error.captureStackTrace = Error.captureStackTrace || function (obj) {
	if (Error.prepareStackTrace) {
		var frame = {
			isEval: function () { return false; },
			getFileName: function () { return "filename"; },
			getLineNumber: function () { return 1; },
			getColumnNumber: function () { return 1; },
			getFunctionName: function () { return "functionName" }
		};

		obj.stack = Error.prepareStackTrace(obj, [frame, frame, frame]);
	} else {
		obj.stack = obj.stack || obj.name || "Error";
	}
};
