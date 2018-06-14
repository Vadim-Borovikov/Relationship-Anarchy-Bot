using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RelationshipAnarchyBot
{
    internal class Logic
    {
        private readonly List<Feature> _features;
        public readonly TelegramBotClient Bot;

        private List<Feature> _partnerFeatures;

        private const string Cancel = "/cancel";
        private const string Done = "/done";
        private const string FeaturesQuestion = "What would you like to have?";

        public Logic(IConfigurationRoot configuration)
        {
            _features = new List<Feature>();
            IConfigurationSection configFeatures = configuration.GetSection("features");
            foreach (IConfigurationSection features in configFeatures.GetChildren())
            {
                var feature = new Feature(features["name"], features["description"]);
                _features.Add(feature);
            }

            string token = configuration["token"];
            Bot = new TelegramBotClient(token);
            Bot.OnMessage += OnMessageReceived;
            Bot.OnCallbackQuery += OnCallbackQueryReceived;
        }

        private async void OnMessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.Type != MessageType.Text)
            {
                return;
            }

            long chatId = e.Message.Chat.Id;
            switch (e.Message.Text)
            {
                case "/help":
                    await ShowHelp(chatId);
                    break;
                case "/mark":
                    await MarkFeatures(chatId);
                    break;
                default:
                    _partnerFeatures = null;
                    if (e.Message.ForwardFrom != null)
                    {
                        string code = e.Message.Text;
                        _partnerFeatures = Decode(code, _features);
                    }

                    if (_partnerFeatures != null)
                    {
                        await Bot.SendTextMessageAsync(chatId, "Partner's wishes decoded! Now /mark yours.", ParseMode.Html);
                    }
                    else
                    {
                        await Bot.SendTextMessageAsync(chatId, "Didn't get it", ParseMode.Html);
                    }
                    break;
            }
        }

        private async Task ShowHelp(long chatId)
        {
            var sb = new StringBuilder();
            foreach (Feature feature in _features)
            {
                sb.AppendLine($"<b>{feature.Name}.</b> {feature.Description}");
            }

            await Bot.SendTextMessageAsync(chatId, sb.ToString(), ParseMode.Html);
        }

        private async Task MarkFeatures(long chatId)
        {
            foreach (Feature feature in _features)
            {
                feature.Selected = Selected.Maybe;
            }
            InlineKeyboardMarkup keyboard = GetKeyboard();
            await Bot.SendTextMessageAsync(chatId, FeaturesQuestion, replyMarkup: keyboard);
        }

        private async void OnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            long chatId = e.CallbackQuery.Message.Chat.Id;
            foreach (Feature feature in _features)
            {
                if (feature.Name == e.CallbackQuery.Data)
                {
                    feature.Toggle();
                    InlineKeyboardMarkup keyboard = GetKeyboard();
                    await Bot.EditMessageTextAsync(chatId, e.CallbackQuery.Message.MessageId, FeaturesQuestion, replyMarkup: keyboard);
                    return;
                }
            }

            switch (e.CallbackQuery.Data)
            {
                case Cancel:
                    await Bot.DeleteMessageAsync(chatId, e.CallbackQuery.Message.MessageId);
                    break;
                case Done:
                    if (_partnerFeatures == null)
                    {
                        await Bot.SendTextMessageAsync(chatId, "Send this code to your partner:");
                        await Bot.SendTextMessageAsync(chatId, $"```{Encode(_features)}```", ParseMode.Markdown);
                    }
                    else
                    {
                        var matches = new List<Feature>();
                        for (int i = 0; i < _features.Count; ++i)
                        {
                            Feature feature = _features[i];
                            Feature partnerFeature = _partnerFeatures[i];

                            if (feature.Selected != Selected.No && partnerFeature.Selected != Selected.No)
                            {
                                matches.Add(feature);
                            }
                        }

                        string message;
                        if (matches.Any())
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine("You matched on:");
                            sb.AppendLine($"{string.Join(", ", matches.Select(f => f.Name))}.");
                            sb.AppendLine("Share this result with your partner!");
                            message = sb.ToString();
                        }
                        else
                        {
                            message = "You didn't match.";
                        }

                        await Bot.SendTextMessageAsync(chatId, message);
                    }
                    break;
            }
        }

        private InlineKeyboardMarkup GetKeyboard()
        {
            var rows = new List<InlineKeyboardButton[]>();
            foreach (Feature feature in _features)
            {
                rows.Add(new[] { feature.GetButton() });
            }

            var cancel = new InlineKeyboardButton { Text = "Cancel 🗙", CallbackData = Cancel };
            var done = new InlineKeyboardButton { Text = "Done ✔", CallbackData = Done };
            rows.Add(new[] { cancel, done });

            return new InlineKeyboardMarkup(rows);
        }

        private static string Encode(List<Feature> features)
        {
            byte[] options = features.Select(f => (byte) f.Selected).ToArray();
            return Convert.ToBase64String(options);
        }

        private static List<Feature> Decode(string code, List<Feature> featuresTemplate)
        {
            byte[] options = Convert.FromBase64String(code);
            if (options.Length != featuresTemplate.Count)
            {
                return null;
            }

            var features = featuresTemplate.Select(f => f.Clone()).ToList();
            for (int i = 0; i < features.Count; ++i)
            {
                features[i].Selected = (Selected) options[i];
            }
            return features;
        }
    }
}
