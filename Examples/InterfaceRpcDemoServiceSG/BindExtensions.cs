using Carter.Request;

namespace Carter.ModelBinding
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    public static class BindExtensions
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

        private static object ConvertToType(string value, Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);

            if (value.Length > 0)
            {
                if (type == typeof(DateTime) || underlyingType == typeof(DateTime))
                {
                    return DateTime.Parse(value, CultureInfo.InvariantCulture);
                }

                if (type == typeof(Guid) || underlyingType == typeof(Guid))
                {
                    return new Guid(value);
                }

                if (type == typeof(Uri) || underlyingType == typeof(Uri))
                {
                    if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri uri))
                    {
                        return uri;
                    }

                    return null;
                }
            }
            else
            {
                if (type == typeof(Guid))
                {
                    return default(Guid);
                }

                if (underlyingType != null)
                {
                    return null;
                }
            }

            if (underlyingType is object)
            {
                return Convert.ChangeType(value, underlyingType);
            }

            return Convert.ChangeType(value, type);
        }



    }
}
