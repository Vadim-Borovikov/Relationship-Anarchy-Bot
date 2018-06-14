using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RelationshipAnarchyBot
{
    internal class Logic
    {
        public readonly TelegramBotClient Bot;

        private readonly List<Feature> _featuresTemplate;
        private readonly byte _featuresVersion;
        private PersonInfo _me;
        private PersonInfo _partner;

        private const string Cancel = "/cancel";
        private const string Done = "/done";

        public Logic(IConfigurationRoot configuration)
        {
            string token = configuration["token"];
            Bot = new TelegramBotClient(token);
            Bot.OnMessage += OnMessageReceived;
            Bot.OnCallbackQuery += OnCallbackQueryReceived;

            _featuresTemplate = LoadFeatures(configuration);

            _featuresVersion = byte.Parse(configuration["featuresVersion"]);
        }

        private async void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.Type != MessageType.Text)
            {
                return;
            }

            long chatId = e.Message.Chat.Id;
            User user = e.Message.From;
            switch (e.Message.Text)
            {
                case "/start":
                    await ShowMenu(chatId);
                    break;
                case "/help":
                    await ShowHelp(chatId);
                    break;
                case "/mark":
                    await MarkFeatures(chatId, user, "What would you like with your partner?");
                    break;
                default:
                    _partner = PersonInfo.Decode(e.Message.Text, _featuresVersion, _featuresTemplate);
                    if (_partner != null)
                    {
                        string question = $"@{_partner.Username}'s wishes decoded!{Environment.NewLine}What would you like?";
                        await MarkFeatures(chatId, user, question);
                    }
                    else
                    {
                        await Bot.SendTextMessageAsync(chatId, "Didn't get it", ParseMode.Html);
                    }
                    break;
            }
        }

        private async Task ShowMenu(long chatId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("/help - features list");
            sb.AppendLine("/mark - mark which features would you like");

            await Bot.SendTextMessageAsync(chatId, sb.ToString(), ParseMode.Html);
        }

        private async Task ShowHelp(long chatId)
        {
            foreach (Feature feature in _featuresTemplate)
            {
                string message = $"<b>{feature.Name}.</b> {feature.Description}";
                await Bot.SendTextMessageAsync(chatId, message, ParseMode.Html, true);
            }
        }

        private async Task MarkFeatures(long chatId, User user, string message)
        {
            _me = new PersonInfo(user.Username, _featuresTemplate, _featuresVersion);
            InlineKeyboardMarkup keyboard = GetKeyboard();
            await Bot.SendTextMessageAsync(chatId, message, replyMarkup: keyboard);
        }

        private async void OnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            long chatId = e.CallbackQuery.Message.Chat.Id;
            foreach (Feature feature in _me.Features)
            {
                if (feature.Name == e.CallbackQuery.Data)
                {
                    feature.Toggle();
                    InlineKeyboardMarkup keyboard = GetKeyboard();
                    await Bot.EditMessageTextAsync(chatId, e.CallbackQuery.Message.MessageId, e.CallbackQuery.Message.Text, replyMarkup: keyboard);
                    return;
                }
            }

            switch (e.CallbackQuery.Data)
            {
                case Cancel:
                    await Bot.DeleteMessageAsync(chatId, e.CallbackQuery.Message.MessageId);
                    break;
                case Done:
                    if (_partner != null)
                    {
                        List<Feature> matches = _me.Match(_partner);
                        string message;
                        if (matches.Any())
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"You matched with @{_partner.Username} on:");
                            sb.AppendLine($"{string.Join(", ", matches.Select(f => f.Name))}.");
                            message = sb.ToString();
                        }
                        else
                        {
                            message = $"You didn't match with @{_partner.Username}.";
                        }

                        await Bot.SendTextMessageAsync(chatId, message);
                    }
                    else
                    {
                        await Bot.SendTextMessageAsync(chatId, "Send this code to your partner:");
                        string code = PersonInfo.Encode(_me);
                        await Bot.SendTextMessageAsync(chatId, $"```{code}```", ParseMode.Markdown);
                    }

                    break;
            }
        }

        private static List<Feature> LoadFeatures(IConfiguration configuration)
        {
            var features = new List<Feature>();
            IConfigurationSection configFeatures = configuration.GetSection("features");
            foreach (IConfigurationSection featuresSection in configFeatures.GetChildren())
            {
                var feature = new Feature(featuresSection["name"], featuresSection["description"]);
                features.Add(feature);
            }
            return features;
        }

        private InlineKeyboardMarkup GetKeyboard()
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (Feature feature in _me.Features)
            {
                rows.Add(new[] { feature.GetButton() });
            }

            var cancel = new InlineKeyboardButton { Text = "Cancel ✖️", CallbackData = Cancel };
            var done = new InlineKeyboardButton { Text = "Done ✅", CallbackData = Done };
            rows.Add(new[] { cancel, done });

            return new InlineKeyboardMarkup(rows);
        }
    }
}
