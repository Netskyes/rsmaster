using System;
using System.IO;
using System.Data;
using System.Data.SQLite;

namespace RSMaster
{
    using Utility;

    public class SQLiteClient
    {
        private SQLiteConnection conn;
        private bool IsConnectionOpen() => (conn != null) && conn.State == ConnectionState.Open;

        public SQLiteClient(string dbName)
        {
            try
            {
                string path = Path.Combine(Util.AssemblyDirectory, dbName);

                if (!File.Exists(path))
                {
                    SQLiteConnection.CreateFile(path);
                }

                conn = new SQLiteConnection(string.Format("Data Source={0};Version=3", path));
                conn.Open();
            }
            catch (Exception e)
            {
                Util.LogException(e);
            }

            if (IsConnectionOpen())
            {
                foreach (var schema in Data.SchemaProvider.Schemas)
                {
                    try
                    {
                        Execute(Command(schema));
                    }
                    catch (Exception e)
                    {
                        Util.LogException(e);
                    }
                }
            }
        }

        public SQLiteCommand Command(string query, SQLiteParameter[] queryParams = null)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = query;

            if (queryParams != null)
            {
                command.Parameters.AddRange(queryParams);
            }
            
            return command;
        }

        public int Execute(string query, SQLiteParameter[] queryParams = null)
        {
            return Execute(Command(query, queryParams));
        }

        public int Execute(SQLiteCommand command)
        {
            return (IsConnectionOpen()) ? command.ExecuteNonQuery() : -1;
        }

        public SQLiteDataReader ExecuteReader(SQLiteCommand command)
        {
            return (IsConnectionOpen()) ? command.ExecuteReader() : null;
        }

        public int FetchNumRows(SQLiteCommand command)
        {
            return (IsConnectionOpen()) ? Convert.ToInt32(command.ExecuteScalar()) : -1;
        }
    }
}
