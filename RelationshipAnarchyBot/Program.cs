using System;
using System.Data;
using System.IO;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types;

namespace RelationshipAnarchyBot
{
    static class Program
    {
        static void Main(string[] args)
        {
            IConfigurationRoot configuration = CreateConfiguration();

            var botLogic = new Logic(configuration);

            User me = botLogic.Bot.GetMeAsync().Result;
            Console.Title = me.Username;

            botLogic.Bot.StartReceiving();
            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
            botLogic.Bot.StopReceiving();
        }

        private static IConfigurationRoot CreateConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            builder.AddJsonFile("appsettings.json", true, true);
            return builder.Build();
        }
    }
}
