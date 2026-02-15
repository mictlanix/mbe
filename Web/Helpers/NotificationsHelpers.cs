// 
// NotificationsHelpers.cs
// 
// Author:
//   Eddy Zavaleta <eddy@mictlanix.com>
// 
// Copyright (C) 2016 Eddy Zavaleta, Mictlanix, and contributors.
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Mictlanix.BE.Web.Helpers {

	public enum TextPartSubtype
	{
		Plain,
		Html
	}

	public static class NotificationsHelpers {
		public static bool SendEmail (string addrFrom, IEnumerable<string> addrTo, IEnumerable<string> addrCc,
		                              IEnumerable<string> addrBcc, string subject, string textBody)
		{
			return SendEmail (addrFrom, addrTo, addrCc, addrBcc, subject, textBody, null);
		}

		public static bool SendEmail (string addrFrom, IEnumerable<string> addrTo, IEnumerable<string> addrCc,
		                              IEnumerable<string> addrBcc, string subject, string textBody,
		                              string attachmentName, Stream attachmentContent)
		{
			var attachments = new List<MimePart>();

			if (attachmentContent != null) {
				var attachment = new MimePart {
					Content = new MimeContent (attachmentContent, ContentEncoding.Default),
					ContentDisposition = new ContentDisposition (ContentDisposition.Attachment),
					ContentTransferEncoding = ContentEncoding.Base64,
					FileName = attachmentName
				};

				attachments.Add (attachment);
			}

			return SendEmail (addrFrom, addrTo, addrCc, addrBcc, subject, textBody, attachments);
		}

		public static bool SendEmail (string addrFrom, IEnumerable<string> addrTo, IEnumerable<string> addrCc,
		                              IEnumerable<string> addrBcc, string subject, string textBody,
		                              IEnumerable<MimePart> attachments)
		{
			//try {
			//	var builder = new BodyBuilder ();
			//	var message = new MimeMessage ();

			//	message.From.Add (new MailboxAddress (string.Empty, addrFrom));

			//	if (addrTo != null) {
			//		foreach (var addr in addrTo) {
			//			message.To.Add (new MailboxAddress (string.Empty, addr));
			//		}
			//	}

			//	if (addrCc != null) {
			//		foreach (var addr in addrCc) {
			//			message.Cc.Add (new MailboxAddress (string.Empty, addr));
			//		}
			//	}

			//	if (addrBcc != null) {
			//		foreach (var addr in addrBcc) {
			//			message.Bcc.Add (new MailboxAddress (string.Empty, addr));
			//		}
			//	}

			//	message.Subject = subject;

			//	if (attachments == null) {
			//		message.Body = new TextPart ("plain") {
			//			Text = textBody
			//		};
			//	} else {
			//		var multipart = new Multipart ("mixed");

			//		multipart.Add (new TextPart ("plain") {
			//			Text = textBody
			//		});

			//		foreach (var attachment in attachments) {
			//			multipart.Add (attachment);
			//		}

			//		message.Body = multipart;
			//	}

			//	using (var client = new SmtpClient ()) {
			//		client.ServerCertificateValidationCallback = (s, c, h, e) => true;

			//		if (WebConfig.SmtpSsl) {
			//			client.Connect (WebConfig.SmtpServer, WebConfig.SmtpPort);
			//		} else {
			//			client.Connect (WebConfig.SmtpServer, WebConfig.SmtpPort, MailKit.Security.SecureSocketOptions.None);
			//		}

			//		// Note: since we don't have an OAuth2 token, disable
			//		// the XOAUTH2 authentication mechanism.
			//		client.AuthenticationMechanisms.Remove ("XOAUTH2");

			//		if (!string.IsNullOrWhiteSpace (WebConfig.SmtpUser)) {
			//			client.Authenticate (WebConfig.SmtpUser, WebConfig.SmtpPassword);
			//		}

			//		client.Send (message);
			//		client.Disconnect (true);
			//	}
			//} catch (Exception e) {
			//	Console.Error.WriteLine (e);
			//	return false;
			//}

			//return true;

			return SendEmail (addrFrom, addrTo, addrCc, addrBcc, subject, textBody, attachments, TextPartSubtype.Plain);
		}

		public static bool SendEmail (string addrFrom, IEnumerable<string> addrTo, IEnumerable<string> addrCc,
					      IEnumerable<string> addrBcc, string subject, string textBody,
					      IEnumerable<MimePart> attachments, TextPartSubtype textPartSubtype)
		{
			try {
				var builder = new BodyBuilder ();
				var message = new MimeMessage ();
				var subtype = textPartSubtype == TextPartSubtype.Plain ? "plain" : "html";
				var clientId = WebConfig.GoogleClientId;
				var clientSecret = WebConfig.GoogleClientSecret;
				var receiver = new FixedPortCodeReceiver (WebConfig.SmtpPort);
				var refreshToken = "REFRESH_TOKEN";

				var credential = new UserCredential (
				    new GoogleAuthorizationCodeFlow (new GoogleAuthorizationCodeFlow.Initializer {
					    ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
					    Scopes = new [] { "https://mail.google.com/" }
				    }),
				    WebConfig.SmtpUser,
				    new TokenResponse { RefreshToken = refreshToken }
				);

				string accessToken = credential.GetAccessTokenForRequestAsync ().Result;

				message.From.Add (new MailboxAddress (string.Empty, addrFrom));

				if (addrTo != null) {
					foreach (var addr in addrTo) {
						message.To.Add (new MailboxAddress (string.Empty, addr));
					}
				}

				if (addrCc != null) {
					foreach (var addr in addrCc) {
						message.Cc.Add (new MailboxAddress (string.Empty, addr));
					}
				}

				if (addrBcc != null) {
					foreach (var addr in addrBcc) {
						message.Bcc.Add (new MailboxAddress (string.Empty, addr));
					}
				}

				message.Subject = subject;

				if (attachments == null) {
					message.Body = new TextPart (subtype) {
						Text = textBody
					};
				} else {
					var multipart = new Multipart ("mixed");

					multipart.Add (new TextPart (subtype) {
						Text = textBody
					});

					foreach (var attachment in attachments) {
						multipart.Add (attachment);
					}

					message.Body = multipart;
				}

				using (var client = new SmtpClient ()) {
					//client.ServerCertificateValidationCallback = (s, c, h, e) => true;

					//if (WebConfig.SmtpSsl) {
					//	client.Connect (WebConfig.SmtpServer, WebConfig.SmtpPort);
					//} else {
					//	client.Connect (WebConfig.SmtpServer, WebConfig.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
					//}

					// Note: since we don't have an OAuth2 token, disable
					// the XOAUTH2 authentication mechanism.
					//client.AuthenticationMechanisms.Remove ("XOAUTH2");
					client.Connect(WebConfig.SmtpServer, WebConfig.SmtpPort, SecureSocketOptions.StartTls);
					var oauth2 = new SaslMechanismOAuth2 (WebConfig.SmtpUser, accessToken);
					client.Authenticate (oauth2);
					client.Send (message);
					client.Disconnect (true);
					//client.Authenticate (new SaslMechanismOAuth2 (WebConfig.SmtpUser, accessToken));

					//if (!string.IsNullOrWhiteSpace (WebConfig.SmtpUser)) {
					//	client.Authenticate (WebConfig.SmtpUser, WebConfig.SmtpPassword);
					//}

					client.Send (message);
					client.Disconnect (true);
				}
			} catch (Exception e) {
				Console.Error.WriteLine (e);
				return false;
			}

			return true;
		}
	}

	public class FixedPortCodeReceiver : ICodeReceiver {
		private readonly int _port;

		public FixedPortCodeReceiver (int port)
		{
			_port = port;
		}

		public string RedirectUri => $"http://localhost:{_port}/";

		public async Task<AuthorizationCodeResponseUrl> ReceiveCodeAsync (AuthorizationCodeRequestUrl url, CancellationToken cancellationToken)
		{
			string authorizationUrl = url.Build ().ToString ();
			System.Diagnostics.Process.Start (new System.Diagnostics.ProcessStartInfo {
				FileName = authorizationUrl,
				UseShellExecute = true
			});

			var listener = new HttpListener ();
			listener.Prefixes.Add (RedirectUri);
			listener.Start ();

			var context = await listener.GetContextAsync ();
			var response = context.Response;
			string responseString = "<html><body>Autenticación completada. Puedes cerrar esta ventana.</body></html>";
			var buffer = System.Text.Encoding.UTF8.GetBytes (responseString);
			response.ContentLength64 = buffer.Length;
			await response.OutputStream.WriteAsync (buffer, 0, buffer.Length);
			response.OutputStream.Close ();

			listener.Stop ();

			var query = context.Request.QueryString;

			var code = context.Request.QueryString ["code"];
			var error = context.Request.QueryString ["error"];

			var responseUrl = new AuthorizationCodeResponseUrl {
				Code = code,
				Error = error
			};

			return responseUrl;
		}
	}
}