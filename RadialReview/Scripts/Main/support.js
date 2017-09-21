
function sendErrorReport() {
    try {
        console.log("Sending Error Report...");
        var message = "[";
        var mArray = [];
        for (var i in consoleStore) {
            if (arrayHasOwnIndex(consoleStore, i)) {
                try {
                    mArray.push(JSON.stringify(consoleStore[i]));
                } catch (e) {
                    mArray.push("---missing line--- " + e);
                }
            }
        }
        message = "[" + mArray.join(",\n") + "]";
        function _send() {
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

            try {
                if (JSON.stringify(data).length > 2097152) {
                    data.ImageData = null;
                }
                if (JSON.stringify(data).length > 2097152) {
                    data.Console = "[]";
                }
            } catch (e) {
                console.error("Error removing data from report.");
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
                    console.error(b, c);
                }
            });
        }
        try {
            $.getScript("/Scripts/home/screenshot.js").done(function () {
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

function supportEmail(title, nil, defaultSubject, defaultBody) {

    function validateEmail(email) {
        var re = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
        return re.test(email);
    }

    var message = "[";
    var mArray = [];
    for (var i in consoleStore) {
        if (arrayHasOwnIndex(consoleStore, i)) {
            try {
                mArray.push(JSON.stringify(consoleStore[i]));
            } catch (e) {
                mArray.push("SupportEmailGenerationError:" + e);
            }
        }
    }
    message = "[" + mArray.join(",\n") + "]";
    var fields = [
            { name: "Subject", text: "Subject", type: "text", value: defaultSubject },
            { name: "Body", text: "Body", type: "textarea", value: defaultBody }
    ];
    var validater = undefined;
    if (typeof (window.UserId) === "undefined" || window.UserId == -1) {
        validater = function (data) {
            if (!validateEmail(data.Email)) {
                return "Please supply a valid email so we know how to reach you.";
            }
        };
        fields.push({ name: "Email", text: "Email", type: "text", placeholder: "Your e-mail here" });
    }
    var image = null;
    var show = function () {
        showModal({
            title: "How can we help you?",
            icon: "default",//{ icon: "modal-icon-default", title: "Contact Support", color: "#ef7622" },
            fields: fields,
            pushUrl: "/support/email",
            validation:validater,
            reformat: function (data) {
                data.Console = message;
                data.Url = window.location.href;
                data.User = window.UserId;
                data.Org = window.OrgId;
                data.PageTitle = title;
                
                if (image != null) {
                    data.ImageData = image;
                }

                try{
                    if (JSON.stringify(data).length > 2097152) {
                        data.ImageData = null;
                    }
                    if (JSON.stringify(data).length > 2097152) {
                        data.Console = "[]";
                    }
                } catch (e) {
                    console.error("Error removing data from report.");
                }

            }
        });
    };
    try {
        $.getScript("/Scripts/home/screenshot.js").done(function () {
            try {
                console.log("begin render");
                screenshotPage(function (res) {
                    image = res;
                    console.log("end render");
                });
                show();
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

