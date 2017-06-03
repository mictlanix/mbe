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
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;

namespace Mictlanix.BE.Web.Helpers {
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
					ContentObject = new ContentObject (attachmentContent, ContentEncoding.Default),
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
			try {
				var builder = new BodyBuilder ();
				var message = new MimeMessage ();

				message.From.Add (new MailboxAddress (addrFrom));

				if (addrTo != null) {
					foreach (var addr in addrTo) {
						message.To.Add (new MailboxAddress (addr));
					}
				}

				if (addrCc != null) {
					foreach (var addr in addrCc) {
						message.Cc.Add (new MailboxAddress (addr));
					}
				}

				if (addrBcc != null) {
					foreach (var addr in addrBcc) {
						message.Bcc.Add (new MailboxAddress (addr));
					}
				}

				message.Subject = subject;

				if (attachments == null) {
					message.Body = new TextPart ("plain") {
						Text = textBody
					};
				} else {
					var multipart = new Multipart ("mixed");

					multipart.Add (new TextPart ("plain") {
						Text = textBody
					});

					foreach (var attachment in attachments) {
						multipart.Add (attachment);
					}

					message.Body = multipart;
				}

				using (var client = new SmtpClient ()) {
					client.ServerCertificateValidationCallback = (s, c, h, e) => true;

					client.Connect (WebConfig.SmtpServer, WebConfig.SmtpPort, WebConfig.SmtpSsl);

					// Note: since we don't have an OAuth2 token, disable
					// the XOAUTH2 authentication mechanism.
					client.AuthenticationMechanisms.Remove ("XOAUTH2");

					if (!string.IsNullOrWhiteSpace (WebConfig.SmtpUser)) {
						client.Authenticate (WebConfig.SmtpUser, WebConfig.SmtpPassword);
					}

					client.Send (message);
					client.Disconnect (true);
				}
			} catch(Exception e) {
				Console.Error.WriteLine (e);
				return false;
			}

			return true;
		}
    }
}