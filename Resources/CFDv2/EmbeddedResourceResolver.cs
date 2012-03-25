using System;
using System.Reflection;
using System.IO;
using System.Xml;

namespace Mictlanix.CFDv2
{
    public class EmbeddedResourceResolver : XmlUrlResolver
    {
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            return GetType().Assembly.GetManifestResourceStream(GetType(), Path.GetFileName(absoluteUri.AbsolutePath));
        }

        public Stream GetResource(string name)
        {
            return GetType().Assembly.GetManifestResourceStream(GetType(), name);
        }
    }
}