using Base;
using Base.DTOs;
using Base.Extensions;
using Base.Interfaces;
using Bot.Texts;

namespace Bot;

public class Setup(IBotStateService BotStateService, INotifyService NotifyService, IScheduleService ScheduleService,
    ISettings Settings, IHistoryService HistoryService, IUserRepository UserRepository)
{
    private const int WaitTimeout = 1;

    private const string SetupWelcome = "Welcome";
    private const string SetupCity = "ASK" + Terms.City;
    private const string SetupDsos = "ASK" + Terms.Dsos;
    private const string SetupStreet = "ASK" + Terms.Street;
    private const string SetupHouse = "ASK" + Terms.House;
    private const string SetupIPs = "ASK" + Terms.IPs;

    public async Task HandleSetupBot(UserDto user, string message)
    {
        if (!user.IsAdmin) return;

        var mode = BotStateService.GetMaintenanceMode();

        if (string.IsNullOrEmpty(mode) && Settings.Keys.Count == 0)
        {
            BotStateService.SetMaintenanceMode(SetupWelcome);
            mode = SetupWelcome;
        }

        switch (mode)
        {
            case SetupWelcome:
                await StartSetup(user);
                break;

            case SetupCity:
                await HandleCity(user, message);
                break;

            case SetupDsos:
                await HandleSetupStep(user, message, Terms.Dsos, SetupStreet);
                break;

            case SetupStreet:
                await HandleStreet(user, message);
                break;

            case SetupHouse:
                await HandleSetupStep(user, message, Terms.House, SetupIPs);
                break;

            case SetupIPs:
                await HandleIPs(user, message);
                break;
        }
    }

    public async Task HandleUserCommand(UserDto user, string message)
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

    private async Task StartSetup(UserDto user)
    {
        await NotifyService.Notify(user.Id, Terms.WellcomeAdminMessage);

        var cities = await ScheduleService.GetCities(WaitTimeout);

        await NotifyService.NotifyWithButtons(
            user.Id, Terms.City, cities.ToArray());

        BotStateService.SetMaintenanceMode(SetupCity);
    }

    private async Task HandleCity(UserDto user, string message)
    {
        if (!await SaveIfValid(Terms.City, message))
        {
            await RetrySetup(user, Terms.City);
            return;
        }

        var dsoses = await ScheduleService.GetDsoses(WaitTimeout);

        await NotifyService.Notify(user.Id, Terms.Done);
        await NotifyService.NotifyWithButtons(
            user.Id, Terms.Dsos, dsoses.ToArray());

        BotStateService.SetMaintenanceMode(SetupDsos);
    }

    private async Task HandleStreet(UserDto user, string message)
    {
        if (!await SaveIfValid(Terms.Street, message))
        {
            await RetrySetup(user, Terms.Street);
            return;
        }

        var houses = await ScheduleService.GetHouses(WaitTimeout);

        await NotifyService.Notify(user.Id, Terms.Done);
        await NotifyService.NotifyWithButtons(
            user.Id, Terms.House, houses.ToArray());

        BotStateService.SetMaintenanceMode(SetupHouse);
    }

    private async Task HandleSetupStep(
        UserDto user, string message, string setting, string nextStep)
    {
        if (!await SaveIfValid(setting, message))
        {
            await RetrySetup(user, setting);
            return;
        }

        await NotifyService.Notify(user.Id, Terms.Done);

        var nextMessage = nextStep switch
        {
            SetupStreet => Terms.Street,
            SetupIPs => Terms.IPs,
            _ => string.Empty
        };

        await NotifyService.Notify(user.Id, nextMessage);

        BotStateService.SetMaintenanceMode(nextStep);
    }

    private async Task HandleIPs(UserDto user, string message)
    {
        if (await SaveIfValid(Terms.IPs, message))
        {
            await NotifyService.Notify(user.Id, Terms.Done);
        }
        else
        {
            await NotifyService.Notify(user.Id, Terms.InvalidData);
            await NotifyService.Notify(user.Id, Terms.CanDoLater);

            Settings.Set(GetSettingKey(Terms.IPs), "8.8.8.8");
        }

        await CompleteSetup(user);
    }

    private async Task RetrySetup(UserDto user, string setting)
    {
        await NotifyService.Notify(user.Id, Terms.InvalidData);
        await NotifyService.Notify(user.Id, setting);
    }

    private async Task CompleteSetup(UserDto user)
    {
        BotStateService.SetMaintenanceMode(string.Empty);
        await HandleUserCommand(user, string.Empty);
    }

    public string GetSettingKey(string term) => term switch
    {
        Terms.City => Constants.CITY,
        Terms.Dsos => Constants.DSOS,
        Terms.Street => Constants.STREET,
        Terms.House => Constants.HOUSE,
        Terms.IPs => Constants.IPS,
    };

    private async Task<bool> ValidateUpdateSetting(string mode, string value)
    {
        var waitTimeout = 200;

        if (mode == Terms.City)
        {
            var values = await ScheduleService.GetCities(waitTimeout);
            return values.Contains(value);
        }

        if (mode == Terms.Dsos)
        {
            var city = Settings.Get(Constants.CITY);
            var values = await ScheduleService.GetDsoses(waitTimeout);
            return values.Contains(value);
        }

        if (mode == Terms.Street)
        {
            var city = Settings.Get(Constants.CITY);
            var dsos = Settings.Get(Constants.DSOS);
            var (cityId, dsosId) = await ScheduleService.GetRegionData(city, dsos, waitTimeout);
            var streetId = await ScheduleService.GetStreetId(value, cityId, dsosId, waitTimeout);
            return streetId > 0;
        }

        if (mode == Terms.House)
        {
            var city = Settings.Get(Constants.CITY);
            var dsos = Settings.Get(Constants.DSOS);
            var street = Settings.Get(Constants.STREET);
            var (cityId, dsosId) = await ScheduleService.GetRegionData(city, dsos, waitTimeout);
            var streetId = await ScheduleService.GetStreetId(street, cityId, dsosId, waitTimeout);
            var houseId = await ScheduleService.GetHouseId(value, cityId, dsosId, streetId, waitTimeout);
            return streetId > 0;
        }

        if (mode == Terms.IPs)
        {
            return value.Split(',').Select(x => x.Trim()).All(x => x.IsValidIpAddress());
        }

        return false;
    }

    private async Task ResetDependentSettings(string key)
    {
        switch (key)
        {
            case Constants.CITY:
                Settings.Set(Constants.DSOS, string.Empty);
                Settings.Set(Constants.STREET, string.Empty);
                Settings.Set(Constants.HOUSE, string.Empty);
                break;

            case Constants.DSOS:
                Settings.Set(Constants.STREET, string.Empty);
                Settings.Set(Constants.HOUSE, string.Empty);
                break;

            case Constants.STREET:
                Settings.Set(Constants.HOUSE, string.Empty);
                break;
        }
    }

    public async Task<bool> SaveIfValid(string mode, string value)
    {
        var key = GetSettingKey(mode);
        var current = Settings.Get(key);
        var isValid = await ValidateUpdateSetting(mode, value);
        if (isValid && current != value)
        {
            Settings.Set(key, value);
            await ResetDependentSettings(key);
            return true;
        }

        return false;
    }
}
