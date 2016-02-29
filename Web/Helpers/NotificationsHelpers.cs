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
using System.Net.Mail;

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
			var attachments = new List<Attachment>();

			if (attachmentContent != null) {
				attachments.Add (new Attachment (attachmentContent, attachmentName));
			}

			return SendEmail (addrFrom, addrTo, addrCc, addrBcc, subject, textBody, attachments);
		}

		public static bool SendEmail (string addrFrom, IEnumerable<string> addrTo, IEnumerable<string> addrCc,
		                              IEnumerable<string> addrBcc, string subject, string textBody,
		                              IEnumerable<Attachment> attachments)
		{
			try {
				var smtp = new SmtpClient {
					Host = WebConfig.SmtpServer,
					Port = WebConfig.SmtpPort,
					EnableSsl = WebConfig.SmtpSsl,
					DeliveryMethod = SmtpDeliveryMethod.Network,
					UseDefaultCredentials = false,
					Credentials = new NetworkCredential(WebConfig.SmtpUser, WebConfig.SmtpPassword)
				};

				using (var message = new MailMessage()) {
					message.From = new MailAddress (addrFrom);

					if (addrTo != null) {
						foreach (var addr in addrTo) {
							message.To.Add (new MailAddress (addr));
						}
					}

					if (addrCc != null) {
						foreach (var addr in addrCc) {
							message.CC.Add (new MailAddress (addr));
						}
					}

					if (addrBcc != null) {
						foreach (var addr in addrBcc) {
							message.Bcc.Add (new MailAddress (addr));
						}
					}

					message.Subject = subject;
					message.BodyEncoding =  System.Text.Encoding.UTF8;
					message.SubjectEncoding =  System.Text.Encoding.UTF8;
					message.IsBodyHtml = false;
					message.Body = textBody;

					if (attachments != null) {
						foreach (var attachment in attachments) {
							message.Attachments.Add (attachment);
						}
					}

					smtp.Send (message);
				}
			} catch(Exception e) {
				Console.Error.WriteLine (e);
				return false;
			}

			return true;
		}
    }
}