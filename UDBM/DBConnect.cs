using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using System.Data;
using System.Data.Common;
using Npgsql;

namespace UDBM { 
  

    class DBConnect<dbCon,dbCmd,dbReader>
        where dbCon : DbConnection, new()
        where dbCmd : DbCommand,new()
        where dbReader : DbDataReader
    {
        public dbCon connection;
        public string server;
        public string database;
        public string uid;
        public string password;
        public string dbVar;

        public DataTable dataGridSet;
        public MySqlDataAdapter MySqldataGridAdaptaer;
        public MySqlCommandBuilder MySqlcmdBldr;
        public NpgsqlDataAdapter PostGresdataGridAdapterl;
        public NpgsqlCommandBuilder PostGrescmdBldr;
        public SqlDataAdapter SqlDataGridAdapter;
        public SqlCommandBuilder SqlCmdBldr;

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
            string connectionString;
            connectionString = "Server=" + server + ";" + "User Id="
            + uid + ";" + "Password=" + password + ";";

            if (dbVar == "postgres") connectionString += "Database = postgres;";

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
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:

                        MessageBox.Show("Cannot connect to server.  Contact administrator \n Error code 0", "UDBM: Error");
                        break;
                    case 1045:
                        MessageBox.Show("Invalid username/password, please try again \n Error code 1045", "UDBM: Error");
                        break;
                    case 1042:
                        MessageBox.Show("Unable to connect to the indicated host \n Error code 1042", "UDBM: Error");
                        break;
                    default:
                        MessageBox.Show(ex.Message + "\n Error code", "UDBM: Error");
                        break;

                }
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
                switch (dbVar)
                {
                    case "mysql":
                        Console.WriteLine("DBC: GetDataSet: Executing: " + sqlCommand);
                        MySqldataGridAdaptaer = new MySqlDataAdapter(sqlCommand, (MySqlConnection)((DbConnection)connection));
                        MySqlcmdBldr = new MySqlCommandBuilder(MySqldataGridAdaptaer);
                        MySqldataGridAdaptaer.Fill(dataGridSet);
                        break;
                    case "postgres":
                        Console.WriteLine("DBC: GetDataSet: Executing: " + sqlCommand);
                        PostGresdataGridAdapterl = new  NpgsqlDataAdapter (sqlCommand, (NpgsqlConnection)((DbConnection)connection));
                        PostGrescmdBldr = new NpgsqlCommandBuilder(PostGresdataGridAdapterl);
                        PostGresdataGridAdapterl.Fill(dataGridSet);
                        break;
                    case "sqlserver":
                        Console.WriteLine("DBC: GetDataSet: Executing: " + sqlCommand);
                        SqlDataGridAdapter = new SqlDataAdapter(sqlCommand, (SqlConnection)((DbConnection)connection));
                        SqlCmdBldr = new SqlCommandBuilder(SqlDataGridAdapter);
                        SqlDataGridAdapter.Fill(dataGridSet);
                        break;
                    default: throw new Exception("DBConnect -> GetDataSet -> Switch not implemented dbvar = " + dbVar);
                } 
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
