using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace YoutubeExplode.Utils.Extensions
{
    internal static class JsonExtensions
    {
        public static JToken? GetPropertyOrNull(this JToken? token, string propertyName)
        {
            if (token == null || token.Type != JTokenType.Object)
            {
                return null;
            }

            var result = token[propertyName];
            if (result != null && result.Type != JTokenType.Null && result.Type != JTokenType.Undefined)
            {
                return result;
            }

            return null;
        }

        public static string? GetStringOrNull(this JToken? token)
        {
            return token?.Type == JTokenType.String
                ? token.Value<string>()
                : null;
        }

        public static int? GetInt32OrNull(this JToken? token)
        {
            return token?.Type == JTokenType.Integer
                ? token.Value<int>()
                : (int?)null;
        }

        public static long? GetInt64OrNull(this JToken? token)
        {
            return token?.Type == JTokenType.Integer
                ? token.Value<long>()
                : (long?)null;
        }

        public static IEnumerable<JToken>? EnumerateArrayOrNull(this JToken? token)
        {
            return token?.Type == JTokenType.Array
                ? token.Children()
                : null;
        }

        public static IEnumerable<JToken> EnumerateArrayOrEmpty(this JToken? token)
        {
            return token?.EnumerateArrayOrNull() ?? Enumerable.Empty<JToken>();
        }

        public static IEnumerable<JProperty>? EnumerateObjectOrNull(this JToken? token)
        {
            return token?.Type == JTokenType.Object
                ? token.Children<JProperty>()
                : null;
        }

        public static IEnumerable<JProperty> EnumerateObjectOrEmpty(this JToken? token)
        {
            return token?.EnumerateObjectOrNull() ?? Enumerable.Empty<JProperty>();
        }

        public static IEnumerable<JToken> EnumerateDescendantProperties(this JToken? token, string propertyName)
        {
            if (token == null)
            {
                yield break;
            }

            var property = token.GetPropertyOrNull(propertyName);
            if (property != null)
                yield return property;

            var deepArrayDescendants = token
                .EnumerateArrayOrEmpty()
                .SelectMany(j => j.EnumerateDescendantProperties(propertyName));

            foreach (var deepDescendant in deepArrayDescendants)
                yield return deepDescendant;

            var deepObjectDescendants = token
                .EnumerateObjectOrEmpty()
                .SelectMany(j => j.Value.EnumerateDescendantProperties(propertyName));

            foreach (var deepDescendant in deepObjectDescendants)
                yield return deepDescendant;
        }
    }
}
