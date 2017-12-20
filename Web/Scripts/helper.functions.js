$(function () {
	$.ajaxSetup({ cache: false });
	
	$(document).click(hidePopups);
	$('.gbgt').click(function (e) {
		e.stopPropagation();
		hidePopups();
        $(this).parent().addClass('gbto');
        $(this).next().css({ "visibility": "visible" });
    });
	
	$.fn.editableContainer.defaults.onblur = 'ignore';
	$.fn.datepicker.defaults.format = 'yyyy-mm-dd';

	$("body").delegate(".input-group.date", "focusin", function(){
		if($(this).data("orientation")) {
			$(this).datepicker({ language: 'es', format: 'yyyy-mm-dd', orientation: $(this).data("orientation") });
		} else {
			$(this).datepicker({ language: 'es', format: 'yyyy-mm-dd' });
		}
	});
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
				$modal = $('<div class="modal fade" tabindex="-1" data-backdrop="static"><div class="modal-dialog"><div class="modal-content"></div></div></div>');
				$modal.attr('id', $(this).attr('data-modal-id'));
				$modal.find(".modal-content").attr('id', $(this).attr('data-modal-id') + "-content");
			}

			$modal.on('shown.bs.modal', function () {
  				$modal.find('input:visible:not([readonly="readonly"]):first').focus();
  				$modal.off('shown.bs.modal');
			});

			$modal.find(".modal-content").load(url, "", function(){
      			$modal.modal('show');
    		});
		}
	});
}
/* jQuery Plugins */
(function( $ ) {
    $.fn.loadUrl = function() {
        var url = this.data('url');
        if(url) {
            this.load(url);
        }
        return this;
    };
}(jQuery));