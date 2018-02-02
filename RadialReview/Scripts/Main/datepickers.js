
//Date stuff
function parseJsonDate(value, allowNumbers) {
	console.warn("obsolete. use Time.parseJsonDate(...) instead");
	return Time.parseJsonDate(value, allowNumbers);
}
function getWeekSinceEpoch(day) {
    console.warn("obsolete. use Time.getWeekSinceEpoch(...) instead");
    return Time.getWeekSinceEpoch(day);
}

function dateFormatter(date) {
	return [date.getMonth() + 1, date.getDate(), date.getFullYear()].join('/');
}

function clientDateFormat(date) {
	//console.info("using clientDateFormat");
	if (date == false)
		return "";
	var _d = date.getDate(),
		dd = _d > 9 ? _d : '0' + _d,
		_m = date.getMonth() + 1,
		mm = _m > 9 ? _m : '0' + _m,
		yyyy = date.getFullYear();
	return window.dateFormat
	   .replace(/mm/gi, mm).replace(/m/gi, _m)
	   .replace(/dd/gi, dd).replace(/d/gi, _d)
	   .replace(/yyyy/gi, yyyy).replace(/yy/gi, (yyyy - 2000));
}

function serverDateFormat(edate) {
	//console.info("using serverDateFormat");
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

function createDatepicker(selector, date, name, id, options) {
	//debugger;
	if (typeof (date) === "undefined") {
		date = new Date();
	} else if (date == false) {
		//do nothing.
	} else {
		var pdate = Time.parseJsonDate(date, true);
		if (!pdate) {
			console.error("Could not determine format from date: " + date);
			var err = $("<span style='color:rgba(255,0,0,.5)' >DateErr</span>");
			$(selector).html(err);
			return err;
		}
		date = pdate;
	}

	if (typeof (name) === "undefined")
		name = "Date";
	if (typeof (id) === "undefined")
		id = name;

	var setClasses = function (obj, d) {
	    //debugger;
	    var o = $(obj);
        if (!o.is(".client-date"))
	        o = $(obj).find(".client-date");
	    $(o).removeClass("past-date");
	    $(o).removeClass("future-date");
	    if (d < new Date())
	        $(o).addClass("past-date");
	    else
	        $(o).addClass("future-date");
	}


	var formatted = serverDateFormat(date);
	var _userFormat = clientDateFormat(date);


	var guid = generateGuid();
	var builder = '<div class="input-append date ' + guid + '">';
	builder += '<input class="form-control client-date" data-val="true"' +
               ' data-val-date="The field Model must be a date." type="text" ' +
               'value="' + _userFormat + '" placeholder="not set">';
	builder += '<span class="add-on"><i class="icon-th"></i></span>';
	builder += '<input type="hidden" class="server-date" id="' + id + '" name="' + name + '" value="' + formatted + '" />';
	builder += '</div>';
	var dp = $(builder);
	setClasses(dp, date);
	$(selector).append(dp);
	var dpOptions = {
		format: window.dateFormat.toLowerCase(),
	};
	if (options) {
		for (var k in options) {
			if (arrayHasOwnIndex(options, k)) {
				dpOptions[k] = options[k];
			}
		}
	}
	//var _offsetMin = offsetMinutes;
	var eventHolder = $("[name='" + name + "']");

	$('.' + guid + ' .client-date').on("change", function () {
		var v = $(this).val();
		if (v == "") {
			$('.' + guid + ' .server-date').val("");
		}
	});



	$('.' + guid + ' .client-date').datepickerX(dpOptions).on('changeDate', function (e) {
		var edate = Time.toServerTime(e.date);// new Date(e.date.getTime() + _offsetMin * 60000);
		var formatted = serverDateFormat(edate);
		$('.' + guid + ' .server-date').val(formatted);


		setClasses(this, e.date);

		$(eventHolder).trigger("change", [{
			clientDate: edate,
			serverDate: formatted,
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

function generateDatepickerLocalize(selector, date, name, id, options) {
	console.warn("obsolete: use createDatepicker");
	if (typeof (date) === "undefined") {
		date = new Date();
	} else if (date == false) {

	} else {
		var pdate = Time.parseJsonDate(date, true);
		if (!pdate) {
			console.error("Could not determine format from date: " + date);
			var err = $("<span style='color:rgba(255,0,0,.5)' >DateErr</span>");
			$(selector).html(err);
			return err;
		}
		date = pdate;
	}
	var offset;
	if (date == false)
		offset = new Date().getTimezoneOffset();
	else
		offset = date.getTimezoneOffset();
	var newDate = date;
	if (date != false)
		newDate = new Date(date.getTime() + offset * 60000);
	return generateDatepicker(selector, newDate, name, id, options, offset);
}

function generateDatepicker(selector, date, name, id, options, offsetMinutes) {
	console.warn("obsolete: use createDatepicker");
	if (typeof (date) === "undefined") {
		date = new Date();
	} else if (date == false) {
		//do nothing.
	} else {
		var pdate = parseJsonDate(date, true);
		if (!pdate) {
			console.error("Could not determine format from date: " + date);
			var err = $("<span style='color:rgba(255,0,0,.5)' >DateErr</span>");
			$(selector).html(err);
			return err;
		}
		date = pdate;
	}

	if (typeof (name) === "undefined")
		name = "Date";
	if (typeof (id) === "undefined")
		id = name;
	if (typeof (offsetMinutes) === "undefined")
		offsetMinutes = 0;

	if (date != false)
		date = new Date(date.getTime() - offsetMinutes * 60000);


	var formatted = serverDateFormat(date);
	var _userFormat = clientDateFormat(date);


	var guid = generateGuid();
	var builder = '<div class="input-append date ' + guid + '">';
	builder += '<input class="form-control client-date" data-val="true"' +
               ' data-val-date="The field Model must be a date." type="text" ' +
               'value="' + _userFormat + '" placeholder="not set">';
	builder += '<span class="add-on"><i class="icon-th"></i></span>';
	builder += '<input type="hidden" class="server-date" id="' + id + '" name="' + name + '" value="' + formatted + '" />';
	builder += '</div>';
	var dp = $(builder);
	$(selector).append(dp);
	var dpOptions = {
		format: window.dateFormat.toLowerCase(),
	};
	if (options) {
		for (var k in options) {
			if (arrayHasOwnIndex(options, k)) {
				dpOptions[k] = options[k];
			}
		}
	}
	var _offsetMin = offsetMinutes;
	var eventHolder = $("[name='" + name + "']");

	// if (date == false) {
	$('.' + guid + ' .client-date').on("change", function () {
		var v = $(this).val();
		//debugger;
		if (v == "") {
			$('.' + guid + ' .server-date').val("");
		}
	});
	//  }
	$('.' + guid + ' .client-date').datepickerX(dpOptions).on('changeDate', function (e) {
		var edate = new Date(e.date.getTime() + _offsetMin * 60000);
		var formatted = serverDateFormat(edate);
		$('.' + guid + ' .server-date').val(formatted);
		$(eventHolder).trigger("change", [{
			clientDate: edate,
			serverDate: formatted,
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

Date.prototype.addDays = function (pDays) {
	
	var oDate = new Date(this.valueOf());
	var mDate = new Date(this.valueOf());
	mDate.setDate(mDate.getDate() + pDays);

	var shift = oDate.getTimezoneOffset() - mDate.getTimezoneOffset();
	//debugger;

	return mDate;
};
Date.prototype.startOfWeek = function (pStartOfWeek) {
	var mDifference = this.getDay() - pStartOfWeek;
	if (mDifference < 0) {
		mDifference += 7;
	}
	return new Date(this.addDays(mDifference * -1));
}

