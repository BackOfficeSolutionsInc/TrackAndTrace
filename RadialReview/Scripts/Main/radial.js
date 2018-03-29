//hasOwnIndex
function arrayHasOwnIndex(array, prop) {
	return array.hasOwnProperty(prop);// && /^0$|^[1-9]\d*$/.test(prop) && prop <= 4294967294; // 2^32 - 2
}


Constants = {
	StartHubSettings: { transport: ['webSockets', 'longPolling'] }
};

function showDebug(show) {
	$("body").toggleClass("show-debug", show);
}

function UrlEncodingFix(str) {
	str = replaceAll("%26%2339%3B", "%27", str);
	str = replaceAll("%26%2334%3B", "%22", str);
	str = replaceAll("%26%2313%3B", "%0A", str);
	str = replaceAll("%26%2310%3B", "%0D", str);
	return str;
}

function escapeString(str) {
	if (typeof (str) !== "string")
		return str;
	str = str.replace(/"/g, "&quot;");
	str = str.replace(/'/g, "&#39;");
	return str;
}

function generateGuid() {
	var result, i, j;
	result = '';
	for (j = 0; j < 32; j++) {
		if (j == 8 || j == 12 || j == 16 || j == 20)
			result = result + '-';
		i = Math.floor(Math.random() * 16).toString(16).toUpperCase();
		result = result + i;
	}
	return result;
}


function metGoal(direction, goal, measured, alternate) {

	if (!$.trim(measured)) {
		return undefined;
	} else if ($.isNumeric(measured)) {
		var m = +((measured + "").replace(/,/gi, "."));
		var g = +((goal + "").replace(/,/gi, "."));
		if (direction == "GreaterThan" || direction == 1) {
			return m >= g;
		} else if (direction == "LessThan" || direction == -1) {
			return m < g;
		} else if (direction == "LessThanOrEqual" || direction == -2) {
			return m <= g;
		} else if (direction == "GreaterThanNotEqual" || direction == 2) {
			return m > g;
		} else if (direction == "EqualTo" || direction == 0) {
			return m == g;
		} else if (direction == "Between" || direction == -3) {
			var ag = +((alternate + "").replace(/,/gi, "."));
			return g <= m && m <= ag;
		} else {
			console.log("Error: goal met could not be calculated. Unhandled direction: " + direction);
			return undefined;
		}
	} else {
		return undefined;
	}
}


function imageListFormat(state) {
	if (!state.id) {
		return state.text;
	}
	var $state = $('<span><img style="max-width:32;max-height:32px"  src="' + $(state.element).data("img") + '" class="img-flag" /> ' + state.text + '</span>');
	return $state;
};

jQuery.cachedScript = function (url, options) {
	options = $.extend(options || {}, {
		dataType: "script",
		cache: true,
		url: url
	});
	return jQuery.ajax(options);
};


/**
 * @brief Wait for something to be ready before triggering a timeout
 * @param {callback} isready Function which returns true when the thing we're waiting for has happened
 * @param {callback} success Function to call when the thing is ready
 * @param {callback} error Function to call if we time out before the event becomes ready
 * @param {int} count Number of times to retry the timeout (default 300 or 6s)
 * @param {int} interval Number of milliseconds to wait between attempts (default 20ms)
// */
function waitUntil(isready, success, error, count, interval) {
	if (count === undefined) {
		count = 300;
	}
	if (interval === undefined) {
		interval = 20;
	}
	if (isready()) {
		success();
		return;
	}
	// The call back isn't ready. We need to wait for it
	setTimeout(function () {
		if (!count) {
			// We have run out of retries
			if (error !== undefined) {
				error();
			}
		} else {
			// Try again
			waitUntil(isready, success, error, count - 1, interval);
		}
	}, interval);
}

function waitUntilVisible(selector, onVisible, duration) {
	if (typeof (duration) === "undefined")
		duration = 60 * 50;

	var interval = 50;
	var count = Math.max(duration / interval, 1);

	waitUntil(function () {
		return $(selector).is(":visible");
	}, onVisible, function () { }, count, interval);
}

function isIOS() {
	var iOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;
	return iOS;
}

if (isIOS()) {

	function setTextareaPointerEvents(value) {
		var nodes = document.getElementsByClassName('scrollOver');
		for (var i = 0; i < nodes.length; i++) {
			nodes[i].style.pointerEvents = value;
		}
	}

	document.addEventListener('DOMContentLoaded', function () {
		setTextareaPointerEvents('none');
	});

	document.addEventListener('touchstart', function () {
		setTextareaPointerEvents('auto');
	});

	document.addEventListener('touchmove', function () {
		e.preventDefault();
		setTextareaPointerEvents('none');
	});

	document.addEventListener('touchend', function () {
		setTimeout(function () {
			setTextareaPointerEvents('none');
		}, 0);
	});

	var b = document.getElementsByTagName('body')[0];
	b.className += ' is-ios';
}

function isSafari() {
	var ua = navigator.userAgent.toLowerCase();
	if (ua.indexOf('safari') != -1) {
		if (ua.indexOf('chrome') > -1) {
		} else {
			return true;
		}
	}
	return false;
}


function setFormula(measurableId) {
	var id = measurableId;
	showModal("Edit formula", "/scorecard/formulapartial/" + id, "/scorecard/setformula?id=" + id, null, function () {
		showAlert("Updating formula...");
	}, function (d) {
		clearAlerts();
		showAlert("Formula updated!");
	});
}