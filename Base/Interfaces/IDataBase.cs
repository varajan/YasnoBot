namespace Base.Interfaces;

public interface IDataBase
{
    void Execute(string sql);
    string GetValue(string sql);
    IList<string> GetColumn(string sql);
    Dictionary<string, string> GetRow(string sql);
    IList<Dictionary<string, string>> GetRows(string sql);
}