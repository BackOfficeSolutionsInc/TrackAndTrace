﻿var Time = new function () {
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

	//this.toUtc = function (date) {
	//	new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), date.getHours(), date.getMinutes(), date.getSeconds()));
	//}

	this.adjustToMidnight = function (date) {
		var d = new Date(date);
		//Adj for daylight savings time screws up the UI when outside DST (3-10-2018), selecting in DST (4-10-2018)
		//d.setHours(this.dst(d) ? 24 : 23, 59, 59, 999);
		d.setHours(23, 59, 59, 999);
		return d;
	}

	this.eachDate = function (obj,func) {
		function _each(obj, seen) {
			if (typeof (obj) === "undefined" || obj == null)
				return obj;
			for (var key in obj) {
				if (arrayHasOwnIndex(obj, key)) {
					var value = obj[key];
					var type = typeof (value);
					if (obj[key] == null) {
						//Do nothing
					} else {
						var parsed = parseJsonDate(obj[key], false);
						if (parsed.getDate !== undefined) {
							var res = func(parsed,obj,key);
							if (typeof(res)!=="undefined" && res!=false && res!=null) {
								obj[key] = res;//new Date(obj[key].getTime() + tzoffset() * 60 * 1000);
							}
						} else if (type == 'object') {
							_continueIfUnseen(value, seen, function () {
								_each(value, seen);
							});
						}
					}
				}
			}
		}

		function _continueIfUnseen(item, seen, onUnseen) {
			if (item) {
				if (item.Key) {
					if (!(item.Key in seen)) {
						seen[item.Key] = true;
						onUnseen();
					} else {
					}
				} else {
					onUnseen();
				}
			} else {
				onUnseen();
			}
		}
		_each(obj, []);
		return obj;
	};

	this.stdTimezoneOffset = function (date) {
		var jan = new Date(date.getFullYear(), 0, 1);
		var jul = new Date(date.getFullYear(), 6, 1);
		return Math.max(jan.getTimezoneOffset(), jul.getTimezoneOffset());
	}

	this.dst = function (date) {
		return date.getTimezoneOffset() < this.stdTimezoneOffset(date);
	}

	var convertDateFromString = function (value) {
		var type = typeof (value);
		var dateRegex1 = /\/Date\([+-]?\d{13,14}\)\//;
		var dateRegex2 = /\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{0,7})?/;
		var dateRegex3 = /^(\d{2})-(\d{2})-(\d{4}) (\d{2}):(\d{2}):(\d{2})$/;

		if (type == 'string' && dateRegex1.test(value)) {
			return new Date(parseInt(value.substr(6)));
		} else if (type == 'string' && dateRegex2.test(value)) {
			var v = value;
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
			if (d !== false) {
				return Time.toLocalTime(d);
			}
			//number string handled below.
		}

		if (type === "object") {
			if (typeof (value.Local) === "boolean" && (typeof (value.Date) === "string" || value.Date.getDate !== undefined)) {
				var d = value.Date;
				if (typeof (value.Date) === "string") {
					d = convertDateFromString(value.Date);
				}
				if (value.Local === false && d !== false) {
					d = Time.toLocalTime(d);
				}
				if (d !== false) {
					return d;
				}
			}
			if (value.getDate !== undefined) {
				//console.warn("timezone not applied");
				return new Date(value.getTime());
			}
		}

		if (allowNumbers == true && type === "string" && !isNaN(value)) {
			var d = new Date(+value);
			return Time.toLocalTime(d);
		}
		return false;
	}

	this.getWeekSinceEpoch = function (day) {
		var oneDay = 24 * 60 * 60 * 1000;
		var span = day.startOfWeek(0);
		return Math.floor((span.getTime() / oneDay) / 7);
	}

	this.getDateSinceEpoch = function (week) {
		var day = 24 * 60 * 60 * 1000 * 7 * week;
		return new Date(day);
	};

	this.formatDate=function(date){
		return window.getFormattedDate(date);
	};
}