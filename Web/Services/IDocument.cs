using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;
using Mictlanix.BE.Web.Models;

namespace Mictlanix.BE.Web.Services {
	interface IDocument {
		Result<IDocument> Complete (IDocument document);
		Result<IDocument> Cancel (IDocument document);
		Result<IDocument> View (IDocument document);
		Result<IDocument> Edit (IDocument document);
		Result<IDocument> Print (IDocument document);
		Result<IDocument> Search (Search<IDocument> search);
	}
}
