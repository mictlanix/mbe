// 
// CFDHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2012-2018 Mictlanix SAS de CV and contributors.
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
using System.Data;
using System.Linq;
using System.Text;
using Mictlanix.BE.Model;
using Mictlanix.BE.Web.Helpers;
using Mictlanix.CFDv40;
using Mictlanix.DFacture.Client40;

namespace Mictlanix.BE.Web.Helpers40 {
		internal static class CFDHelpers40 {

		public static Comprobante IssueCFD (FiscalDocument item)
		{
			return DFactureStamp (item);
		}

		public static bool CancelCFD (FiscalDocument item)
		{
			if (item.IsCancelled) {
				return false;
			}

			return DFactureCancel (item);
		}

		public static Comprobante SignCFD (FiscalDocument item)
		{
			var cfd = FiscalDocumentToCFDv40 (item);
			var cer = item.Issuer.Certificates.Single (x => x.Id == item.IssuerCertificateNumber);
			//-- System.IO.File.WriteAllText (@"C:\Users\Alfredo\Documents\out\cfd.xml", cfd.ToXmlString ());
			cfd.Sign (cer.KeyData, cer.KeyPassword);

			return cfd;
		}


		static Comprobante DFactureStamp (FiscalDocument item)
		{
			var cfd = SignCFD (item);

			//-- fixme System.IO.File.WriteAllText (@"cfd.xml", cfd.ToXmlString ());

			var cli = new DFactureClient40 (WebConfig.DFactureUser, WebConfig.DFacturePassword, WebConfig.DFactureUrl);
			
			var tfd = cli.Stamp (cfd);

			if (cfd.Complemento == null) {
				cfd.Complemento = new List<object> ();
			}

			cfd.Complemento.Add (tfd);

			//-- fixme System.IO.File.WriteAllText (@"C:\Users\Alfredo\Documents\out\cfd.xml", cfd.ToXmlString ());

			return cfd;
		}

		static bool DFactureCancel (FiscalDocument item)
		{
			
			var cer = item.Issuer.Certificates.First (x => x.IsActive);
			var cli = new DFactureClient40 (WebConfig.DFactureUser, WebConfig.DFacturePassword, WebConfig.DFactureUrl);

			try {
				return cli.Cancel (item.Issuer.Id, item.Recipient, item.StampId, item.Total.ToString (),
							Convert.ToBase64String (cer.CertificateData),
						      	Convert.ToBase64String (cer.KeyData),
							Encoding.UTF8.GetString (cer.KeyPassword),
							reason:item.CancellationReason.Id,
							uuidRelated:item.CancellationSubstitution);
			} catch (DFactureClientException40 ex) {
				throw ex;
			}
		}

		public static bool PrivateKeyTest (byte [] data, byte [] password)
		{
			return CFDLib.Utils.PrivateKeyTest (data, password);
		}

		static Comprobante FiscalDocumentToCFDv40 (FiscalDocument item)
		{
			if (item.Type == FiscalDocumentType.PaymentReceipt) {
				return PaymentReceiptToCFDv40 (item);
			}

			return InvoiceToCFDv40 (item);
		}

		static Comprobante PaymentReceiptToCFDv40 (FiscalDocument item)
		{
			var cer = item.Issuer.Certificates.SingleOrDefault (x => x.Id == item.IssuerCertificateNumber);
			var cfd = new Comprobante {
				Serie = item.Batch,
				Folio = item.Serial.ToString (),
				Fecha = item.Issued.GetValueOrDefault (),
				LugarExpedicion = item.IssuedLocation,
				NoCertificado = item.IssuerCertificateNumber.PadLeft (20, '0'),
				Certificado = (cer == null ? null : SecurityHelpers.EncodeBase64 (cer.CertificateData)),
				SubTotal = 0,
				Moneda = c_Moneda.XXX,
				Total = 0,
				TipoDeComprobante = c_TipoDeComprobante.Pago,
				Exportacion = c_Exportacion.NoAplica,
				Emisor = new ComprobanteEmisor {
					Rfc = item.Issuer.Id,
					Nombre = item.IssuerName,
					RegimenFiscal = (c_RegimenFiscal) int.Parse (item.IssuerRegime.Id),
				},
				Receptor = new ComprobanteReceptor {
					Rfc = item.Recipient,
					Nombre = item.RecipientName,
					UsoCFDI = c_UsoCFDI.Pagos,
					RegimenFiscalReceptor = (c_RegimenFiscal) int.Parse (item.TaxpayerRegime.Id),
					DomicilioFiscalReceptor = item.TaxpayerPostalCode
				},
				Conceptos = new ComprobanteConcepto [] {
					new ComprobanteConcepto {
						ClaveProdServ = "84111506",
						Cantidad = 1,
						ClaveUnidad = "ACT",
						Descripcion = "Pago",
						ValorUnitario = 0,
						Importe = 0,
						ObjetoImp=c_ObjetoImp.NoObjetoImpuesto
					}
				},
				Complemento = new List<object> ()
			};
			var pagos = new Pagos {
				Pago = new PagosPago [] {
					new PagosPago {
						FechaPago = item.PaymentDate.GetValueOrDefault (),
						FormaDePagoP = (c_FormaPago)(int)item.PaymentMethod,
						MonedaP = item.Currency.GetDisplayName (),
						TipoCambioP = 1,
						TipoCambioPSpecified = true,
						Monto = item.PaymentAmount,
						NumOperacion = string.IsNullOrWhiteSpace (item.PaymentReference) ? null : item.PaymentReference,
						NomBancoOrdExt = string.IsNullOrWhiteSpace (item.Reference) ? null : item.Reference,
						DoctoRelacionado = new PagosPagoDoctoRelacionado [item.Relations.Count]
					}
				}
			};
			

			int i = 0;
			foreach (var relation in item.Relations) {
				pagos.Pago [0].DoctoRelacionado [i] = new PagosPagoDoctoRelacionado {
					IdDocumento = relation.Relation.StampId,
					Serie = relation.Relation.Batch,
					Folio = relation.Relation.Serial.ToString (),
					MonedaDR = relation.Relation.Currency.GetDisplayName (),					
					NumParcialidad = relation.Installment.ToString (),
					ImpSaldoAnt = relation.PreviousBalance,					
					ImpPagado = relation.Amount,
					ImpSaldoInsoluto = relation.OutstandingBalance,
					EquivalenciaDR=1,
					EquivalenciaDRSpecified=true,
					ObjetoImpDR = c_ObjetoImp.SiObjetoImpuesto,					 
				};

				//taxes for doctoRel


				decimal baseDR = 0m;
				

				bool hasTraslado= relation.Relation.Details.Any (d => d.TaxRate != 0);
				var distinctTaxRates = relation.Relation.Details.Select (d => d.TaxRate).Distinct ();				
				var trasladoCount = distinctTaxRates.Count ();
				bool hasRetencion= relation.Relation.RetentionRate>0m;

				if (hasTraslado && !hasRetencion) {
					baseDR = Math.Round (relation.Amount / (1+WebConfig.DefaultVAT), 6);
				} else if (hasTraslado && hasRetencion) {
					baseDR = Math.Round (relation.Amount / 1.05333m, 6);
				}

				if (hasTraslado || hasRetencion) {

					var impuestosDR = new PagosPagoDoctoRelacionadoImpuestosDR ();
					
					if (hasTraslado) {
						decimal importeDR = Math.Round (baseDR * WebConfig.DefaultVAT, 6);
						var  traslados = new PagosPagoDoctoRelacionadoImpuestosDRTrasladoDR [trasladoCount];

						var distinctTaxRatesArray = distinctTaxRates.ToArray ();
						for (int ii = 0; ii < distinctTaxRatesArray.Length; ii++) {
							var rate = distinctTaxRatesArray [ii];
							var traslado = new PagosPagoDoctoRelacionadoImpuestosDRTrasladoDR {
								BaseDR = baseDR,
								ImpuestoDR = c_Impuesto.IVA,
								TipoFactorDR = c_TipoFactor.Tasa,
								TasaOCuotaDR=rate,
								ImporteDR= importeDR,
								ImporteDRSpecified=true,
								TasaOCuotaDRSpecified = true,								 
							};
							
							traslados [ii] = traslado;
						}
						impuestosDR.TrasladosDR = traslados;
					}
					if (hasRetencion) {
						var retenciones = new PagosPagoDoctoRelacionadoImpuestosDRRetencionDR [1];
						decimal importeDR = Math.Round (baseDR * (relation.Relation.RetentionRate), 6);
						var retencion = new PagosPagoDoctoRelacionadoImpuestosDRRetencionDR {
							 BaseDR= baseDR,
							 ImpuestoDR=c_Impuesto.IVA,
							 TasaOCuotaDR= relation.Relation.RetentionRate,
							 TipoFactorDR=c_TipoFactor.Tasa,
							 ImporteDR = importeDR,							  
						};
						retenciones [0] = retencion;
						impuestosDR.RetencionesDR = retenciones;
					}

					pagos.Pago [0].DoctoRelacionado [i].ImpuestosDR = impuestosDR;
				}
				i++;
			}

			// taxes ImpuestosP

			decimal totalTrasladosBaseIVA16 = 0;
			decimal totalTrasladosImpuestoIVA16 = 0;
			decimal totalRetencionesIVA = 0;

			decimal montoTotalPagos = Math.Round (pagos.Pago
				    .SelectMany (p => p.DoctoRelacionado)
				    .Sum (d => d.ImpPagado),2);
				    
			bool nodoRet =true;
			bool nodoTras =true;

			var impuestosP = new PagosPagoImpuestosP ();
			try {
				var sumRetenciones = pagos.Pago
				    .SelectMany (p => p.DoctoRelacionado)
				    .SelectMany (d => d.ImpuestosDR.RetencionesDR)
				    .GroupBy (r => r.TasaOCuotaDR)
				    .ToDictionary (g => g.Key, g => new {
					    SumaImporteP = g.Sum (r => r.ImporteDR),
					    SumaBaseP = g.Sum (r => r.BaseDR)
				    });

				totalRetencionesIVA = Math.Round (pagos.Pago
				    .SelectMany (p => p.DoctoRelacionado)
				    .SelectMany (d => d.ImpuestosDR.RetencionesDR)
				    .Sum (d => d.ImporteDR),2);

				var retencionesP = new PagosPagoImpuestosPRetencionP [sumRetenciones.Count];
				int t = 0;
				foreach (var tr in sumRetenciones) {
					var retencionP = new PagosPagoImpuestosPRetencionP {
						ImporteP = Math.Round (tr.Value.SumaImporteP,6),
						 ImpuestoP=c_Impuesto.IVA
					};
					retencionesP [t++] = retencionP;
				};
								
				impuestosP.RetencionesP= retencionesP;

			} catch (Exception) {
				nodoRet = false;				
			}

			try {
				var sumTraslados = pagos.Pago
					.SelectMany (p => p.DoctoRelacionado)
					.SelectMany (d => d.ImpuestosDR.TrasladosDR)
					.GroupBy (r => r.TasaOCuotaDR)
					.ToDictionary (g => g.Key, g => new {
						SumaImporteP = g.Sum (r => r.ImporteDR),
						SumaBaseP = g.Sum (r => r.BaseDR)
					});

				var totalesTrasladosIva16 = pagos.Pago
					.SelectMany (p => p.DoctoRelacionado)
					.SelectMany (d => d.ImpuestosDR.TrasladosDR)
					.Aggregate (new { ImporteDR = 0m, BaseDR = 0m }, (a, d) => new {
						ImporteDR = a.ImporteDR + d.ImporteDR,
						BaseDR = a.BaseDR + d.BaseDR
					});

				totalTrasladosBaseIVA16 = Math.Round (totalesTrasladosIva16.BaseDR,2);
				totalTrasladosImpuestoIVA16 = Math.Round (totalesTrasladosIva16.ImporteDR,2);

				var trasladosP = new PagosPagoImpuestosPTrasladoP [sumTraslados.Count];

				int t = 0;
				foreach (var tr in sumTraslados) {
					 var trasladoP = new PagosPagoImpuestosPTrasladoP {
						BaseP= Math.Round (tr.Value.SumaBaseP,6),
						ImporteP= Math.Round (tr.Value.SumaImporteP,6),
						TipoFactorP=c_TipoFactor.Tasa,
						TasaOCuotaP=tr.Key,
						ImportePSpecified=true,
						ImpuestoP=c_Impuesto.IVA,
						TasaOCuotaPSpecified=true,
					};
					trasladosP [t++] = trasladoP;					
				};

				impuestosP.TrasladosP = trasladosP;

			} catch (Exception) {
				nodoTras = false;
			}

			if (nodoRet || nodoTras) {				

				pagos.Pago [0].ImpuestosP=impuestosP;
				pagos.Totales = new PagosTotales {
					TotalRetencionesIVA= totalRetencionesIVA,
					TotalRetencionesIVASpecified = nodoRet,
					TotalTrasladosBaseIVA16= totalTrasladosBaseIVA16,
					TotalTrasladosBaseIVA16Specified = nodoTras,
					TotalTrasladosImpuestoIVA16 = totalTrasladosImpuestoIVA16,
					TotalTrasladosImpuestoIVA16Specified = nodoTras,
					MontoTotalPagos = montoTotalPagos
				};
			}

			cfd.Complemento.Add (pagos);

			return cfd;
		}

		static Comprobante InvoiceToCFDv40 (FiscalDocument item)
		{
			var cer = item.Issuer.Certificates.SingleOrDefault (x => x.Id == item.IssuerCertificateNumber);
			var cfd = new Comprobante {
				TipoDeComprobante = (c_TipoDeComprobante) FDT2TDC (item.Type),
				NoCertificado = item.IssuerCertificateNumber.PadLeft (20, '0'),
				Serie = item.Batch,
				Folio = item.Serial.ToString (),
				Fecha = item.Issued.GetValueOrDefault (),
				LugarExpedicion = item.IssuedLocation,
				MetodoPago = item.Terms == PaymentTerms.Immediate ? c_MetodoPago.PagoEnUnaSolaExhibicion : c_MetodoPago.PagoEnParcialidadesODiferido,
				MetodoPagoSpecified = true,
				FormaPago = (c_FormaPago) (int) item.PaymentMethod,
				FormaPagoSpecified = true,
				TipoCambio = item.ExchangeRate,
				Exportacion = c_Exportacion.NoAplica,
				TipoCambioSpecified = item.Currency != CurrencyCode.MXN,
				Moneda = c_Moneda.MXN, 
				SubTotal = item.Subtotal,
				Total = item.Total,								
				Sello = item.IssuerDigitalSeal,
				Certificado = (cer == null ? null : SecurityHelpers.EncodeBase64 (cer.CertificateData)),
				Emisor = new ComprobanteEmisor {
					Rfc = item.Issuer.Id,
					Nombre = item.IssuerName,
					RegimenFiscal = (c_RegimenFiscal) int.Parse (item.IssuerRegime.Id),
				},
				Receptor = new ComprobanteReceptor {
					Rfc = item.Recipient,
					Nombre = item.RecipientName,
					UsoCFDI = CfdiUsage2UsoCFDI (item.Usage.Id),
					RegimenFiscalReceptor = (c_RegimenFiscal) int.Parse (item.TaxpayerRegime.Id),
					DomicilioFiscalReceptor=item.TaxpayerPostalCode
				},
				Conceptos = new ComprobanteConcepto [item.Details.Count]
			};

			int i = 0;
			foreach (var detail in item.Details) {
				cfd.Conceptos [i] = new ComprobanteConcepto {
					Cantidad = detail.Quantity,
					ClaveUnidad = detail.UnitOfMeasurement.Id,
					Unidad = detail.UnitOfMeasurementName,
					NoIdentificacion = detail.ProductCode,
					ClaveProdServ = detail.ProductService.Id,
					Descripcion = detail.ProductName,
					ValorUnitario = detail.NetPrice,
					ObjetoImp = detail.TaxRate >= 0m ? c_ObjetoImp.SiObjetoImpuesto: c_ObjetoImp.NoObjetoImpuesto ,
					Importe = detail.Subtotal,
					Descuento = detail.Discount,
					DescuentoSpecified = detail.Discount > 0m
				};


				if (detail.Subtotal == detail.Discount) {
					i++;
					continue;
				}

				if (detail.TaxRate >= 0m) {
					
					cfd.Conceptos [i].Impuestos = new ComprobanteConceptoImpuestos {
						Traslados = new ComprobanteConceptoImpuestosTraslado [] {
							new ComprobanteConceptoImpuestosTraslado {
								Impuesto = c_Impuesto.IVA,
								TipoFactor = c_TipoFactor.Tasa,
								Base = detail.TaxBase,
								Importe = detail.Taxes,
								ImporteSpecified = true,
								TasaOCuota = detail.TaxRate,
								TasaOCuotaSpecified = true
							}
						},
						Retenciones = item.RetentionRate <= 0m ? null : new ComprobanteConceptoImpuestosRetencion [] {
							new ComprobanteConceptoImpuestosRetencion {
								Impuesto = c_Impuesto.IVA,
								TipoFactor = c_TipoFactor.Tasa,
								Base = detail.TaxBase,
								Importe = detail.RetentionTaxes,
								TasaOCuota = item.RetentionRate
							}
						}
					};
				} else {
					cfd.Conceptos [i].Impuestos = new ComprobanteConceptoImpuestos {
						Traslados = new ComprobanteConceptoImpuestosTraslado [] {
							new ComprobanteConceptoImpuestosTraslado {
								Impuesto = c_Impuesto.IVA,
								TipoFactor = c_TipoFactor.Exento,
								Base = detail.TaxBase
							}
						}
					};
				}

				i++;
			}

			if (item.Discount > 0) {
				cfd.Descuento = item.Discount;
				cfd.DescuentoSpecified = true;
			}

			cfd.Impuestos = new ComprobanteImpuestos ();

			var taxes = new List<ComprobanteImpuestosTraslado> ();

			if (cfd.Conceptos.Any (c => c.Impuestos != null && c.Impuestos.Traslados.Any (x => x.TasaOCuota == decimal.Zero))) {
				taxes.Add (new ComprobanteImpuestosTraslado {
					Impuesto = c_Impuesto.IVA,
					TipoFactor = c_TipoFactor.Tasa,
					TasaOCuota = 0.000000m,
					TasaOCuotaSpecified = true,
					Base = Model.ModelHelpers.TotalRounding (cfd.Conceptos.Where (x => x.Impuestos != null).Sum (c => c.Impuestos.Traslados.Where (x => x.TasaOCuota == 0.000000m).Sum (x => x.Base))),
					Importe = 0.00m,
					ImporteSpecified = true
				});
			}

			if (cfd.Conceptos.Any (c => c.Impuestos != null && c.Impuestos.Traslados.Any (x => x.TasaOCuota == WebConfig.DefaultVAT))) {
				taxes.Add (new ComprobanteImpuestosTraslado {
					Impuesto = c_Impuesto.IVA,
					TipoFactor = c_TipoFactor.Tasa,
					TasaOCuota = WebConfig.DefaultVAT,
					TasaOCuotaSpecified = true,
					Base = Model.ModelHelpers.TotalRounding (cfd.Conceptos.Where (x => x.Impuestos != null).Sum (c => c.Impuestos.Traslados.Where (x => x.TasaOCuota == WebConfig.DefaultVAT).Sum (x => x.Base))),
					Importe = cfd.Conceptos.Where (x => x.Impuestos != null).Sum (c => c.Impuestos.Traslados.Where (x => x.TasaOCuota == WebConfig.DefaultVAT).Sum (x => x.Importe)),
					ImporteSpecified = true
				});
			}

			cfd.Impuestos.Traslados = taxes.ToArray ();
			cfd.Impuestos.TotalImpuestosTrasladados = cfd.Impuestos.Traslados.Sum (x => x.Importe);
			cfd.Impuestos.TotalImpuestosTrasladadosSpecified = true;

			if (item.RetentionRate > 0m) {
				cfd.Impuestos.Retenciones = new ComprobanteImpuestosRetencion [] {
					new ComprobanteImpuestosRetencion {
						Impuesto = c_Impuesto.IVA,
						Importe = item.RetentionTaxes,
					}
				};

				cfd.Impuestos.TotalImpuestosRetenidos = cfd.Impuestos.Retenciones.Sum (x => x.Importe);
				cfd.Impuestos.TotalImpuestosRetenidosSpecified = true;
			}

			
			if (item.LocalRetentionRate > 0m) {
				var implocal = new ImpuestosLocalesRetencionesLocales {
					ImpLocRetenido = item.LocalRetentionName,
					Importe = item.LocalRetentionTaxes,
					TasadeRetencion = Math.Round (item.LocalRetentionRate * 100m, 2)
				};

				if (cfd.Complemento == null) {
					cfd.Complemento = new List<object> ();
				}

				cfd.Complemento.Add (new ImpuestosLocales {
					TotaldeRetenciones = implocal.Importe,
					RetencionesLocales = new ImpuestosLocalesRetencionesLocales [] {
						implocal
					}
				});
			}
			
			if (item.Relations.Any ()) {				

				//master node
				ComprobanteCfdiRelacionados [] cFdV40ComprobanteCfdiRelacionados = new ComprobanteCfdiRelacionados [1];

				//object
				var rels = new ComprobanteCfdiRelacionados {
					//list
					CfdiRelacionado = new ComprobanteCfdiRelacionadosCfdiRelacionado [item.Relations.Count]
				};

				if (item.Type == FiscalDocumentType.AdvancePaymentsApplied) {
					rels.TipoRelacion = c_TipoRelacion.AplicacionDeAnticipo;
				} else if (item.Type == FiscalDocumentType.CreditNote) {
					rels.TipoRelacion = c_TipoRelacion.NotaDeCredito;
				} else {
					rels.TipoRelacion = c_TipoRelacion.Sustitucion;
				}

				i = 0;

				//fill the list of the object
				foreach (var relation in item.Relations) {
					rels.CfdiRelacionado [i++] = new ComprobanteCfdiRelacionadosCfdiRelacionado {
						UUID = relation.Relation.StampId
					};
				}

				//fill master node with object and list
				cFdV40ComprobanteCfdiRelacionados [0] = rels;

				//put in cfd
				cfd.CfdiRelacionados = cFdV40ComprobanteCfdiRelacionados;
			}
			
			return cfd;
		}

		// FiscalDocumentType -> TipoDeComprobante
		static int FDT2TDC (FiscalDocumentType type)
		{
			switch (type) {
			case FiscalDocumentType.Invoice:
			case FiscalDocumentType.FeeReceipt:
			case FiscalDocumentType.RentReceipt:
			case FiscalDocumentType.DebitNote:
				return (int) c_TipoDeComprobante.Ingreso;
			case FiscalDocumentType.CreditNote:
			case FiscalDocumentType.AdvancePaymentsApplied:
				return (int) c_TipoDeComprobante.Egreso;
			case FiscalDocumentType.PaymentReceipt:
				return (int) c_TipoDeComprobante.Pago;
			}

			throw new ArgumentOutOfRangeException (nameof (type));
		}

		// FiscalDocumentType -> TipoDeComprobante
		static c_UsoCFDI CfdiUsage2UsoCFDI (string code)
		{
			switch (code) {
			case "G01":
				return c_UsoCFDI.AdquisicionDeMercancias;
			case "G02":
				return c_UsoCFDI.DevolucionesDescuentosOBonificaciones;
			case "G03":
				return c_UsoCFDI.GastosEnGeneral;
			case "I01":
				return c_UsoCFDI.Construcciones;
			case "I02":
				return c_UsoCFDI.MobilarioYEquipoDeOficinaPorInversiones;
			case "I03":
				return c_UsoCFDI.EquipoDeTransporte;
			case "I04":
				return c_UsoCFDI.EquipoDeComputoYAccesorios;
			case "I05":
				return c_UsoCFDI.DadosTroquelesMoldesMatricesYHerramental;
			case "I06":
				return c_UsoCFDI.ComunicacionesTelefonicas;
			case "I07":
				return c_UsoCFDI.ComunicacionesSatelitales;
			case "I08":
				return c_UsoCFDI.OtraMaquinariaYEquipo;
			case "D01":
				return c_UsoCFDI.HonorariosMedicosDentalesYGastosHospitalarios;
			case "D02":
				return c_UsoCFDI.GastosMedicosPorIncapacidadODiscapacidad;
			case "D03":
				return c_UsoCFDI.GastosFunerales;
			case "D04":
				return c_UsoCFDI.Donativos;
			case "D05":
				return c_UsoCFDI.InteresesRealesEfectivamentePagadosPorCreditosHipotecarios;
			case "D06":
				return c_UsoCFDI.AportacionesVoluntariasAlSAR;
			case "D07":
				return c_UsoCFDI.PrimasPorSegurosDeGastosMedicos;
			case "D08":
				return c_UsoCFDI.GastosDeTransportacionEscolarObligatoria;
			case "D09":
				return c_UsoCFDI.DepositosEnCuentasParaElAhorroPrimasQueTenganComoBasePlanesDePensiones;
			case "D10":
				return c_UsoCFDI.PagosPorServiciosEducativos;
			case "P01": // for compatibility
				return c_UsoCFDI.GastosEnGeneral;
			}


			throw new ArgumentOutOfRangeException (nameof (code));
		}

	}
}
