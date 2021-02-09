using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using StormChasers.Modules;
using StormChasers.Modules.InteractiveModule;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.Interactive;

namespace StormChasers
{
    public class Program
    {
        private DiscordSocketClient _client;
        private CommandService _command;
        private IServiceProvider _service;
        private IConfiguration _config;

        private String prefix = "";
        private String playing = "";
        private String donate = "";
        private String serverid = "";

        static void Main(string[] args)
        {
            new Program().RunBotAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "configs/config.json");
            _config = _builder.Build();
            //new?
            donate = _config.GetSection("setup").GetSection("donate").Value;
            playing = _config.GetSection("setup").GetSection("playing").Value;
            serverid = _config.GetSection("setup").GetSection("serverid").Value;
            prefix = _config.GetSection("setup").GetSection("prefix").Value;

            //old?
            /*donate = _config["donate"];
            playing = _config["playing"];
            serverid = _config["serverinfo"]; */
        }

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _command = new CommandService();
            _service = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_command)
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            _client.Log += _client_Log;

            await RegisterCommandsAsync();
            await _client.LoginAsync(TokenType.Bot, _config.GetSection("setup").GetSection("token").Value);
            await _client.StartAsync();
            await _client.SetGameAsync(playing, type: ActivityType.Playing);
            await Task.Delay(-1);
        }

        private Task _client_Log(LogMessage arg)
        {
            Console.WriteLine("Command: " + arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            await _command.AddModulesAsync(Assembly.GetEntryAssembly(), _service);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            // if statement to not allow other bots to activate this bot
            if (message.Author.IsBot) return;

            int argPos = 0;
            if(message.HasStringPrefix(prefix, ref argPos))
            {
                var result = await _command.ExecuteAsync(context, argPos, _service);
                if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
            }
        }
    }
}
