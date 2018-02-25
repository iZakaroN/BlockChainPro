using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Core.Contracts;
using Core.Serialization;

namespace Core
{
    public class Cryptography
    {
	    public BlockHashed ProcessBlock(BlockData block, Address stamp, TargetHashBits target)
	    {
		    var blockToHash = SignBlock(block, stamp, target);

		    //TODO: Experiment if difficulty adjustment is calculated before or after finding proper hash, for now difficulty is constant
		    return HashBlock(blockToHash, target);
	    }


        private BlockHashed HashBlock(BlockSigned blockToProcess, TargetHashBits target)
        {
            var dataToHash = blockToProcess.SerializeToBinary();
            var targetHash = target.ToHash();
            var nounce = Hash.Genesis;
            Hash hash;
            do
            {
                hash = CalculateHash(dataToHash, nounce);
                if (hash.Compare(targetHash) > 0)
                    nounce.Increment(1);
                else
                    break;

            } while (true);
            return new BlockHashed(
                blockToProcess,
                nounce,
                hash);
        }

	    public BlockSigned SignBlock(BlockData block, Address stamp, TargetHashBits target)
	    {

		    //TODO: Sign block with private key, for now just place address without real signing
		    return new BlockSigned(block, stamp, target);

	    }

	    public static Hash CalculateHash(byte[] data)
	    {
		    using (var sha256 = SHA256.Create())
		    {
			    return new Hash(sha256.ComputeHash(data));
		    }
	    }
	    public static Hash CalculateHash(byte[] data, Hash nounce)
	    {
		    using (var sha256 = SHA256.Create())
		    {
			    sha256.TransformBlock(data, 0, data.Length, data, 0);
			    var nounceData = nounce.ToBinary();
			    sha256.TransformFinalBlock(nounceData, 0, nounceData.Length);

			    return new Hash(sha256.Hash);
		    }
	    }
    }

	public static class CryptohraphyExtensions
	{
		public static Hash Hash(this string value)
		{
			return Cryptography.CalculateHash(value.ToBinary());
		}
	}
}
