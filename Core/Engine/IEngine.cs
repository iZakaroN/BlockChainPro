using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine
{
    public interface IEngine
    {
	    Task AcceptTransactionsAsync(TransactionsBundle transactions);

		/// <summary>
		/// Return last block available
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
	    Task AcceptBlockAsync(BlockBundle block);
    }
}
