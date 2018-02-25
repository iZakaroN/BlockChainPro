using System.Linq;
using Core.Serialization;
using Newtonsoft.Json;

namespace Core.Contracts
{
    public class TargetHashBits
    {
        public static readonly TargetHashBits Genesis = new TargetHashBits(0, 0xffffffffffffff);
        public const int OffsetByteSize = sizeof(byte);
        public const int OffsetBitSize = sizeof(byte) * 8;
        private const int OffsetShift = (sizeof(ulong) - OffsetByteSize) * 8;
        public const ulong OffsetMask = ~0ul << OffsetShift;
        private const int FractionBits = OffsetShift;
        private const byte OffsetMax = Hash.BitLength - FractionBits;

        public TargetHashBits(ulong value)
        {
            Value = value;
        }

        public TargetHashBits(byte offset, ulong fraction)
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

        public Hash ToHash()
        {
            // TODO: make it to shift bits instead bytes
            var result = Hash.Genesis;
            var bitsOffset = GetBitOffset();
            var bytesOffset = bitsOffset / Hash.SegmentBitSize;
            var byteBitsOffset = bitsOffset % Hash.SegmentBitSize;
            var byteBitsFractionMask = Hash.SegmentMask >> byteBitsOffset;
            var byteBitsReminderMask = ~byteBitsFractionMask;
            var byteBitsReminderOffset = Hash.SegmentBitSize - byteBitsOffset;
            var fractionBytes = GetFraction().ToBinary().Reverse().Skip(OffsetByteSize).ToArray();//reverse bigendian to lowendian and skip bytes reserverd for offset
            byte reminderBits = 0;
            int i = 0;
            for (; i < fractionBytes.Length && bytesOffset + i < Hash.SegmentsLength; i++)
            {
                var fractionBits = (byte)((fractionBytes[i] >> byteBitsOffset) & byteBitsFractionMask);
                var currentBits = (byte)(fractionBits | reminderBits);
                reminderBits = (byte)((fractionBytes[i] << byteBitsReminderOffset) & byteBitsReminderMask);
                result.Value[bytesOffset + i] = currentBits;
            }
            if (bytesOffset + i < Hash.SegmentsLength)
                result.Value[bytesOffset + i] = reminderBits;
            /*else
                //Loose precision as reminder do not fit in lowest byte in hash
             */

            return result;
        }

        public TargetHashBits Adjust(long currentTimeDelta, long targetTimeDelta)
        {
            if (targetTimeDelta != currentTimeDelta)
            {
                int offset = 0;
                ulong fraction;
                if (targetTimeDelta < currentTimeDelta)
                {
                    while (targetTimeDelta < currentTimeDelta)
                    {
                        targetTimeDelta <<= 1;
                        offset -= 1;
                    }

                    // TODO: optimize using intermediate bit shifts instead of decimal calulations
                    //Because fraction offset was moved above the target (by power of 2), 
                    //now reduce fraction itself with (1/2 < fractionMultiplyer < 1) to match the exact target
                    var fractionMultiplyer = (decimal)currentTimeDelta / targetTimeDelta;
                    //Because last byte of the fraction is reserved for offset, there will be a space for one bit shift
                    //so fraction can be normalized even if high bit goes away from fraction reduction
                    fraction = (ulong)((GetFraction() << 1) * fractionMultiplyer);
                    offset++;
                }
                else
                {
                    while (currentTimeDelta < targetTimeDelta)
                    {
                        currentTimeDelta <<= 1;
                        offset += 1;
                    }

                    // TODO: optimize using intermediate bit shifts instead of decimal calulations
                    //Because fraction offset was moved below the target (by power of 2), 
                    //now increase fraction itself with (1 < fractionMultiplyer < 2) to match the exact target
                    var fractionMultiplyer = (decimal)currentTimeDelta / targetTimeDelta;
                    //Because last byte of the fraction is reserved for offset, there will be a space for one bit if necessary
                    fraction = (ulong)(GetFraction() * fractionMultiplyer);

                }
                // In case fraction was increased Adjust(Normalize) fraction/offset to match their masks
                if ((fraction & OffsetMask) != 0)
                {
                    fraction >>= 1;
                    offset--;
                }
                offset = GetBitOffset() + offset;
                if (offset<0)
                    return Genesis;
                else if (offset > OffsetMax)
                    return new TargetHashBits(OffsetMax, fraction >> (offset - OffsetMax));
                return new TargetHashBits((byte)offset, fraction);

            }
            return this;
        }


    }
}
