using Base;
using Base.DTOs;
using Base.Extensions;
using Base.Interfaces;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using YasnoBot.YasnoLogger.Extensions;
using YasnoLogger.Interfaces;

namespace Bot;

public class YasnoBot
{
    private static ILogger Logger => ServicesProvider.Get<ILogger>();
    private static IUserRepository UserRepository => ServicesProvider.Get<IUserRepository>();
    private static INotifyService NotifyService => ServicesProvider.Get<INotifyService>();
    private static IScheduleService ScheduleService => ServicesProvider.Get<IScheduleService>();
    private static IHistoryService HistoryService => ServicesProvider.Get<IHistoryService>();
    private static IBotStateService DataService => ServicesProvider.Get<IBotStateService>();
    private static ISettings Settings => ServicesProvider.Get<ISettings>();

    private static async Task Main()
    {
        var botClient = new TelegramBotClient(BotToken);
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions { AllowedUpdates = [] };
        var notifyService = new TelegramBotNotifyService(botClient);

        ServicesProvider.AddApplicationServices();
        ServicesProvider.Add<INotifyService>(notifyService);

        var scheduledTasks = new ScheduledTasks(botClient, Logger, notifyService, ScheduleService, UserRepository, HistoryService, DataService);

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        await scheduledTasks.OnStartUp();
        _ = Task.Run(async () => await scheduledTasks.DoEveryMinute(() => scheduledTasks.StatusChangedNotification(), 0));
        _ = Task.Run(async () => await scheduledTasks.DoEveryMinute(() => scheduledTasks.NewScheduleNotification(), 5));
        _ = Task.Run(async () => await scheduledTasks.DoEveryMinute(() => scheduledTasks.UpcomingBlackoutNotification(), 10));
        _ = Task.Run(async () => await scheduledTasks.DoEveryMinute(() => scheduledTasks.UpdateIcon(), 15));

        Console.WriteLine("Starting Bot.");
        await Task.Delay(Timeout.Infinite, cts.Token);
        cts.Cancel();
    }

    private static string BotToken
    {
        get
        {
            var pattern = @"^\d{10}:[a-zA-Z0-9-_]{35}$";
            var botIdTxtFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BotID.txt");
            var envVariableToken = Environment.GetEnvironmentVariable("YASNO_BOT_TOKEN").NullIfEmpty();
            var fileToken = File.Exists(botIdTxtFile) ? File.ReadAllLines(botIdTxtFile).FirstOrDefault(x => !string.IsNullOrEmpty(x)) : null;
            var token = envVariableToken ?? fileToken;

            if (string.IsNullOrEmpty(token) || !Regex.IsMatch(token, pattern))
            {
                throw new Exception("BotId is not configured\r\nCheck README.md for instructions.");
            }

            return token;
        }
    }

    private static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken token)
    {
        Logger.Log(exception);
        Console.WriteLine($"Error: {exception.Message}");
        return Task.CompletedTask;
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
    {
        if (update.Message is not { } message)
            return;
        if (message.Text is not { } messageText)
            return;

        Logger.Log($"{message.Chat.Id} - {message.Chat.Username} - {message.Text}");
        Console.WriteLine($"Received a message from {message.From?.Username}: {messageText}");
        await bot.SendChatAction(message.Chat.Id, ChatAction.Typing, cancellationToken: token);

        try
        {
            var user = UserRepository.Get(message.Chat.Id);
            if (user is null)
            {
                user = new UserDto
                {
                    Id = message.Chat.Id,
                    Notify = true
                };
                UserRepository.Save(user);
            }

            // Admin commands
            if (message.Text.StartsWith("/set/"))
            {
                var key = message.Text.SubString("/set/", ":").Trim();
                var value = message.Text.SubString(":").Trim();

                Settings.Set(key, value);
                await NotifyService.Notify(message.Chat.Id, $"Setting updated: {key} = {value}");
                return;
            }
            // Admin commands

            var replyMessage = string.Empty;
            var turnNotificationBtn = user.Notify ? Terms.TurnNotificationsOff : Terms.TurnNotificationsOn;

            switch (message.Text)
            {
                case "/settings":
                    var allSettings = Settings.Keys.Select(key => $"{key}: {Settings.Get(key)}");
                    var settingsMessage = string.Join("\n", allSettings) + "\n\n/set/<KEY>: <VALUE>";
                    await NotifyService.Notify(message.Chat.Id, settingsMessage);
                    return;

                case "/logs":
                    await NotifyService.Notify(message.Chat.Id, Logger.TopLogs);
                    return;

                case Terms.TurnNotificationsOn:
                case Terms.TurnNotificationsOff:
                    user.Notify = message.Text == Terms.TurnNotificationsOn;
                    UserRepository.Save(user);
                    replyMessage = user.Notify ? Terms.NotificationsOn : Terms.NotificationsOff;
                    turnNotificationBtn = user.Notify ? Terms.TurnNotificationsOff : Terms.TurnNotificationsOn;
                    break;

                case Terms.CurrentStatus:
                    var lastRecord = HistoryService.GetLastRecord();
                    if (lastRecord is null)
                    {
                        replyMessage = Terms.NoDataAvailable;
                    }
                    else
                    {
                        var messageTemplate = lastRecord.Status == Status.PowerOn ? Terms.ThereIsElectricity : Terms.ThereIsNoElectricity;
                        replyMessage = string.Format(messageTemplate, lastRecord.DateTime.GetTimeDifference());
                    }
                    break;

                case Terms.Today:
                case Terms.Tomorrow:
                    var fullSchedule = ScheduleService.GetCurrent();
                    if (fullSchedule is null)
                    {
                        replyMessage = Terms.NoDataAvailable;
                    }
                    else
                    {
                        var schedule = message.Text == Terms.Today ? fullSchedule.Today : fullSchedule.Tomorrow;
                        replyMessage = GetScheduleMessage(schedule, message.Text == Terms.Today);
                    }
                    break;
            }

            var buttons = new List<List<string>>
            {
                new() { turnNotificationBtn },
                new() { Terms.CurrentStatus },
                new() { Terms.Today, Terms.Tomorrow },
            };
            await NotifyService.NotifyWithButtons(message.Chat.Id, replyMessage, buttons);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Logger.Log(e);
        }
    }

    private static string GetScheduleMessage(ScheduleDto schedule, bool today)
    {
        if (schedule is null || schedule.Slots is null || !schedule.Slots.Any())
        {
            return Terms.NoScheduleAvailable;
        }

        var now = new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute);
        var message = $"{Terms.Schedule}:\n";
        if (schedule.IsEmergence) message += Terms.EmergencyBlackout;

        foreach (var slot in schedule.Slots)
        {
            var arrow = today && now.IsBetween(slot.Start, slot.End) ? Icons.Arrow : string.Empty;
            var status = slot.Status == Status.PowerOn ? Icons.BatteryFull : Icons.BatteryEmpty;
            message += $"{slot.Start:HH:mm} - {slot.End:HH:mm} {status} {arrow}\n";
        }
        return message;
    }
}
