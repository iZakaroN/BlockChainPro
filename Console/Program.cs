﻿using System;
using System.Collections.Generic;
using System.Linq;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Engine;
using BlockChanPro.Core.Serialization;
using Newtonsoft.Json;

namespace BlockChanPro.Console
{
	class Program
	{
		private static readonly Dictionary<string, Action<Queue<string>>> ParameterParsers =
			new Dictionary<string, Action<Queue<string>>>
			{
				{"-a", ParseAddress},
				{"-ap", ParseAddressPassword},
				{"-h", Help}
			};

		private static readonly Dictionary<string, Action<Queue<string>>> CommandParsers = new Dictionary<string, Action<Queue<string>>>(StringComparer.OrdinalIgnoreCase)
		{
			{"Wallet.Create", WalletCreate },
			{"Wallet.Recover", WalletRecover },
			{"Mine.Start", MineStart },
			{"Mine.Stop", MineStop },
			{"Send", Send },
			{"Confirm", Confirm },
			{"Info", Info },
			{"Exit", Exit},

		};

		private static readonly Cryptography Cryptography = new Cryptography();

		private static bool _exitConsole;
		private static Address? _address;
		private static readonly Engine Engine = new Engine(new Console());

		static void Main(string[] args)
		{

			var parsedParameters = new HashSet<string>();

			Queue<string> paramQueue = new Queue<string>(args);
			while (paramQueue.TryDequeue(out var paramName))
			{
				if (parsedParameters.Contains(paramName))
					Exit($"Parameter {paramName} specified multiple times");
				else if (ParameterParsers.TryGetValue(paramName, out var paramParser))
					paramParser(paramQueue);
				else
					Exit($"Invalid parameter {paramName}");
				parsedParameters.Add(paramName);
			}

			//Just for faster testing
			_address = new Address("test".Hash());
			Console.OutMarker();
			while (!_exitConsole)
			{
				var userInput = new Queue<string>(SplitCommandLine(System.Console.ReadLine()));
				if (userInput.TryDequeue(out var command) && CommandParsers.TryGetValue(command, out var commandAction))
				{
					commandAction(userInput);
				}
				else
				{
					Console.OutLine($"Unknown command '{command}'. Valid commands are:");
					Help(userInput);
				}
			}
		}

		public static IEnumerable<string> SplitCommandLine(string commandLine)
		{
			var inQuotes = false;

			return commandLine.Split(c =>
				{
					if (c == '\"')
						inQuotes = !inQuotes;

					return !inQuotes && c == ' ';
				})
				.Select(arg => arg.Trim().Trim('\"'))
				.Where(arg => !string.IsNullOrEmpty(arg));
		}

		private static void ParseAddress(Queue<string> arguments)
		{
			if (arguments.TryDequeue(out var addressParam))
			{
				if (addressParam.TryDeserializeFromJson<Hash>(out var addressHash))
					_address = new Address(addressHash);
				else
					Exit($"Cannot parse address {addressParam}");
			}
			Exit("No address specified");
		}

		private static void ParseAddressPassword(Queue<string> arguments)
		{
			if (arguments.TryDequeue(out var addressPasswordParam))
			{
				_address = new Address(addressPasswordParam.Hash());
			}
			Exit("No address specified");
		}

		private static void Help(Queue<string> arguments)
		{
			var commandsHelp = CommandParsers.Keys.Aggregate("", (s, c) => s + (s == "" ? "" : ", ") + c);
			Console.OutLine(commandsHelp);
		}

		private static void Exit(Queue<string> arg)
		{
			Exit(arg.Aggregate("", (s, v) => $"{s} {v}"));
			_exitConsole = true;
		}

		private static void Exit(string exitMessage)
		{
			Console.OutLine(exitMessage);
			_exitConsole = true;
		}

		private static void MineStart(Queue<string> arg)
		{
			if (_address.HasValue)
			{
				int? threads = null;
				if (arg.TryDequeue(out var threadsArg))
					if (!Int32.TryParse(threadsArg, out var t))
					{
						Console.OutLine("Cannot parse number of cores");
						Console.OutLine("Description: Start mining. If number of threads is not specified a number logical processor are used specified");
						Console.OutLine("Usage: > Mine.Start <number of threads>");
						return;
					}
					else
						threads = t;
				Engine.Mine(_address.Value, threads);
				CommandFinished($"Mining started in favor to address {_address.Value.Value.SerializeToJson()}");
				//CommandFinished($"Block mined: {block.SerializeToJson(Formatting.Indented)}");
				return;
			}

			NoAddress();
		}

		private static void MineStop(Queue<string> arg)
		{
			Engine.MineStop();
		}

		private static void NoAddress()
		{
			Console.OutLine("Recover or Create an address first");
		}

		/*private static void Genesis(Queue<string> arg)
		{
			var genesisBlock = _engine.MineGenesis();
			CommandFinished($"Genesis block created: {genesisBlock.SerializeToJson(Formatting.Indented)}");
		}*/

		private static void Info(Queue<string> arg)
		{
			var result = Engine.CalulateTransactionsInfo();
			CommandFinished(result.SerializeToJson(Formatting.Indented));
		}

		#region Transactions
		private static readonly List<Recipient> PendingRecipients = new List<Recipient>();

		private static void Send(Queue<string> arg)
		{
			if (arg?.Count == 2)
			{
				if (Address.TryParse(arg.Dequeue(), out var targetAddress))
				{
					if (long.TryParse(arg.Dequeue(), out var amount))
					{
						var recipient = new Recipient(targetAddress, amount);
						PendingRecipients.Add(new Recipient(targetAddress, amount));
						CommandFinished(recipient.SerializeToJson(Formatting.Indented));
						return;
					} else Console.OutLine("Cannot parse the amount");
				}
				else Console.OutLine("Invalid address");
			}
			else Console.OutLine("Invalid number of arguments");
			Console.OutLine("Description: Prepare currency to be send to target address. After multiple send operations are prepared, transaction need to be confirmed");
			Console.OutLine("Usage: >send <targetAddress> <decimal amount>");
		}

		private static void Confirm(Queue<string> arg)
		{
			if (arg.Count == 2)
			{
				if (decimal.TryParse(arg.Dequeue(), out var fee))
				{
					var hashedPassword = arg.Dequeue().Hash();
					if (_address.HasValue)
					{
						if (hashedPassword == _address.Value.Value)
						{
							var result = Cryptography.Sign(
								new Transaction(_address.Value, PendingRecipients.ToArray(), fee),
								_address.Value);

							Engine.SendTransaction(result);
							CommandFinished(result.SerializeToJson(Formatting.Indented));
							return;
						}
						else Console.OutLine("Password do not match");
					} else
						NoAddress();
				}
				else Console.OutLine("Cannot parse the fee");
			}
			else Console.OutLine("Invalid number of arguments");
			Console.OutLine("Description: Confirm a transaction and send a transaction with all the amounts from pending send operations");
			Console.OutLine("Usage: >confirm <fee> <password>");
		}
		#endregion Transactions

		#region Address

		//TODO: safe encrypted wallet
		private static void WalletCreate(Queue<string> arg)
		{
			if (arg.Count == 1)
			{
				var passwordHash = arg.Dequeue().Hash();
				System.Console.Write("Confirm password: ");
				var passwordConfirm = System.Console.ReadLine();
				if (passwordHash == passwordConfirm.Hash())
				{
					_address = new Address(passwordHash);
					CommandFinished(_address.SerializeToJson(Formatting.Indented));
					return;
				}
				else Console.OutLine("Password confirmation do not match original password");
			}
			else Console.OutLine("Invalid number of arguments");
			Console.OutLine("Description: Create a new address to be used with the following operations");
			Console.OutLine("Usage: > Wallet.Create <password>");
		}

		private static void WalletRecover(Queue<string> arg)
		{
			if (arg.Count == 1)
			{
				_address = new Address(arg.Dequeue().Hash());
				CommandFinished(_address.SerializeToJson(Formatting.Indented));
			}
			else Console.OutLine("Invalid number of arguments");
			Console.OutLine("Description: Recover an existing address to be used with the following operations");
			Console.OutLine("Usage: > Wallet.Recover <password>");
		}

		#endregion Address


		private static void CommandFinished(string commandResult)
		{
			if (!string.IsNullOrWhiteSpace(commandResult))
			{
				Console.OutLine("Accepted ->");
				Console.OutLine(commandResult);
			} else
				Console.OutLine("Accepted");
		}

		private static void Warrning(string s)
		{
			System.Console.WriteLine(s);
		}

	}
}