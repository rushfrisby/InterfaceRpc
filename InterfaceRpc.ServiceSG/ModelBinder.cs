using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InterfaceRpc.ServiceSG
{
    internal class ModelBinder
    {
        /// <summary>
        /// Bind the incoming request body to a model
        /// </summary>
        /// <param name="request">Current <see cref="HttpRequest"/></param>
        /// <typeparam name="T">Model type</typeparam>
        /// <returns>Bound model</returns>
        public static async Task<T> Bind<T>(this HttpRequest request)
        {
            if (request.HasFormContentType)
            {
                var res = request.Form.ToDictionary(key => key.Key, val =>
                {
                    var type = typeof(T);
                    var propertyType = type.GetProperty(val.Key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)?.PropertyType;

                    if (propertyType == null)
                    {
                        return null;
                    }

                    if (propertyType.IsArray() || propertyType.IsCollection() || propertyType.IsEnumerable())
                    {
                        var colType = propertyType.GetElementType();
                        if (colType == null)
                        {
                            colType = propertyType.GetGenericArguments().First();
                        }

                        return val.Value.Select(y => ConvertToType(y, colType));
                    }

                    return ConvertToType(val.Value[0], propertyType);
                });

                var json = JsonSerializer.Serialize(res);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            var binder = (IModelBinder)request.HttpContext.RequestServices.GetService(typeof(IModelBinder));
            return await binder.Bind<T>(request);
        }
    }
}
