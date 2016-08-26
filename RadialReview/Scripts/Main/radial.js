
$(window).bind('beforeunload', function (event) {
	if ($(".unsaved").length > 0)
		return "There are items you have not saved.";
	return;
});

$(".modifiable").change(modify);
$(".modifiable").bind('input', modify);

function modify() {
	$(this).addClass("unsaved");
	$(".saveButton").addClass("unsaved");
}

function clearUnsaved() {
	$(".unsaved").removeClass("unsaved");
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
//Index of for IE
if (!Array.prototype.indexOf) {
	Array.prototype.indexOf = function (elt /*, from*/) {
		var len = this.length >>> 0;

		var from = Number(arguments[1]) || 0;
		from = (from < 0) ? Math.ceil(from) : Math.floor(from);
		if (from < 0)
			from += len;

		for (; from < len; from++) {
			if (from in this && this[from] === elt)
				return from;
		}
		return -1;
	};
}

(function ($) {
	$.fn.valList = function () {
		return $.map(this, function (elem) {
			return elem.value || "";
		}).join(",");
	};
	$.fn.nameList = function () {
		return $.map(this, function (elem) {
			return elem.name || "";
		}).join(",");
	};
	$.fn.asString = function () {
		if (Object.prototype.toString.call(this) === '[object Array]') {
			return $.map(this, function (elem) {
				return elem || "";
			}).join(",");
		}
		return this;
	};

	$.fn.serializeObject = function () {
		var o = {};
		var a = this.serializeArray();
		$.each(a, function () {
			if (o[this.name] !== undefined) {
				if (!o[this.name].push) {
					o[this.name] = [o[this.name]];
				}
				o[this.name].push(this.value || '');
			} else {
				o[this.name] = this.value || '';
			}
		});
		return o;
	};

})(jQuery);

/*(function () {
  function CustomEvent ( event, params ) {
    params = params || { bubbles: false, cancelable: false, detail: undefined };
    var evt = document.createEvent( 'CustomEvent' );
    evt.initCustomEvent( event, params.bubbles, params.cancelable, params.detail );
    return evt;
   }

  CustomEvent.prototype = window.Event.prototype;

  window.CustomEvent = CustomEvent;
})();*/

function qtip() {
	$('[title]').qtip({
		position: {
			my: 'bottom left',  // Position my top left...
			at: 'top center', // at the bottom right of...
			target: 'mouse'
		}
	});
}

function save(key, value) {
	if ('localStorage' in window && window['localStorage'] !== null) {
		window.localStorage[key] = value;
	} else {
		console.log("Could not save");
	}
}

function load(key) {
	if ('localStorage' in window && window['localStorage'] !== null) {
		return window.localStorage[key];
	} else {
		console.log("Could not load");
	}
}


function ForceUnhide() {
	var speed = 40;
	$(".startHiddenGroup").each(function (i, e) {
		$(e).find(".startHidden").each(function (i, e2) {
			setTimeout(function () {
				$(e2).addClass("unhide");
			}, speed * i);
		});
	});
}

function ForceHide() { $(".startHidden").removeClass("startHidden").removeClass("unhide").addClass("startHidden"); }

function refresh() { location.reload(); }

/*
if callback returns text or bool, there is an error
*/
function showTextAreaModal(title, callback, defaultText) {
	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	if (typeof defaultText === "undefined")
		defaultText = "";

	$('#modalBody').html("<div class='error' style='color:red;font-weight:bold'></div><textarea class='form-control verticalOnly' rows=5>" + defaultText + "</textarea>");
	$("#modalTitle").html(title);
	$("#modalForm").unbind('submit');
	$("#modal").modal("show");

	$("#modalForm").submit(function (e) {
		e.preventDefault();
		var result = callback($('#modalBody').find("textarea").val());
		if (result) {
			if (typeof result !== "string") {
				result = "An error has occurred. Please check your input.";
			}
			$("#modalBody .error").html(result);
		} else {
			$("#modal").modal("hide");
		}
	});
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

function getFormattedDate(date) {

	if (typeof (date) === "undefined") {
		date = new Date();
	} else if (typeof (date) === "string") {
		console.error("Could not determine format from date string: " + date)
	}

	var _d = date.getDate(),
                dd = _d > 9 ? _d : '0' + _d,
                _m = date.getMonth() + 1,
                mm = _m > 9 ? _m : '0' + _m,
                yyyy = date.getFullYear(),
                formatted = mm + '-' + dd + '-' + (yyyy);

	var _userFormat = window.dateFormat
        .replace(/mm/gi, mm).replace(/m/gi, _m)
        .replace(/dd/gi, dd).replace(/d/gi, _d)
        .replace(/yyyy/gi, yyyy).replace(/yy/gi, (yyyy - 2000));
	return _userFormat;
}

function generateDatepicker(selector, date, name, id) {
	if (typeof (date) === "undefined") {
		date = new Date();
	} else if (typeof (date) === "string") {
		console.error("Could not determine format from date string: " + date)
	}

	if (typeof (name) === "undefined")
		name = "Date";
	if (typeof (id) === "undefined")
		id = name;

	var _d = date.getDate(),
                dd = _d > 9 ? _d : '0' + _d,
                _m = date.getMonth() + 1,
                mm = _m > 9 ? _m : '0' + _m,
                yyyy = date.getFullYear(),
                formatted = mm + '-' + dd + '-' + (yyyy);

	var _userFormat = window.dateFormat
        .replace(/mm/gi, mm).replace(/m/gi, _m)
        .replace(/dd/gi, dd).replace(/d/gi, _d)
        .replace(/yyyy/gi, yyyy).replace(/yy/gi, (yyyy - 2000));

	var guid = generateGuid();
	var builder = '<div class="input-append date ' + guid + '">';
	builder += '<input class="form-control client-date" data-val="true"' +
               ' data-val-date="The field Model must be a date." type="text" ' +
               'value="' + _userFormat + '">';
	builder += '<span class="add-on"><i class="icon-th"></i></span>';
	builder += '<input type="hidden" class="server-date" id="' + id + '" name="' + name + '" value="' + formatted + '" />';
	builder += '</div>';
	var dp = $(builder);
	$(selector).append(dp);
	$('.' + guid + ' .client-date').datepickerX({
		format: window.dateFormat.toLowerCase(),
		//onRender: function (date) {
		//    return date.valueOf() <= now.valueOf() ? 'disabled' : '';
		//}
	}).on('changeDate', function (e) {
		var _d = e.date.getDate(),
            d = _d > 9 ? _d : '0' + _d,
            _m = e.date.getMonth() + 1,
            m = _m > 9 ? _m : '0' + _m,
            formatted = m + '-' + d + '-' + (e.date.getFullYear());
		$('.' + guid + ' .server-date').val(formatted);
	});
}

function metGoal(direction, goal, measured, alternate) {

	if (!$.trim(measured)) {
		return undefined;
	} else if ($.isNumeric(measured)) {
		var m = +measured;
		var g = +goal;
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
			var ag = +alternate;
			return g<=m && m<=ag;
		} else {
			console.log("Error: goal met could not be calculated. Unhandled direction: " + direction);
			return undefined;
		}
	} else {
		return undefined;
	}
}

Date.prototype.addDays = function (pDays) {
	var mDate = new Date(this.valueOf());
	mDate.setDate(mDate.getDate() + pDays);
	return mDate;
};
Date.prototype.startOfWeek = function (pStartOfWeek) {
	var mDifference = this.getDay() - pStartOfWeek;

	if (mDifference < 0) {
		mDifference += 7;
	}

	return new Date(this.addDays(mDifference * -1));
}

function getWeekSinceEpoch(day) {
	var oneDay = 24 * 60 * 60 * 1000;
	var span = day.startOfWeek(0);
	return Math.floor((span.getTime() / oneDay) / 7);
}


function showModal(title, pullUrl, pushUrl, callback, validation, onSuccess, onCancel) {

	$("#modal-icon").attr("class", "");
	$("#modal #class-container").attr("class", "");
	$("#modalCancel").removeClass("hidden");

	if (typeof (title) === "object") {
		var obj = title;
		var push = pullUrl;
		var cback = pushUrl;
		return showModalObject(obj, push, cback);
	}

	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	$("#modal").addClass("loading");
	$('#modal').modal('show');

	$.ajax({
		url: pullUrl,
		type: "GET",
		//Couldnt retrieve modal partial view
		error: function (jqxhr, status, error) {
			$('#modal').modal('hide');
			$("#modal").removeClass("loading");
			$("#modalForm").unbind('submit');
			if (status == "timeout")
				showAlert("The request has timed out. If the problem persists, please contact us.");
			else
				showAlert("Something went wrong. If the problem persists, please contact us.");
		},
		//Retrieved Partial Modal
		success: function (modal) {
			if (!modal) {
				$('#modal').modal('hide');
				$("#modal").removeClass("loading");
				showAlert("Something went wrong. If the problem persists, please contact us.");
				return;
			}
			_bindModal(modal, title, callback, validation, function (formData) {
				_submitModal(formData, pushUrl, onSuccess, false);
			});
		}
	});
}
/*
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///  obj ={                                                                                                                     ///
///      title:,                                                                                                                ///
///      icon : <success,warning,danger,info,primary,default> or {icon:"css icon name",title:"Title Text!",color:"Hex-Color"}   ///
///      fields: [{                                                                                                             ///
///          name:(optional)                                                                                                    ///
///          text:(optional)                                                                                                    ///
///          type: <text,textarea,checkbox,radio,span,header,h1,h2,h3,h4,h5,h6,number,date,time,file,yesno,label>(optional)     ///
///          value: (optional)                                                                                                  ///
///          placeholder: (optional)                                                                                            ///
///      },...],                                                                                                                ///
///      pushUrl:"",                                                                                                            ///
///      success:function,                                                                                                      ///
///      cancel:function,                                                                                                       ///  
///      reformat: function,
///      noCancel: bool
///  }                                                                                                                          ///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
*/
function showModalObject(obj, pushUrl, onSuccess, onCancel) {
	$("#modalCancel").toggleClass("hidden", obj.noCancel || false);
	if (typeof (pushUrl) === "undefined")
		pushUrl = obj["push"] || obj["pushUrl"];
	if (typeof (onSuccess) === "undefined")
		onSuccess = obj["success"];
	if (typeof (onSuccess) !== "undefined" && typeof (pushUrl) !== "undefined") {
		var oldSuccess = onSuccess;
		onSuccess = function (formData, contentType) { _submitModal(formData, pushUrl, oldSuccess, true, contentType); };
	}
	if (typeof (onSuccess) === "undefined" && typeof (pushUrl) !== "undefined")
		onSuccess = function (formData, contentType) { _submitModal(formData, pushUrl, null, true, contentType); };

	var onClose = obj.close;

	if (typeof (onCancel) === "undefined")
		onCancel = obj["cancel"];

	if (!obj.fields && obj.pullUrl && obj.title && pushUrl)
		return showModal(obj.title, obj.pullUrl, pushUrl, onSuccess, obj.validation, obj.success);

	if (typeof (obj.title) === "undefined") {
		obj.title = "";
		console.warn("No title supplied");
	}

	obj.modalClass = obj.modalClass || "";

	var reformat = obj.reformat;

	var iconType = typeof (obj.icon);
	if (iconType !== "undefined") {
		obj.modalClass += " modal-icon";
		$("#modal-icon").attr("class", "modal-icon");
		if (iconType === "string") {
			obj.modalClass += " modal-icon-" + obj.icon;
			//obj.title = iconType.toLowerCase() + "!";
		}else if (iconType === "object") {
			var time = +new Date();
			var custom = "modal-icon-custom" + time;
			obj.modalClass += " " + custom;
			if (!obj.icon.icon)
				obj.modalClass += " modal-icon-info";

			var icon = (obj.icon.icon || ("icon-" + custom)).replace(".", "");
			var title = escapeString(obj.icon.title || "Hey!");
			var color = escapeString(obj.icon.color || "#5bc0de");
			$("#modal-icon").addClass(icon);
			icon = icon.replace(" ", ".")
			document.styleSheets[0].addRule("." + custom + " ." + icon + ":after", 'content: "' + title + '" !important;');
			document.styleSheets[0].addRule("." + custom + " ." + icon + ":before", 'background-color: ' + color + ';');
			document.styleSheets[0].addRule("." + custom + " #modalOk", 'background-color: ' + color + ';');
		}

	}

	$("#modal #class-container").attr("class", obj.modalClass);

	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	$("#modal").addClass("loading");
	$('#modal').modal('show');

	var allowed = ["text", "hidden", "textarea", "checkbox", "radio", "number", "date", "time", "header", "span", "h1", "h2", "h3", "h4", "h5", "h6", "file", "yesno","label"];
	var addLabel = ["text", "textarea", "checkbox", "radio", "number", "date", "time", "file"];
	var tags = ["span", "h1", "h2", "h3", "h4", "h5", "h6","label"];
	var anyFields = ""
	
	if (typeof (obj.field) !== "undefined") {
		if (typeof (obj.fields) !== "undefined") {
			throw "A 'field' and a 'fields' property exists";
		} else {
			obj.fields = obj.field;
		}
	}

	if (typeof (obj.fields) === "object") {
		var allDeep = true;
		for (var f in obj.fields) {
			if (typeof (obj.fields[f]) !== "object") {
				allDeep = false;
				break;
			}
		}
		if (!allDeep) {
			obj.fields = [obj.fields];
		}
	}

	var fieldsTypeIsArray = Array.isArray(obj.fields);//typeof (obj.fields);

	var contentType = null;

	var builder = '<div class="form-horizontal modal-builder">';
	var runAfter = [];
	var genInput = function (type, name, placeholder, value, others) {
		others = others || "";
		if (type == "number")
			others += " step=\"any\"";

		if (type=="checkbox" && ((typeof(value)==="string" && (value.toLowerCase() === 'true'))||(typeof(value)==="boolean" && value)))
			others +="checked";

		return '<input type="' + escapeString(type) + '" class="form-control blend"' +
                      ' name="' + escapeString(name) + '" id="' + escapeString(name) + '" ' +
                      escapeString(placeholder) + ' value="' + escapeString(value) + '" ' +others+'/>';

	}

	for (var f in obj.fields) {
		try {
			var field = obj.fields[f];
			var name = field.name || f;
			var label = typeof (field.text) !== "undefined" || !fieldsTypeIsArray;
			var text = field.text || name;
			var originalValue = field.value;
			var value = field.value || "";
			var placeholder = field.placeholder;
			var type = (field.type || "text").toLowerCase();

			if (type == "header")
				type = "h4";

			if (typeof (placeholder) !== "undefined")
				placeholder = "placeholder='" + placeholder + "'";
			else placeholder = "";
			var input = "";
			var inputIndex = allowed.indexOf(type);
			if (inputIndex == -1) {
				console.warn("Input type not allowed:" + type);
				continue;
			}
			if (Object.prototype.toString.call(value) === '[object Date]' && type == "date") {
				value = value.toISOString().substring(0, 10);
			}

			if (type == "file")
				contentType = 'enctype="multipart/form-data"';

			if (tags.indexOf(type) != -1) {
				input = "<" + type + " name=" + escapeString(name) + '" id="' + escapeString(name) + '">' + value + '</' + type + '>';
			} else if (type == "textarea") {
				input = '<textarea class="form-control blend verticalOnly" rows=5 name="' + escapeString(name) + '" id="' + escapeString(name) + '" ' + escapeString(placeholder) + '>' + value + '</textarea>';
			} else if (type == "date") {
				var guid = generateGuid();
				var curName = name;
				var curVal = originalValue;
				input = '<div class="date-container date-' + guid + '"></div>';
				runAfter.push(function () {
					generateDatepicker('.date-' + guid, curVal, curName, curName);
				});
			} else if (type == "yesno") {
				var selectedYes = (value == true) ? 'checked="checked"' : "";
				var selectedNo = (value == true) ? "" : 'checked="checked"';
				input = '<div class="form-group input-yesno">' +
                            '<label for="true" class="col-xs-4 control-label"> Yes </label>' +
                            '<div class="col-xs-2">' + genInput("radio", name, placeholder, "true",selectedYes) + '</div>' +
                            '<label for="false" class="col-xs-1 control-label"> No </label>' +
                            '<div class="col-xs-2">' + genInput("radio", name, placeholder, "false",selectedNo) + '</div>' +
                        '</div>';
			} else {
				input = genInput(type, name, placeholder, value);
			}

			if (addLabel.indexOf(type) != -1 && label) {
				builder += '<div class="form-group"><label for="' + name + '" class="col-sm-2 control-label">' + text + '</label><div class="col-sm-10">' + input + '</div></div>';
			} else {
				builder += input;
			}
		} catch (e) {
			console.error(e);
		}
	}
	builder += "</div>";
	_bindModal(builder, obj.title, undefined, undefined, onSuccess, onCancel, reformat, onClose, contentType);
	for (var i = 0; i < runAfter.length; i++) {
		runAfter[i]();
	}
}

function _bindModal(html, title, callback, validation, onSuccess, onCancel, reformat, onClose, contentType) {
	$('#modalBody').html(html);
	$("#modalTitle").html(title);
	$("#modal").removeClass("loading");
	//Reregister submit button
	$("#modalForm").unbind('submit');

	var onCloseArg = onClose;
	var onCancelArg = onCancel;
	var onSuccessArg = onSuccess;
	var contentTypeArg = contentType;
	var validationArg = validation;
	var reformatArg = reformat;
	var callbackArg = callback;

	$("#modalForm input:visible,#modalForm textarea:visible,#modalForm button:not(.close):visible").first().focus();

	$("#modalForm").submit(function (ev) {
		ev.preventDefault();

		var formData = $("#modalForm").serializeObject();
		$("#modalForm").find("input:checkbox").each(function () {
			formData[$(this).prop("name")] = $(this).is(":checked") ? "True" : "False";
		});

		if (typeof (reformatArg) === "function") {
			var o = reformatArg(formData);
			if (typeof (o) !== "undefined" && o != null)
				formData = o;//Data was returned, otherwise formdata was manipulated
		}

		if (validationArg) {
			var message = undefined;
			if (typeof (validationArg) === "string") {
				message = eval(validationArg + '()');
			} else if (typeof (validationArg) === "function") {
				message = validationArg();
			}
			if (message !== undefined && message != true) {
				if (message == false) {
					$("#modalMessage").html("Error");
				}
				else {
					$("#modalMessage").html(message);
				}
				$("#modalMessage").removeClass("hidden");
				return;
			}
		}
		$("#modal").modal("hide");
		$("#modal").removeClass("loading");
		//onSuccess(formData);

		if (onSuccessArg) {
			if (typeof onSuccessArg === "string") {
				eval(onSuccessArg + "(formData," + contentTypeArg + ")");
			} else if (typeof onSuccessArg === "function") {
				onSuccessArg(formData, contentTypeArg);
			}
		}
		if (onCloseArg) {
			if (typeof onCloseArg === "string") {
				eval(onCloseArg + "()");
			} else if (typeof onCloseArg === "function") {
				onCloseArg();
			}
		}
	});

	$("#modal button[data-dismiss='modal']").unbind('click.radialModal');


	$("#modal button[data-dismiss='modal']").on("click.radialModal", function () {
		if (typeof onCancelArg === "string") {
			eval(onCancelArg + "()");
		} else if (typeof onCancelArg === "function") {
			onCancelArg();
		}
		if (typeof onCancelArg === "string") {
			eval(onCancelArg + "()");
		} else if (typeof onCancelArg === "function") {
			onCancelArg();
		}
	});

	$("#modal").removeClass("loading");
	$('#modal').modal('show');
	var count = 0;
	setTimeout(function () {
		if (callbackArg) {
			if (typeof (callbackArg) === "string")
				eval(callbackArg + '()');
			else if (typeof (callbackArg) === "function")
				callbackArg();
		} else {
			$('#modal input:not([type=hidden]):not(.disable):first').focus();
		}
	}, 50);
}

function _submitModal(formData, pushUrl, onSuccess, useJson, contentType) {
	///FORM DATA IS NOT USED
	///TODO use form data;
	var serialized
	//var serialized = $.param(formData);
	//var contentType = null;

	if (typeof (contentType) === "undefined")
		contentType = null;
	var processData = null;
	if (useJson && contentType == null) {
		serialized = JSON.stringify(formData);
		contentType = "application/json; charset=utf-8";
	} else if (contentType == 'enctype="multipart/form-data"') {
		serialized = new FormData($('#modalForm')[0]);
		processData = false;
		contentType = false;
	} else {
		serialized = $("#modalForm").serialize();
		contentType = contentType || "application/x-www-form-urlencoded";
	}
	var onSuccessArg = onSuccess;

	$.ajax({
		url: pushUrl,
		type: "POST",
		contentType: contentType,
		data: serialized,// JSON.stringify(formData),
		processData: processData,
		success: function (data, status, jqxhr) {
			if (!data) {
				$("#modal").modal("hide");
				$("#modal").removeClass("loading");
				showAlert("Something went wrong. If the problem persists, please contact us.");
			} else {
				if (onSuccessArg) {
					if (typeof onSuccessArg === "string") {
						eval(onSuccessArg + "(data,formData)");
					} else if (typeof onSuccessArg === "function") {
						onSuccessArg(data, formData);
					}
				} else {
				}
			}
		},
		error: function (jqxhr, status, error) {
			if (error == "timeout") {
				showAlert("The request has timed out. If the problem persists, please contact us.");
			} else {
				showAlert("Something went wrong. If the problem persists, please contact us.");
			}
			$("#modal").modal("hide");
			$("#modal").removeClass("loading");
		}
	});
}
function UnstoreJsonAlert() {
	var data = localStorage.getItem("Alert");
	localStorage.setItem("Alert", null);

	var alert = JSON.parse(data);
	if (alert !== undefined && alert != null && alert != "null") {
		clearAlerts();
		var type = alert.type;
		var title = alert.title;
		var message = alert.message;
		if (type === undefined) type = "alert-success";
		if (title === undefined) title = "Success!";
		if (message === undefined) message = "";
		showAlert(message, type, title);
	}
}

function StoreJsonAlert(json) {
	var alert = new Object();
	alert.message = json.Message;
	if (!json.MessageType)
		json.MessageType = "danger";
	alert.type = "alert-" + json.MessageType.toLowerCase();
	alert.title = json.Heading;
	localStorage.setItem("Alert", JSON.stringify(alert));
}

function showHtmlErrorAlert(html, defaultMessage) {
	var message = defaultMessage;
	debugger;
	if (typeof (html) === "object" && typeof (html.responseText) === "string") {
		message = $(html).find("title").text();
	}else if (typeof (html) === "string") {
		message = $(html).text();
	}
	
	if (typeof (message) === "undefined" || message == null || message == "") {
		message = "An error occurred.";
	}
	showAlert(message);
}

function showAlert(message, alertType, preface) {
	if (alertType === undefined)
		alertType = "alert-danger";
	if (preface === undefined)
		preface = "Warning!";
	if (Object.prototype.toString.call(message) === '[object Array]') {
		if (message.length > 1) {
			var msg = "<ul style='margin-bottom:0px;'>";
			for (var i in message) {
				msg += "<li>" + message[i] + "</li>"
			}
			message = msg + "</ul>"
		} else {
			message = message.join("");
		}
	}

	var alert = $("<div class=\"alert " + alertType + " alert-dismissable start\"><button type=\"button\" class=\"close\" data-dismiss=\"alert\" aria-hidden=\"true\">&times;</button><strong>" + preface + "</strong> <span class=\"message\">" + message + "</span></div>");
	$("#alerts").prepend(alert);
	setTimeout(function () { alert.removeClass("start"); }, 1);
}

var alertsTimer = null;
function clearAlerts() {
	var found = $("#alerts .alert").remove();
	/*found.css({ height: "0px", opacity: 0.0, padding: "0px", border: "0px", margin: "0px" });
    if (alertsTimer) {
        clearTimeout(alertsTimer);
    }
    alertsTimer = setTimeout(function () {
        found.remove();
    }, 1000);*/
}

function showAngularError(d, status, headers, config, statusTxt) {
	if (typeof (d.data) !== "undefined" && d.data != null) {
		showJsonAlert(d.data);
	} else {
		if (typeof (d.statusText) !== "undefined" && d.statusText !== "") {
			showAlert(d.statusText);
		} else {
			showJsonAlert();
		}
	}
}

function showJsonAlert(data, showSuccess, clearOthers) {
	try {
		if (clearOthers) {
			clearAlerts();
		}
		var stdError = "Something went wrong.";

		if (!data) {
			showAlert(stdError);
		} else if (typeof (data) === "string") {
			if (data.trim().length < 300)
				showAlert(data.trim(), "alert-danger", "Error");
			else
				showAlert(stdError);
		} else {
			var message = data.Message;
			if (message === undefined)
				message = "";
			if (data.Trace) {
				console.error(data.TraceMessage);
				console.error(data.Trace);
			}
			console.log(data.Message);
			if (!data.Silent && (data.MessageType !== undefined && data.MessageType != "Success" || showSuccess)) {
				var mType = data.MessageType || "danger";
				showAlert(message, "alert-" + mType.toLowerCase(), data.Heading);
			}
			if (data.Error) {
				debugger;
				sendErrorReport();
			}

		}
	} catch (e) {
		console.error(e);
	}
	if (!data)
		return false;
	return !data.Error;
}

function getKeySelector(selector, prefix) {
	prefix = prefix || "";
	var output = { selector: selector, key: false };

	if ($(selector).data("key")) {
		output.key = prefix + $(selector).data("key");
	} else if ($(selector).attr("name")) {
		output.key = prefix + $(selector).attr("name");
		output.selector = "[name=" + $(selector).attr("name") + "]";
		/*if ($(selector).is("[type='radio']")) {
            output.selector += ":checked";
        }*/
	} else if ($(selector).attr("id")) {
		output.key = prefix + $(selector).attr("id");
		output.selector = "#" + $(selector).attr("id");
	}

	return output;
}

function getVal(selector) {
	var self = $(selector);
	if (self.is("[type='checkbox']")) {
		return self.is(':checked');
	}
	if (self.is("[type='radio']")) {
		return self.filter(":checked").val();
	}
	else if (self.hasClass("panel-collapse")) {
		return self.hasClass("in");
	} else {
		return self.val();
	}
}

function setVal(selector, val) {
	var self = $(selector);
	if (self.is("[type='checkbox']")) {
		self.prop('checked', val == "true");
	} else if (self.is("[type='radio']")) {
		self.prop('checked', function () {
			return $(this).attr("value") == val;
		});
	} else if (self.hasClass("panel-collapse")) {
		self.collapse(val == "true" ? "show" : "hide");
	} else {
		self.val(val);
	}
	self.change();
}

function getInitials(name, initials) {
	if (typeof (name) === "undefined" || name == null) {
		name = "";
	}

	if (typeof (initials) === "undefined") {
		var m = name.match(/\b\w/g) || [];
		initials = m.join(' ');
	}
	return initials;
}

function profilePicture(url, name, initials) {
	var picture = "";
	if (url !== "/i/userplaceholder") {
		picture = "<span class='picture' style='background: url(" + url + ") no-repeat center center;'></span>";
	} else {
		var hash = 0;
		if (typeof (name) === "undefined") {
			name = "";
		}
		if (name.length != 0) {
			for (var i = 0; i < name.length; i++) {
				{
					var chr = name.charCodeAt(i);
					hash = ((hash << 5) - hash) + chr;
					hash |= 0; // Convert to 32bit integer
				}
			}
			hash = hash % 360;

			initials = getInitials(name, initials).toUpperCase();
			//initials = name.match(/\b\w/g).join(' ');
			picture = "<span class='picture' style='background-color:hsla(" + hash + ", 36%, 49%, 1);color:hsla(" + hash + ", 36%, 72%, 1)'><span class='initials'>" + initials + "</span></span>";

		}
	}

	return "<span class='profile-picture'>" +
        "<span class='picture-container' title='" + escapeString(name) + "'>" +
            picture +
    "</span>" +
"</span>";
}



(function ($) {
	$.fn.setCursorToTextEnd = function () {
		var $initialVal = this.val();
		this.val($initialVal);
	};

	$(".panel-collapse").collapse({
		toggle: false
	});

	$(".autoheight").each(function (index) {
		var maxHeight = 0;
		$(this).children().each(function () {
			maxHeight = Math.max(maxHeight, $(this).height());
		});
		$(this).height(maxHeight);
	});

	var scrollTopModal = 0;

	$("#modalForm").on("show.bs.modal", function () {
		scrollTopModal = $("body").scrollTop();
	}).on("hidden.bs.modal", function () {
		setTimeout(function () { $("body").scrollTop(scrollTopModal); }, 1);
	}).on("shown.bs.modal", function () {
		setTimeout(function () { $("body").scrollTop(scrollTopModal); }, 1000);
	});


})(jQuery);

$.fn.flash = function (ms, backgroundColor, borderColor, color) {
	ms = ms || 1000;
	color = color || '#3C763D';
	borderColor = borderColor || '#D6E9C6';
	backgroundColor = backgroundColor || '#DFF0D8';


	var originalBackgroundColor = this.css('background-color');
	var originalBorderColor = this.css('border-color');
	var originalBoxColor = this.css('boxShadow');
	var originalColor = this.css('color');

	this.css({ 'border-color': borderColor, 'background-color': backgroundColor, "boxShadow": "0px 0px 5px 3px " + borderColor, "color": color })
    .animate({ 'border-color': originalBorderColor, 'background-color': originalBackgroundColor, "boxShadow": "0px 0px 0px 0px " + borderColor, "color": originalColor }, ms, function () {
    	$(this).css("boxShadow", originalBoxColor);
    	$(this).css("background-color", "");
    	$(this).css("border-color", "");
    	$(this).css("color", "");
    	$(this).css("boxShadow", "");
    });


};
/////////////////////////////////////////////////////////////////
//Ajax Interceptors

var interceptAjax = function (event, request, settings) {
	//console.log(event);
	//console.log(settings);
	try {
		var result = $.parseJSON(request.responseText);
		try {
			if (result.Refresh) {
				if (result.Silent !== undefined && !result.Silent) {
					result.Refresh = false;
					StoreJsonAlert(result);
				}
				location.reload();
			} else if (result.Redirect) {
				var url = result.Redirect;
				if (result.Silent !== undefined && !result.Silent) {
					result.Refresh = false;
					result.Redirect = false;
					StoreJsonAlert(result);
				}
				window.location.href = url;
			} else {
				if (result.Silent !== undefined && !result.Silent) {
					showJsonAlert(result, true, true);
				}
			}
		} catch (e) {
			console.log(e);
		}
	} catch (e) {
	}
};

$(document).ajaxSuccess(interceptAjax);
$(document).ajaxError(interceptAjax);



$(document).ajaxSend(function (event, jqX, ajaxOptions) {
	if (ajaxOptions.url == null) {
		ajaxOptions.url = "";
	}



	if (typeof (ajaxOptions.data) === "string" && ajaxOptions.data.indexOf("_clientTimestamp") != -1) {
		return;
		/*var start = ajaxOptions.data.indexOf("_clientTimestamp")+17;
        debugger;
        date = ajaxOptions.data.substr(start).split("&")[0];*/
	}

	//var date = (new Date().getTime());



	if (ajaxOptions.url.indexOf("_clientTimestamp") == -1) {
		if (!window.tzoffset) {
			var jan = new Date(new Date().getYear() + 1900, 0, 1, 2, 0, 0), jul = new Date(new Date().getYear() + 1900, 6, 1, 2, 0, 0);
			window.tzoffset = (jan.getTime() % 24 * 60 * 60 * 1000) >
                         (jul.getTime() % 24 * 60 * 60 * 1000)
                         ? jan.getTimezoneOffset() : jul.getTimezoneOffset();
		}
		if (ajaxOptions.url.indexOf("?") == -1)
			ajaxOptions.url += "?";
		else
			ajaxOptions.url += "&";

		ajaxOptions.url += "_clientTimestamp=" + ((+new Date()) + (window.tzoffset * 60 * 1000));
	}
});
/////////////////////////////////////////////////////////////////

(function ($) {
	$.fn.focusTextToEnd = function () {
		this.focus();
		var $thisVal = this.val();
		this.val('').val($thisVal);
		return this;
	};
	$.fn.insertAt = function (elements, index) {
		var children = this.children();
		if (index >= children.size()) {
			this.append(elements);
			return this;
		}
		var before = children.eq(index);
		$(elements).insertBefore(before);
		return this;
	};
}(jQuery));


function dateFormatter(date) {
	/*if(Date.parse('2/6/2009')=== 1233896400000){
        return [date.getMonth()+1, date.getDate(), date.getFullYear()].join('/');
    }*/
	return [date.getMonth() + 1, date.getDate(), date.getFullYear()].join('/');
}

$(function () {
	/*$('img').hide().on('load', function () {
        // do something, maybe:
        $(this).fadeIn();
        $(this).addClass("loaded");
    });*/

	$(window).bind('beforeunload', function () {
		if (document.activeElement) $(document.activeElement).blur();

	});



	$(".footer-bar-container").each(function () {
		var h = parseInt($(this).attr("data-height"));
		$(this).find(".footer-bar-contents").css("bottom",/*-h+*/"0px");
		$(this).css("bottom", -h + "px");
		$(this).find(".footer-bar-contents").css("height", h + "px");
	});

	$("body").on("click", ".footer-bar-tab .clicker", function () {
		var tab = $(this).parent(".footer-bar-tab");
		var on = !$(tab).hasClass("shifted");
		$(tab).toggleClass("shifted", on);
		var parent = $(tab).parent(".footer-bar-container");
		parent.toggleClass("shifted", on);
		parent.find(".footer-bar-contents").toggleClass("shifted", on);

		var curHeight = 0;
		$(".footer-bar-container").each(function () {
			var isShift = $(this).hasClass("shifted");
			var selfH = parseInt($(this).attr("data-height"));
			if (isShift) {
				curHeight += selfH;
			}
			$(this).css("bottom", -(-curHeight + selfH) + "px");
			//$(this).parent(".footer-bar-container").css("bottom", curHeight + "px");
		});

		$(".body-full-width #main").css("padding-bottom", Math.max(20, curHeight) + "px");

		$(window).trigger("footer-resize", on);
	});

	$('.picture').each(function () {
		var picture = $(this);
		var bg = $(picture).css('background-image');
		if (bg && bg != "none") {
			$(picture).fadeToggle();
			var src = bg.replace(/(^url\()|(\)$|[\"\'])/g, '');
			var $img = $('<img>').attr('src', src).on('load', function () {
				// do something, maybe:
				$(picture).fadeIn();
			});
		}
	});
});


$(document).ready(function () {
	var event = new CustomEvent("jquery-loaded", {});
	document.dispatchEvent(event);
});

$(function () {
	//Adds links to hash nav buttons
	var updateFromHash = function () {
		var hash = window.location.hash;
		hash && $('ul.nav a[href="' + hash + '"]').tab('show');
	};
	updateFromHash();
	window.addEventListener("hashchange", updateFromHash, false);

	$(document).on("click", '.nav a', function (e) {
		$(this).tab('show');
		var scrollmem = $('body').scrollTop();
		window.location.hash = this.hash;
		$('html,body').scrollTop(scrollmem);
	});
});


$(function () {
	$(document).on("click", ".undoable .undo-button", function () {
		var undoable = $(this).closest(".undoable");
		var url = undoable.data("undo-url");
		var action = undoable.data("undo-action");
		if (typeof (url) !== "undefined") {
			$.ajax({
				url: url,
				success: function (data) {
					if (showJsonAlert(data)) {
						if (("" + action).indexOf("unclass") != -1) {
							$(undoable).removeClass("undoable");
						}
						if (("" + action).indexOf("remove") != -1) {
							$(undoable).remove();
						}
						if (("" + action).indexOf("unhide") != -1) {
							$(undoable).show();
						}
					}
				}
			});
		} else {
			showAlert("No action for undoable.", "Error!");
		}
	});
});


window.addEventListener("submit", function (e) {
	var form = e.target;
	if (form.getAttribute("enctype") === "multipart/form-data") {
		if (form.dataset.ajax) {
			e.preventDefault();
			e.stopImmediatePropagation();
			var xhr = new XMLHttpRequest();
			xhr.open(form.method, form.action);
			xhr.onreadystatechange = function () {
				if (xhr.readyState == 4 && xhr.status == 200) {
					if (form.dataset.ajaxUpdate) {
						var updateTarget = document.querySelector(form.dataset.ajaxUpdate);
						if (updateTarget) {
							updateTarget.innerHTML = xhr.responseText;
						}
					}
				}
			};
			xhr.send(new FormData(form));
		}
	}
}, true);

function debounce(func, wait, immediate) {
	var timeout;
	return function () {
		var context = this, args = arguments;
		var later = function () {
			timeout = null;
			if (!immediate) func.apply(context, args);
		};
		var callNow = immediate && !timeout;
		clearTimeout(timeout);
		timeout = setTimeout(later, wait);
		if (callNow) func.apply(context, args);
	};
};

Constants = {
	StartHubSettings: { transport: ['webSockets', 'longPolling'] }
};


function sendErrorReport() {
	try{
		console.log("Sending Error Report...");
		var message = "[";
		var mArray = [];
		for (var i in consoleStore) {
			mArray.push(JSON.stringify(consoleStore[i]));
		}
		message = "[" + mArray.join(",\n") + "]";
	
		function _send(){
			data = {};
			data.Console = message;
			data.Url = window.location.href;
			data.User = window.UserId;
			data.Org = window.OrgId;
			data.PageTitle = window.Title;
			data.Status = "JavascriptError";
			data.Subject = "Javascript Error - " + data.PageTitle;

			if (image != null) {
				data.ImageData = image;
			}
			$.ajax({
				method: "POST",
				url: "/support/email",
				data: data,
				success: function (d) {
					console.log("Report was sent.");
				},
				error: function (a, b, c) {
					console.error("Error sending report:");
					console.error(b,c);
				}
			});
		}
	
		try {
			$.getScript("/Scripts/home/html2canvas.js").done(function () {
				try {
					console.log("...begin render");
					screenshotPage(function (res) {
						image = res;
						console.log("...end render");
						_send();
					});			
				} catch (e) {
					_send();
				}
			}).error(function () {
				_send();
			});
		} catch (e) {
			_send();
		}
	} catch (e) {
		console.error("Error sending report:");
		console.error(e);
	}

}

function supportEmail(title) {
	var message = "[";
	var mArray = [];
	for (var i in consoleStore) {
		mArray.push(JSON.stringify(consoleStore[i]));
	}
	message = "[" + mArray.join(",\n") + "]";
	var fields = [
            { name: "Subject", text: "Subject", type: "text", },
            { name: "Body", text: "Body", type: "textarea" }
	];

	if (typeof (window.UserId) === "undefined")
		fields.push({ name: "Email", text: "Email", type: "text" });
	var image = null;
	var show = function () {
		showModal({
			title: "How can we help you?",
			icon: "default",//{ icon: "modal-icon-default", title: "Contact Support", color: "#ef7622" },
			fields: fields,
			pushUrl: "/support/email",
			reformat: function (data) {
				data.Console = message;
				data.Url = window.location.href;
				data.User = window.UserId;
				data.Org = window.OrgId;
				data.PageTitle = title;

				if (image != null) {
					data.ImageData = image;
				}
			}
		});
	};
	try {
		$.getScript("/Scripts/home/html2canvas.js").done(function () {
			try {
				console.log("begin render");
				screenshotPage(function (res) {
					image = res;
					console.log("end render");
				});
				show();
				//html2canvas(document.body, {
				//    allowTaint: true,
				//    onrendered: function (canvas) {
				//        image = canvas;

				//        
				//        show();
				//    }
				//});
			} catch (e) {
				show();
			}
		}).error(function () {
			show();
		});
	} catch (e) {
		show();
	}

}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//Create issues or todos 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
function imageListFormat(state) {
	if (!state.id) {
		return state.text;
	}
	var $state = $('<span><img style="max-width:32;max-height:32px"  src="' + $(state.element).data("img") + '" class="img-flag" /> ' + state.text + '</span>');
	return $state;
};

$("body").on("click", ".issuesModal:not(.disabled)", function () {
	var dat = $(this).data();
	var parm = $.param(dat);
	var m = $(this).data("method");
	if (!m)
		m = "Modal";
	var title = dat.title || "Add an issue";
	showModal(title, "/Issues/" + m + "?" + parm, "/Issues/" + m);
});

$("body").on("click", ".todoModal:not(.disabled)", function () {
	var dat = $(this).data();
	var parm = $.param(dat);
	var m = $(this).data("method");
	if (!m)
		m = "Modal";
	var title = dat.title || "Add a to-do";
	showModal(title, "/Todo/" + m + "?" + parm, "/Todo/" + m, null, function () {
		debugger;
		var found = $('#modalBody').find(".select-user");
		if (found.length && found.val() == null)
			return "You must select at least one to-do owner.";
		return true;
	});
});
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
function getParameterByName(name, url) {
	if (!url) url = window.location.href;
	name = name.replace(/[\[\]]/g, "\\$&");
	var regex = new RegExp("[?&]" + name + "(=([^&#]*)|&|#|$)"),
        results = regex.exec(url);
	if (!results) return null;
	if (!results[2]) return '';
	return decodeURIComponent(results[2].replace(/\+/g, " "));
}


jQuery.cachedScript = function (url, options) {
	options = $.extend(options || {}, {
		dataType: "script",
		cache: true,
		url: url
	});
	return jQuery.ajax(options);
};

var tname = "";
var tmethod = ""
function startTour(name, method) {
	if (typeof (method) === "undefined" || method == null || (typeof (method) === "string" && method.trim() == ""))
		method = "start";
	$.getScript("/Scripts/Tour/lib/anno.js").done(function () {

		//ensureLoaded("/Scripts/Tour/lib/jquery.scrollintoview.min.js");
		//ensureLoaded("/Content/Tour/lib/anno.css");
		if (typeof (Tours) === "undefined") {
			Tours = {
				NextButton: function () {
					return {
						text: "Next1",
						click: function (a, e) {
							var anno = a;
							$(anno.target).click();
							e.preventDefault();
						}
					};
				},
				clickToAdvance: function (anno) {

					var existingShow = anno.onShow;
					var existingHide = anno.onHide;

					anno.onShow = function (anno, $target, $annoElem) {
						var an = anno;
						if (typeof (existingShow) !== "undefined")
							existingShow(anno, $target, $annoElem);
						var handler = function (e) {
							console.log("c2a handler");
							if (anno._chainNext != null) {
								waitUntil(function () { return $(anno._chainNext.target).length > 0; }, function () {
									setTimeout(function () {
										if (typeof (anno.action) === "function")
											anno.action();
										an.switchToChainNext();
									}, 250);
								}, function () {
									showAlert("Could not load tour.");
								});
							} else {
								if (typeof (anno.action) === "function")
									anno.action();
								an.switchToChainNext();
							}
						}

						$target[0].addEventListener('click', handler, true) // `true` is essential                   
						return handler
					};
					anno.onHide = function (anno, $target, $annoElem, handler) {
						if (typeof (existingHide) !== "undefined")
							existingHide(anno, $target, $annoElem);
						if ($target.length > 0) {
							$target[0].removeEventListener('click', handler, true);
						}
					}
					return anno;
				},
				appendParams: function (anno, selector, tourName, tourMethod) {
					var existingShow = anno.onShow;
					var existingHide = anno.onHide;

					anno.onShow = function (anno, $target, $annoElem) {
						if (typeof (existingShow) !== "undefined")
							existingShow(anno, $target, $annoElem);
						tname = tourName;
						tmethod = tourMethod;
						var handler = function (e) {
							if (typeof (e.target.href) !== "undefined" && $(e.target).is(selector)) {
								e.preventDefault();
								if (e.target.href.indexOf("?") != -1) window.location.href = e.target.href + '&tname=' + tourName + "&tmethod=" + tourMethod;
								else window.location.href = e.target.href + '?tname=' + tourName + "&tmethod=" + tourMethod;
							}
							if (typeof (e.target.onclick) !== "undefined" && $(e.target).is(selector)) {
								e.preventDefault();
								var str = "" + e.target.onclick;
								var findQ = "'";
								var idx = str.indexOf("location.href='");
								if (idx == -1) {
									idx = str.indexOf('location.href="');
									var findQ = '"';
								}
								if (idx != -1) {
									idx += 14;
									var endQ = str.indexOf(findQ, idx + 1);
									var query = (str.substr(idx, endQ - idx).indexOf("?") == -1) ? "?" : "&";
									str = str.substr(0, endQ) + query + 'tname=' + tourName + "&tmethod=" + tourMethod + str.substr(endQ);
									e.target.onclick = eval(str);
								}
							}
						}
						$target[0].addEventListener('click', handler, true) // `true` is essential
						return handler
					};

					anno.onHide = function (anno, $target, $annoElem, handler) {
						if (typeof (existingHide) !== "undefined")
							existingHide(anno, $target, $annoElem, handler);
						$target[0].removeEventListener('click', handler, true);
						tname = "";
						tmethod = "";
					}
					return anno;
				}
			}
		};
		try {
			Anno.prototype.overlayClick = function () {
				console.log("overlay clicked");
			};
			$.getScript("/Scripts/Tour/" + name + ".js").done(function () { Tours[name][method](); });
		} catch (e) {
			showAlert("Tour could not be loaded.");
		}
	});
}

function shouldBeginTour() {
	var tourName = getParameterByName("tname");
	if (typeof (tourName) !== "undefined" && tourName != null && (typeof (tourName) === "string" && tourName.trim() != "")) {
		startTour(tourName, getParameterByName("tmethod"));
	}
}
shouldBeginTour();


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

//Debounce
(function (n, t) { var $ = n.jQuery || n.Cowboy || (n.Cowboy = {}), i; $.throttle = i = function (n, i, r, u) { function o() { function o() { e = +new Date; r.apply(h, c) } function l() { f = t } var h = this, s = +new Date - e, c = arguments; u && !f && o(); f && clearTimeout(f); u === t && s > n ? o() : i !== !0 && (f = setTimeout(u ? l : o, u === t ? n - s : n)) } var f, e = 0; return typeof i != "boolean" && (u = r, r = i, i = t), $.guid && (o.guid = r.guid = r.guid || $.guid++), o }; $.debounce = function (n, r, u) { return u === t ? i(n, r, !1) : i(n, u, r !== !1) } })(this);


///POLYFILLS
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