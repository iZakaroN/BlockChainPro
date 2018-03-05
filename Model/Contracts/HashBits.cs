using System;
using System.Linq;
using BlockChanPro.Model.Serialization;
using Newtonsoft.Json;

namespace BlockChanPro.Model.Contracts
{
    public class HashBits
    {
		private const int AdjustmentPercentLimit = 50;
	    public static readonly HashBits MinTarget = new HashBits(0x00, 0xffffffffffffff);
        public static readonly HashBits GenesisTarget = new HashBits(0x0f, 0xffffffffffffff);
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

        public HashBits Adjust(long currentTimeDelta, long targetTimeDelta, int adjustmentPercentLimit = AdjustmentPercentLimit, HashBits minTarget = null)
        {
	        minTarget = minTarget ?? GenesisTarget;
	        //Adjustment log
			/*var coefficient = (decimal)targetTimeDelta / currentTimeDelta;
	        Console.WriteLine($"@@@");
	        Console.WriteLine($"@ Adjustment -> currentTimeDelta: {TimeSpan.FromTicks(currentTimeDelta)}, targetTimeDelta: {TimeSpan.FromTicks(targetTimeDelta)}, Expected coefficient: {coefficient}");*/

			if (targetTimeDelta != currentTimeDelta)
            {
                int offsetAdjust = 0;
                ulong fractionAdjust;
                if (targetTimeDelta < currentTimeDelta)
                {
	                var limit = targetTimeDelta + targetTimeDelta * adjustmentPercentLimit / 100; // X(1+A)
					if (currentTimeDelta > limit)
						currentTimeDelta = limit;

					while (targetTimeDelta < currentTimeDelta)
                    {
                        targetTimeDelta <<= 1;
                        offsetAdjust -= 1;
                    }

                    // TODO: optimize using intermediate bit shifts instead of floating calculations
                    //Because fraction offset was moved above the target (by power of 2), 
                    //reduce fraction itself with (1/2 < fractionMultiplyer < 1) to match the exact target
                    var fractionMultiplyer = (decimal)currentTimeDelta / targetTimeDelta;
                    //Because last byte of the fraction is reserved for offset, there will be a space for one bit shift
                    //so fraction can be normalized even if high bit goes away from fraction reduction
                    fractionAdjust = (ulong)((GetFraction() << 1) * fractionMultiplyer);
                    offsetAdjust++;
                }
                else
                {
	                var limit = targetTimeDelta * 100 / (100 + adjustmentPercentLimit);// X/(1+A)
					if (currentTimeDelta < limit)
		                currentTimeDelta = limit;


					while (currentTimeDelta < targetTimeDelta)
                    {
                        currentTimeDelta <<= 1;
                        offsetAdjust += 1;
                    }

					// TODO: optimize using intermediate bit shifts instead of floating calculations
					//Because fraction offset was moved below the target (by power of 2), 
					//increase fraction itself with (1 < fractionMultiplyer < 2) to match the exact target
					var fractionMultiplyer = (decimal)currentTimeDelta / targetTimeDelta;
					//Because last byte of the fraction is reserved for offset, there will be a space for one bit if necessary
					fractionAdjust = (ulong)(GetFraction() * fractionMultiplyer);

                }
                // In case fraction was increased Adjust(Normalize) fraction/offset to match their masks
                if ((fractionAdjust & OffsetMask) != 0)
                {
                    fractionAdjust >>= 1;
                    offsetAdjust--;
                }

	            var newOffset = GetBitOffset() + offsetAdjust;
	            //Adjustment log
	            /*var currentCoefficient = ((decimal) GetFraction() / fractionAdjust) * (decimal)Math.Pow(2, -(GetBitOffset()- newOffset));
				Console.WriteLine($"@ Adjustment -> Current: {currentCoefficient} (o:{newOffset:x1}, f:{fractionAdjust:x16})");*/

				if (newOffset < minTarget.GetBitOffset())
                    return minTarget;
                else if (newOffset > OffsetMax)
                    return new HashBits(OffsetMax, fractionAdjust >> (newOffset - OffsetMax));
                return new HashBits((byte)newOffset, fractionAdjust);

            }
            return this;
        }

	    public override string ToString()
	    {
		    return $"{GetType().Name}({this.SerializeToJson()})";
	    }

	    public long Difficulty(HashBits genesisTarget)
	    {
		    return (long) ((decimal)Math.Pow(2, GetBitOffset() - genesisTarget.GetBitOffset()) *((decimal)genesisTarget.GetFraction() / GetFraction()));

	    }
    }
}
