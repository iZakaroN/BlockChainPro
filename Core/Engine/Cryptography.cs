using System.Security.Cryptography;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Serialization;

namespace BlockChanPro.Core.Engine
{
    public class Cryptography
    {
	    private readonly SHA256 _sha256 = SHA256.Create();

		public BlockSigned SignBlock(BlockData block, Address sign, HashBits target)
	    {

		    //TODO: Sign block with private key (encrypt merkle tree root hash), for now just place address without real signing
		    return new BlockSigned(block, sign, target);

	    }

	    public TransactionSigned Sign(Transaction transaction, Address sender)
	    {
		    var dataToHash = transaction.SerializeToBinary();
		    return new TransactionSigned(
			    transaction, 
			    CalculateHash(dataToHash));
	    }

		public Hash CalculateHash(byte[] data)
	    {
		    return new Hash(_sha256.ComputeHash(data));
		    

		}
	    public Hash CalculateHash(byte[] data, Hash nounce)
	    {
		    _sha256.TransformBlock(data, 0, data.Length, data, 0);
		    var nounceData = nounce.ToBinary();
		    _sha256.TransformFinalBlock(nounceData, 0, nounceData.Length);

		    return new Hash(_sha256.Hash);
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
