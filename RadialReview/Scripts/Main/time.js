var Time = new function () {
	var tzoffset = function () {
		if (!window.tzoffset) {
			//var jan = new Date(new Date().getYear() + 1900, 0, 1, 2, 0, 0), jul = new Date(new Date().getYear() + 1900, 6, 1, 2, 0, 0);
			//window.tzoffset = (jan.getTime() % 24 * 60 * 60 * 1000) >
			//			 (jul.getTime() % 24 * 60 * 60 * 1000)
			//			 ? jan.getTimezoneOffset() : jul.getTimezoneOffset();
			window.tzoffset = new Date().getTimezoneOffset();
		}
		return window.tzoffset;
	}

	this.tzoffset = tzoffset;

	this.addTimestamp = function (url) {
		tzoffset();
		var date = (+new Date());// - (window.tzoffset * 60 * 1000));
		return url + ((url.indexOf("?") != -1) ? "&_clientTimestamp=" + date : "?_clientTimestamp=" + date) + "&_tz=" + (-window.tzoffset);
	}

	this.toLocalTime = function (date) {
		return new Date(date.getTime() - date.getTimezoneOffset() * 60 * 1000);
	}

	this.toServerTime = function (date) {
		return new Date(date.getTime() + date.getTimezoneOffset() * 60 * 1000);
	}

	var convertDateFromString = function (value) {
		var type = typeof (value);
		var dateRegex1 = /\/Date\([+-]?\d{13,14}\)\//;
		var dateRegex2 = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{0,7})?/;
		var dateRegex3 = /^(\d{2})-(\d{2})-(\d{4}) (\d{2}):(\d{2}):(\d{2})$/;
		
		if (type == 'string' && dateRegex1.test(value)) {
			return new Date(parseInt(value.substr(6)));
		} else if (type == 'string' && dateRegex2.test(value)) {
			var v=value;
			if (v.indexOf("Z", v.length - "Z".length) !== -1)
				v = v.slice(0, -1);
			return new Date(v);
		} else if (type == 'string' && dateRegex3.test(value)) {
			return new Date(value);
		}
		return false;

	}
	/**
	 * parse json from the server.
	 * @constructor
	 */
	this.parseJsonDate = function (value, allowNumbers) {
		var type = typeof (value);
		var dateRegex1 = /\/Date\([+-]?\d{13,14}\)\//;
		var dateRegex2 = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{0,7})?/;
		var dateRegex3 = /^(\d{2})-(\d{2})-(\d{4}) (\d{2}):(\d{2}):(\d{2})$/;
		if (type === "undefined" || value === null)
			return false;
		if (allowNumbers == true && type === "number") {
			Time.toLocalTime(new Date(value));
			return new Date(value - new Date().getTimezoneOffset() * 60000);
		}
		if (type === "string") {
			var d = convertDateFromString(value);
			if (d!==false) {
				return Time.toLocalTime(d);
			}
			//number string handled below.
		}

		if (type === "object") {
			if (typeof(value.Local) === "boolean" && (typeof (value.Date) === "string" || value.Date.getDate !== undefined)) {
				var d = value.Date;
				if (typeof (value.Date) === "string") {
					d = convertDateFromString(value.Date);
				}
				if (value.Local === false && d!==false) {
					d = Time.toLocalTime(d);
				}
				if (d !== false) {
					return d;
				}				
			}
			if (value.getDate !== undefined) {
				console.warn("timezone not applied");
				return new Date(value.getTime());
			}
		}

		if (allowNumbers == true && type === "string" && !isNaN(value)) {
			var d = new Date(+value);
			return Time.toLocalTime(d);
		}

		return false;

		//if (type == 'string' && dateRegex1.test(value)) {
		//	var d = new Date(parseInt(value.substr(6)));
		//	return Time.toLocalTime(d);
		//	//return new Date(new Date(parseInt(value.substr(6))).getTime() - new Date().getTimezoneOffset() * 60000)
		//} else if (type == 'string' && dateRegex2.test(value)) {
		//	var d = new Date(value);
		//	return Time.toLocalTime(d);
		//	//return  new Date(new Date(value).getTime() - new Date().getTimezoneOffset() * 60000);
		//} else if (type == 'string' && dateRegex3.test(value)) {
		//	var d = new Date(value);
		//	return Time.toLocalTime(d);
		//	//return new Date(new Date(value).getTime() - new Date().getTimezoneOffset() * 60000);
		//} else if (type == "object") {
		//	//It's local, We dont want to convert it.
		//	if (value.local === true && typeof (value.date) === "string") {
		//		if (dateRegex1.test(value)) {
		//			var d = new Date(parseInt(value.substr(6)));
		//			return d;
		//		} else {
		//		}
		//	}
		//}
		//else if (value.getDate !== undefined) {
		//	console.warn("timezone not applied");
		//	return new Date(value.getTime());
		//}

	}

	this.getWeekSinceEpoch = function (day) {
		var oneDay = 24 * 60 * 60 * 1000;
		var span = day.startOfWeek(0);
		return Math.floor((span.getTime() / oneDay) / 7);
	}

}