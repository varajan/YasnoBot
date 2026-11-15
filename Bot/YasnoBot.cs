using Base;
using Base.DTOs;
using Base.Interfaces;
using Bot.Tasks;
using Bot.Texts;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bot;

public class YasnoBot
{
    private static ILogger Logger => ServicesProvider.Get<ILogger>();
    private static IUserRepository UserRepository => ServicesProvider.Get<IUserRepository>();
    private static INotifyService NotifyService => ServicesProvider.Get<INotifyService>();
    private static IScheduleService ScheduleService => ServicesProvider.Get<IScheduleService>();
    private static IHistoryService HistoryService => ServicesProvider.Get<IHistoryService>();
    private static IBotStateService BotStateService => ServicesProvider.Get<IBotStateService>();
    private static ISettings Settings => ServicesProvider.Get<ISettings>();
    private static IStatusService StatusService => ServicesProvider.Get<IStatusService>();
    private static ScheduledTasks? scheduledTasks;
    private static OneTimeRunTasks? oneTimeRunTasks;

    private static async Task Main()
    {
        var botClient = new TelegramBotClient(BotToken);
        using var cts = new CancellationTokenSource();
        var receiverOptions = new ReceiverOptions { AllowedUpdates = [] };
        var notifyService = new TelegramBotNotifyService(botClient);

        ServicesProvider.AddApplicationServices();
        ServicesProvider.Add<INotifyService>(notifyService);

        scheduledTasks = new ScheduledTasks(botClient, Logger, notifyService, ScheduleService, UserRepository, StatusService, HistoryService, BotStateService);
        oneTimeRunTasks = new OneTimeRunTasks(Logger, notifyService, UserRepository, HistoryService, ScheduleService);

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: cts.Token
        );

        _ = Task.Run(async () => await oneTimeRunTasks.DoOnce(() => oneTimeRunTasks.SendNewVersionNotification()));
        _ = Task.Run(async () => await oneTimeRunTasks.DoOnce(() => oneTimeRunTasks.SendCurrentStatusNotification()));

        _ = Task.Run(async () => await scheduledTasks.DoEveryMinute(() => scheduledTasks.GetCurrentStatus(), 0));
        _ = Task.Run(async () => await scheduledTasks.DoEveryMinute(() => scheduledTasks.StatusChangedNotification(), 5));
        _ = Task.Run(async () => await scheduledTasks.DoEveryMinute(() => scheduledTasks.NewScheduleNotification(), 10));
        _ = Task.Run(async () => await scheduledTasks.DoEveryMinute(() => scheduledTasks.UpcomingBlackoutNotification(), 15));
        _ = Task.Run(async () => await scheduledTasks.DoEveryMinute(() => scheduledTasks.UpdateIcon(), 20));

        _ = Task.Run(async () => await scheduledTasks.DoEvery10Minutes(() => scheduledTasks.UpdateSchedule()));

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
            //var envVariableToken = Environment.GetEnvironmentVariable("YASNO_BOT_TOKEN").NullIfEmpty();
            var fileToken = File.Exists(botIdTxtFile) ? File.ReadAllLines(botIdTxtFile).FirstOrDefault(x => !string.IsNullOrEmpty(x)) : null;
            var token = fileToken;
            //var token = envVariableToken ?? fileToken;

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
            var usersCount = UserRepository.GetAll().Count;
            var user = UserRepository.Get(message.Chat.Id);
            if (user is null)
            {
                user = new UserDto
                {
                    Id = message.Chat.Id,
                    Notify = true,
                    IsAdmin = usersCount == 0 // First user to interact with the bot becomes admin
                };
                UserRepository.Save(user);
            }

            var adminCommands = new List<string> { Terms.Settings, Terms.Logs, Terms.Update, Terms.City, Terms.Street, Terms.House, Terms.Dsos, Terms.IPs };

            if (adminCommands.Contains(message.Text))
            {
                await HandleAdminCommand(user, message.Text);
                return;
            }
            else if (user.IsAdmin && !string.IsNullOrEmpty(BotStateService.GetMaintenanceMode()))
            {
                await HandleMaintenanceMessage(user, message.Text);
                return;
            }
            else
            {
                await HandleUserCommand(user, message.Text);
                return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Logger.Log(e);
        }
    }

    private static async Task HandleAdminCommand(UserDto user, string message)
    {
        if (!user.IsAdmin)
        {
            await NotifyService.Notify(user.Id, Terms.AccessDenied);
            return;
        }

        var messages = new Messages(HistoryService, ScheduleService);

        switch (message)
        {
            case Terms.Settings:
                var settingsMessage = messages.SettingsMessage(Settings);
                var buttons = new List<List<string>> { new() { Terms.GoBack, Terms.Update }, };
                await NotifyService.NotifyWithButtons(user.Id, settingsMessage, buttons);
                return;

            case Terms.Logs:
                await NotifyService.Notify(user.Id, Logger.TopLogs);
                return;

            case Terms.Update:
                var updateButtons = new List<List<string>>
                {
                    new() { Terms.City, Terms.Street, Terms.House },
                    new() { Terms.Dsos, Terms.IPs },
                    new() { Terms.GoBack },
                };
                var updateMessage = messages.SettingsMessage(Settings);
                await NotifyService.NotifyWithButtons(user.Id, updateMessage, updateButtons);
                return;

            case Terms.City:
                BotStateService.SetMaintenanceMode(message);
                var cities = await ScheduleService.GetCities();
                var cityMessage = string.Format(Terms.NewSettingQuestion, BotStateService.GetMaintenanceMode());
                await NotifyService.NotifyWithButtons(user.Id, cityMessage, cities.Append(Terms.GoBack).ToArray());
                return;

            case Terms.Dsos:
                BotStateService.SetMaintenanceMode(message);
                var dsoses = await ScheduleService.GetDsoses();
                var dsosMessage = string.Format(Terms.NewSettingQuestion, BotStateService.GetMaintenanceMode());
                await NotifyService.NotifyWithButtons(user.Id, dsosMessage, dsoses.Append(Terms.GoBack).ToArray());
                return;

            case Terms.House:
                BotStateService.SetMaintenanceMode(message);
                var houses = await ScheduleService.GetHuoses();
                var houseMessage = string.Format(Terms.NewSettingQuestion, BotStateService.GetMaintenanceMode());
                await NotifyService.NotifyWithButtons(user.Id, houseMessage, houses.Append(Terms.GoBack).ToArray());
                return;

            case Terms.Street:
            case Terms.IPs:
                BotStateService.SetMaintenanceMode(message);

                var settingKey = GetSettingKey(message);
                var maintenanceMessage = messages.GetSettingMessage(Settings, settingKey, message);
                var maintenanceButtons = new List<List<string>> { new() { Terms.Update, Terms.GoBack }, };
                await NotifyService.NotifyWithButtons(user.Id, maintenanceMessage, maintenanceButtons);
                return;

            case Terms.GoBack:
                BotStateService.SetMaintenanceMode(string.Empty);
                await HandleUserCommand(user, string.Empty);
                return;
        }
    }

    private static async Task HandleUserCommand(UserDto user, string message)
    {
        var replyMessage = Terms.NotificationsOn;
        var turnNotificationBtn = user.Notify ? Terms.TurnNotificationsOff : Terms.TurnNotificationsOn;
        var messages = new Messages(HistoryService, ScheduleService);

        switch (message)
        {
            case Terms.TurnNotificationsOn:
            case Terms.TurnNotificationsOff:
                user.Notify = message == Terms.TurnNotificationsOn;
                UserRepository.Save(user);
                replyMessage = user.Notify ? Terms.NotificationsOn : Terms.NotificationsOff;
                turnNotificationBtn = user.Notify ? Terms.TurnNotificationsOff : Terms.TurnNotificationsOn;
                break;

            case Terms.CurrentStatus:
                replyMessage = messages.GetCurrentStatusMessage() ?? Terms.NoDataAvailable;
                break;

            case Terms.Today:
            case Terms.Tomorrow:
                replyMessage = messages.GetScheduleMessage(message == Terms.Today) ?? Terms.NoDataAvailable;
                break;
        }

        var buttons = new List<List<string>>
            {
                new() { turnNotificationBtn },
                new() { Terms.CurrentStatus },
                new() { Terms.Today, Terms.Tomorrow },
            };
        if (user.IsAdmin)
        {
            buttons.Add([Terms.Settings, Terms.Logs]);
        }

        await NotifyService.NotifyWithButtons(user.Id, replyMessage, buttons);
    }

    private static async Task HandleMaintenanceMessage(UserDto user, string message)
    {
        var messages = new Messages(HistoryService, ScheduleService);
        var mode = BotStateService.GetMaintenanceMode();

        if (message == Terms.GoBack)
        {
            BotStateService.SetMaintenanceMode(string.Empty);
            await HandleUserCommand(user, string.Empty);
            return;
        }

        var key = GetSettingKey(mode);
        var current = Settings.Get(key);
        var isValid = await ValidateUpdateSetting(mode, message);
        if (isValid && current != message)
        {
            Settings.Set(key, message);
            await ResetDependentSettings(key);
            await NotifyService.Notify(user.Id, Terms.Done);
        }
        else
        {
            await NotifyService.Notify(user.Id, Terms.InvalidData);
            return;
        }

        var updateButtons = new List<List<string>>
                {
                    new() { Terms.City, Terms.Street, Terms.House },
                    new() { Terms.Dsos, Terms.IPs },
                    new() { Terms.GoBack },
                };
        var updateMessage = messages.SettingsMessage(Settings);
        await NotifyService.NotifyWithButtons(user.Id, updateMessage, updateButtons);
    }

    private static async Task<bool> ValidateUpdateSetting(string mode, string value)
    {
        if (mode == Terms.City)
        {
            var values = await ScheduleService.GetCities();
            return values.Contains(value);
        }

        if (mode == Terms.Dsos)
        {
            var city = Settings.Get(Constants.CITY);
            var values = await ScheduleService.GetDsoses();
            return values.Contains(value);
        }

        if (mode == Terms.Street)
        {
            var city = Settings.Get(Constants.CITY);
            var dsos = Settings.Get(Constants.DSOS);
            var (cityId, dsosId) = await ScheduleService.GetRegionData(city, dsos);
            var streetId = await ScheduleService.GetStreetId(value, cityId, dsosId);
            return streetId > 0;
        }

        if (mode == Terms.House)
        {
            var city = Settings.Get(Constants.CITY);
            var dsos = Settings.Get(Constants.DSOS);
            var street = Settings.Get(Constants.STREET);
            var (cityId, dsosId) = await ScheduleService.GetRegionData(city, dsos);
            var streetId = await ScheduleService.GetStreetId(street, cityId, dsosId);
            var houseId = await ScheduleService.GetHouseId(value, cityId, dsosId, streetId);
            return streetId > 0;
        }

        if (mode == Terms.IPs)
        {
            // validate IP is valid.
        }

        return false;
    }

    private static async Task ResetDependentSettings(string key)
    {
        switch (key)
        {
            case Constants.CITY:
                Settings.Set(Constants.DSOS, null);
                Settings.Set(Constants.STREET, null);
                Settings.Set(Constants.HOUSE, null);
                break;

            case Constants.DSOS:
                Settings.Set(Constants.STREET, null);
                Settings.Set(Constants.HOUSE, null);
                break;

            case Constants.STREET:
                Settings.Set(Constants.HOUSE, null);
                break;
        }
    }

    private static string GetSettingKey(string term) => term switch
    {
        Terms.City => Constants.CITY,
        Terms.Dsos => Constants.DSOS,
        Terms.Street => Constants.STREET,
        Terms.House => Constants.HOUSE,
        Terms.IPs => Constants.IPS,
    };
}
