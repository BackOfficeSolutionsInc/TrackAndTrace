function tweenValue(that,e) {
    that.textContent = that.textContent || "" + s;
    var i = d3.interpolate(that.textContent, e),
        prec = (e + "").split("."),
        round = (prec.length > 1) ? Math.pow(10, prec[1].length) : 1;

    return function (t) {
        this.textContent = Math.round(i(t) * round) / round;
    };
}