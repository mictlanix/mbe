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
using Mictlanix.BE.Model;
using Mictlanix.CFDLib;

namespace Mictlanix.BE.Web.Helpers
{
	internal static class CFDv2Helpers
	{
		static readonly DateTime CFDv22_DATE = new DateTime (2012, 7, 1, 0, 0, 0);

		public static dynamic IssueCFD (FiscalDocument item)
		{
			var dt = DateTime.Now;
			var cfd = InvoiceToCFD (item);

			cfd.fecha = new DateTime (dt.Year, dt.Month, dt.Day,
					                  dt.Hour, dt.Minute, dt.Second,
					                  DateTimeKind.Unspecified);
			cfd.Sign (item.Issuer.KeyData, item.Issuer.KeyPassword);

			return cfd;
		}

		public static dynamic SignCFD (FiscalDocument item)
		{
			var cfd = InvoiceToCFD (item);

			cfd.Sign (item.Issuer.KeyData, item.Issuer.KeyPassword);

			return cfd;
		}

		// Choose what version according to law
		// v2.2 current version
		// v2.0 before July 1, 2012
		public static dynamic InvoiceToCFD (FiscalDocument item)
		{
			if (!item.Issued.HasValue || item.Issued >= CFDv22_DATE) {
				return InvoiceToCFDv22 (item);
			} else {
				return InvoiceToCFDv20 (item);
			}
		}

		static Mictlanix.CFDv20.Comprobante InvoiceToCFDv20 (FiscalDocument item)
		{
			var cfd = new Mictlanix.CFDv20.Comprobante
            {
                tipoDeComprobante = (Mictlanix.CFDv20.ComprobanteTipoDeComprobante)item.Type,
                noAprobacion = item.ApprovalNumber.ToString (),
                anoAprobacion = item.ApprovalYear.ToString (),
                noCertificado = item.CertificateNumber.ToString ().PadLeft (20, '0'),
                serie = item.Batch,
                folio = item.Serial.ToString (),
                fecha = item.Issued.GetValueOrDefault (),
                formaDePago = Resources.SinglePayment,
                certificado = SecurityHelpers.EncodeBase64 (item.Issuer.CertificateData),
                Emisor = new Mictlanix.CFDv20.ComprobanteEmisor
                {
                    nombre = item.IssuedFrom.TaxpayerName,
                    rfc = item.IssuedFrom.TaxpayerId,
                    DomicilioFiscal = new Mictlanix.CFDv20.t_UbicacionFiscal
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
                Receptor = new Mictlanix.CFDv20.ComprobanteReceptor
                {
                    nombre = item.BillTo.TaxpayerName,
                    rfc = item.BillTo.TaxpayerId,
                    Domicilio = new Mictlanix.CFDv20.t_Ubicacion
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
                Conceptos = new Mictlanix.CFDv20.ComprobanteConcepto[item.Details.Count],
                Impuestos = new Mictlanix.CFDv20.ComprobanteImpuestos
                {
                    Traslados = new Mictlanix.CFDv20.ComprobanteImpuestosTraslado[1]
                },
				subTotal = item.Subtotal,
				total = item.Total,
				sello = item.DigitalSeal
            };
            
			int i = 0;
			foreach (var detail in item.Details) {
				cfd.Conceptos [i++] = new Mictlanix.CFDv20.ComprobanteConcepto
                {
                    cantidad = detail.Quantity,
                    unidad = detail.UnitOfMeasurement,
                    noIdentificacion = detail.ProductCode,
                    descripcion = detail.ProductName,
                    valorUnitario = detail.Price,
                    importe = detail.Total
                };
			}

			cfd.Impuestos.Traslados [0] = new Mictlanix.CFDv20.ComprobanteImpuestosTraslado
            {
                impuesto = Mictlanix.CFDv20.ComprobanteImpuestosTrasladoImpuesto.IVA,
                importe = item.Taxes,
                tasa = Configuration.VAT * 100m
            };

			return cfd;
		}
		
		static Mictlanix.CFDv22.Comprobante InvoiceToCFDv22 (FiscalDocument item)
		{
			var cfd = new Mictlanix.CFDv22.Comprobante
            {
                tipoDeComprobante = (Mictlanix.CFDv22.ComprobanteTipoDeComprobante)item.Type,
                noAprobacion = item.ApprovalNumber.ToString (),
                anoAprobacion = item.ApprovalYear.ToString (),
                noCertificado = item.CertificateNumber.ToString ().PadLeft (20, '0'),
                serie = item.Batch,
                folio = item.Serial.ToString (),
                fecha = item.Issued.GetValueOrDefault (),
				metodoDePago = item.PaymentMethod.GetDisplayName(),
				NumCtaPago = item.PaymentReference,
				LugarExpedicion = item.IssuedLocation,
				subTotal = item.Subtotal,
				total = item.Total,
				sello = item.DigitalSeal,
                formaDePago = Resources.SinglePayment,
                certificado = SecurityHelpers.EncodeBase64 (item.Issuer.CertificateData),
                Emisor = new Mictlanix.CFDv22.ComprobanteEmisor
                {
                    nombre = item.IssuedFrom.TaxpayerName,
                    rfc = item.IssuedFrom.TaxpayerId,
					RegimenFiscal = new Mictlanix.CFDv22.ComprobanteEmisorRegimenFiscal [1] {
						new Mictlanix.CFDv22.ComprobanteEmisorRegimenFiscal {
							Regimen = item.IssuedFrom.TaxpayerRegime
						}
					},
                    DomicilioFiscal = new Mictlanix.CFDv22.t_UbicacionFiscal {
                        calle = item.IssuedFrom.Street,
                        noExterior = item.IssuedFrom.ExteriorNumber,
                        noInterior = item.IssuedFrom.InteriorNumber,
                        colonia = item.IssuedFrom.Neighborhood,
                        codigoPostal = item.IssuedFrom.ZipCode,
						localidad = item.IssuedFrom.Locality,
                        municipio = item.IssuedFrom.Borough,
                        estado = item.IssuedFrom.State,
                        pais = item.IssuedFrom.Country
                    },
                    ExpedidoEn = (item.IssuedAt == null) ? null : new Mictlanix.CFDv22.t_Ubicacion {
                        calle = item.IssuedAt.Street,
                        noExterior = item.IssuedAt.ExteriorNumber,
                        noInterior = item.IssuedAt.InteriorNumber,
                        colonia = item.IssuedAt.Neighborhood,
                        codigoPostal = item.IssuedAt.ZipCode,
						localidad = item.IssuedAt.Locality,
                        municipio = item.IssuedAt.Borough,
                        estado = item.IssuedAt.State,
                        pais = item.IssuedAt.Country
                    }
                },
                Receptor = new Mictlanix.CFDv22.ComprobanteReceptor
                {
                    nombre = item.BillTo.TaxpayerName,
                    rfc = item.BillTo.TaxpayerId,
                    Domicilio = new Mictlanix.CFDv22.t_Ubicacion
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
                Conceptos = new Mictlanix.CFDv22.ComprobanteConcepto[item.Details.Count],
                Impuestos = new Mictlanix.CFDv22.ComprobanteImpuestos
                {
                    Traslados = new Mictlanix.CFDv22.ComprobanteImpuestosTraslado[1]
                }
            };
            
			int i = 0;
			foreach (var detail in item.Details) {
				cfd.Conceptos [i++] = new Mictlanix.CFDv22.ComprobanteConcepto
                {
                    cantidad = detail.Quantity,
                    unidad = detail.UnitOfMeasurement,
                    noIdentificacion = detail.ProductCode,
                    descripcion = detail.ProductName,
                    valorUnitario = Math.Round (detail.Price, 6, MidpointRounding.AwayFromZero),
                    importe = detail.Total
                };
			}

			cfd.Impuestos.Traslados [0] = new Mictlanix.CFDv22.ComprobanteImpuestosTraslado
            {
                impuesto = Mictlanix.CFDv22.ComprobanteImpuestosTrasladoImpuesto.IVA,
                importe = item.Taxes,
                tasa = Configuration.VAT * 100m
            };

			return cfd;
		}
		
		public static IEnumerable<CFDv2ReportItem> MonthlyReport (IEnumerable<FiscalDocument> items)
		{
			var list = new List<CFDv2ReportItem> (items.Count ());
			
			foreach (var item in items) {
				var state = true;
				var dup = false;
				
				if (item.IsCancelled) {
					var dt1 = item.Issued.Value;
					var dt2 = item.CancellationDate.Value;
										
					if (dt1.Year == dt2.Year && dt1.Month == dt2.Month) {
						dup = true;
					} else {
						state = false;
					}
				}
				
				var row = new CFDv2ReportItem {
					Taxpayer = item.BillTo.TaxpayerId,
					Batch = item.Batch,
					Serial = item.Serial.Value,
					ApprovalYear = item.ApprovalYear.Value,
					ApprovalNumber = item.ApprovalNumber.Value,
					Date = item.Issued.Value,
					Amount = item.Total,
					Taxes = item.Taxes,
					IsActive = state,
					Type = (CFDv2ReportItemType)item.Type
				};
				
				list.Add (row);
				
				if (dup) {
					row = (CFDv2ReportItem)row.Clone ();
					row.IsActive = false;
					list.Add (row);
				}
			}
			
			return list;
		}
	}
}
