using System;
using System.Threading.Tasks;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Engine;
using BlockChanPro.Core.Engine.Data;
using BlockChanPro.Model.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace BlockChanPro.MSTESTS
{
	[TestClass]
	public class BlockValidationTests
	{
		private readonly Address _address;
		private readonly IFeedback _feedback;
		private readonly Cryptography _cryptography;
		private readonly MinerFactory _minerFactory;
		private readonly ChainData _chainData;

		public BlockValidationTests()
		{
			_address = new Address("test1".Hash());
			_feedback = new NullFeedBack();
			_cryptography = new Cryptography();
			_minerFactory = new MinerFactory(_cryptography);
			_chainData = new ChainData(_feedback, _cryptography);
		}

		[TestMethod]
		public async Task Serialization_BlockHashed_Validate_JsonString()
		{
			var block = await GenerateBlock(1);

			var serializedBlock = JsonConvert.SerializeObject(block);
			var deserializedBlock = JsonConvert.DeserializeObject<BlockHashed>(serializedBlock);
			var serializedBlockCheck = JsonConvert.SerializeObject(deserializedBlock);

			Assert.AreEqual(serializedBlock, serializedBlockCheck);
		}

		[TestMethod]
		public async Task GenerateBlock_Genesis_Validate()
		{
			var genesisBlock = await GenerateGenesisBlock();

			_chainData.ValidateGenesisBlock(genesisBlock);
		}

		[TestMethod]
		[ExpectedException(typeof(BlockchainException))]
		public async Task GenerateBlock_GenesisBlock_Invalidate_Hash()
		{
			var genesisBlock = await GenerateGenesisBlock();

			var newGenesisSignedBlock = Genesis.GetBlockData(_cryptography, DateTime.UtcNow.Ticks);
			_chainData.ValidateGenesisBlock(new BlockHashed(newGenesisSignedBlock, genesisBlock.HashTarget));
		}

		[TestMethod]
		public async Task GenerateBlock_1_Validate()
		{
			var genesisBlock = await GenerateGenesisBlock();
			var block = await GenerateBlock(genesisBlock);

			_chainData.ValidateBlock(genesisBlock, block);
		}

		[TestMethod]
		[ExpectedException(typeof(BlockchainException))]
		public async Task GenerateBlock_1_Invalidate_Parent()
		{
			var genesisBlock = await GenerateGenesisBlock();
			var block = await GenerateBlock(genesisBlock);
			var newGenesisBlock = await GenerateGenesisBlock();

			_chainData.ValidateBlock(newGenesisBlock, block);
		}

		[TestMethod]
		[ExpectedException(typeof(BlockchainException))]
		public async Task GenerateBlock_2_Invalidate_Parent()
		{
			var block = await GenerateBlock(2);
			var nextBlock = await GenerateBlock(block);
			var newBlock = await GenerateBlock(2);

			_chainData.ValidateBlock(newBlock, nextBlock);
		}

		[TestMethod]
		public async Task GenerateBlock_10_Validate()
		{
			var block = await GenerateBlock(10);
			var nextBlock = await GenerateBlock(block);

			_chainData.ValidateBlock(block, nextBlock);
		}

		[TestMethod]
		[ExpectedException(typeof(BlockchainException))]
		public async Task GenerateBlock_Invalidate_Height()
		{
			var block = await GenerateBlock(2);
			var nextBlock = await GenerateBlock(block);
			var newBlock = await GenerateBlock(1);

			_chainData.ValidateBlock(newBlock, nextBlock);
		}

		private async Task<BlockHashed> GenerateGenesisBlock()
		{
			var feedback = new NullFeedBack();
			var genesisMiner = _minerFactory.Create(Genesis.GetBlockData(_cryptography, DateTime.UtcNow.Ticks), feedback);
			genesisMiner.Start(1);
			return await genesisMiner.GetBlock();
		}

		private async Task<BlockHashed> GenerateBlock(BlockHashed previousBlock, int? threads = null)
		{
			var miner = _minerFactory.Create(_address, previousBlock, new TransactionSigned[] { }, _feedback);
			miner.Start(threads ?? Environment.ProcessorCount);
			return await miner.GetBlock();
		}

		private async Task<BlockHashed> GenerateBlock(int blockNumber, int? threads = null)
		{
			var previousBlock = await GenerateGenesisBlock();
			while (blockNumber-- > 0)
				previousBlock = await GenerateBlock(previousBlock, threads);

			return previousBlock;
		}
	}
}