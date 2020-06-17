using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Looker.RTL
{
    /// <summary>
    /// Sweet extension methods for syntactic sugar
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Merge assigned properties from <c>source</c> into <c>dest</c> 
        /// </summary>
        /// <remarks>
        /// Sorta similar to ECMAScript spread operator.
        /// </remarks>
        /// <example>
        /// settings = settings.Merge(AuthSession.Settings);
        /// </example>
        /// <param name="dest">Destination object</param>
        /// <param name="source">Source object</param>
        /// <typeparam name="T">Type of object (homogeneous)</typeparam>
        /// <returns><c>dest</c> with merged values</returns>
        public static T Merge<T>(this T dest, T source)
        {
            if (dest == null) return source;
            var t = typeof(T);
            var properties = t.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source, null);
                if (value != null)
                    prop.SetValue(dest, value, null);
            }

            return dest;
        }

        public static bool IsNullOrEmpty(this string value) => string.IsNullOrEmpty(value);

        public static bool IsFull(this string value) => !string.IsNullOrEmpty(value);
    }
    
    public static class SdkUtils
    {
        /// <summary>
        /// Encode parameter if not already encoded 
        /// </summary>
        /// <param name="value">any value</param>
        /// <returns>The Url encoded string of the parameter</returns>
        public static string EncodeParam(object value)
        {
            string encoded;
            switch (value)
            {
                case null:
                    return "";
                case DateTime time:
                {
                    var d = time;
                    encoded = d.ToString("O");
                    break;
                }
                case bool toggle:
                {
                    encoded = Convert.ToString(toggle).ToLowerInvariant();
                    break;
                }
                default:
                    encoded = Convert.ToString(value);
                    break;
            }

            var decoded = WebUtility.UrlDecode(encoded);
            if (encoded == decoded)
            {
                encoded = WebUtility.UrlEncode(encoded);
            }

            return encoded;
        }

        /// <summary>
        /// Converts <c>values</c> to query string parameter format
        /// </summary>
        /// <param name="values">Name/value collection to encode</param>
        /// <returns>Query string parameter formatted values. <c>null</c> values are omitted.</returns>
        public static string EncodeParams(Values values = null)
        {
            if (values == null) return "";

            var args = values
                .Where(pair =>
                    pair.Value != null || (pair.Value is string && pair.Value.ToString().IsFull()))
                .Select(x => $"{x.Key}=encodeParam(x.Value)");

            return string.Join("&", args);
        }

        /// <summary>
        /// constructs the path argument including any optional query parameters 
        /// </summary>
        /// <param name="path">the current path of the request</param>
        /// <param name="obj">optional collection of query parameters to encode and append to the path</param>
        /// <returns>path + any query parameters as a URL string</returns>
        public static string AddQueryParams(string path, Values obj = null)
        {
            if (obj == null)
            {
                return path;
            }

            var sb = new StringBuilder(path);
            var qp = EncodeParams(obj);
            if (qp.IsNullOrEmpty()) return sb.ToString();
            sb.Append("?");
            sb.Append(qp);

            return sb.ToString();
        }
        
        /// <summary>
        /// Awaits the SDK response task and returns either success or throws the error
        /// </summary>
        /// <param name="task">SDK response to await</param>
        /// <typeparam name="TSuccess">Success type</typeparam>
        /// <typeparam name="TError">Error type</typeparam>
        /// <returns><c>TSuccess</c> or throws <c>TError</c></returns>
        /// <exception cref="SdkError"></exception>
        public static async Task<TSuccess> Ok<TSuccess, TError>(Task<SdkResponse<TSuccess, TError>> task)
            where TSuccess : class where TError : Exception
        {
            var result = await task;
            if (result.Ok)
            {
                return result.Value;
            }

            throw result.Error;
        }

        public static ResponseMode ResponseMode(string contentType)
        {
            if (contentType.IsNullOrEmpty()) return RTL.ResponseMode.Unknown;
            
            if (Constants.ContentPatternString.IsMatch(contentType))
            {
                return RTL.ResponseMode.String;
            }

            if (Constants.ContentPatternBinary.IsMatch(contentType))
            {
                return RTL.ResponseMode.Binary;
            }

            return RTL.ResponseMode.Unknown;
        }

        /// <summary>
        /// Read all input from a stream into a byte array
        /// </summary>
        /// <param name="inStream">input stream</param>
        /// <returns>byte array of input stream content</returns>
        public static byte[] ReadAllBytes(Stream inStream)
        {
            using var outStream = new MemoryStream();
            inStream.CopyTo(outStream);
            return outStream.ToArray();
        }

        /// <summary>
        /// Convert a stream to a byte array
        /// </summary>
        /// <param name="stream">input stream</param>
        /// <returns>byte array of input stream content</returns>
        public static byte[] StreamToByteArray(Stream stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                return memoryStream.ToArray();
            }
            else
            {
                return ReadAllBytes(stream);
            }
        }        
    }
}