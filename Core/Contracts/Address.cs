using System;
using Core.Serialization;

namespace Core.Contracts
{
	public struct Address
	{
		public static Address God  = new Address("God".Hash());
		public static Address Adam = new Address("Adam".Hash());
		public static Address Eva  = new Address("Eva".Hash());
		//For easy use just use int for now

		public Address(Hash value)
		{
			Value = value;
		}

		public Hash Value { get; }

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