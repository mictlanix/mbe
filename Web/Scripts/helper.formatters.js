function ProductFormatter(item) {
				var htmlPrice = typeof (item.price) != 'undefined' ?
								`&nbsp;<span>Precio:${item.price.toLocaleString('es-MX', { currency: 'MXN', style: 'currency' })}</span>` : `<span>Sin precio de lista</span>`;
				var tagQuantity = typeof (item.quantity) != 'undefined' ?
								`&nbsp;<span style = "${item.quantity <= 0 ? 'color: red;' : ''} ">Cantidad: ${item.quantity}</span>` : '';

				var existance = ``;
				var htmlWarehouse = typeof (item.warehouse) != null && item.stockable ? `Almacén: ${item.warehouse}` : ``;

				if (item.stockable) {
								if (item.quantity == null && item.warehouse == null) {
												existance = `<span>No disponible en ningún almacén</span>`;
								} else {
												existance = `${tagQuantity} ${htmlWarehouse}`;
								}
				}


				var fmt = `
					<li title = ${item.name}  style='${(item.quantity > 0 || !item.stockable) ? "" : "color: #CCCCCC"}'>
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
									${htmlPrice} ${existance}
		    </div>
					</li>
				`;

				return fmt;
}

function ProductQuotationFormatter(item) {
				var htmlPrice = '';
				var tagQuantity = '';

				console.log(item);

				if (typeof (item.price) != 'undefined') {
								htmlPrice = '&nbsp;<span>Precio: $' + item.price + '</span>';
				}

				if (typeof (item.quantity) != 'undefined') {
								//tagQuantity = "&nbsp;<span>Cantidad:" + item.quantity + '</span>';
								tagQuantity = `&nbsp;<span style = "${item.quantity <= 0 ? 'color: red;' : ''} ">Cantidad: ${item.quantity===null ? 'Sólo en Catálogo de Productos' : item.quantity }</span>`;
				}

				var fmt = `
					<li title = ${item.name}  style="${(item.quantity > 0 || !item.stockable) ? '' : 'color: #999999'}">
						<img style='float:left;max-width:50px;height:50px;' src='${item.url}' alt=''/>
						<div style='margin:6px 0 0 52px;height:45px;'>
   							<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>
							   ${item.name}
							</div>
							<span>
									${item.code === null ? '' : ` <div style="width:250px; display:inline-block"> Código: ${item.code} </div>` }
									${item.model === null ? '' : ` <div style="width:200px; display:inline-block"> Modelo: ${item.model} </div>`}
									${item.sku === null ? '' : ` <div style="width:200px; display:inline-block">SKU: ${item.sku} </div>` }			
							</span>
									${htmlPrice} ${item.stockable ? tagQuantity : ""}
						</div>
					</li>
				`;
				console.log("Listo el formato....");
				return fmt;
}

function ExpenseFormatter(item) {

				var desc = "";

				if (typeof (item.comment) != 'undefined' && item.comment !== null) {
								desc = "&nbsp<span style='font-weight:normal;'>Descripción: " + item.comment + "</span>";
				}


				var fmt = "<li title='" + item.name + "'> " +
								"<div style='margin:6px 0 0 52px;height:45px;' >" +
								"<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" +
								"<span>Nombre: " + item.name + "</span><br/>" + desc +
								"</div>" +
								"</div >" +
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

function SparePartFormatter(item) {

				var desc = "";

				if (typeof (item.comment) != 'undefined' && item.comment !== null) {
								desc = "&nbsp<span style='font-weight:normal;'>Descripción: " + item.comment + "</span>";
				}


				var fmt = "<li title='" + item.name + "'> " +
								"<div style='margin:6px 0 0 52px;height:45px;' >" +
								"<div style='font-weight:bold;overflow:hidden;text-overflow:ellipsis;white-space:nowrap'>" +
								"<span>" + item.name + "</span><br/>" +
								"<span>" + item.quantity + "</span><br/>"
								+ desc +
								"</div>" +
								"</div >" +
								"</li > ";

				return fmt;
}