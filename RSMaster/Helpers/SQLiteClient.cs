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
                string table1 = "CREATE TABLE IF NOT EXISTS accounts (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "name VARCHAR (100) NOT NULL," +
                    "username VARCHAR (200) NULL UNIQUE," +
                    "password VARCHAR (200) NULL," +
                    "world INTEGER NULL," +
                    "script VARCHAR (100) NULL," +
                    "proxyname VARCHAR (100) NULL," +
                    "proxyenabled INTEGER DEFAULT 0," +
                    "bankpin VARCHAR (10) NULL DEFAULT '0000'," +
                    "temporary INTEGER DEFAULT 0," +
                    "created DATETIME DEFAULT (DATETIME(CURRENT_TIMESTAMP))" +
                ");";

                string table2 = "CREATE TABLE IF NOT EXISTS proxies (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "alias VARCHAR(100) NOT NULL," +
                    "host VARCHAR(100) NOT NULL," +
                    "port VARCHAR(100) NOT NULL," +
                    "username VARCHAR(140)," +
                    "password VARCHAR(140)," +
                    "type VARCHAR(100) NOT NULL," + 
                    "created DATETIME DEFAULT (DATETIME(CURRENT_TIMESTAMP))" +
                ");";

                string table3 = "CREATE TABLE IF NOT EXISTS schedule (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "accountid INTEGER NOT NULL," +
                    "script VARCHAR (100) NULL," +
                    "day INTEGER NULL," +
                    "beginningTime VARCHAR (10) NULL," +
                    "endingTime VARCHAR (10) NULL," +
                    "active INTEGER DEFAULT 0" +
                ");";

                Execute(Command(table1));
                Execute(Command(table2));
                Execute(Command(table3));
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
