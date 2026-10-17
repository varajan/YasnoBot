using Base.DTOs;
using Base.Interfaces;
using YasnoLogger.Interfaces;

namespace Base.Services;

public class UserRepository(IDataBase dataBase) : IUserRepository
{
    public bool Exists(long userId) =>
        dataBase.GetRows($"SELECT * FROM Users WHERE ID = {userId}").FirstOrDefault() != null;

    public UserDto? Get(long userId)
    {
        var user = dataBase.GetRows($"SELECT * FROM Users WHERE ID = {userId}").FirstOrDefault();

        return (user is null) ? null : GetUser(user);
    }

    public List<UserDto> GetAll() =>
        dataBase.GetRows("SELECT * FROM Users").Select(GetUser).ToList();

    public void Save(UserDto user)
    {
        if (Exists(user.Id))
        {
            dataBase.Execute($"UPDATE Users SET Notify = {(user.Notify ? 1 : 0)} WHERE ID = {user.Id}");
            return;
        }

        dataBase.Execute($"INSERT INTO Users (ID, Notify) VALUES ({user.Id}, {(user.Notify ? 1 : 0)})");
    }

    private static UserDto GetUser(Dictionary<string, string> user) => new UserDto
    {
        Id = long.Parse(user["ID"]),
        Notify = user["Notify"] == "1"
    };
}
