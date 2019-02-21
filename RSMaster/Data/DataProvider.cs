using RSMaster.UI.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;

namespace RSMaster
{
    using Utility;

    internal static class DataProvider
    {
        private static readonly SQLiteClient client;
        private static readonly object dbLock = new object();

        static DataProvider()
        {
            client = new SQLiteClient("master.db3");
        }

        #region Account Repository

        public static IEnumerable<AccountModel> GetAccounts() => GetModels<AccountModel>("accounts");
        public static AccountModel GetAccountById(int accountId) => GetAccounts().FirstOrDefault(x => x.Id == accountId);
        public static bool DeleteAccount(int accountId) => DeleteModel(accountId, "accounts");
        public static bool SaveAccount(AccountModel account) => SaveModel(account, "accounts");
        public static bool UpdateAccount(AccountModel account) => UpdateModel(account, "accounts");

        #endregion

        public static IEnumerable<T> GetModels<T>(string tableName, DataRequestFilter filter = null) where T : IViewModel
        {
            if (filter is null)
                filter = new DataRequestFilter();

            var result = new List<T>();
            var conditions = (filter.Conditions != null) 
                ? " WHERE " + string.Join(",", filter.Conditions.Select(x => x.Key + " = '" + x.Value + "'")).TrimEnd(',') : string.Empty;

            var reader = client.ExecuteReader
                (client.Command(string.Format("SELECT * FROM {0}{1}{2}{3}",
                tableName, conditions, $" ORDER BY {filter.OrderColumn} {filter.OrderBy}", $" LIMIT {filter.Limit}")));

            var columns = typeof(T).GetProperties().Select(x => x.Name);
            Dictionary<int, Dictionary<string, object>> results;

            lock (dbLock)
            {
                results = GetResults(reader, columns.ToList());
            }

            foreach (var row in results)
            {
                var values = row.Value.Where
                    (x => x.Value != DBNull.Value);
                var instance = (T)Activator.CreateInstance(typeof(T));

                foreach (var value in values)
                {
                    var prop = instance.GetType().GetProperties().FirstOrDefault
                        (x => x.Name == value.Key);

                    // x64 conversion
                    if (prop.PropertyType == typeof(int?))
                    {
                        prop.SetValue(instance, (int?)(long)value.Value);
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        prop.SetValue(instance, (int)(long)value.Value);
                    }
                    else
                    {
                        prop.SetValue(instance, value.Value);
                    }
                }

                result.Add(instance);
            }

            return result;
        }

        public static bool SaveModel(IViewModel model, string tableName)
        {
            var data = model.GetType().GetProperties().Where
                (x => !Attribute.IsDefined(x, typeof(Extensions.PropInsertIgnore))).ToDictionary
                (x => x.Name, x => x.GetValue(model));

            var columns = data.Keys.Select(x => x).ToList();
            var query = string.Format("INSERT INTO {0} ({1}) VALUES({2});", 
                tableName, 
                string.Join(",", columns).TrimEnd(','), 
                string.Join(",", columns.Select(x => "@" + x)).TrimEnd(','));

            var parameters = new List<SQLiteParameter>(data.Select(x => new SQLiteParameter(x.Key, x.Value))).ToArray();
            var command = client.Command(query, parameters);

            lock (dbLock)
            {
                return client.Execute(command) > 0;
            }
        }

        public static bool UpdateModel(IViewModel model, string tableName)
        {
            var data = model.GetType().GetProperties().Where
                (x => !Attribute.IsDefined(x, typeof(Extensions.PropUpdateIgnore))).ToDictionary
                (x => x.Name, x => x.GetValue(model));

            var query = string.Format("UPDATE {0} SET {1} WHERE {2}", 
                tableName, 
                string.Join(",", data.Select(x => x.Key + " = @" + x.Key)).TrimEnd(','),
                "Id = " + model.Id);

            var parameters = new List<SQLiteParameter>(data.Select(x => new SQLiteParameter(x.Key, x.Value))).ToArray();
            var command = client.Command(query, parameters);

            lock (dbLock)
            {
                return client.Execute(command) > 0;
            }
        }

        public static bool DeleteModel(int modelId, string tableName)
        {
            var command = client.Command(string.Format("DELETE FROM {0} WHERE Id = {1}", tableName, modelId));
            lock (dbLock)
            {
                return client.Execute(command) > 0;
            }
        }

        private static Dictionary<int, Dictionary<string, object>> GetResults(SQLiteDataReader reader, List<string> columns)
        {
            var result = new Dictionary<int, Dictionary<string, object>>();
            if (reader == null || columns == null)
                return result;

            while (reader.Read())
            {
                var values = new Dictionary<string, object>();
                foreach (var col in columns)
                {
                    if (!reader.HasColumn(col))
                        continue;

                    try
                    {
                        values.Add(col, reader[col]);
                    }
                    catch (Exception e)
                    {
                        Util.LogException(e);
                    }
                }

                result.Add(reader.GetInt32(0), values);
            }

            return result;
        }

        public static bool HasColumn(this SQLiteDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
