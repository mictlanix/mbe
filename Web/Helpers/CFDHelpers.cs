// 
// CFDv2Helpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2012-2013 Eddy Zavaleta, Mictlanix, and contributors.
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
	internal static class CFDHelpers
	{
		public static dynamic SignCFD (FiscalDocument item)
		{
			var cfd = InvoiceToCFD (item);
			var cer = item.Issuer.Certificates.Single (x => x.Id == item.CertificateNumber);

			cfd.Sign (cer.KeyData, cer.KeyPassword);

			return cfd;
		}

		public static bool PrivateKeyTest (byte[] data, byte[] password)
		{
			return Mictlanix.CFDLib.Utils.PrivateKeyTest (data, password);
		}

		// Choose what version according to law
		// v2.2 current version
		// v2.0 before July 1, 2012
		public static dynamic InvoiceToCFD (FiscalDocument item)
		{
			if (!item.Issued.HasValue || item.Issued >= Mictlanix.CFDv22.Comprobante.RELEASE_DATE) {
				return InvoiceToCFDv22 (item);
			} else {
				return InvoiceToCFDv20 (item);
			}
		}

		static Mictlanix.CFDv20.Comprobante InvoiceToCFDv20 (FiscalDocument item)
		{
			var cer = item.Issuer.Certificates.SingleOrDefault (x => x.Id == item.CertificateNumber);

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
				certificado = (cer == null ? null : SecurityHelpers.EncodeBase64 (cer.CertificateData)),
                Emisor = new Mictlanix.CFDv20.ComprobanteEmisor
				{
					rfc = item.Issuer.Id,
                    nombre = item.IssuerName,
                    DomicilioFiscal = new Mictlanix.CFDv20.t_UbicacionFiscal
                    {
                        calle = item.IssuerAddress.Street,
						noExterior = item.IssuerAddress.ExteriorNumber,
						noInterior = item.IssuerAddress.InteriorNumber,
						colonia = item.IssuerAddress.Neighborhood,
						codigoPostal = item.IssuerAddress.PostalCode,
						localidad = item.IssuerAddress.Locality,
						municipio = item.IssuerAddress.Borough,
						estado = item.IssuerAddress.State,
                        pais = item.IssuerAddress.Country
                    }
                },
                Receptor = new Mictlanix.CFDv20.ComprobanteReceptor
				{
					rfc = item.Recipient.Id,
                    nombre = item.RecipientName,
                    Domicilio = new Mictlanix.CFDv20.t_Ubicacion
                    {
						calle = item.RecipientAddress.Street,
						noExterior = item.RecipientAddress.ExteriorNumber,
						noInterior = item.RecipientAddress.InteriorNumber,
						colonia = item.RecipientAddress.Neighborhood,
						codigoPostal = item.RecipientAddress.PostalCode,
						localidad = item.RecipientAddress.Locality,
						municipio = item.RecipientAddress.Borough,
						estado = item.RecipientAddress.State,
						pais = item.RecipientAddress.Country
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
                    valorUnitario = detail.NetPrice,
                    importe = detail.Subtotal
                };
			}

			// TODO: VAT Summaries
			cfd.Impuestos.Traslados [0] = new Mictlanix.CFDv20.ComprobanteImpuestosTraslado
            {
                impuesto = Mictlanix.CFDv20.ComprobanteImpuestosTrasladoImpuesto.IVA,
                importe = item.Taxes,
                tasa = Configuration.DefaultVAT * 100m
            };

			return cfd;
		}
		
		static Mictlanix.CFDv22.Comprobante InvoiceToCFDv22 (FiscalDocument item)
		{
			var cer = item.Issuer.Certificates.SingleOrDefault (x => x.Id == item.CertificateNumber);

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
				certificado = (cer == null ? null : SecurityHelpers.EncodeBase64 (cer.CertificateData)),
                Emisor = new Mictlanix.CFDv22.ComprobanteEmisor
				{
					rfc = item.Issuer.Id,
					nombre = item.IssuerName,
					RegimenFiscal = new Mictlanix.CFDv22.ComprobanteEmisorRegimenFiscal [1] {
						new Mictlanix.CFDv22.ComprobanteEmisorRegimenFiscal {
							Regimen = item.IssuerRegime
						}
					},
                    DomicilioFiscal = new Mictlanix.CFDv22.t_UbicacionFiscal {
                        calle = item.IssuerAddress.Street,
						noExterior = item.IssuerAddress.ExteriorNumber,
						noInterior = item.IssuerAddress.InteriorNumber,
						colonia = item.IssuerAddress.Neighborhood,
						codigoPostal = item.IssuerAddress.PostalCode,
						localidad = item.IssuerAddress.Locality,
						municipio = item.IssuerAddress.Borough,
						estado = item.IssuerAddress.State,
						pais = item.IssuerAddress.Country
                    },
                    ExpedidoEn = (item.IssuedAt == null) ? null : new Mictlanix.CFDv22.t_Ubicacion {
                        calle = item.IssuedAt.Street,
                        noExterior = item.IssuedAt.ExteriorNumber,
                        noInterior = item.IssuedAt.InteriorNumber,
                        colonia = item.IssuedAt.Neighborhood,
                        codigoPostal = item.IssuedAt.PostalCode,
						localidad = item.IssuedAt.Locality,
                        municipio = item.IssuedAt.Borough,
                        estado = item.IssuedAt.State,
                        pais = item.IssuedAt.Country
                    }
                },
                Receptor = new Mictlanix.CFDv22.ComprobanteReceptor
				{
					rfc = item.Recipient.Id,
                    nombre = item.RecipientName,
                    Domicilio = new Mictlanix.CFDv22.t_Ubicacion
                    {
                        calle = item.RecipientAddress.Street,
						noExterior = item.RecipientAddress.ExteriorNumber,
						noInterior = item.RecipientAddress.InteriorNumber,
						colonia = item.RecipientAddress.Neighborhood,
						codigoPostal = item.RecipientAddress.PostalCode,
						localidad = item.RecipientAddress.Locality,
						municipio = item.RecipientAddress.Borough,
						estado = item.RecipientAddress.State,
						pais = item.RecipientAddress.Country
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
                    valorUnitario = detail.NetPrice,
                    importe = detail.Subtotal
                };
			}

			// TODO: VAT Summaries
			cfd.Impuestos.Traslados [0] = new Mictlanix.CFDv22.ComprobanteImpuestosTraslado
            {
                impuesto = Mictlanix.CFDv22.ComprobanteImpuestosTrasladoImpuesto.IVA,
                importe = item.Taxes,
                tasa = Configuration.DefaultVAT * 100m
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
					Taxpayer = item.Recipient.Id,
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
