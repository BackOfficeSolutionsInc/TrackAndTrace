
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

function ForceHide() {$(".startHidden").removeClass("startHidden").removeClass("unhide").addClass("startHidden");}

function refresh() {location.reload();}

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


function showModal(title, pullUrl, pushUrl, callback, validation, onSuccess, onCancel) {

    $("#modal-icon").attr("class", "");
    $("#modal #class-container").attr("class", "");

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
///          type: <text,textarea,checkbox,radio,span,header,h1,h2,h3,h4,h5,h6,number,date,time,file>(optional)                 ///
///          value: (optional)                                                                                                  ///
///          placeholder: (optional)                                                                                            ///
///      },...],                                                                                                                ///
///      pushUrl:"",                                                                                                            ///
///      success:function,                                                                                                      ///
///      cancel:function,                                                                                                       ///
///  }                                                                                                                          ///
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
*/
function showModalObject(obj,pushUrl,onSuccess,onCancel) {
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
        if (iconType==="string")
            obj.modalClass += " modal-icon-" + obj.icon;
        else if (iconType === "object") {
            var time = +new Date();
            var custom = "modal-icon-custom" + time;
            obj.modalClass += " " + custom;
            if (!obj.icon.icon)
                obj.modalClass += " modal-icon-info";
            var icon = (obj.icon.icon || "").replace(".", "");
            var title = escapeString(obj.icon.title || "Hey!");
            var color = escapeString(obj.icon.color || "#5bc0de");
            $("#modal-icon").addClass(icon);
            icon=icon.replace(" ",".")
            document.styleSheets[0].addRule("." + custom + " ." + icon + ":after", 'content: "' + title + '";');
            document.styleSheets[0].addRule("." + custom + " ." + icon + ":before", 'background-color: ' + color + ';');
            document.styleSheets[0].addRule("." + custom + " #modalOk", 'background-color: ' + color + ';');
        }

    }

    $("#modal #class-container").attr("class",obj.modalClass);

    $("#modalMessage").html("");
    $("#modalMessage").addClass("hidden");
    $("#modal").addClass("loading");
    $('#modal').modal('show');

    var allowed = ["text", "hidden", "textarea", "checkbox", "radio", "number", "date", "time", "header", "span", "h1", "h2", "h3", "h4", "h5", "h6", "file"];
    var addLabel = ["text", "textarea", "checkbox", "radio", "number", "date", "time","file"];
    var tags = ["span","h1", "h2", "h3", "h4", "h5", "h6"];
    var anyFields = ""
    if (typeof (obj.field) !== "undefined") {
        if (typeof (obj.fields) !== "undefined") {
            throw "A 'field' and a 'fields' property exists";
        } else {
            obj.fields = obj.field;
        }
    }

    if (typeof (obj.fields) === "object"){
        var allDeep =true;
        for(var f in obj.fields){
            if (typeof (obj.fields[f]) !== "object"){
                allDeep=false;
                break;
            }
        }
        if (!allDeep){
            obj.fields = [obj.fields];
        }
    }
    
    var fieldsTypeIsArray = Array.isArray(obj.fields);//typeof (obj.fields);
    
    var contentType = null;

    var builder = '<div class="form-horizontal modal-builder">';
    for (var f in obj.fields) {
        try{
            var field = obj.fields[f];
            var name = field.name||f;
            var label = typeof (field.text) !== "undefined" || !fieldsTypeIsArray;
            var text = field.text || name;
            var value = field.value||"";
            var placeholder = field.placeholder;
            var type = (field.type || "text").toLowerCase();

            if (type == "header")
                type = "h4";

            if (typeof (placeholder) !== "undefined")
                placeholder = "placeholder='" + placeholder + "'";
            else placeholder = "";
            var input = "";
            var inputIndex = allowed.indexOf(type);
            if (inputIndex == -1){
                console.warn("Input type not allowed:" + type);
                continue;
            }
            if (Object.prototype.toString.call(value) === '[object Date]' && type=="date") {
                value = value.toISOString().substring(0, 10);
            }

            if (type == "file")
                contentType = 'enctype="multipart/form-data"';

            if (tags.indexOf(type)!=-1) {
                input = "<" + type + " name=" + escapeString(name) + '" id="' + escapeString(name) + '">' + value + '</' + type + '>';
            }else if (type == "textarea") {
                input = '<textarea class="form-control blend verticalOnly" rows=5 name="' + escapeString(name) + '" id="' + escapeString(name) + '" ' + escapeString(placeholder) + '>' + value + '</textarea>';
            } else {
                input = '<input type="' + escapeString(type) + '" class="form-control blend" name="' + escapeString(name) + '" id="' + escapeString(name) + '" ' + escapeString(placeholder) + ' value="' + escapeString(value) + '"/>';
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

    $("#modalForm").submit(function (ev) {
        ev.preventDefault();

        var formData = $("#modalForm").serializeObject();

        if (typeof (reformatArg) === "function") {
            var o = reformatArg(formData);
            if (typeof (o) !== "undefined" && o!=null)
                formData = o;
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

function _submitModal(formData, pushUrl, onSuccess,useJson, contentType) {
    ///FORM DATA IS NOT USED
    ///TODO use form data;
    var serialized
    //var serialized = $.param(formData);
    //var contentType = null;

    if (typeof (contentType) === "undefined")
        contentType = null;
    var processData = null;
    if (useJson && contentType==null) {
        serialized = JSON.stringify(formData);
        contentType = "application/json; charset=utf-8";
    } else if (contentType == 'enctype="multipart/form-data"') {
        serialized = new FormData($('#modalForm')[0]);
        processData = false;
        contentType = false;
    }else{
        serialized = $("#modalForm").serialize();
        contentType = contentType || "application/x-www-form-urlencoded";
    }
    var onSuccessArg = onSuccess;

    $.ajax({
        url: pushUrl,
        type: "POST",
        contentType: contentType,
        data: serialized,// JSON.stringify(formData),
        processData:processData,
        success: function (data, status, jqxhr) {
            if (!data) {
                $("#modal").modal("hide");
                $("#modal").removeClass("loading");
                showAlert("Something went wrong. If the problem persists, please contact us.");
            } else {
                if (onSuccessArg) {
                    if (typeof onSuccessArg === "string") {
                        eval(onSuccessArg + "(data)");
                    } else if (typeof onSuccessArg === "function") {
                        onSuccessArg(data);
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

            initials = getInitials(name, initials);
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


//Debounce
(function (n, t) { var $ = n.jQuery || n.Cowboy || (n.Cowboy = {}), i; $.throttle = i = function (n, i, r, u) { function o() { function o() { e = +new Date; r.apply(h, c) } function l() { f = t } var h = this, s = +new Date - e, c = arguments; u && !f && o(); f && clearTimeout(f); u === t && s > n ? o() : i !== !0 && (f = setTimeout(u ? l : o, u === t ? n - s : n)) } var f, e = 0; return typeof i != "boolean" && (u = r, r = i, i = t), $.guid && (o.guid = r.guid = r.guid || $.guid++), o }; $.debounce = function (n, r, u) { return u === t ? i(n, r, !1) : i(n, u, r !== !1) } })(this);
