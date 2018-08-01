using SerializerDotNet;
using System.IO;
using System.Net;

namespace InterfaceRpc.Service
{
	internal class SerializationHelper
	{
		// There are no references to this method because it is called via reflection. Do not remove!
		private static TResult GetRequestEntity<TResult>(HttpListenerContext context)
		{
			byte[] result;
			using (var ms = new MemoryStream())
			{
				context.Request.InputStream.CopyTo(ms);
				result = ms.ToArray();
			}
			var serializer = Serializer.GetSerializerFor(context.Request.ContentType);
			return serializer.Deserialize<TResult>(result);
		}
	}
}
