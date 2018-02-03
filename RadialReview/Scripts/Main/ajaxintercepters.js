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
var requesterId = generateGuid();
$(document).ajaxSend(function (event, jqX, ajaxOptions) {
	if (ajaxOptions.url == null) {
		ajaxOptions.url = "";
	}
	if (typeof (ajaxOptions.data) === "string" && ajaxOptions.data.indexOf("_clientTimestamp") != -1) {
		return;
	}
	if (ajaxOptions.url.indexOf("_clientTimestamp") == -1) {
		ajaxOptions.url = Time.addTimestamp(ajaxOptions.url);
	}
	if (ajaxOptions.url.indexOf("_rid") == -1) {
	    if (ajaxOptions.url.indexOf("?") == -1) {
	        ajaxOptions.url += "?nil=0";
	    }
	    ajaxOptions.url += "&_rid=" + requesterId;
	}
	console.info(ajaxOptions.type + " " + ajaxOptions.url);
	if (typeof (ajaxOptions.type) === "string" && ajaxOptions.type.toUpperCase() == "POST" && !(ajaxOptions.url.indexOf("/support/email") == 0)) {
		console.info(ajaxOptions.data);
	}
});
/////////////////////////////////////////////////////////////////