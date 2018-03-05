using System;
using System.Linq;
using Newtonsoft.Json;


namespace BlockChanPro.Core.Serialization
{
	/// <summary>
	/// Source: https://stackoverflow.com/questions/11829035/newton-soft-json-jsonserializersettings-for-object-with-property-as-byte-array
	/// </summary>
    public class BytesToHexConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(byte[]);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var hex = serializer.Deserialize<string>(reader);
                if (!string.IsNullOrEmpty(hex))
                {
	                return hex.ParseHex();
                }
            }
            return Enumerable.Empty<byte>();
        }

	    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
	        if (value is byte[] bytes)
	        {
		        var @string = bytes.ToHexString();
		        serializer.Serialize(writer, @string);
	        } else
				throw new ArgumentException(nameof(value));
        }
    }

    public sealed class HexStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(uint) == objectType;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue($"0x{value:x16}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var str = reader.ReadAsString();
            if (str == null || !str.StartsWith("0x"))
                throw new JsonSerializationException();
            return Convert.ToUInt32(str);
        }
    }
}
