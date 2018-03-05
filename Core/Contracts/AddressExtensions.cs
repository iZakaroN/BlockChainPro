using BlockChanPro.Core.Engine;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Contracts
{
	public struct AddressExtensions
	{
		public static bool TryParse(string s, out Address address)
		{
			//TODO: validate real address
			address = new Address(s.Hash());
			return true;
			/*if (int.TryParse(s, out var addressNumber))
			{
				address = new Address(addressNumber);
				return true;
			}
			address = default(Address);
			return false;*/
		}
	}
}