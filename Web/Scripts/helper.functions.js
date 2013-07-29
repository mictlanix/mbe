$(function () {
	$.ajaxSetup({ cache: false });
	
	$(document).click(hidePopups);
	$('.gbgt').click(function (e) {
		e.stopPropagation();
		hidePopups();
        $(this).parent().addClass('gbto');
        $(this).next().css({ "visibility": "visible" });
    });
	
    $('input.date').datepicker({ language: 'es', format: 'yyyy-mm-dd' });
});
function onSearchBegin() {
    $("#search-results").html(null);
}
function hidePopups() {
	$('.gbgt').each(function () {
        $(this).parent().removeClass('gbto');
        $(this).next().css({ "visibility": "hidden" });
  	});
}
function bindModal(selector) {
	$(selector).off('click');
	$(selector).click(function(e) {
		e.preventDefault();
		var url = $(this).attr('href');
		
		if(url.indexOf('#') == 0) {
			$(url).modal('open');
		} else {
			var $modal = $('#' + $(this).attr('data-modal-id'));
		
			if($modal.length == 0) {
				$modal = $('<div></div>');
				$modal.attr('id', $(this).attr('data-modal-id'))
					  .addClass('modal hide fade')
					  .attr('tabindex', -1)
					  .css('width', 666);
			}
			
			$modal.load(url, '', function(){
				$modal.on('shown', function () {
					$modal.find('input.date').datepicker({ language: 'es', format: 'yyyy-mm-dd' });
      				$modal.find('input:visible:not([readonly="readonly"]):first').focus();
      				$modal.off('shown');
				});
      			$modal.modal();
    		});
		}
	});
}
/* jQuery Plugins */
(function( $ ) {
    $.fn.loadUrl = function() {
    	this.load(this.data('url'));
        return this;
    };
}(jQuery));