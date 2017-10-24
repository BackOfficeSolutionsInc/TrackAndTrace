var Time = new function () {
    var tzoffset = function () {
        if (!window.tzoffset) {
            var jan = new Date(new Date().getYear() + 1900, 0, 1, 2, 0, 0), jul = new Date(new Date().getYear() + 1900, 6, 1, 2, 0, 0);
            window.tzoffset = (jan.getTime() % 24 * 60 * 60 * 1000) >
						 (jul.getTime() % 24 * 60 * 60 * 1000)
						 ? jan.getTimezoneOffset() : jul.getTimezoneOffset();
        }
        return window.tzoffset;
    }

    this.tzoffset = tzoffset;

    this.addTimestamp = function (url) {
        tzoffset();
        var date = (+new Date());// - (window.tzoffset * 60 * 1000));
        return url + ((url.indexOf("?") != -1) ? "&_clientTimestamp=" + date : "?_clientTimestamp=" + date) + "&_tz=" + (-window.tzoffset);
    }

    this.toServerTime = function (date) {
        return new Date(date.getTime() + tzoffset() * 60 * 1000);
    }
}