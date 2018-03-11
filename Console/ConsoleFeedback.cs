using System;
using System.Diagnostics;
using BlockChanPro.Core.Engine;
using BlockChanPro.Model.Contracts;

namespace BlockChanPro.Console
{
	public class ConsoleFeedback : IFeedback
	{
		private readonly object _lock = new object();
		private static readonly TimeSpan HashRateTime = TimeSpan.FromSeconds(1);
		private int _lastNumberOfThreads;
		private Stopwatch _hashTime;
		private ulong _hashesElapsed;
		//Use lock because of colors and cursor positions
		private static readonly object OutLock = new object();

		public static void Out(string s)
		{
			lock (OutLock)
			{
				var x = System.Console.CursorLeft;
				var y = System.Console.CursorTop;
				System.Console.SetCursorPosition(0, System.Console.CursorTop - 1);
				System.Console.WriteLine($"\r{s}");
				if (y == System.Console.CursorTop)
					System.Console.SetCursorPosition(x, y);
			}
		}

		public static void OutLine(string s, ConsoleColor? foreground = null, ConsoleColor? background = null)
		{
			lock (OutLock)
			{
				var x = System.Console.CursorLeft;
				var y = System.Console.CursorTop;
				if (System.Console.CursorTop > 0)
					System.Console.SetCursorPosition(0, System.Console.CursorTop - 1);

				if (foreground.HasValue)
					System.Console.ForegroundColor = foreground.Value;
				if (background.HasValue)
					System.Console.BackgroundColor = background.Value;
				System.Console.WriteLine($"\r{s}");
				System.Console.ResetColor();

				OutMarker();

				if (y == System.Console.CursorTop)
					System.Console.SetCursorPosition(x, y);
			}
		}

		public static void OutMarker()
		{
			System.Console.WriteLine("   ");
			System.Console.Write("\r> ");
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
			OutLine($"# {operation} failed: {message}", ConsoleColor.Red);
		}

		public void MiningStart(int threadsCount)
		{
			if (_lastNumberOfThreads != threadsCount)
			{
				_lastNumberOfThreads = threadsCount;
				OutLine($"# Mining threads changed to {threadsCount}");
			}
		}

		public void MiningHashProgress(ulong hashesCalculated)
		{
			ulong? hashRate = null;
			lock (_lock)
			{
				_hashesElapsed += hashesCalculated;
				var hashTimeElapsed = _hashTime.Elapsed;
				if (hashTimeElapsed > HashRateTime)
				{
					hashRate = (ulong)(_hashesElapsed * ((decimal)TimeSpan.FromSeconds(1).Ticks / hashTimeElapsed.Ticks));

					_hashTime.Restart();
					_hashesElapsed = 0;
				}
			}
			if (hashRate.HasValue)
				Out($"# Hash rate {hashRate}, Threads {_lastNumberOfThreads}");
		}

		public void MineNewBlock(long difficulty, HashBits targetBits)
		{
			_hashTime = Stopwatch.StartNew();
			_hashesElapsed = 0;
			OutLine($"# Start mine new block. Difficulty = {difficulty}, TargetBits: {targetBits.Value:x16}");

		}

		public void MineCanceled()
		{
			OutLine("# Block mining was canceled");
		}

		public void NewBlockAccepted(int blockHeight, long blockTime, Hash blockHash)
		{
			OutLine($"# New block accepted, H:{blockHeight}, DT:{TimeSpan.FromTicks(blockTime)}, BH:{blockHash}", ConsoleColor.DarkCyan);
		}

		public void NewBlockMined(int blockHeight, long mineTime)
		{
			OutLine($"# New block mined, H:{blockHeight}, DT:{TimeSpan.FromTicks(mineTime)}", ConsoleColor.Green);
		}

		public void NewBlockRejected(int blockHeight, long blockTime, Hash blockHash, string message)
		{
			OutLine($"# New block rejected, H:{blockHeight}, DT:{TimeSpan.FromTicks(blockTime)}, BH:{blockHash}, => {message}", ConsoleColor.White, ConsoleColor.DarkRed);
		}

		public void NewTransaction(TransactionSigned transaction)
		{
			OutLine($"# New transaction accepted, TH:{transaction.Sign}");
		}

		public void SyncChainStart()
		{
			OutLine("# Sync: Start block chain synchronization");
		}

		public void SyncChainFinished()
		{
			OutLine("# Sync: Block chain synchronization finished");
		}

		public void SyncChainAlreadyInSync()
		{
			OutLine("# Sync: Block chain already synchronizing");
		}

		public void SyncChainProcessing(int syncStartBlockIndex, int latestBlockIndex, int peerCount)
		{
			OutLine($"# Sync: Found {peerCount} with valid block chain with height {latestBlockIndex}. Syncing from block {syncStartBlockIndex}");
		}

		public void SyncChainRetrieveBlocks(int syncBlockIndex, int syncBlockPageSize, string hostAbsoluteUri)
		{
			OutLine($"# Sync: Retrieve blocks [{syncBlockIndex}..{syncBlockIndex + syncBlockPageSize - 1}] from {hostAbsoluteUri}");
		}

		public void SyncChainPendingBlocks(int startIndex, int resultLength)
		{
			OutLine($"# Sync: Not sequential blocks found [{startIndex}..{startIndex + resultLength - 1}]. Queue as pending blocks");
		}

		public void SyncChainInvalidBlocks(int startIndex, int syncBlockPageSize)
		{
			OutLine($"# Sync: Found not invalid blocks [{startIndex}..{startIndex + syncBlockPageSize - 1}]");
		}

		public void SyncChainProcessPendingBlocks(int startIndex, int syncBlockPageSize)
		{
			OutLine($"# Sync: Process pending blocks [{startIndex}..{startIndex + syncBlockPageSize - 1}]");
		}

		public void NewPeer(string peerUrl)
		{
			OutLine($"# New peer discovered '{peerUrl}'");
		}
	}
}
