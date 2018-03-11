using System.Threading.Tasks;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Core.Engine
{
    public class SyncBlocksTask
    {
	    public int StartIndex { get; }
	    public int PageSize { get; }
	    public Task<BlockHashed[]> Task { get; }

	    public SyncBlocksTask(int startIndex, int pageSize, Task<BlockHashed[]> task)
	    {
		    StartIndex = startIndex;
		    PageSize = pageSize;
		    Task = task;
	    }

	    public bool Failed { get; set; } = false;
	    public bool Processed { get; set; } = false;

		public bool Ready => !Failed && Task.IsCompletedSuccessfully;
	    public bool AwaitProcessing => Ready && Task.Result.Length > 0;
	    public bool Completed => Failed || Task.IsCompletedSuccessfully  && Task.Result.Length == 0;

	}
}
