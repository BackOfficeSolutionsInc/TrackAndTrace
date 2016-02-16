/*
 * jQuery autoResize (textarea auto-resizer)
 * @copyright James Padolsey http://james.padolsey.com
 * @version 1.04
 http://james.padolsey.com/demos/plugins/jQuery/autoresize.jquery.js
 */

(function ($) {

    $.fn.autoResize = function (options) {

        // Just some abstracted details,
        // to make plugin users happy:
        var settings = $.extend({
            
            onResize: function () { },
            animate: false,
            animateDuration: 150,
            animateCallback: function () { },
            extraSpace: 0,
            limit: 1000,
            useOriginalHeight:false
        }, options);

        this.destroyList = [];
        var self = this;
        var theClone=null;
        // Only textarea's auto-resize:
        this.filter('textarea').each(function () {
            // Get rid of scrollbars and disable WebKit resizing:
            var textarea = $(this).css({ resize: 'none', 'overflow-y': 'hidden' }),

                // Cache original height, for use later:
                origHeight = settings.useOriginalHeight?textarea.height():0,

                // Need clone of textarea, hidden off screen:

                clone = (function () {

                    // Properties which may effect space taken up by chracters:
                    var props = ['height', 'width', 'lineHeight', 'textDecoration', 'letterSpacing'],
                        propOb = {};

                    // Create object of styles to apply:
                    $.each(props, function (i, prop) {
                        propOb[prop] = textarea.css(prop);
                    });

                    var c=textarea.clone().removeAttr('id').removeAttr('name').css({
                        position: 'absolute',
                        top: 0,
                        left: -9999
                    }).css(propOb).attr('tabIndex', '-1').insertBefore(textarea);
                    if (theClone != null)
                        $(theClone).remove();
                    theClone = c;
                    // Clone the actual textarea removing unique properties
                    // and insert before original textarea:
                    self.destroyList.push(c);
                    return c;

                })(),
                lastScrollTop = null,
                updateSize = function () {
                    var props = ['height', 'width', 'lineHeight', 'textDecoration', 'letterSpacing'],
                        propOb = {};

                    // Create object of styles to apply:
                    $.each(props, function (i, prop) {
                        propOb[prop] = textarea.css(prop);
                    });
                    clone.css(propOb);
                    // Prepare the clone:
                    clone.height(0).val($(this).val()).scrollTop(10000);

                    // Find the height of text:
                    var scrollTop = Math.max(clone.scrollTop(), origHeight) + settings.extraSpace,
                        toChange = $(this).add(clone);

                    // Don't do anything if scrollTip hasen't changed:
                    if (lastScrollTop === scrollTop) { return; }
                    lastScrollTop = scrollTop;

                    // Check for limit:
                    if (scrollTop >= settings.limit) {
                        $(this).css('overflow-y', '');
                        return;
                    }
                    // Fire off callback:
                    settings.onResize.call(this);

                    // Either animate or directly apply height:
                    settings.animate && textarea.css('display') === 'block' ?
                        toChange.stop().animate({ height: scrollTop }, settings.animateDuration, settings.animateCallback)
                        : toChange.height(scrollTop);
                };

            // Bind namespaced handlers to appropriate events:

            //$(body).off(".dynSiz","")
            textarea
                .unbind('.dynSiz')
                .bind('keyup.dynSiz', updateSize)
                .bind('keydown.dynSiz', updateSize)
                .bind('change.dynSiz', updateSize);

            var resizeEnd = function () { updateSize.call(textarea); };

            var resizeTimeout;
            $(window).bind('resize.dynSiz', function () {
                clearTimeout(resizeTimeout);
                resizeTimeout = setTimeout(resizeEnd, 100);
            });

            setTimeout(function () {
                updateSize.call(textarea);
            }, 1);

        });

        this.destroy = function () {
            for (var i = 0; i < this.destroyList.length; i++) {
                debugger;
                $(this.destroyList[i]).remove();
            }
            this.destroyList=[]
        };

        // Chain:
        return this;

    };



})(jQuery);