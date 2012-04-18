// 
// CFDv2Helpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.org>
// 
// Copyright (C) 2012 Eddy Zavaleta, Mictlanix, and contributors.
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.IO;
using System.Text;
using Mictlanix.CFDv2;
using Mictlanix.CFDv2.Resources;
using Mictlanix.BE.Model;

namespace Mictlanix.BE.Web.Helpers
{
	public static class CFDv2Helpers
	{
		internal static Comprobante IssueCFD (SalesInvoice item)
		{
			var dt = DateTime.Now;
			var cfd = InvoiceToCFD (item);

			cfd.fecha = new DateTime (dt.Year, dt.Month, dt.Day,
					                  dt.Hour, dt.Minute, dt.Second,
					                  DateTimeKind.Unspecified);
			cfd.sello = CFDv2Utils.DigitalSignature (cfd, item.Issuer.KeyData,
                                     				 item.Issuer.KeyPassword);

			return cfd;
		}

		internal static Comprobante SignCFD (SalesInvoice item)
		{
			var cfd = InvoiceToCFD (item);
			cfd.sello = CFDv2Utils.DigitalSignature (cfd, item.Issuer.KeyData,
                                     				 item.Issuer.KeyPassword);
			return cfd;
		}
		
		internal static Comprobante InvoiceToCFD (SalesInvoice item)
		{
			var cfd = new Comprobante
            {
                tipoDeComprobante = ComprobanteTipoDeComprobante.ingreso,
                noAprobacion = item.ApprovalNumber.ToString (),
                anoAprobacion = item.ApprovalYear.ToString (),
                noCertificado = item.CertificateNumber.ToString ().PadLeft (20, '0'),
                serie = item.Batch,
                folio = item.Serial.ToString (),
                fecha = item.Issued.GetValueOrDefault (),
                formaDePago = Resources.SinglePayment,
                certificado = SecurityHelpers.EncodeBase64 (item.Issuer.CertificateData),
                Emisor = new ComprobanteEmisor
                {
                    nombre = item.IssuedFrom.TaxpayerName,
                    rfc = item.IssuedFrom.TaxpayerId,
                    DomicilioFiscal = new t_UbicacionFiscal
                    {
                        calle = item.IssuedFrom.Street,
                        noExterior = item.IssuedFrom.ExteriorNumber,
                        noInterior = item.IssuedFrom.InteriorNumber,
                        colonia = item.IssuedFrom.Neighborhood,
                        codigoPostal = item.IssuedFrom.ZipCode,
						localidad = item.IssuedFrom.Locality,
                        municipio = item.IssuedFrom.Borough,
                        estado = item.IssuedFrom.State,
                        pais = item.IssuedFrom.Country
                    }
                },
                Receptor = new ComprobanteReceptor
                {
                    nombre = item.BillTo.TaxpayerName,
                    rfc = item.BillTo.TaxpayerId,
                    Domicilio = new t_Ubicacion
                    {
                        calle = item.BillTo.Street,
                        noExterior = item.BillTo.ExteriorNumber,
                        noInterior = item.BillTo.InteriorNumber,
                        colonia = item.BillTo.Neighborhood,
                        codigoPostal = item.BillTo.ZipCode,
						localidad = item.IssuedFrom.Locality,
                        municipio = item.BillTo.Borough,
                        estado = item.BillTo.State,
                        pais = item.BillTo.Country
                    }
                },
                Conceptos = new ComprobanteConcepto[item.Details.Count],
                Impuestos = new ComprobanteImpuestos
                {
                    Traslados = new ComprobanteImpuestosTraslado[1]
                },
				subTotal = item.Subtotal,
				total = item.Total,
				sello = item.DigitalSeal
            };
            
			int i = 0;
			foreach (SalesInvoiceDetail detail in item.Details) {
				cfd.Conceptos [i++] = new ComprobanteConcepto
                {
                    cantidad = detail.Quantity,
                    unidad = detail.UnitOfMeasurement,
                    noIdentificacion = detail.ProductCode,
                    descripcion = detail.ProductName,
                    valorUnitario = detail.Price,
                    importe = detail.Total
                };
			}

			cfd.Impuestos.Traslados [0] = new ComprobanteImpuestosTraslado
            {
                impuesto = ComprobanteImpuestosTrasladoImpuesto.IVA,
                importe = item.Taxes,
                tasa = Configuration.VAT * 100m
            };
			
			//cfd.descuentoSpecified = true;
			// cfd.descuento = 

			return cfd;
		}
		
		public static string OriginalString (Comprobante cfd)
		{
			return CFDv2Utils.OriginalString (cfd);
		}
		
		public static Stream SerializeToXmlStream (Comprobante cfd)
		{
			return CFDv2Utils.SerializeToXmlStream (cfd);
		}
		
		public static string SerializeToXmlString (Comprobante cfd)
		{
			using (var ms = CFDv2Utils.SerializeToXmlStream (cfd)) {
				return Encoding.UTF8.GetString (ms.ToArray ());
			}
		}
	}
}
