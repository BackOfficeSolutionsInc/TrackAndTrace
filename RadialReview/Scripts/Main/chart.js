jQuery.fn.appendGraph = function (options) {
    var id = ("chart-" + Math.random());
    var chart = $("<div id=" + id + ">");

    chart.addClass("metric-graphic");
    var o = $(this[0]);
    o.append(chart)

    var defaultOptions = {
        title: "",
        data: [],
        target: chart[0],
        interpolate: d3.curveLinear,
        center_title_full_width: true
        //id :"#"+id,
    };

    options = $.extend({},defaultOptions, options);

    if (typeof (options.width) === "undefined" && typeof (options.full_width) === "undefined") {       
        options.full_width = true;//$(o).parent().width();
    }
    if (typeof (options.height) === "undefined" && typeof (options.full_height) === "undefined") {
        //options.height = $(o).parent().height();
        options.full_width = true;
    }

    function conditionOptions(obj) {
        for (var k in obj) {
            if (obj.hasOwnProperty(k)) {
                var x = obj[k];
                if (typeof (x) === "undefined" || x === null) {
                    //debugger;
                    delete obj[k];
                }
            }
        }

        if (typeof (obj.data) !== "undefined") {
            obj.data = obj.data.map(function (x) {
                return x.map(function (y) {
                    y.date = Time.parseJsonDate(y.date);
                    return y;
                });
            });
        }
    }

    $.getScript("/Scripts/Charts/metrics-graphics.js").done(function () {
        if (typeof (options.url) !== "undefined") {
            $.ajax({
                url: options.url,
                success: function (obj) {                                      
                    options = $.extend({}, options, data);
                    conditionOptions(options);  
                    MG.data_graphic(options);
                }
            });
        } else {
            conditionOptions(options);
            MG.data_graphic(options);
        }
    });

    return chart; // This is needed so others can keep chaining off of this
};