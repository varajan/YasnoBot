namespace Base.Interfaces;

public interface IApi
{
    Task<T?> Get<T>(string url) where T : class;
}
