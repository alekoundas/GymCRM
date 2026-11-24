using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Core.Dtos;

namespace API.Tests.Contracts
{
    /// <summary>
    /// What a type looks like in the app's json: the names of its properties as they travel, and what
    /// is in each.
    /// </summary>
    internal static class JsonShape
    {
        /// <summary>
        /// The type in a word or two, the same on every machine.
        /// </summary>
        public static string Name(Type type)
        {
            Type? underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
                return Name(underlying) + "?";

            if (type == typeof(byte[]))
                return "base64";

            if (type.IsArray)
                return Name(type.GetElementType()!) + "[]";

            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type) && type.GetGenericArguments().Length == 1)
                return Name(type.GetGenericArguments()[0]) + "[]";

            if (type.IsGenericType)
                return $"{type.Name.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>";

            return type.Name switch
            {
                "String" => "text",
                "Int32" => "number",
                "Boolean" => "yes or no",
                "DateTime" => "date",
                "Guid" => "id",
                _ => type.Name
            };
        }

        /// <summary>
        /// Each of the types and every type in them, one after another by name.
        /// </summary>
        public static string Describe(IEnumerable<Type> types, JsonSerializerOptions options)
        {
            SortedDictionary<string, string> shapes = new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (Type type in types)
                Collect(type, options, shapes);

            return string.Join("\n", shapes.Values);
        }

        private static void Collect(Type type, JsonSerializerOptions options, SortedDictionary<string, string> shapes)
        {
            Type plain = Nullable.GetUnderlyingType(type) ?? type;

            if (plain.IsArray)
            {
                Collect(plain.GetElementType()!, options, shapes);
                return;
            }

            if (plain.IsEnum)
            {
                shapes[plain.Name] = $"{plain.Name}\n  {string.Join(", ", Enum.GetNames(plain))}\n";
                return;
            }

            foreach (Type argument in plain.GetGenericArguments())
                Collect(argument, options, shapes);

            // Once, whatever it is of.
            Type definition = plain.IsGenericType ? plain.GetGenericTypeDefinition() : plain;

            if (definition.Assembly != typeof(ApiResponse<>).Assembly || shapes.ContainsKey(Name(definition)))
                return;

            shapes[Name(definition)] = "";
            JsonTypeInfo info = options.GetTypeInfo(plain);
            StringBuilder text = new StringBuilder().Append(Name(definition)).Append('\n');

            foreach (JsonPropertyInfo property in info.Properties.OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                Type propertyType = definition.GetProperty(((PropertyInfo)property.AttributeProvider!).Name)!.PropertyType;

                text.Append($"  {property.Name}: {Name(propertyType)}{(property.Set == null ? ", only sent" : "")}{(property.IsRequired ? ", required" : "")}\n");
            }

            shapes[Name(definition)] = text.ToString();

            foreach (JsonPropertyInfo property in info.Properties)
                Collect(property.PropertyType, options, shapes);
        }
    }
}
