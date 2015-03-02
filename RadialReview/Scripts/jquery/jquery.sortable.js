/*
 * HTML5 Sortable jQuery Plugin
 * http://farhadi.ir/projects/html5sortable
 * 
 * Copyright 2012, Ali Farhadi
 * Released under the MIT license.
 */
(function($) {
var dragging, placeholders = $();
$.fn.sortable = function(options) {
	var method = String(options);
	var 
	options = $.extend({
		connectWith: false
	}, options);
	return this.each(function() {
		var parent3 = $(this);
		if (/^enable|disable|destroy$/.test(method)) {
			var items = $(this).children($(this).data('items')).attr('draggable', method == 'enable');
			if (method == 'destroy') {
				items.add(this).removeData('connectWith items')
					.off('dragstart.h5s dragend.h5s selectstart.h5s dragover.h5s dragenter.h5s drop.h5s');
			}
			return;
		}
		var isHandle, index, items = $(this).children(options.items);
		var placeholder = $('<' + (/^ul|ol$/i.test(this.tagName) ? 'li' : 'div') + ' class="sortable-placeholder">');
		items.find(options.handle).mousedown(function() {
			isHandle = true;
		}).mouseup(function() {
			isHandle = false;
		});
		$(this).data('items', options.items)
		placeholders = placeholders.add(placeholder);
		if (options.connectWith) {
			$(options.connectWith).add(this).data('connectWith', options.connectWith);
		}
		items.attr('draggable', 'true').on('dragstart.h5s', function(e) {
			if (options.handle && !isHandle) {
				return false;
			}
			isHandle = false;
			var dt = e.originalEvent.dataTransfer;
			dt.effectAllowed = 'move';
			dt.setData('Text', 'dummy');
			index = (dragging = $(this)).addClass('sortable-dragging').index();
		}).on('dragend.h5s', function() {
			try {
				//dragging.removeClass('sortable-dragging').show();
				dragging.removeClass('sortable-dragging').css("opacity",1);
			} catch(ex){
				
			}
			placeholders.detach();
			if (index != dragging.index()) {
				items.parent().trigger('sortupdate', {item: dragging});
			}
			dragging = null;
		}).not('a[href], img').on('selectstart.h5s', function() {
			this.dragDrop && this.dragDrop();
			return false;
		//}).end().add([this, placeholder]).on('dragover.h5s dragenter.h5s drop.h5s', function(e) {
		}).end().add([this, placeholder]).on('dragleave.h5s dragenter.h5s drop.h5s', function(e) {
			var dd = dragging;
			if (!items.is(dd) && options.connectWith !== $(dd).parent().data('connectWith')) {
				return true;
			}
			var allowDrop = false;
			if (e.type=="dragenter" || e.type=="dragover") {
				
				var a = $(this).closest("ol,ul");
				var b = $(e.target).closest("ol,ul");
				if (!a.is(b))
					allowDrop=true;

			}
			if (e.type == 'drop') {
				e.stopPropagation();
				placeholders.filter(':visible').after(dd);
				return false;
			}
			e.preventDefault();
			e.originalEvent.dataTransfer.dropEffect = 'move';
			if (items.is(this)) {
				var p = $(this).offset();
				var o = e.originalEvent;

				var diff = o.y - p.top;
				var h = $(this).height();

				if (e.type == "dragenter" && !allowDrop)
					return false;


				if (diff > h || true ) {
					if (dd != null && options.forcePlaceholderSize) {
						placeholder.height(dd.outerHeight());
					}
					if (!allowDrop) {
						dd.css("opacity", .5);
						//dd.hide();
					}


					$(this)[placeholder.index() < $(this).index() ? 'after' : 'before'](placeholder);
					placeholders.not(placeholder).detach();
				}
			} else if (!placeholders.is(this) && !$(this).children(options.items).length) {
				if (e.type == "dragenter")
					debugger;
				placeholders.detach();
				$(this).append(placeholder);
			}
			return false;
		});
	});
};
})(jQuery);
