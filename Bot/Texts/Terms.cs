namespace Bot.Texts;

public class Terms
{
    public const string Done = $"{Icons.Ok} Готово";
    public const string Logs = $"{Icons.Logs} Логи";
    public const string Settings = $"{Icons.Gear} Налаштування";
    public const string AccessDenied = $"{Icons.Warning} Доступ заборонено";
    public const string Update = $"{Icons.Gear} Оновити";
    public const string GoBack = $"{Icons.Back} Повернутися";
    public const string NoSettings = $"{Icons.Warning} Немає доступних налаштувань";
    public const string NewSettingQuestion = $"{{0}}. Яке нове значення?";
    public const string InvalidData = "Некоректне значення";
    public const string CanDoLater = "Можете зробити це пізніше";


    public const string City = $"{Icons.City} Місто";
    public const string Dsos = $"{Icons.Dsos} Оператор системи розподілу";
    public const string Street = $"{Icons.Street} Вулиця";
    public const string House = $"{Icons.House} Будинок";
    public const string IPs = $"{Icons.Network} IP-адреси";

    public const string TurnNotificationsOff = $"{Icons.BellOff} Вимкнути сповіщення";
    public const string TurnNotificationsOn = $"{Icons.BellOn} Увімкнути сповіщення";
    public const string CurrentStatus = $"{Icons.Chart} Поточний статус";
    public const string Today = $"{Icons.Calendar} Сьогодні";
    public const string Tomorrow = $"{Icons.Calendar} Завтра";

    public const string NoScheduleAvailable = "Розклад відсутній";
    public const string ExpectedAt = "Очікувалось о ";
    public const string AnyTime = "будь-якої миті";
    public const string Unknown = "невідомо";
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

    public const string WellcomeAdminMessage = @"Вітаю!

Ти адміністратор цього бота. Давай налаштуємо його роботу.

Введи назву міста, постачальника електроенергії та адресу. За цими даними бот отримуватиме графіки відключень і повідомлятиме про них.

Також додай IP-адреси кількох пристроїв у твоїй домашній мережі, які працюють лише за наявності електроенергії.

Не забудь закріпити за ними статичні IP-адреси в налаштуваннях роутера. Це дозволить боту визначати, коли електроенергію вимкнули або відновили.";

}

public class Icons
{
    public const string Ok = "✅";
    public const string BatteryFull = "🔋";
    public const string BatteryEmpty = "🪫";
    public const string Calendar = "📅";
    public const string Gear = "⚙";
    public const string Logs = "📝";
    public const string Arrow = "👈";
    public const string Warning = "⚠";
    public const string Back = "⬅";
    public const string Alert = "‼";
    public const string BellOn = "🔔";
    public const string BellOff = "🔕";
    public const string Chart = "📊";
    public const string Timer = "⏳";
    public const string Clock = "⏰";
    public const string City = "🏙";
    public const string Dsos = "🏭";
    public const string Street = "🛣";
    public const string House = "🏠";
    public const string Network = "🌐";
}
