using System;
using System.Collections.Generic;
using BlockChanPro.Core.Serialization;
using Newtonsoft.Json;

namespace BlockChanPro.Core.Contracts
{
    public struct Hash
	{
		public const int BitLength = 256;
        public const int SegmentByteSize = sizeof(byte);
        public const int SegmentBitSize = SegmentByteSize * 8;
		public const int SegmentsLength = BitLength / SegmentBitSize;
	    public static byte SegmentMask = (byte)(~0ul >> (sizeof(ulong) * 8 - SegmentBitSize));
	    public static long SegmentValueLimit = SegmentMask + 1;


        public static Hash Genesis => new Hash(new byte[]
		{
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
		});

		/*public static Hash GenesisTargetHash = new Hash(new byte[]
        {
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
        });*/

        public Hash(byte[] hashSegments)
		{
			Contract.Requires(hashSegments.Length == SegmentsLength, $"{nameof(hashSegments)}");

			Value = hashSegments;
		}

        public void Increment(long value)
        {

            var i = Value.Length - 1;
            var segmentIncrease = value;
            do
            {
                var increase = Value[i] + segmentIncrease;
                Value[i] = (byte)(increase % SegmentValueLimit);
                segmentIncrease = increase / SegmentValueLimit;
                i--;
            } while (i >= 0 && segmentIncrease > 0);
            Contract.Requires<OverflowException>(segmentIncrease == 0);
        }

        /// <summary>
        /// Compare a hash with a target hash
        /// </summary>
        /// <param name="target">Target hash ;)</param>
        /// <returns>
        /// < : -1
        /// == : 0
        /// > : 1
        /// </returns>
        public int Compare(Hash target)
        {
            var i = 0;
            while (i < Value.Length - 1 && Value[i] == target.Value[i])
                i++;
            return Value[i] == target.Value[i] ? 0 : Value[i] < target.Value[i] ? -1 : 1;
        }

        private bool CompareOperation(Hash target, Func<Hash,Hash, bool> operation)
        {
            var i = 0;
            while (i < Value.Length - 1 && Value[i] == target.Value[i])
                i++;
            return Value[i] < target.Value[i];
        }

        [JsonConverter(typeof(BytesToHexConverter))]
        public byte[] Value { get;  }

        public override bool Equals(object obj)
        {
            if (!(obj is Hash))
            {
                return false;
            }

            var hash = (Hash)obj;
            return EqualityComparer<byte[]>.Default.Equals(Value, hash.Value);
        }

        public override int GetHashCode()
        {
            var hashCode = -783812246;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Value);
            return hashCode;
        }

        public static bool operator ==(Hash a, Hash b)
		{
			if (a.Value.Length != b.Value.Length)
				return false;
			for (var i = 0; i < a.Value.Length; i++)
				if (a.Value[i] != b.Value[i])
					return false;
			return true;
		}

		public static bool operator !=(Hash a, Hash b)
		{
			return !(a == b);
		}

	}
}