using BlockChanPro.Model.Serialization;

namespace BlockChanPro.Model.Contracts
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

	    public override string ToString()
	    {
		    return $"{GetType().Name}({this.SerializeToJson()})";
	    }
    }
}