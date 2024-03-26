function ProductFormatter(item) {
    var htmlPrice = '';
    var tagQuantity = '';

    if (typeof (item.price) != 'undefined') {
        htmlPrice = '&nbsp;<span>Precio: $' + item.price + '</span>';
    }

    if (typeof (item.quantity) != 'undefined') {
        tagQuantity = "&nbsp;<span>Cantidad:" + item.quantity + '</span>';
        tagQuantity = `&nbsp;<span style = "${item.quantity <= 0 ? 'color: red;' : ''} ">Cantidad: ${item.quantity}</span>`;
    }

   // var fmt = "<li title='" + item.name + "'>" +
   //             "<img style='float:left;max-width:50px;height:50px;' src='" + item.url + "' alt=''/>" +
   //             "<div style='margin:6px 0 0 52px;height:45px;'>" +
   //               "<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" + item.name + "</div>" +
   //               "<span>Código: " + item.code + " Modelo: " + item.model + " SKU: " + item.sku + "</span>" + htmlPrice +
   //                 tagQuantity +
   //             "</div>" +
		 //"</li>";

	 var fmt = `
					<li title = ${item.name}  style="${item.quantity > 0 && item.stockable ? '': 'color: #999999'}">
						<img style='float:left;max-width:50px;height:50px;' src='${item.url}' alt=''/>
						<div style='margin:6px 0 0 52px;height:45px;'>
   						<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>
						   ${item.name}
						</div>
							<span>
									${item.code === null ? '' : 'Código: ' + item.code}
									${item.model === null ? '' : 'Modelo: ' + item.model}
									${item.sku === null ? '' : 'SKU: ' + item.sku}
							</span>
									${htmlPrice} ${item.stockable ? tagQuantity: ""} Almacén: ${item.warehouse}
		             </div>
					</li>
				`;

    return fmt;
}

function ExpenseFormatter(item) {

	var desc = "";

	if (typeof (item.comment) != 'undefined' && item.comment !== null) {
		desc = "&nbsp<span style='font-weight:normal;'>Descripción: " + item.comment + "</span>";
	}

	
	var fmt = "<li title='" + item.name + "'> " +
						"<div style='margin:6px 0 0 52px;height:45px;' >"+
							"<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" +
								"<span>Nombre: " + item.name +"</span><br/>"+ desc +
							"</div>" + 
						"</div >"+
					"</li > ";

	return fmt;
}

function CustomerFormatter(item) {
	 
	var fmt = "<li title='" + item.name + "'>" +
		"<div style='margin:6px 0 0 52px;height:45px;'>" +
		"<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" + item.name + "</div>" +
		"<span>Código: " + item.code + "</span>" +
		"</div>" +
		"</li>";

	return fmt;
}

function DocumentFormatter(item) {
    var fmt = "<li title='" + item.stamp + "'>" +
                "<div style='margin:5px 0;height:38px;'>" +
                    "<div style='overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" +
                        "Folio Fiscal: " + item.stamp +
                    "</div>" +
                    "<span>Serie: " + item.batch + " Folio: " + item.serial + "</span>" +
                "</div>" +
              "</li>";
    
    return fmt;
}
