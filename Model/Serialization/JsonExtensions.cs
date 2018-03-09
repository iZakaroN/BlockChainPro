using System;
using System.Linq;
using Newtonsoft.Json;

namespace BlockChanPro.Model.Serialization
{
    public static class JsonExtensions
    {
	    public static string SerializeToJson<T>(this T value, Formatting formatting = Formatting.None)
	    {
		    return JsonConvert.SerializeObject(value, formatting);
	    }

	    public static T DeserializeFromJson<T>(this string value)
	    {
		    return JsonConvert.DeserializeObject<T>(value);
	    }

	    public static bool TryDeserializeFromJson<T>(this string input, out T output)
	    {
		    try
		    {
			    output = input.DeserializeFromJson<T>();
			    return true;
		    }
		    catch (JsonSerializationException)
		    {
			    output = default(T);
			    return false;
		    }
	    }

	    public static byte[] SerializeToBinary<T>(this T value)
	    {
		    var serializedValue = JsonConvert.SerializeObject(value);
		    return serializedValue.ToBinary();
	    }

	    public static string ToHexString(this byte[] bytes)
	    {
		    return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLower();
	    }

	    public static byte[] ParseHex(this string hex)
	    {
		    var result = Enumerable.Range(0, hex.Length)
			    .Where(x => x % 2 == 0)
			    .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
			    .ToArray();
		    return result;
	    }

    }
}
