using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using System.Data.Common;

namespace UDBM { 
  

    class DBConnect<dbCon,dbCmd,dbReader,dbAdapter,dbCmdBld>
        where dbCon : DbConnection, new()
        where dbCmd : DbCommand,new()
        where dbReader : DbDataReader
        where dbAdapter : DbDataAdapter,new()
        where dbCmdBld : DbCommandBuilder, new()
    {
        public dbCon connection;
        public string server;
        public string database;
        public string uid;
        public string password;
        public string dbVar;

        public DataTable dataGridSet;
        public dbAdapter DataGridAdapter;
        public dbCmdBld CommandBuilder;

        public DBConnect()
        {
            dbVar = "mysql";
            server = "localhost";
            uid = "root";
            password = "";
            string connectionString;
            connectionString = "Server=" + server + ";" + "User Id="
            + uid + ";" + "Password=" + password + ";";
            if (dbVar == "postgres") connectionString += "Database = postgres;";
            connection = new dbCon();
            connection.ConnectionString = connectionString;
        }

       /* public DBConnect(string db)
        {
            dbVar = db;
            server = "localhost";
            database = "testdb";
            uid = "root";
            password = "";
            string connectionString;
            connectionString = "Server=" + server + ";" 
                + "User Id=" + uid + ";" + "Password=" + password + ";";
            connection = new dbCon();
            connection.ConnectionString = connectionString;
        }*/

        public DBConnect(string pserver, string puser, string ppass, string db)
        {
            dbVar = db;
            server = pserver;
            uid = puser;
            password = ppass;
            string connectionString="";

            if (dbVar == "oracle")
            {
                connectionString += $"Data Source=(DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = {server})(PORT = 1521))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = ORCL))); ";
            }
            else
                connectionString += "Server=" + server + ";";
            connectionString += "User Id=" + uid + ";";
            connectionString += "Password=" + password + ";";
            if (dbVar == "postgres")
                connectionString += "Database = postgres;";

            connection = new dbCon();
            connection.ConnectionString = connectionString;
            
            
        }

        private bool OpenConnection()
        {
            try
            {
                connection.Open();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n Error code", "UDBM: Error");
                return false;
            }
        }

        private bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "UDBM: Error");
                return false;
            }
        }

        public List<List< string >> Select(string query)
        {

            List<List<string>> list = new List<List<string>>();

            if (this.OpenConnection() == true)
            {
                dbCmd cmd = new dbCmd();
                cmd.Connection = connection;
                cmd.CommandText = query;
                
                try
                {
                    dbReader dataReader = (dbReader)cmd.ExecuteReader();   
                while (dataReader.Read())
                {
                    list.Add( new List<string>());
                    for (int i = 0; i < dataReader.FieldCount;i++ )
                        list.Last().Add(dataReader[i] + "");
                }
                dataReader.Close();
                this.CloseConnection();
                return list;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "UDBM: Error");
                    this.CloseConnection();
                    list.Add(new List<string>());
                    list[0].Add("Error");
                    return list;
                }

            }
            else
                return list;
        }

        public void Show(List<string>[] toshow)
        {
            foreach (List<string> list in toshow)
            {
                if (list == null) continue;
                foreach (string s in list)
                    Console.WriteLine(s);
            }
        }

        public void usedb(string dbname)
        {
            Console.WriteLine("DBC: Usedb: changing to database "+dbname); 
            database = dbname;
            string connectionString;
            connectionString = "SERVER=" + server + ";"
                 + "UID=" + uid + ";" + "PASSWORD=" + password + "; Database = " + database + ";";

            connection = new dbCon();
            connection.ConnectionString = connectionString;
        }

        public DataTable GetDataSet(string sqlCommand, string tbname="")
        {
            Console.WriteLine("Executing: " + sqlCommand);
            dataGridSet = new DataTable();
            try
            {
                Console.WriteLine("DBC: GetDataSet: Executing: " + sqlCommand);

                DataGridAdapter = new dbAdapter();
                dbCmd cmdToExec = new dbCmd()
                {
                    Connection = connection,
                    CommandText = sqlCommand
                };
                DataGridAdapter.SelectCommand = cmdToExec;
                DataGridAdapter.Fill(dataGridSet);
                CommandBuilder = new dbCmdBld();
                CommandBuilder.DataAdapter = DataGridAdapter;

            }
           catch(Exception e){
               MessageBox.Show(e.Message, "UDBM: Error");
               return null;
           }
                Console.WriteLine("DBConnect -> GetDataSet -> Executed successfullt");
            return dataGridSet;

        }          

    }

}
