$(function () {
	$.fn.cTable = function(o) {

		this.wrap('<div class="cTContainer" />');
		this.wrap('<div class="relativeContainer" />');
		//Update below template as how you have it in orig table
		var origTableTmpl = '<table border="1" cellspacing="1" cellpadding="0" align="center" width="95%" ></table>';
		//get row 1 and clone it for creating sub tables
		var row1 = this.find('tr').slice(0, o.fRows).clone();

		var r1c1ColSpan = 0;
		for (var i = 0; i < o.fCols; i++) {
			r1c1ColSpan += this[0].rows[0].cells[i].colSpan;
		}

		//create table with just r1c1 which is fixed for both scrolls
		var tr1c1 = $(origTableTmpl);
		row1.each(function() {
			var tdct = 0;
			$(this).find('td').filter(function() {
				tdct += this.colSpan;
				return tdct > r1c1ColSpan;
			}).remove();
		});
		row1.appendTo(tr1c1);
		tr1c1.wrap('<div class="fixedTB" />');
		tr1c1.parent().prependTo(this.closest('.relativeContainer'));

		//create a table with just c1        
		var c1 = this.clone().prop({ 'id': '' });
		c1.find('tr').slice(0, o.fRows).remove();
		c1.find('tr').each(function(idx) {
			var c = 0;
			$(this).find('td').filter(function() {
				c += this.colSpan;
				return c > r1c1ColSpan;
			}).remove();
		});

		var prependRow = row1.first().clone();
		prependRow.find('td').empty();
		c1.prepend(prependRow).wrap('<div class="leftSBWrapper" />')
		c1.parent().wrap('<div class="leftContainer" />');
		c1.closest('.leftContainer').insertAfter('.fixedTB');

		//create table with just row 1 without col 1
		var r1 = $(origTableTmpl);
		row1 = this.find('tr').slice(0, o.fRows).clone();
		row1.each(function() {
			var tds = $(this).find('td'), tdct = 0;
			tds.filter(function() {
				tdct += this.colSpan;
				return tdct <= r1c1ColSpan;
			}).remove();
		});
		row1.appendTo(r1);
		r1.wrap('<div class="topSBWrapper" />')
		r1.parent().wrap('<div class="rightContainer" />')
		r1.closest('.rightContainer').appendTo('.relativeContainer');

		$('.relativeContainer').css({ 'width': 'auto', 'height': o.height });

		this.wrap('<div class="SBWrapper"> /')
		this.parent().appendTo('.rightContainer');
		this.prop({ 'width': o.width });

		var tw = 0;
		//set width and height of rendered tables
		for (var i = 0; i < o.fCols; i++) {
			tw += $(this[0].rows[0].cells[i]).outerWidth(true);
		}
		tr1c1.width(tw);
		c1.width(tw);

		$('.rightContainer').css('left', tr1c1.outerWidth(true));

		for (var i = 0; i < o.fRows; i++) {
			var tr1c1Ht = $(c1[0].rows[i]).outerHeight(true);
			var thisHt = $(this[0].rows[i]).outerHeight(true);
			var finHt = (tr1c1Ht > thisHt) ? tr1c1Ht : thisHt;
			$(tr1c1[0].rows[i]).height(finHt);
			$(r1[0].rows[i]).height(finHt);
		}
		$('.leftContainer').css({ 'top': tr1c1.outerHeight(true), 'width': tr1c1.outerWidth(true) });

		var rtw = $('.relativeContainer').width() - tw;
		$('.rightContainer').css({ 'width': rtw, 'height': o.height, 'max-width': o.width - tw });

		var trs = this.find('tr');
		trs.slice(1, o.fRows).remove();
		trs.slice(0, 1).find('td').empty();
		trs.each(function() {
			var c = 0;
			$(this).find('td').filter(function() {
				c += this.colSpan;
				return c <= r1c1ColSpan;
			}).remove();
		});

		r1.width(this.outerWidth(true));

		for (var i = 1; i < c1[0].rows.length; i++) {
			var c1Ht = $(c1[0].rows[i]).outerHeight(true);
			var thisHt = $(this[0].rows[i]).outerHeight(true);
			var finHt = (c1Ht > thisHt) ? c1Ht : thisHt;
			$(c1[0].rows[i]).height(finHt);
			$(this[0].rows[i]).height(finHt);
		}

		$('.SBWrapper').css({ 'height': $('.relativeContainer').height() - $('.topSBWrapper').height() });

		$('.SBWrapper').scroll(function() {
			var rc = $(this).closest('.relativeContainer');
			var lfW = rc.find('.leftSBWrapper');
			var tpW = rc.find('.topSBWrapper');

			lfW.css('top', ($(this).scrollTop() * -1));
			tpW.css('left', ($(this).scrollLeft() * -1));
		});

		$(window).resize(function() {
			$('.rightContainer').width(function() {
				return $(this).closest('.relativeContainer').outerWidth() - $(this).siblings('.leftContainer').outerWidth();
			});

		});
	};
});