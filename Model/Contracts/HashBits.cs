using BlockChanPro.Model.Serialization;
using Newtonsoft.Json;

namespace BlockChanPro.Model.Contracts
{
    public class HashBits
    {
	    public static readonly HashBits MinTarget = new HashBits(0x00, 0xffffffffffffff);
        public const int OffsetByteSize = sizeof(byte);
        public const int OffsetBitSize = sizeof(byte) * 8;
        private const int OffsetShift = (sizeof(ulong) - OffsetByteSize) * 8;
        public const ulong OffsetMask = ~0ul << OffsetShift;
        private const int FractionBits = OffsetShift;
        public const byte OffsetMax = Hash.BitLength - FractionBits;

        public HashBits(ulong value)
        {
            Value = value;
        }

        public HashBits(byte offset, ulong fraction)
        {
            Contract.Requires(offset <= OffsetMax, nameof(offset));
            Contract.Requires((fraction & ~OffsetMask) == fraction, nameof(fraction));
            var offsetEncoded = (ulong)offset << OffsetShift;
            Value = offsetEncoded | fraction;
        }

        [JsonConverter(typeof(HexStringJsonConverter))]
        public ulong Value { get; }

        /// <summary>
        /// The offset in bits of hash Fraction from highest hash bit (>>)
        /// </summary>
        public byte GetBitOffset()
        {
            return (byte)((Value & OffsetMask) >> OffsetShift);
        }

        public ulong GetFraction()
        {
            return Value & ~OffsetMask;
        }

	    public override string ToString()
	    {
		    return $"{GetType().Name}({this.SerializeToJson()})";
	    }
}
}
