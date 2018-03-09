using BlockChanPro.Model.Contracts;
using System;
using BlockChanPro.Model.Serialization;
using System.Linq;

namespace BlockChanPro.Core.Contracts
{
    public static class HashExtensions
    {
	    public static Hash ToHash(this HashBits @this)
	    {
		    // TODO: make it to shift bits instead bytes
		    var result = Genesis.Hash;
		    var bitsOffset = @this.GetBitOffset();
		    var bytesOffset = bitsOffset / Hash.SegmentBitSize;
		    var byteBitsOffset = bitsOffset % Hash.SegmentBitSize;
		    var byteBitsFractionMask = Hash.SegmentMask >> byteBitsOffset;
		    var byteBitsReminderMask = ~byteBitsFractionMask;
		    var byteBitsReminderOffset = Hash.SegmentBitSize - byteBitsOffset;
		    var fractionBytes = @this.GetFraction().ToBinary().Reverse().Skip(HashBits.OffsetByteSize).ToArray();//reverse bigendian to lowendian and skip bytes reserverd for offset
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

	    public static HashBits Adjust(this HashBits @this, long currentTimeDelta, long targetTimeDelta, int adjustmentPercentLimit = Genesis.AdjustmentPercentLimit, HashBits minTarget = null)
	    {
		    minTarget = minTarget ?? Genesis.Target;
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
				    fractionAdjust = (ulong)((@this.GetFraction() << 1) * fractionMultiplyer);
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
				    fractionAdjust = (ulong)(@this.GetFraction() * fractionMultiplyer);

			    }
			    // In case fraction was increased Adjust(Normalize) fraction/offset to match their masks
			    if ((fractionAdjust & HashBits.OffsetMask) != 0)
			    {
				    fractionAdjust >>= 1;
				    offsetAdjust--;
			    }

			    var newOffset = @this.GetBitOffset() + offsetAdjust;
			    //Adjustment log
			    /*var currentCoefficient = ((decimal) GetFraction() / fractionAdjust) * (decimal)Math.Pow(2, -(GetBitOffset()- newOffset));
			    Console.WriteLine($"@ Adjustment -> Current: {currentCoefficient} (o:{newOffset:x1}, f:{fractionAdjust:x16})");*/

			    if (newOffset < minTarget.GetBitOffset())
				    return minTarget;
			    else if (newOffset > HashBits.OffsetMax)
				    return HashBits.Create(HashBits.OffsetMax, fractionAdjust >> (newOffset - HashBits.OffsetMax));
			    return HashBits.Create((byte)newOffset, fractionAdjust);

		    }
		    return @this;
	    }

	    public static long Difficulty(this HashBits @this, HashBits genesisTarget)
	    {
		    return (long)((decimal)Math.Pow(2, @this.GetBitOffset() - genesisTarget.GetBitOffset()) * ((decimal)genesisTarget.GetFraction() / @this.GetFraction()));

	    }
    }
}
