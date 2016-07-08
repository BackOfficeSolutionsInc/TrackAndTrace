
function screenshotPage(callback) {
    var blobToDataURL  =function (blob, callback) {
        var a = new FileReader();
        a.onload = function (e) { callback(e.target.result); }
        a.readAsDataURL(blob);
    }

    var urlsToAbsolute = function (nodeList) {
        if (!nodeList.length) {
            return [];
        }
        var attrName = 'href';
        if (nodeList[0].__proto__ === HTMLImageElement.prototype || nodeList[0].__proto__ === HTMLScriptElement.prototype) {
            attrName = 'src';
        }
        nodeList = [].map.call(nodeList, function (el, i) {
            var attr = el.getAttribute(attrName);
            if (!attr) {
                return;
            }
            var absURL = /^(https?|data):/i.test(attr);
            if (absURL) {
                return el;
            } else {
                return el;
            }
        });
        return nodeList;
    }
    var addOnPageLoad_ = function () {
        window.addEventListener('DOMContentLoaded', function (e) {
            var scrollX = document.documentElement.dataset.scrollX || 0;
            var scrollY = document.documentElement.dataset.scrollY || 0;
            window.scrollTo(scrollX, scrollY);
        });
    }
    urlsToAbsolute(document.images);
    urlsToAbsolute(document.querySelectorAll("link[rel='stylesheet']"));
    var screenshot = document.documentElement.cloneNode(true);
    var b = document.createElement('base');
    b.href = document.location.protocol + '//' + location.host;
    var head = screenshot.querySelector('head');
    head.insertBefore(b, head.firstChild);
    //screenshot.style.pointerEvents = 'none';
    screenshot.style.overflow = 'hidden';
    screenshot.style.webkitUserSelect = 'none';
    screenshot.style.mozUserSelect = 'none';
    screenshot.style.msUserSelect = 'none';
    screenshot.style.oUserSelect = 'none';
    screenshot.style.userSelect = 'none';
    screenshot.dataset.scrollX = window.scrollX;
    screenshot.dataset.scrollY = window.scrollY;
    var script = document.createElement('script');
    script.textContent = '(' + addOnPageLoad_.toString() + ')();';
    screenshot.querySelector('body').appendChild(script);
    var blob = new Blob([screenshot.outerHTML], {
        type: 'text/html'
    });
    blobToDataURL(blob, callback);
    //return blob;
}


//function generateScreenshot() {
//    window.URL = window.URL || window.webkitURL;
//    window.open(window.URL.createObjectURL(screenshotPage()));
//}
//exports.screenshotPage = screenshotPage;
//exports.generate = generate;