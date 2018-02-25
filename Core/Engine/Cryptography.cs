using System.Security.Cryptography;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Serialization;

namespace BlockChanPro.Core.Engine
{
    public class Cryptography
    {

	    public BlockSigned SignBlock(BlockData block, Address stamp, HashBits target)
	    {

		    //TODO: Sign block with private key (encrypt merkel tree root hash), for now just place address without real signing
		    return new BlockSigned(block, stamp, target);

	    }

	    public Hash CalculateHash(byte[] data)
	    {
		    using (var sha256 = SHA256.Create())
		    {
			    return new Hash(sha256.ComputeHash(data));
		    }
	    }
	    public Hash CalculateHash(byte[] data, Hash nounce)
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
		//TODO: Temporary used as address placeholder
		private static readonly Cryptography Default = new Cryptography();
		public static Hash Hash(this string value)
		{
			return Default.CalculateHash(value.ToBinary());
		}
	}
}
