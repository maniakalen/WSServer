using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;

namespace WatsonWebsocketServer
{
    

    class DbConnector
    {
        private MySqlConnection connection;
        private string server;
        private string database;
        private string uid;
        private string password;
        private int port;
        private string authTable;

        //Constructor
        public DbConnector(string server, int port, string database, string uid, string pwd, string aTable)
        {
            this.server = server;
            this.database = database;
            this.uid = uid;
            this.password = pwd;
            this.port = port;
            this.authTable = aTable;
            Initialize();
        }

        //Initialize values
        private void Initialize()
        {

            string connectionString;
            connectionString = "SERVER=" + server + ";PORT=" + port + ";DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            Console.WriteLine("Connection string: {0}", connectionString);
            connection = new MySqlConnection(connectionString);
        }

        //open connection to database
        private bool OpenConnection()
        {
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        //Close connection
        private bool CloseConnection()
        {
            try
            {
                Console.WriteLine("Closing connection to MySQL...");
                if (connection.State != ConnectionState.Closed && connection.State != ConnectionState.Broken)
                {
                    connection.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }

        //Insert statement
        public void Insert()
        {
        }

        //Update statement
        public void Update()
        {
        }

        //Delete statement
        public void Delete()
        {
        }

        //Select statement
        public void Select()
        {
        }

        //Count statement
        public int AuthUser(int uid, string hash)
        {
            int to = hash.Length;
            int pos;
            if ((pos = hash.IndexOf(" ")) > 0)
            {
                to = pos;
            }
            try
            {
                this.OpenConnection();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM " + this.authTable + " WHERE user_id = " + uid + " AND hash = '" + hash.Substring(0, to) + "'", this.connection);
                object result = cmd.ExecuteScalar();
                this.CloseConnection();
                if (result != null)
                {
                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return 0;
        }

        //Backup
        public void Backup()
        {
        }

        //Restore
        public void Restore()
        {
        }
    }
}

