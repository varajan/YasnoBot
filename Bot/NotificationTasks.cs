using Base.DTOs;
using Base.Interfaces;

namespace Bot;

public abstract class NotificationTasks(ILogger logger, INotifyService notifyService, IUserRepository userRepository)
{
    private List<UserDto> UsersToNotify => userRepository.GetAll().Where(u => u.Notify).ToList();

    protected async Task NotifyUsers(string message)
    {
        foreach (var user in UsersToNotify)
        {
            try
            {
                await notifyService.Notify(user.Id, message);
            }
            catch (Exception e)
            {
                logger.Log($"Failed to notify user {user.Id}: {e.Message}");
            }
        }
    }
}
