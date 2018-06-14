using System;
using Telegram.Bot.Types.ReplyMarkups;

namespace RelationshipAnarchyBot
{
    [Serializable]
    public class Feature
    {
        public Feature(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public Feature Clone() => new Feature(Name, Description) { Selected = Selected };

        public InlineKeyboardButton GetButton()
        {
            var button = new InlineKeyboardButton
            {
                Text = $"{Name}: ",
                CallbackData = Name
            };
            switch (Selected)
            {
                case Selected.Maybe:
                    button.Text += "❔";
                    break;
                case Selected.Yes:
                    button.Text += "👍";
                    break;
                case Selected.No:
                    button.Text += "❌";
                    break;
            }

            return button;
        }

        public void Toggle()
        {
            switch (Selected)
            {
                case Selected.Maybe:
                    Selected = Selected.Yes;
                    break;
                case Selected.Yes:
                    Selected = Selected.No;
                    break;
                case Selected.No:
                    Selected = Selected.Maybe;
                    break;
            }
        }

        public readonly string Name;
        public readonly string Description;
        public Selected Selected;
    }
}
