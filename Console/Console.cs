using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BlockChanPro.Core;
using BlockChanPro.Core.Contracts;
using BlockChanPro.Core.Engine;
using BlockChanPro.Model.Contracts;
using BlockChanPro.Model.Serialization;
using BlockChanPro.Web.Api;
using BlockChanPro.Web.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BlockChanPro.Console
{
	internal class ConsoleProgram
	{
		private static readonly Dictionary<string, Action<Queue<string>>> ParameterParsers =
			new Dictionary<string, Action<Queue<string>>>
			{
				{"-a", ParseAddress},
				{"-ap", ParseAddressPassword},
				{"-?", Help},
				{"-p", SetHostPort},
				{"-c", SetTrustedPeer}
			};

		private static readonly Dictionary<string, Action<Queue<string>>> CommandParsers = new Dictionary<string, Action<Queue<string>>>(StringComparer.OrdinalIgnoreCase)
		{
			{"Wallet.Create", WalletCreate },
			{"Wallet.Recover", WalletRecover },
			{"Mine.Start", MineStart },
			{"Mine.Stop", MineStop },
			{"Connect", ConnectToPeer },
			{"Send", Send },
			{"Confirm", Confirm },
			{"Info", Info },
			{"Log.Off", TurnLogOff },
			{"Log.On", TurnLogOn },
			{"Exit", Exit},

		};

		private static bool _exitConsole;
		private static string _host;
		private static string _trustedPeer;
		private static Address? _address;
		private static bool _webHostLog = true;

		private static readonly ConsoleFeedback Console = new ConsoleFeedback();
		private static DependencyContainer _dependencies;
		// ReSharper disable once NotAccessedField.Local
		private static Task _webHostTask;
		private static readonly CancellationTokenSource WebHostCancel = new CancellationTokenSource();
		private static IApplicationLifetime _applicationLifetime;

		private static void Main(string[] args)
		{
			SetHostPort(5000);
			var parsedParameters = new HashSet<string>();

			var paramQueue = new Queue<string>(args);
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
			_dependencies = new DependencyContainer(_host, Console);
			Startup.Initialize(
				_dependencies.Network, 
				_dependencies.Engine, 
				(s,l) => l > LogLevel.Error || _webHostLog, 
				appLifetime => _applicationLifetime = appLifetime);

			var webHost = new WebHostBuilder()
				.UseKestrel()
				.UseUrls(_host)
				.UseContentRoot(Directory.GetCurrentDirectory())
				.ConfigureAppConfiguration((hostingContext, config) =>
				{
					/*var env = hostingContext.HostingEnvironment;
					config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
						.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);*/
					config.AddEnvironmentVariables();
				})
				.ConfigureLogging((hostingContext, logging) =>
				{
					//logging.AddConsole(o => { });
				})
				.UseStartup<Startup>()
				.Build();

			_webHostTask = webHost.RunAsync(WebHostCancel.Token);
			//webHost.Start();
			//_webHostTask = Host.BuildWebHost(_host).RunAsync(WebHostCancel.Token);
			//TODO: Cleanup all this web host/console sync shits and use execution chain of Startup
			WaitHandle.WaitAny(new[]
			{
				_applicationLifetime.ApplicationStopping.WaitHandle,
				_applicationLifetime.ApplicationStarted.WaitHandle,
			});

			Thread.Sleep(100);//Wait asp to console out the listening port

			ConsoleFeedback.OutMarker();
			//_webHostLog = true;
			_webHostLog = false;
			if (_trustedPeer != null)
				ConnectToPeer(_trustedPeer);

			//Just for faster testing
			if (_address == null)
				_address = new Address("test".Hash());
			ConsoleFeedback.OutMarker();
			while (!_exitConsole)
			{
				var userInput = new Queue<string>(SplitCommandLine(System.Console.ReadLine()));
				if (userInput.TryDequeue(out var command) && CommandParsers.TryGetValue(command, out var commandAction))
				{
					try
					{
						commandAction(userInput);
					}
					catch (Exception e)
					{
						Console.Error(command, e.Message);
					}
				}
				else
				{
					ConsoleFeedback.OutLine($"Unknown command '{command}'. Valid commands are:");
					Help(userInput);
				}
			}
			WebHostCancel.Cancel();
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
			ConsoleFeedback.OutLine(commandsHelp);
		}

		#region Network
		private static void SetHostPort(Queue<string> obj)
		{
			if (obj.TryDequeue(out var portString) && Int32.TryParse(portString, out var port))
			{
				SetHostPort(port);
			}
			else
				Exit("No listening port specified");
		}

		private static void SetHostPort(int port)
		{
			var host = $"{Dns.GetHostName()}:{port}";
			if (!host.TryParseUrl(out var uri))
				throw new ArgumentException("Invalid host url");
			_host = uri.AbsoluteUri;
		}

		private static void SetTrustedPeer(Queue<string> obj)
		{
			if (obj.TryDequeue(out var host))
			{
				if (!host.TryParseUrl(out var uri))
					throw new ArgumentException("Invalid peer url");
				_trustedPeer = uri.AbsoluteUri;
			}
			else
				ConsoleFeedback.OutLine("No peer address specified");
		}

		private static void ConnectToPeer(Queue<string> obj)
		{
			if (obj.TryDequeue(out var peer))
			{
				ConnectToPeer(peer);
			}
			else
				ConsoleFeedback.OutLine("No peer address specified");
		}

		private static void ConnectToPeer(string url)
		{
			try
			{
				if (!url.TryParseUrl(out var uri))
					throw new ArgumentException("Invalid peer url");
				var peerUrl = uri.AbsoluteUri;
				ConsoleFeedback.OutLine($"Connecting to peer '{peerUrl}' ...");
				var knownNodes = _dependencies.Engine.ConnectToPeerAsync(peerUrl).GetAwaiter().GetResult();
				ConsoleFeedback.OutLine($"Connect to network succeeded. Total known nodes {knownNodes}");
			}
			catch (Exception e)
			{
				Console.Error("Connect", e.Message);
			}
		}
		#endregion Network



		private static void Exit(Queue<string> arg)
		{
			Exit(arg.Aggregate("", (s, v) => $"{s} {v}"));
			_exitConsole = true;
		}

		private static void Exit(string exitMessage)
		{
			ConsoleFeedback.OutLine(exitMessage);
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
						ConsoleFeedback.OutLine("Cannot parse number of cores");
						ConsoleFeedback.OutLine("Description: Start mining. If number of threads is not specified a number logical processor are used specified");
						ConsoleFeedback.OutLine("Usage: > Mine.Start <number of threads>");
						return;
					}
					else
						threads = t;
				_dependencies.Engine.Mine(_address.Value, threads);
				CommandFinished($"Mining started in favor to address {_address.Value.Value.SerializeToJson()}");
				//CommandFinished($"Block mined: {block.SerializeToJson(Formatting.Indented)}");
				return;
			}

			NoAddress();
		}

		private static void MineStop(Queue<string> arg)
		{
			_dependencies.Engine.MineStop();
		}

		private static void NoAddress()
		{
			ConsoleFeedback.OutLine("Recover or Create an address first");
		}

		/*private static void Genesis(Queue<string> arg)
		{
			var genesisBlock = _engine.MineGenesis();
			CommandFinished($"Genesis block created: {genesisBlock.SerializeToJson(Formatting.Indented)}");
		}*/

		private static void Info(Queue<string> arg)
		{
			var result = _dependencies.ChainData.CalulateTransactionsInfo();
			CommandFinished(result.SerializeToJson(Formatting.Indented));
		}

		private static void TurnLogOn(Queue<string> obj)
		{
			_webHostLog = true;
		}

		private static void TurnLogOff(Queue<string> obj)
		{
			_webHostLog = false;
		}


		#region Transactions
		private static readonly List<Recipient> PendingRecipients = new List<Recipient>();

		private static void Send(Queue<string> arg)
		{
			if (arg?.Count == 2)
			{
				if (AddressExtensions.TryParse(arg.Dequeue(), out var targetAddress))
				{
					if (long.TryParse(arg.Dequeue(), out var amount))
					{
						var recipient = new Recipient(targetAddress, amount);
						PendingRecipients.Add(new Recipient(targetAddress, amount));
						CommandFinished(recipient.SerializeToJson(Formatting.Indented));
						return;
					} else ConsoleFeedback.OutLine("Cannot parse the amount");
				}
				else ConsoleFeedback.OutLine("Invalid address");
			}
			else ConsoleFeedback.OutLine("Invalid number of arguments");
			ConsoleFeedback.OutLine("Description: Prepare currency to be send to target address. After multiple send operations are prepared, transaction need to be confirmed");
			ConsoleFeedback.OutLine("Usage: >send <targetAddress> <decimal amount>");
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
							var result = _dependencies.Cryptography.Sign(
								new Transaction(_address.Value, PendingRecipients.ToArray(), fee),
								_address.Value);

							_dependencies.Engine.SendTransaction(result);
							CommandFinished(result.SerializeToJson(Formatting.Indented));
							return;
						} else
							ConsoleFeedback.OutLine("Password do not match");
					} else
						NoAddress();
				}
				else ConsoleFeedback.OutLine("Cannot parse the fee");
			}
			else ConsoleFeedback.OutLine("Invalid number of arguments");
			ConsoleFeedback.OutLine("Description: Confirm a transaction and send a transaction with all the amounts from pending send operations");
			ConsoleFeedback.OutLine("Usage: >confirm <fee> <password>");
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
				} else
					ConsoleFeedback.OutLine("Password confirmation do not match original password");
			}
			else ConsoleFeedback.OutLine("Invalid number of arguments");
			ConsoleFeedback.OutLine("Description: Create a new address to be used with the following operations");
			ConsoleFeedback.OutLine("Usage: > Wallet.Create <password>");
		}

		private static void WalletRecover(Queue<string> arg)
		{
			if (arg.Count == 1)
			{
				_address = new Address(arg.Dequeue().Hash());
				CommandFinished(_address.SerializeToJson(Formatting.Indented));
			}
			else ConsoleFeedback.OutLine("Invalid number of arguments");
			ConsoleFeedback.OutLine("Description: Recover an existing address to be used with the following operations");
			ConsoleFeedback.OutLine("Usage: > Wallet.Recover <password>");
		}

		#endregion Address


		private static void CommandFinished(string commandResult)
		{
			if (!string.IsNullOrWhiteSpace(commandResult))
			{
				ConsoleFeedback.OutLine("Accepted ->");
				ConsoleFeedback.OutLine(commandResult);
			} else
				ConsoleFeedback.OutLine("Accepted");
		}
	}
}