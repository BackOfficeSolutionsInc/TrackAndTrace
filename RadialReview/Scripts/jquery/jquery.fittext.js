function fitToBox(box) {
    //box.style.fontSize = ;
    $(box).each(function (i,e) {
        var maxFontSize = 1000;
        var minFontSize = 0;

        inc = (maxFontSize - minFontSize) / 2;
        currentFontSize = minFontSize + inc;
        var _elm = $(e);
        _elm.css("font-size", currentFontSize + "%");
        var ww = _elm.width();
        var hh = _elm.height();
        
        while (inc >= 1) {
            if ((e.clientHeight < e.scrollHeight) || (e.clientWidth < e.scrollWidth)) {
                dir = -1;
            } else {
                dir = 1;
            }
            inc = Math.floor(inc / 2);
            currentFontSize += (dir * inc);
            _elm.css("font-size", currentFontSize + "%");
        }
    });
}