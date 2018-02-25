namespace BlockChanPro.Core.Contracts
{
    public class HashTarget
    {
        public HashTarget(Hash nounce, Hash hash)
        {
            Nounce = nounce;
            Hash = hash;
        }
        public Hash Nounce { get; }
        public Hash Hash { get; }
    }
}