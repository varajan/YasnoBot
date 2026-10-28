namespace Bot;

public class Terms
{
    public const string TurnNotificationsOff = $"{Icons.BellOff} Вимкнути сповіщення";
    public const string TurnNotificationsOn = $"{Icons.BellOn} Увімкнути сповіщення";
    public const string CurrentStatus = $"{Icons.Chart} Поточний статус";
    public const string Today = $"{Icons.Calendar} Сьогодні";
    public const string Tomorrow = $"{Icons.Calendar} Завтра";

    public const string Schedule = "Розклад";
    public const string NoScheduleAvailable = "Розклад відсутній";
    public const string NoDataAvailable = "Немає даних";
    public const string EmergencyBlackout = $"{Icons.Warning} Аварійні відключення";

    public const string ThereIsElectricity = $"{Icons.BatteryFull} Світло є {{0}}";
    public const string ThereIsNoElectricity = $"{Icons.BatteryEmpty} Світла немає {{0}}";

    public const string ThereWasElectricity = $"{Icons.BatteryEmpty} Світло було {{0}}";
    public const string ThereWasNoElectricity = $"{Icons.BatteryFull} Світла не було {{0}}";

    public const string NotificationsOn = "Сповіщення увімкнено";
    public const string NotificationsOff = "Сповіщення вимкнено";

    public const string NewVersionMessage = "Бот оновлено до версії {0}";

    public const string NewScheduleMessage = $"{Icons.Calendar} Доступний новий розклад!";
    public const string NewScheduleMessage4Today = $"{Icons.Calendar} Доступний новий розклад на сьогодні!";
    public const string NewScheduleMessage4Tomorrow = $"{Icons.Calendar} Доступний новий розклад на завтра!";
    public const string EmergencyBlackoutMessage = $"{Icons.Alert} Починаються аварійні відключення!";
    public const string NextBlackoutMessage = $"{Icons.BatteryEmpty} Наступне відключення через 5 хвилин";
}

public class Icons
{
    public const string BatteryFull = "🔋";
    public const string BatteryEmpty = "🪫";
    public const string Calendar = "📅";
    public const string Arrow = "👈";
    public const string Warning = "⚠";
    public const string Alert = "‼";
    public const string BellOn = "🔔";
    public const string BellOff = "🔕";
    public const string Chart = "📊";
}
