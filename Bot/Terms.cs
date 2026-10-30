namespace Bot;

public class Terms
{
    public const string TurnNotificationsOff = $"{Icons.BellOff} Вимкнути сповіщення";
    public const string TurnNotificationsOn = $"{Icons.BellOn} Увімкнути сповіщення";
    public const string CurrentStatus = $"{Icons.Chart} Поточний статус";
    public const string Today = $"{Icons.Calendar} Сьогодні";
    public const string Tomorrow = $"{Icons.Calendar} Завтра";

    public const string NoScheduleAvailable = "Розклад відсутній";
    public const string NoDataAvailable = "Немає даних";
    public const string EmergencyBlackout = $"{Icons.Warning} Аварійні відключення";

    public const string ThereIsElectricity = $"{Icons.BatteryFull} Світло є {{0}}\n{Icons.Clock} Наступне вимкнення: {{1}}.";
    public const string ThereIsNoElectricity = $"{Icons.BatteryEmpty} Світла немає {{0}}\n{Icons.Clock} Орієнтовно до: {{1}}.";

    public const string ThereWasElectricity =
        $"{Icons.BatteryEmpty} Вимкнули світло.\n{Icons.Timer} Було: {{0}}\n{Icons.Clock} Орієнтовно до: {{1}}.";
    public const string ThereWasNoElectricity =
        $"{Icons.BatteryFull} Дали світло.\n{Icons.Timer} Не було: {{0}}\n{Icons.Clock} Наступне вимкнення: {{1}}.";

    public const string NotificationsOn = "Сповіщення увімкнено";
    public const string NotificationsOff = "Сповіщення вимкнено";

    public const string NewVersionMessage = "Бот запущено, поточна версія {0}";

    public const string NewScheduleMessage4Today = $"{Icons.Calendar} Новий розклад на сьогодні.";
    public const string NewScheduleMessage4Tomorrow = $"{Icons.Calendar} Новий розклад на завтра.";
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
    public const string Timer = "⏳";
    public const string Clock = "⏰";
}
