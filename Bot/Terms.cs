namespace Bot;

public class Terms
{
    public static string TurnNotificationsOff => $"{Icons.BellOff} Вимкнути сповіщення";
    public static string TurnNotificationsOn => $"{Icons.BellOn} Увімкнути сповіщення";
    public const string CurrentStatus = $"{Icons.Chart} Поточний статус";
    public static string Today => $"{Icons.Calendar} Сьогодні";
    public static string Tomorrow => $"{Icons.Calendar} Завтра";

    public const string Schedule = "Розклад";
    public const string NoScheduleAvailable = "Розклад відсутній";
    public const string NoDataAvailable = "Немає даних";
    public static string EmergencyBlackout => $"{Icons.Warning} Аварійні відключення";

    public static string ThereIsElectricity => $"{Icons.BatteryFull} Світло є вже {{0}}";
    public static string ThereIsNoElectricity => $"{Icons.BatteryEmpty} Світла немає вже {{0}}";

    public static string ThereWasElectricity => $"{Icons.BatteryEmpty} Світло було {{0}}";
    public static string ThereWasNoElectricity => $"{Icons.BatteryFull} Світла не було {{0}}";

    public const string NotificationsOn = "Сповіщення увімкнено";
    public const string NotificationsOff = "Сповіщення вимкнено";

    public const string NewVersionMessage = "Доступна нова версія {0}";

    public static string NewScheduleMessage => $"{Icons.Calendar} Доступний новий розклад!";
    public static string EmergencyBlackoutMessage => $"{Icons.Alert} Починаються аварійні відключення!";
    public static string NextBlackoutMessage => $"{Icons.BatteryEmpty} Наступне відключення через 5 хвилин";
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
