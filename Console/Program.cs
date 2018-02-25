using System;
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

		private static readonly Dictionary<string, Action<string>> CommandParsers = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
		{
			{"Exit", Exit},
			{"Genesis", Genesis },
			{"Mine", Mine },
			{"Info", Info },
			{"Send", Send },
			{"Confirm", Confirm },
			{"CreateAddress", CreateAddress },
			{"ChangeAddress", ChangeAddress }

		};

		private static bool _exitConsole;
		private static Address? _address;
		private static Engine _engine = new Engine();

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

			while (!_exitConsole)
			{
				System.Console.Write(">");
				var userInput = System.Console.ReadLine().Trim();
				var commandEndPosition = userInput.IndexOf(' ');
				var command = (commandEndPosition != -1) ? userInput.Substring(0, commandEndPosition) : userInput;
				var commandOptions = (commandEndPosition != -1) ? userInput.Substring(commandEndPosition) : "";
				if (CommandParsers.TryGetValue(command, out var commandAction))
				{
					commandAction(commandOptions?.Trim());
				}
				else
				{
					System.Console.WriteLine($"Unknown command '{command}'. Valid commands are:");
					var commandsHelp = CommandParsers.Keys.Aggregate("", (s, c) => s + (s == "" ? "" : ", ") + c);
					System.Console.WriteLine(commandsHelp);
				}
			}
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
			_exitConsole = true;
		}

		private static void Exit(string arg)
		{
			_exitConsole = true;
		}

		private static void Mine(string arg)
		{
			if (_address.HasValue)
			{
				var block = _engine.Mine(_address.Value);
				CommandFinished($"Block mined: {block.SerializeToJson(Formatting.Indented)}");
				return;
			}
			System.Console.WriteLine("Set current currency address first");
		}

		private static void Genesis(string arg)
		{
			var genesisBlock = _engine.Genesis();
			CommandFinished($"Genesis block created: {genesisBlock.SerializeToJson(Formatting.Indented)}");
		}

		private static void Info(string arg)
		{
			var result = _engine.CalulateTransactionsInfo();
			CommandFinished(result.SerializeToJson(Formatting.Indented));
		}

		#region Transactions
		private static readonly List<Recipient> PendingRecipients = new List<Recipient>();

		private static void Send(string arg)
		{
			var arguments = arg?.Split(' ');
			if (arguments?.Length == 2)
			{
				if (Address.TryParse(arguments[0], out var targetAddress))
				{
					if (long.TryParse(arguments[1], out var amount))
					{
						var recipient = new Recipient(targetAddress, amount);
						PendingRecipients.Add(new Recipient(targetAddress, amount));
						CommandFinished(recipient.SerializeToJson(Formatting.Indented));
						return;
					} else System.Console.WriteLine("Cannot parse the amount");
				}
				else System.Console.WriteLine("Invalid address");
			}
			else System.Console.WriteLine("Invalid number of arguments");
			System.Console.WriteLine("Description: Prepare currency to be send to target address. After multiple send operations are prepared, transaction need to be confirmed");
			System.Console.WriteLine("Usage: >send [targetAddress] [decimal amount]");
		}

		private static void Confirm(string arg)
		{
			var arguments = arg.Split(' ');
			if (arguments.Length == 2)
			{
				if (decimal.TryParse(arguments[0], out var fee))
				{
					var hashedPassword = arguments[1].Hash();
					if (_address.HasValue)
					{
						if (hashedPassword == _address.Value.Value)
						{
							var result = new Transaction(_address.Value, PendingRecipients.ToArray(), fee);
							_engine.SendTransaction(result);
							CommandFinished(result.SerializeToJson(Formatting.Indented));
							return;
						}
						else System.Console.WriteLine("Password do not match");
					} else
						System.Console.WriteLine("Set current currency address first");
				}
				else System.Console.WriteLine("Cannot parse the fee");
			}
			else System.Console.WriteLine("Invalid number of arguments");
			System.Console.WriteLine("Description: Confirm a transaction and send a transaction with all the amounts from pending send operations");
			System.Console.WriteLine("Usage: >confirm [fee] [password]");
		}
		#endregion Transactions

		#region Address

		private static void CreateAddress(string arg)
		{
			if (!string.IsNullOrEmpty(arg))
			{
				if (arg.Contains(' '))
					Warrning(
						"Currently you cannot use passwords that contains SPACE because of console parameter parsing. Consider use password without space into it");
				var passwordHash = arg.Hash();
				System.Console.Write("Confirm password: ");
				var passwordConfirm = System.Console.ReadLine();
				if (passwordHash == passwordConfirm.Hash())
				{
					_address = new Address(passwordHash);
					CommandFinished(_address.SerializeToJson(Formatting.Indented));
					return;
				}
				else System.Console.WriteLine("Password confirmation do not match original password");
			}
			else System.Console.WriteLine("Invalid number of arguments");
			System.Console.WriteLine("Description: Create a new address and change it to it current");
			System.Console.WriteLine("Usage: >CreateAddress [password]");
		}

		private static void ChangeAddress(string arg)
		{
			var arguments = arg.Split(' ');
			if (arguments.Length == 1)
			{
				_address = new Address(arg.Hash());
				CommandFinished(_address.SerializeToJson(Formatting.Indented));
			}
			else System.Console.WriteLine("Invalid number of arguments");
			System.Console.WriteLine("Description: Change the current currency address");
			System.Console.WriteLine("Usage: >ChangeAddress [password]");
		}

		#endregion Address


		private static void CommandFinished(string commandResult)
		{
			System.Console.WriteLine("Accepted -> Result:");
			System.Console.WriteLine(commandResult);
		}

		private static void Warrning(string s)
		{
			System.Console.WriteLine(s);
		}

	}
}