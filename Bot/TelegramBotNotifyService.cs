using Base.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot;

public class TelegramBotNotifyService(ITelegramBotClient bot) : INotifyService
{
    public async Task NotifyWithButtons(long userId, string message, params string[] buttons)
    {
        var rkm = buttons.Any()
            ? GetButtonsFromList(buttons)
            : GetButtonsFromMatrix([]);

        await bot.SendMessage(userId, message, replyMarkup: rkm, parseMode: ParseMode.Markdown);
    }

    public async Task Notify(long userId, string message) => await bot.SendMessage(userId, message, parseMode: ParseMode.Markdown);

    private static ReplyKeyboardMarkup GetButtonsFromMatrix(List<List<string>> buttons)
    {
        var rkm = new ReplyKeyboardMarkup { Keyboard = [] };
        foreach (var row in buttons)
        {
            rkm.Keyboard = rkm.Keyboard.Append([.. row]);
        }

        return rkm;
    }

    private static ReplyKeyboardMarkup GetButtonsFromList(string[] buttons)
    {
        var buttonsInRow = buttons.Any(x => x.Length > 9) ? 3 : 4;
        var rkm = new ReplyKeyboardMarkup { Keyboard = [] };
        var last = buttons.Last();
        buttons = buttons.Take(buttons.Length - 1).ToArray();

        while (buttons.Any())
        {
            var x = buttons.Take(buttonsInRow).ToList();
            buttons = buttons.Skip(buttonsInRow).ToArray();

            if (x.Count == 4) { rkm.Keyboard = rkm.Keyboard.Append([x[0], x[1], x[2], x[3]]); continue; }
            if (x.Count == 3) { rkm.Keyboard = rkm.Keyboard.Append([x[0], x[1], x[2]]); continue; }
            if (x.Count == 2) { rkm.Keyboard = rkm.Keyboard.Append([x[0], x[1]]); continue; }
            if (x.Count == 1) { rkm.Keyboard = rkm.Keyboard.Append([x[0]]); }
        }

        rkm.Keyboard = rkm.Keyboard.Append([last]);

        return rkm;
    }
}