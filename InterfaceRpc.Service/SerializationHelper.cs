using Microsoft.AspNetCore.Http;
using SerializerDotNet;
using System.IO;

namespace InterfaceRpc.Service
{
	internal class SerializationHelper
	{
		// There are no references to this method because it is called via reflection. Do not remove!
		private static TResult GetRequestEntity<TResult>(HttpContext context)
		{
			byte[] result;
			using (var ms = new MemoryStream())
			{
				context.Request.Body.CopyTo(ms);
				result = ms.ToArray();
			}
            var contentType = GetContentType(context.Request);
			var serializer = Serializer.GetSerializerFor(contentType);
			return serializer.Deserialize<TResult>(result);
		}

		// There are no references to this method because it is called via reflection. Do not remove!
		public static T Cast<T>(object o)
		{
			return (T)o;
		}

        public static string GetContentType(HttpRequest request)
        {
            return !string.IsNullOrWhiteSpace(request.ContentType) ?
                request.ContentType.Trim() :
                Constants.DefaultContentType;
        }
	}
}
