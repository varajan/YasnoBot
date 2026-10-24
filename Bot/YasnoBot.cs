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
    private static IDataBase DataBase => ServicesProvider.Get<IDataBase>();

    private static async Task Main()
    {
        var botClient = new TelegramBotClient(BotToken);
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions { AllowedUpdates = [] };
        var notifyService = new TelegramBotNotifyService(botClient);
        var tasks = new Tasks(botClient);

        ServicesProvider.AddApplicationServices();
        ServicesProvider.Add<INotifyService>(notifyService);

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        await Tasks.OnStartUp();
        _ = Task.Run(async () => await Tasks.DoEveryMinute(() => Tasks.StatusChangedNotification(), 0));
        _ = Task.Run(async () => await Tasks.DoEveryMinute(() => Tasks.NewScheduleNotification(), 5));
        _ = Task.Run(async () => await Tasks.DoEveryMinute(() => Tasks.UpcomingBlackoutNotification(), 10));
        _ = Task.Run(async () => await Tasks.DoEveryMinute(() => tasks.UpdateIcon(), 15));

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
            var settings = ServicesProvider.Get<ISettings>();

            if (message.Text == "/settings")
            {
                var allSettings = settings.Keys.Select(key => $"{key}: {settings.Get(key)}");
                var settingsMessage = string.Join("\n", allSettings) + "\n\n/settings/<KEY>: <VALUE>";

                await NotifyService.Notify(message.Chat.Id, settingsMessage);
                return;
            }

            if (message.Text.StartsWith("/settings/"))
            {
                var key = message.Text.SubString("/settings/", ":").Trim();
                var value = message.Text.SubString(":").Trim();

                settings.Set(key, value);
                await NotifyService.Notify(message.Chat.Id, $"Setting updated: {key} = {value}");
                return;
            }

            if (message.Text == "/logs")
            {
                await NotifyService.Notify(message.Chat.Id, Logger.TopLogs);
            }
            // Admin commands

            // TEST commands

            if (message.Text == "/test")
            {
                await NotifyService.Notify(message.Chat.Id, "blackout, soon, light, schedule, emergency");
                return;
            }

            if (message.Text.Contains("/test"))
            {
                var task = message.Text.Split('/').Last().Trim();

                switch (task)
                {
                    case "blackout":
                        // write light - now-5
                        var dateTime = DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss");
                        DataBase.Execute($"INSERT INTO History (DateTime, Status) VALUES ('{dateTime}', {(int)Status.PowerOn})");

                        // write blackout - now
                        dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        DataBase.Execute($"INSERT INTO History (DateTime, Status) VALUES ('{dateTime}', {(int)Status.Blackout})");
                        break;

                    case "soon":
                        // schedule, where next shutdown is in 5 min
                        var nextBlackout = DateTime.Now.AddMinutes(5);

                        var newSchedule = new TwoDayScheduleDto
                        {
                            Today = new()
                            {
                                Slots =
                                [
                                    new ScheduleSlotDto{ Start = new TimeOnly(0, 0), End = new TimeOnly(nextBlackout.Hour, nextBlackout.Minute), Status = Status.PowerOn },
                                    new ScheduleSlotDto{ Start = new TimeOnly(nextBlackout.Hour, nextBlackout.Minute), End = new TimeOnly(23, 59), Status = Status.Blackout },
                                ]
                            },
                            Tomorrow = new()
                            {
                                Slots =
                                [
                                    new ScheduleSlotDto{ Start = new TimeOnly(0, 0), End = new TimeOnly(nextBlackout.Hour, nextBlackout.Minute), Status = Status.PowerOn },
                                    new ScheduleSlotDto{ Start = new TimeOnly(nextBlackout.Hour, nextBlackout.Minute), End = new TimeOnly(23, 59), Status = Status.Blackout },
                                ]
                            }
                        };
                        DataBase.Execute($"INSERT OR REPLACE INTO Data (Key, Value) VALUES ('Schedule', '{newSchedule.Serialize()}')");

                        dateTime = DateTime.Now.AddMinutes(-1).ToString("yyyy-MM-dd HH:mm:ss");
                        DataBase.Execute($"INSERT INTO History (DateTime, Status) VALUES ('{dateTime}', {(int)Status.PowerOn})");
                        break;

                    case "light":
                        // write blackout - now-5
                        dateTime = DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss");
                        DataBase.Execute($"INSERT INTO History (DateTime, Status) VALUES ('{dateTime}', {(int)Status.Blackout})");

                        // write light - now
                        dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        DataBase.Execute($"INSERT INTO History (DateTime, Status) VALUES ('{dateTime}', {(int)Status.PowerOn})");
                        break;

                    case "schedule":
                        var now = DateTime.Now;
                        newSchedule = new TwoDayScheduleDto
                        {
                            Today = new()
                            {
                                Slots =
                                [
                                    new ScheduleSlotDto{ Start = new TimeOnly(0, 0), End = new TimeOnly(now.Hour, now.Minute), Status = Status.PowerOn },
                                    new ScheduleSlotDto{ Start = new TimeOnly(now.Hour, now.Minute), End = new TimeOnly(23, 59), Status = Status.Blackout },
                                ]
                            },
                            Tomorrow = new()
                            {
                                Slots =
                                [
                                    new ScheduleSlotDto{ Start = new TimeOnly(0, 0), End = new TimeOnly(now.Hour, now.Minute), Status = Status.PowerOn },
                                    new ScheduleSlotDto{ Start = new TimeOnly(now.Hour, now.Minute), End = new TimeOnly(23, 59), Status = Status.Blackout },
                                ]
                            }
                        };
                        DataBase.Execute($"INSERT OR REPLACE INTO Data (Key, Value) VALUES ('Schedule', '{newSchedule.Serialize()}')");
                        break;

                    case "emergency":
                        newSchedule = new TwoDayScheduleDto
                        {
                            Today = new() { Slots = [], IsEmergence = true },
                            Tomorrow = new() { Slots = [], IsEmergence = true }
                        };
                        DataBase.Execute($"INSERT OR REPLACE INTO Data (Key, Value) VALUES ('Schedule', '{newSchedule.Serialize()}')");
                        break;
                }

                return;
            }

            // TEST commands

            if (user.Notify && message.Text == Terms.TurnNotificationsOff)
            {
                user.Notify = false;
                UserRepository.Save(user);
            }

            if (!user.Notify && message.Text == Terms.TurnNotificationsOn)
            {
                user.Notify = true;
                UserRepository.Save(user);
            }

            var replyMessage = user.Notify ? Terms.NotificationsOn : Terms.NotificationsOff;
            var btnText = user.Notify ? Terms.TurnNotificationsOff : Terms.TurnNotificationsOn;

            if (message.Text == Terms.CurrentStatus)
            {
                var historyService = ServicesProvider.Get<IHistoryService>();
                var lastRecord = historyService.GetLastRecord();

                if (lastRecord is null)
                {
                    replyMessage = Terms.NoDataAvailable;
                }
                else
                {
                    var messageTemplate = lastRecord.Status == Status.PowerOn ? Terms.ThereIsElectricity : Terms.ThereIsNoElectricity;
                    replyMessage = string.Format(messageTemplate, lastRecord.DateTime.GetTimeDifference());
                }
            }

            if (message.Text == Terms.Today || message.Text == Terms.Tomorrow)
            {
                var scheduleService = ServicesProvider.Get<IScheduleService>();
                var fullSchedule = scheduleService.GetCurrent();

                if (fullSchedule is null)
                {
                    replyMessage = Terms.NoDataAvailable;
                }
                else
                {
                    var schedule = message.Text == Terms.Today ? fullSchedule.Today : fullSchedule.Tomorrow;
                    replyMessage = GetScheduleMessage(schedule, message.Text == Terms.Today);
                }
            }

            var buttons = new List<List<string>>
            {
                new() { btnText },
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
            var arrow = today && now.IsBetween(slot.Start, slot.End) ? "👈" : "";
            var status = slot.Status == Status.PowerOn ? "🔋" : "🪫";
            message += $"{slot.Start:HH:mm} - {slot.End:HH:mm} {status} {arrow}\n";
        }
        return message;
    }
}
