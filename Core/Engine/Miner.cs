using System.Threading;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Serialization;

namespace BlockChanPro.Core.Engine
{
	public class Miner
	{
		// TODO: Change when finish threaded mining is implemented
		private const ulong NounceStep = ulong.MaxValue;
		private ulong _nextNounce = 0;


		private readonly HashBits _targetHashBits;
		private readonly BlockSigned _signedBlock;
		private readonly Hash _signedBlockHash;
		private readonly Cryptography _cryptography;

		public Miner(HashBits targetHashBits, BlockSigned signedBlock, Hash signedBlockHash, Cryptography cryptography)
		{
			_targetHashBits = targetHashBits;
			_signedBlock = signedBlock;
			_signedBlockHash = signedBlockHash;
			_cryptography = cryptography;
		}

		public BlockHashed Start(CancellationToken cancellationToken)
		{
			var hashTarget = FindHashTarget(_signedBlockHash, _targetHashBits, new HashBits(HashBits.OffsetMax, _nextNounce), NounceStep, cancellationToken);
			return new BlockHashed(_signedBlock, hashTarget);
		}

		public HashTarget FindHashTarget(Hash source, HashBits hashTargetBits, HashBits initialNounce, ulong maxItterations, CancellationToken cancellationToken)
		{
			var hashTarget = hashTargetBits.ToHash();
			var nounce = initialNounce.ToHash();
			var hashBinary = source.ToBinary();
			ulong itterations = 0;
			do
			{
				var hash = _cryptography.CalculateHash(hashBinary, nounce);
				if (hash.Compare(hashTarget) < 0)
					return new HashTarget(nounce, hash);
				nounce.Increment(1);
			} while (++itterations < maxItterations && !cancellationToken.IsCancellationRequested);

			return null;
		}

	}
}
