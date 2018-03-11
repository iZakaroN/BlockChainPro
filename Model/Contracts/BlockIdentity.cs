namespace BlockChanPro.Model.Contracts
{
    public class BlockIdentity
    {
	    public int Height { get; }
	    public Hash Hash { get; }

	    public BlockIdentity(int height, Hash hash)
	    {
		    Height = height;
		    Hash = hash;
	    }

	    protected bool Equals(BlockIdentity other)
	    {
		    return Height == other.Height && Equals(Hash, other.Hash);
	    }

	    public override bool Equals(object obj)
	    {
		    if (ReferenceEquals(null, obj)) return false;
		    if (ReferenceEquals(this, obj)) return true;
		    if (obj.GetType() != this.GetType()) return false;
		    return Equals((BlockIdentity)obj);
	    }

	    public override int GetHashCode()
	    {
		    unchecked
		    {
			    return (Height * 397) ^ (Hash != null ? Hash.GetHashCode() : 0);
		    }
	    }

	    public static bool operator ==(BlockIdentity a, BlockIdentity b)
	    {
		    var aIsNull = a is null;
		    var bIsNull = b is null;
		    if (aIsNull && bIsNull)
			    return true;
		    if (aIsNull || bIsNull)
			    return false;

		    return a.Height == b.Height && a.Hash == b.Hash;
	    }

	    public static bool operator !=(BlockIdentity a, BlockIdentity b)
	    {
		    return !(a == b);
	    }

	}
}
