


function circle(x, y, name, r1, r2, n, spd, width, initialDelta, outerClss, clss) {
    var renderSpd = 50.0;
    spd *= renderSpd;

    $("#spinnerContainer").prepend("<div class='" + name +" "+outerClss+ " spinner' del='0' style='position:absolute;top:" + (y-r2) + "px;left:" + (x-r2) + "px;width:" + (r2 * 2) + "px;height:" + (r2 * 2) + "px;'></div>")
    for (var i = 0; i < n; i += 1) {
        var id = name +"_"+ i;
        $("."+name).prepend("<div class='" + id + " " + clss + " radial' del='0' style='position:absolute;top:"+(r2-width/2)+"px;left:"+(0)+"px;width:" + (r2 * 2) + "px;height:" + width + "px;'><div class='line' style='width:" + ((r2 - r1)) + "px;float:right;height:" + width + "px;'></div></div>");
        $('div.' + id).jqrotate(i * 360 / n + initialDelta);
    }

    setInterval(function () {
        var id = name;
        var del = parseFloat($('div.' + id).attr("del")) + spd / 100;
        $('div.' + id).jqrotate(i * 360 / n + initialDelta + del);
        $('div.' + id).attr("del", del);
        
    }, renderSpd);
}