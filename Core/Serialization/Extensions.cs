using System;
using System.Text;
using BlockChanPro.Core.Contracts;
using Newtonsoft.Json;

namespace BlockChanPro.Core.Serialization
{
	public static class Extensions
	{
		//TODO: Implement fast and cross platform binary serialization, for now use integrated serialization
		public static byte[] ToOneDimention(this byte[][] source)
		{
			var resultCount = 0;
			for (var i = 0; i < source.Length; i++)
				resultCount += source[i].Length;
			var result = new byte[resultCount];
			for (var i = 0; i < resultCount; i += source[i].Length)
				source[i].CopyTo(result, i);
			return result;
		}

		public static byte[] ToBinary(this decimal dec)
		{
			int[] intArray = decimal.GetBits(dec);
			var dataSegments = new byte[intArray.Length][];
			for(var i = 0; i< intArray.Length;i++)
				dataSegments[i] = intArray[i].ToBinary();

			return dataSegments.ToOneDimention();
		}

		public static byte[] ToBinary(this long value)
		{
			return BitConverter.GetBytes(value);
		}

	    public static byte[] ToBinary(this ulong value)
	    {
	        return BitConverter.GetBytes(value);
	    }

		public static byte[] ToBinary(this int value)
		{
			return BitConverter.GetBytes(value);
		}

	    public static byte[] ToBinary(this uint value)
	    {
	        return BitConverter.GetBytes(value);
	    }

		public static byte[] ToBinary(this string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}


		public static byte[] ToBinary<T>(this T[] value, Func<T, byte[]> toBinary)
		{
			var dataSegmentCount = value.Length;
			var dataSegments = new byte[dataSegmentCount][];
			for (var i = 0; i < value.Length; i++)
				dataSegments[i] = toBinary(value[i]);

			return dataSegments.ToOneDimention();
		}

		public static byte[] ToBinary(this BlockData value)
		{
			var dataSegments = new[]
			{
				value.Index.ToBinary(),
				value.TimeStamp.ToBinary(),
				value.Message.ToBinary(),
				value.Transactions.ToBinary(ToBinary),
				value.PreviousHash.ToBinary()
			};
			return dataSegments.ToOneDimention();
		}

		public static byte[] ToBinary(this Transaction value)
		{
			var dataSegments = new[]
			{
				value.Sender.ToBinary(),
				value.Recipients.ToBinary(ToBinary),
				value.Fee.ToBinary(),
				value.TimeStamp.ToBinary(),
			};
			return dataSegments.ToOneDimention();
		}

		public static byte[] ToBinary(this Recipient value)
		{
			var dataSegments = new[]
			{
				value.Address.ToBinary(),
				value.Amount.ToBinary(),
			};
			return dataSegments.ToOneDimention();
		}

		public static byte[] ToBinary(this Address value)
		{
			return value.Value.ToBinary();
		}

		public static byte[] ToBinary(this Hash value)
		{
			return value.Value;
			//return value.Value.GetRawData(BitConverter.GetBytes);
		}

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
	}
}