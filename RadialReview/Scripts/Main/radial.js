﻿
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
	str = str.replace(/"/g, "&quot;");
	str = str.replace(/'/g, "&#39;");
	return str;
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
	}

})(jQuery);

function qtip() {
	$('[title]').qtip({
		position: {
			my: 'bottom left',  // Position my top left...
			at: 'top center', // at the bottom right of...
			target: 'mouse'
		}
	});

	/*$('td').hover(function() {
        var index = $(this).index();
        var f = $(this).closest('table');
        if (f) {
            var found = f.find("th:nth-child(" + (index) + ")");
            if (found) {

                found.qtip('show');
                found.data('visible', true);
            }
        }
    }, function () {

    });*/

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
	/*$(".startHidden").each(function (i, e) {
        setTimeout(function () {
            $(e).addClass("unhide");
        }, speed * i);
    });*/
}

function ForceHide() {
	$(".startHidden")
        .removeClass("startHidden")
        .removeClass("unhide")
        .addClass("startHidden");
}

function refresh() {
	location.reload();
}


function showModal(title, pullUrl, pushUrl, callback, validation, onSuccess) {
	$("#modalMessage").html("");
	$("#modalMessage").addClass("hidden");
	$.ajax({
		url: pullUrl,
		type: "GET",
		//Couldnt retrieve modal partial view
		error: function (jqxhr, status, error) {
			$("#modalForm").unbind('submit');
			if (status == "timeout")
				showAlert("The request has timed out. If the problem persists, please contact us.");
			else
				showAlert("Something went wrong. If the problem persists, please contact us.");
		},
		//Retrieved Partial Modal
		success: function (modal) {
			if (!modal) {
				showAlert("Something went wrong. If the problem persists, please contact us.");
				return;
			}
			$('#modalBody').html(modal);
			$("#modalTitle").html(title);
			//Reregister submit button
			$("#modalForm").unbind('submit');

			$("#modalForm").submit(function (e) {
				e.preventDefault();
				var serialized = $("#modalForm").serialize();

				if (validation) {
					var message = eval(validation + '()');
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
				$.ajax({
					url: pushUrl,
					type: "POST",
					data: serialized,
					success: function (data, status, jqxhr) {
						if (!data) {
							$("#modal").modal("hide");
							showAlert("Something went wrong. If the problem persists, please contact us.");
						} else {
							//StoreJsonAlert(data);
							if (onSuccess) {
								if (typeof onSuccess === "string") {
									eval(onSuccess + "(data)");
								} else if (typeof onSuccess === "function") {
									onSuccess(data);
								}
								//$("#modal").modal("hide");
							}else {
								/*if (data.Error) {
									//console.log(data.Trace);
									//console.log(data.Message);
									//if (!data.ForceNoShow)
									showJsonAlert(data);
								} else {
									//$("#modal").modal("hide");
									if (data.Refresh)
										location.reload();
								}*/
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
					}
				});
			});
			$('#modal').modal('show');
			var count = 0;
			setTimeout(function () {
				if (callback) {
					eval(callback + '()');
				} else {
					$('#modal input:not([type=hidden]):not(.disable):first').focus();
				}
			}, 550);
		}
	});
}

function UnstoreJsonAlert() {
	clearAlerts();
	var data = localStorage.getItem("Alert");
	localStorage.setItem("Alert", null);

	var alert = JSON.parse(data);
	if (alert !== undefined && alert != null && alert != "null") {
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

function showAlert(message, alertType, preface) {
	if (alertType === undefined)
		alertType = "alert-danger";
	if (preface === undefined)
		preface = "Warning!";
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

function showJsonAlert(data, showSuccess, clearOthers) {
	try {
		if (clearOthers) {
			clearAlerts();
		}

		if (!data) {
			showAlert("Something went wrong. If the problem persists, please contact us.");
			debugger;
		} else {
			var message = data.Message;
			if (message === undefined)
				message = "";
			console.log(data.Trace);
			console.log(data.Message);
			if (data.MessageType != "Success" || showSuccess) {
				showAlert(message, "alert-" + data.MessageType.toLowerCase(), data.Heading);
			}
			if (data.Error) {
				debugger;
			}

		}
	} catch (e) {
		Console.log(e);
	}
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
	});


};
/////////////////////////////////////////////////////////////////
//Ajax Interceptors

var interceptAjax = function(event, request, settings) {
	console.log(event);
	console.log(settings);
	try {
		var result = $.parseJSON(request.responseText);
		if (result.Refresh) {
			if (!result.Silent) {
				result.Refresh = false;
				StoreJsonAlert(result);
			}
			location.reload();
		} else {
			if (!result.Silent) {
				showJsonAlert(result, true, true);
			}
		}
	} catch (e) {
		console.log(e);
	}
};

$(document).ajaxSuccess(interceptAjax);
$(document).ajaxError(interceptAjax);

/////////////////////////////////////////////////////////////////

(function ($) {
	$.fn.focusTextToEnd = function () {
		this.focus();
		var $thisVal = this.val();
		this.val('').val($thisVal);
		return this;
	};
}(jQuery));
