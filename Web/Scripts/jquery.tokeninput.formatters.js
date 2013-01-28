function ProductFormatter(item) {
    var htmlPrice = "";

    if (typeof (item.price) != "undefined")
    {
        htmlPrice = "&nbsp;<span>Precio: " + item.price + "</span>"
    }

    var fmt = "<li>" +
                "<img style='max-width:50px;height:50px;vertical-align:top;' src='" + item.url + "' title='" + item.name + "'/>" +
                "<div style='display:inline-block;height:50px;vertical-align:top;'>" +
                  "<span style='font-weight:bold;'>" + item.name + "</span><br/>" +
                  "<span>Código: " + item.code + " Modelo: " + item.model + " SKU: " + item.sku + "</span>" +
                  htmlPrice +
                "</div>" +
              "</li>";

    return fmt;
}

