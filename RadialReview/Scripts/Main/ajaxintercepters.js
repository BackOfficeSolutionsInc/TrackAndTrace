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

		ajaxOptions.url += "_clientTimestamp=" + ((+new Date()) /*+ (window.tzoffset * 60 * 1000)*/);
	}
	console.info(ajaxOptions.type + " " + ajaxOptions.url);
	if (typeof (ajaxOptions.type) === "string" && ajaxOptions.type.toUpperCase() == "POST" && !(ajaxOptions.url.indexOf("/support/email") == 0)) {
		console.info(ajaxOptions.data);
	}
});
/////////////////////////////////////////////////////////////////