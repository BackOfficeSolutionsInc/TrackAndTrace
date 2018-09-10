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

	//this.toUtc = function (date) {
	//	new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), date.getHours(), date.getMinutes(), date.getSeconds()));
	//}

	this.serverDateFormat =function serverDateFormat(edate) {
		if (edate == false)
			return "";
		var _d = edate.getDate(),
		_m = edate.getMonth() + 1,
		_mm = edate.getMinutes(),
		_h = edate.getHours(),
		_s = edate.getSeconds(),
		d = _d > 9 ? _d : '0' + _d,
		m = _m > 9 ? _m : '0' + _m,
		h = _h > 9 ? _h : '0' + _h,
		mm = _mm > 9 ? _mm : '0' + _mm,
		s = _s > 9 ? _s : '0' + _s;
		return m + '-' + d + '-' + (edate.getFullYear()) + " " + h + ":" + mm + ":" + s;
	}


	this.adjustToMidnight = function (date) {
		var d = new Date(date);
		//Adj for daylight savings time screws up the UI when outside DST (3-10-2018), selecting in DST (4-10-2018)
		//var isDst = this.dst(d);
		//debugger;
		//d.setHours( isDst? 22 : 23, 59, 59, 999);
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
	this.shouldLocalize = function (date) {
		var type = typeof (date);
		if (type === "object") {
			if (typeof (date.Local) === "boolean") {
				return date.Local;
			}
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

	//Formats:
	//dd-MM-yyyy	= 12-31-1999
	//HH:mm:ss		= 23:59:59
	//hh:mm:sst		= 12:59:59p
	//hh:mm:sstt	= 12:59:59pm
	this.formatDate = function (date, format) {
		return window.getFormattedDate(date, format);
	};


	this.createClientDatepicker = function (options) {

		var selector = options.selector;
		var serverTime = options.serverTime;
		var displayAsLocal = options.displayAsLocal || true;
		var name = options.name;
		var id = options.id;
		var datePickerOptions = options.datePickerOptions ;
		var endOfDay = options.endOfDay || true;

		if (typeof (selector) === "undefined") {
			console.error("Client Datepicker cannot have an undefined selector");
			return;
		}


		if (typeof (name) === "undefined" || name==null)
			name = "Date";
		if (typeof (id) === "undefined" || id ==null)
			id = name;

		//How should the date be displayed?
		var displayTime = serverTime;
		if (displayAsLocal) {
			displayTime = Time.toLocalTime(serverTime);
		}
		
		//Create Html
		var guid = generateGuid();
		var builder = '';
		builder += '<div class="input-append date ' + guid + '">';
		builder += '<input class="form-control client-date" type="text" value="' + clientDateFormat(displayTime) + '" placeholder="not set" data-val="true" data-val-date="The field must be a date.">';
		builder +=	 '<span class="add-on"><i class="icon-th"></i></span>';
		builder +=	 '<input type="hidden" class="server-date" id="' + id + '" name="' + name + '" value="' + Time.serverDateFormat(serverTime) + '" />';
		builder += '</div>';
		var dp = $(builder);
		$(selector).append(dp);
		//Datepicker Options
		var dpOptions = { format: window.dateFormat.toLowerCase() };

		//Copy options
		if (datePickerOptions) {
			for (var k in datePickerOptions) {
				if (arrayHasOwnIndex(datePickerOptions, k)) {
					dpOptions[k] = datePickerOptions[k];
				}
			}
		}
		//setup events
		var eventHolder = $(dp).find("[name='" + name + "']");
		$('.' + guid + ' .client-date').on("change", function () {
			var v = $(this).val();
			if (v == "") {
				$('.' + guid + ' .server-date').val("");
			}
		});


		$('.' + guid + ' .client-date').datepickerX(dpOptions).on('changeDate', function (e) {
			var date = e.date;
			if (endOfDay) {
				date = Time.adjustToMidnight(date);
			}
			var displayDate = date;//Should not change ever.. we selected this, it's what we want to see
			
			var serverDate = date;
			if (displayAsLocal) {
				/*===Display local time===*/
				//Need to convert the selected time (which is local) into server time
				serverDate = Time.toServerTime(date);
			} else {
				/*===Display server time===*/
				//We'll send this data back as it was selected...
				serverDate = serverDate;
			}

			var formattedServerDate = Time.serverDateFormat(serverDate);
			
			$('.' + guid + ' .server-date').val(formattedServerDate);

			$(eventHolder).trigger("change", [{
				clientDate: displayDate,
				serverDate: formattedServerDate,
				containerElement: $(selector),
				clientDateElement: $(this),
				serverDateElement: $(eventHolder)
			}]);
			$(this).datepickerX("hide");
		}).on("hide", function () {
			$(eventHolder).trigger("close");
		});
		return eventHolder;
	}
}