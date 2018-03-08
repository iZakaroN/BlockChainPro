using BlockChanPro.Core.Contracts;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine
{
    public static class Rules
    {
        public static long CalulateBlockReward(BlockHashed lastBlock)
        {
            var rewardReduction = lastBlock.Signed.Data.Index / Transaction.BlockCountRewardReduction;

            return Genesis.Reward >> rewardReduction;
        }

	    public static HashBits CalculateTargetHash(BlockHashed lastBlock, BlockData blockToProcess)
	    {
		    var targetHashBits =
			    lastBlock.Signed.HashTargetBits.Adjust(
				    blockToProcess.TimeStamp - lastBlock.Signed.Data.TimeStamp,
				    BlockData.BlockTime);
		    return targetHashBits;
	    }
    }
}
