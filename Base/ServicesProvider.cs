using Base.Interfaces;
using Base.Services;
using Microsoft.Extensions.DependencyInjection;
using YasnoLogger.Interfaces;
using YasnoLogger.Loggers;

namespace Base;

public static class ServicesProvider
{
    public static IServiceProvider Instance { get; private set; }
    private static readonly ServiceCollection services = [];

    public static void AddApplicationServices()
    {
        services.AddSingleton<ILogger, FileLogger>();
        services.AddSingleton<IDataBase, SQLiteDataBase>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<IStatusService, StatusService>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<ISettings, Settings>();
        services.AddSingleton<IScheduleService, YasnoService>();
        services.AddSingleton<IApi, ApiService>();

        Instance = services.BuildServiceProvider();
    }

    public static void Add<TImplementation>(TImplementation implemenation) where TImplementation : class
    {
        services.AddSingleton(implemenation);
        Instance = services.BuildServiceProvider();
    }

    public static T Get<T>() => Instance.GetRequiredService<T>();
    public static object Get(Type type) => Instance.GetRequiredService(type);
}