# Change Log
Todos los cambios relevantes en este proyecto serán documentados en este archivo

## Desarrollo
### Nuevo
- Visualizar en pdf el reporte de pagos recibidos.
- Dar de alta a un cliente en la venta con solo escribir el nombre.
- Mostrar existencia del producto para venta en pedidos.
- Nuevo formato de impresión para comprobantes fiscales.

### Cambios
- En Devoluciones de clientes inicializar a ceros las cantidades a devolver para que el usuario las edite.
- Calcular el descuento/cargo (%) al editar el precio en pedidos y punto de venta.
- Eliminación por completo del módulo SupplierReturns.
- Eliminación del formato de impresión de compras.
- Manejo general de descuentos en todos los módulos.

### Correcciones
- En reporte de pagos recibidos no mostrar pagos no confirmados y restringirlo por tienda.
- Espacio en blanco faltante en las cantidades con letra.
- Referencia a "OcassionalCustomer" en vez de "CustomerName" para la entidad SalesOrder.
