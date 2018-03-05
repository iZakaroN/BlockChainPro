using System;
using System.Diagnostics;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Engine;
using BlockChanPro.Core.Serialization;

namespace BlockChanPro.Console
{
    internal class Console : IFeedBack
    {
		private readonly object _lock = new object();
	    private static readonly TimeSpan HashRateTime = TimeSpan.FromSeconds(1);
	    private int _lastNumberOfThreads;
	    private Stopwatch _hashTime;
	    private ulong _hashesElapsed;
	    public void MineNewBlock(long difficulty, HashBits targetBits)
	    {
		    _hashTime = Stopwatch.StartNew();
		    _hashesElapsed = 0;
			OutLine($"# Start mine new block. Difficulty = {difficulty}, TargetBits: {targetBits.Value:x16}");

		}

	    public static void Out(string s)
	    {
		    System.Console.Write($"\r{s}");
	    }

	    public static void OutLine(string s)
	    {
		    System.Console.WriteLine($"\r{s}");
		    OutMarker();
	    }

	    public static void OutMarker()
	    {
		    System.Console.Write("\r> ");
	    }

	    public void StartProcess(int threadsCount)
	    {
		    if (_lastNumberOfThreads != threadsCount)
		    {
			    _lastNumberOfThreads = threadsCount;
			    OutLine($"# Mining threads changed to {threadsCount}");
		    }
	    }

	    public void NewBlockFound(int blockHeight, long blockTime, Hash blockHash)
	    {
		    OutLine($"# New block found, H:{blockHeight}, DT:{TimeSpan.FromTicks(blockTime)}, BH:{blockHash.Value.ToHexString()}");
	    }

		public void NewBlockMined(int blockHeight, long mineTime)
	    {
		    OutLine($"# New block mined, H:{blockHeight}, DT:{TimeSpan.FromTicks(mineTime)}, ");
	    }

	    public void HashProgress(ulong hashesCalculated)
	    {
		    ulong? hashRate = null;
		    lock (_lock)
		    {
			    _hashesElapsed += hashesCalculated;
			    var hashTimeElapsed = _hashTime.Elapsed;
			    if (hashTimeElapsed > HashRateTime)
			    {
				    hashRate = (ulong) (_hashesElapsed * ((decimal) TimeSpan.FromSeconds(1).Ticks / hashTimeElapsed.Ticks));

				    _hashTime.Restart();
				    _hashesElapsed = 0;
			    }
		    }
			if (hashRate.HasValue)
				Out($"# Hash rate {hashRate}, Threads {_lastNumberOfThreads}");


		}

		public void MinedBlockCanceled()
	    {
		    OutLine("# Block mining was canceled");
	    }

		public void Start(string operation, string message)
	    {
			//Out($"Start: {operation}, {message}");
		}

		public void Stop(string operation, string message)
	    {
			//Out($"Stop: {operation}, {message}");
		}

		public void Error(string operation, string message)
	    {
		    OutLine($"Error: {operation}, {message}");
	    }
    }
}
