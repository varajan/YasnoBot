using Microsoft.Data.Sqlite;
using MoreLinq;
using YasnoBot.YasnoLogger.Extensions;
using YasnoLogger.Interfaces;

namespace Base.Services;

public class SQLiteDataBase(ILogger logger) : IDataBase
{
    private static string DatabaseFileName => $"{AppDomain.CurrentDomain.BaseDirectory}/DB.db";
    private static string ConnectionString => $"Data Source={DatabaseFileName}; Cache=Shared";

    private static SqliteConnection _connection;
    private SqliteConnection Connection
    {
        get
        {
            if (_connection == null || !IsReady)
            {
                _connection = new SqliteConnection(ConnectionString);
                _connection.Open();

                var newVersion = 1;
                var currentVersion = GetValue("PRAGMA user_version;").ToInt();
                if (currentVersion != newVersion)
                {
                    var tableNames = GetColumn("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'");
                    var dropTablesCommands = tableNames.Select(x => $"DROP TABLE {x};");
                    var createTablesCommand = @$"
                        CREATE TABLE Users (ID Number PRIMARY KEY, Notify INTEGER);
                        CREATE TABLE History (DateTime TEXT PRIMARY KEY, Status INTEGER);
                        CREATE TABLE Data (Key TEXT PRIMARY KEY, Value TEXT);
                        CREATE TABLE Settings (Key TEXT PRIMARY KEY, Value TEXT); 

                        PRAGMA user_version = {newVersion};";

                    dropTablesCommands.ForEach(Execute);
                    Execute(createTablesCommand, _connection);
                }
            }

            return _connection;
        }
    }

    private static bool IsReady => File.Exists(DatabaseFileName);

    public void Execute(string sql) => Execute(sql, Connection);

    private void Execute(string sql, SqliteConnection connection = null)
    {
        var cmd = new SqliteCommand(sql, connection ?? Connection);

        try
        {
            cmd.ExecuteNonQuery();
            logger.Log(sql);
        }
        catch (Exception e)
        {
            Log(e, sql);
        }
        finally
        {
            cmd.Dispose();
        }
    }

    public string GetValue(string sql)
    {
        string result = null;
        var cmd = new SqliteCommand(sql, Connection);

        try
        {
            result = (cmd.ExecuteScalar() ?? string.Empty).ToString();
        }
        catch (Exception e)
        {
            Log(e, sql);
        }
        finally
        {
            cmd.Dispose();
        }

        return result;
    }

    public IList<string> GetColumn(string sql)
    {
        var result = new List<string>();
        var cmd = new SqliteCommand(sql, Connection);

        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader[Columns(sql).First()].ToString());
            }
        }
        catch (Exception e)
        {
            Log(e, sql);
        }
        finally
        {
            cmd.Dispose();
        }

        return result;
    }

    public IList<Dictionary<string, string>> GetRows(string sql)
    {
        var result = new List<Dictionary<string, string>>();
        var cmd = new SqliteCommand(sql, Connection);

        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var values = Columns(sql).ToDictionary(column => column, column => reader[column].ToString());
                result.Add(values);
            }
        }
        catch (Exception e)
        {
            Log(e, sql);
        }
        finally
        {
            cmd.Dispose();
        }

        return result;
    }

    public Dictionary<string, string> GetRow(string sql)
    {
        var result = new Dictionary<string, string>();
        var cmd = new SqliteCommand(sql, Connection);

        try
        {
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result = Columns(sql).ToDictionary(column => column, column => reader[column].ToString());
            }
        }
        catch (Exception e)
        {
            Log(e, sql);
        }
        finally
        {
            cmd.Dispose();
        }

        return result;
    }

    private List<string> Columns(string sql)
    {
        var columns = sql.Replace("DISTINCT", string.Empty).SubString("select", "from").Trim().Split(",").Trim().ToList();

        if (columns.Count == 1 && columns[0] == "*")
        {
            var table = sql.SubString("from").Trim().Split(' ').First();
            columns = [.. GetColumn($"SELECT name FROM pragma_table_info('{table}')")];
        }

        return columns;
    }

    private void Log(Exception ex, string sql)
    {
        Console.WriteLine($"{ex.Message}{Environment.NewLine}{sql}{Environment.NewLine}{ex.StackTrace}");
        logger.Log(sql);
        logger.Log(ex);
    }
}
