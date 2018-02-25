using BlockChanPro.Core.Contracts;

namespace BlockChanPro.Core.Engine
{
    public static class Rules
    {
        public static long CalulateBlockReward(BlockHashed lastBlock)
        {
            int rewardReduction = lastBlock.Signed.Data.Index / Transaction.BlockCountRewardReduction;

            return Transaction.GenesisReward >> rewardReduction;
        }

	    public static HashBits CalculateTargetHash(BlockHashed lastBlock, BlockData blockToProcess)
	    {
		    var targetHashBits =
			    lastBlock.Signed.HashTargetBits.Adjust(blockToProcess.TimeStamp - lastBlock.Signed.Data.TimeStamp,
				    BlockData.BlockTime);
		    return targetHashBits;
	    }
    }
}
