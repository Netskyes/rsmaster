using System;
using System.Collections.Generic;

namespace RSMaster.Data
{
    internal static class SchemaProvider
    {
        public static List<string> Schemas = new List<string>();

        static SchemaProvider()
        {
            Schemas.Add(Groups());
            Schemas.Add(Accounts());
            Schemas.Add("ALTER TABLE accounts ADD COLUMN groupId INTEGER NULL");
            Schemas.Add("ALTER TABLE accounts ADD COLUMN comments TEXT NULL");
            Schemas.Add(Proxies());
            Schemas.Add(Schedule());
            Schemas.Add(Unlocks());
            Schemas.Add("ALTER TABLE unlocks ADD COLUMN subemail INTEGER NULL");
        }

        private static string Groups()
        {
            return "CREATE TABLE IF NOT EXISTS groups (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "name VARCHAR(200) NOT NULL," +
                    "color VARCHAR(100) NOT NULL," +
                    "created DATETIME DEFAULT (DATETIME(CURRENT_TIMESTAMP))" +
                ");";
        }

        private static string Accounts()
        {
            return "CREATE TABLE IF NOT EXISTS accounts (" +
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
                    "groupId INTEGER NULL," +
                    "created DATETIME DEFAULT (DATETIME(CURRENT_TIMESTAMP))" +
                ");";
        }

        private static string Proxies()
        {
            return "CREATE TABLE IF NOT EXISTS proxies (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "alias VARCHAR(100) NOT NULL," +
                    "host VARCHAR(100) NOT NULL," +
                    "port VARCHAR(100) NOT NULL," +
                    "username VARCHAR(140)," +
                    "password VARCHAR(140)," +
                    "type VARCHAR(100) NOT NULL," +
                    "created DATETIME DEFAULT (DATETIME(CURRENT_TIMESTAMP))" +
                ");";
        }

        private static string Schedule()
        {
            return "CREATE TABLE IF NOT EXISTS schedule (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "accountid INTEGER NOT NULL," +
                    "script VARCHAR (100) NULL," +
                    "day INTEGER NULL," +
                    "beginningTime VARCHAR (10) NULL," +
                    "endingTime VARCHAR (10) NULL," +
                    "active INTEGER DEFAULT 0" +
                ");";
        }

        private static string Unlocks()
        {
            return "CREATE TABLE IF NOT EXISTS unlocks (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "email VARCHAR (100) NOT NULL," +
                    "emailpassword VARCHAR(140) NOT NULL," +
                    "password VARCHAR(140) NOT NULL," +
                    "newpassword VARCHAR(140) NOT NULL," +
                    "emailprovider VARCHAR(40) NOT NULL," +
                    "created DATETIME DEFAULT (DATETIME(CURRENT_TIMESTAMP))" +
                ");";
        }
    }
}
