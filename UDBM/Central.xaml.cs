using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UDBM
{
    /// <summary>
    /// Interaction logic for Central.xaml
    /// </summary>
    public partial class Central : Window
    {
        dynamic db;
        string dbVar, actualDatabase, actualTable;
        string[] highlightKeyWords;

        DataTable ManageDataSet;

        public Central()
        {
            InitializeComponent();
            highlightKeyWords = ("select use insert create values table database column alter change drop and or abs sum avg * as null int char" +
                                  "varchar text blob decimal float where limit day month year desc delete trunc truncate transaction rollback" +
                                  "commit floor ceiling from if in isnull is like join schema with union all inner outter right left not").Split(' ');
        }

        public Central(string host, string user, string pass, string type)
        {
            InitializeComponent();
            highlightKeyWords = ("select use insert create values table database column alter change drop and or abs sum avg * as null int char" +
                                  "varchar text blob decimal float where limit day month year desc delete trunc truncate transaction rollback" +
                                  "commit floor ceiling from if in isnull is like join schema with union all inner outter right left not").Split(' ');

            switch (type)
            {
                case "MySQL":
                    Console.WriteLine("MainForm() switch works at MySQL");
                    db = new DBConnect<MySqlConnection, MySqlCommand, MySqlDataReader>(host, user, pass, "mysql");
                    dbVar = "mysql";
                    break;
                case "PostgreSQL":
                    Console.WriteLine("MainForm() switch works at PostgeSQL");
                    db = new DBConnect<NpgsqlConnection, NpgsqlCommand, NpgsqlDataReader>(host, user, pass, "postgres");
                    dbVar = "postgres";
                    break;
                default:
                    MessageBox.Show("Main.cs Constructor switch not implemented type = "+type);
                    break;
            }
        }

        private void ListDatabases()
        {
            try
            {
                List<List<string>> listdb = null;

                switch (dbVar)
                {
                    case "mysql":
                        listdb = db.Select("Show databases;");
                        Console.WriteLine("ListDatabses: Selected databases for Mysql");
                        break;
                    case "postgres":
                        listdb = db.Select("SELECT datname FROM pg_database WHERE datistemplate = false;");
                        Console.WriteLine("ListDatabses: Selected databases for Postgres");
                        db.Show(listdb.ToArray());
                        break;
                    default:
                        listdb = null;
                        MessageBox.Show(db.dbVar + " database switch not implemented at private void ListDatabases()");
                        break;
                }


                foreach (List<string> dbnames in listdb)
                {

                    if (dbnames == null) continue;
                    string dbname = dbnames[0];
                    List<List<string>> listtb = new List<List<string>>();

                    switch (dbVar)
                    {
                        case "mysql":
                            listtb = db.Select("use " + dbname + "; show tables;");
                            break;
                        case "postgres":
                            db.usedb(dbname);
                            listtb = db.Select("SELECT tablename FROM pg_catalog.pg_tables where schemaname='public'");
                            Console.WriteLine(db.connection.Database);
                            break;
                        default:
                            MessageBox.Show(db.dbVar + " database switch not implemented at  private void ListDatabases(DBConnect db)");
                            break;

                    }

                    Console.WriteLine("ListDatabases -> Process database " + dbname);

                    var dbNode = new TreeViewItem() { Header = dbname };
                    foreach (List<string> tbnames in listtb)
                    {
                        if (tbnames == null) continue;
                        Console.WriteLine("ListDatabases -> Set tabele: " + tbnames[0]);
                        dbNode.Items.Add(new TreeViewItem() { Header = tbnames[0] });
                    }
  
                    treeViewDatabases.Items.Add(dbNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void SelectTable(object sender, bool doRefresh = false, string condition = "true")
        {
           /*  if (treeViewDatabases.SelectedItem == null || 
                (TreeViewItem)((TreeViewItem)treeViewDatabases.SelectedItem).Parent != selectedNode.Parent ||
                treeViewDatabases.SelectedItem != selectedNode)
                checkedListBox.Items.Clear(); */
            if(doRefresh) checkedListBox.Items.Clear();

            string dbname = actualDatabase;
            string tbname = actualTable;
            string sqlSelectCols = "";

            foreach (dbField field in checkedListBox.Items)
            {
                sqlSelectCols += field.name + ",";
            }
            if (!String.IsNullOrWhiteSpace(sqlSelectCols)) sqlSelectCols = sqlSelectCols.Remove(sqlSelectCols.Length - 1);
            else sqlSelectCols = "*";
            Console.WriteLine("ListDatabases -> Selection: " + sqlSelectCols);

            if (String.IsNullOrWhiteSpace(condition)) condition = "true";

            string limit = "";
            if (!String.IsNullOrWhiteSpace(userLimit.Text)) limit = " LIMIT " + userLimit.Text;

            string query;

            switch (dbVar)
            {
                case "mysql":
                    query = "Select " + sqlSelectCols + " from " + dbname + "." + tbname + " where " + condition + limit + " ;";
                    break;
                case "postgres":
                    query = "Select " + sqlSelectCols + " from " + tbname + " where " + condition + limit + " ;";
                    break;
                default:
                    MessageBox.Show("ListDatabases -> Switch not implemented at SelectTable dbVar = " + dbVar);
                    throw new Exception("Switch not implemented at Main.cs SelectTable");
            }
            Console.WriteLine("ListDatabases -> SelectTable: exec querry: " + query);
            DataTable tableData = db.GetDataSet(query, tbname);
            if (tableData != null)
                ManageDataGrid.DataContext = tableData;


            if (checkedListBox.Items.Count < ManageDataGrid.Columns.Count)
            {
                checkedListBox.Items.Clear();
                foreach (DataGridColumn column in ManageDataGrid.Columns)
                    checkedListBox.Items.Add(new dbField((string)column.Header,false));
            }

            if (((string)ManageDataGrid.Columns[0].Header).ToLower() == "id")
                ManageDataGrid.Columns[0].Width = 50;
        }

        private void Main_Load(object sender, EventArgs e)
        {
            this.Owner.Hide();

            Console.WriteLine("Main_Load -> Start ListDatabases();");
            ListDatabases();
            Console.WriteLine("Main_Load -> Databases Listed");
        }

        private void RefreshDatabasesTree()
        {
            TreeViewItem selectedNode = (TreeViewItem)treeViewDatabases.SelectedItem;
            treeViewDatabases.Items.Clear();
            this.ListDatabases();

            var tvi = treeViewDatabases.ItemContainerGenerator.ContainerFromItem(treeViewDatabases.Items[0]) as TreeViewItem;
            if (tvi != null) tvi.IsSelected = true;

            checkedListBox.Items.Clear();
            ManageDataGrid.ItemsSource = null;

            tPropName.Text = "";

            userLimit.Text = "";
            userWhere.Text = "";

            dispDatabase.Text = "";
            dispTable.Text = "";

           // if (((TreeViewItem)treeViewDatabases.SelectedItem).Parent != treeViewDatabases)
                ((TreeViewItem)((TreeViewItem)treeViewDatabases.SelectedItem).Parent).IsExpanded = true;

            



        }

        private void RefreshReadData_Click(object sender, EventArgs e)
        {
            TreeViewItem sel = (TreeViewItem)treeViewDatabases.SelectedItem;

            if (sel.Parent != (DependencyObject)treeViewDatabases)
                this.SelectTable(sender, false , userWhere.Text);
            else Console.WriteLine("Central -> RefreshReadData_Click -> Database node Selected");
        }

        public void updateDbFromDataGrid(DataTable data)
        {
            try
            {
                switch (dbVar)
                {
                    case "mysql":
                        db.MySqldataGridAdaptaer.Update(data);
                        break;
                    case "postgres":
                        db.PostGresdataGridAdapterl.Update(data);
                        break;
                    default:
                        throw new Exception("Switch not implemented at void updateDbFromDataGrid(DataSet data) dbVar = "+dbVar);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "UDBM: Error",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        private void btnSaveGridData_Click(object sender, EventArgs e)
        {
            updateDbFromDataGrid(ManageDataSet);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Owner.Show();
        }

        private void PropertiesManageData_Click(object sender, EventArgs e)
        {
            TreeViewItem selectedItem = (TreeViewItem)treeViewDatabases.SelectedItem;
            TreeViewItem parentItem = (TreeViewItem)selectedItem.Parent;
            if ((string)PropertiesManageData.Content == "Manage Data")
            {
                TreeViewItem seletedItem = (TreeViewItem)treeViewDatabases.SelectedItem;

                actualDatabase = (string)((TreeViewItem)seletedItem.Parent).Header;
                actualTable = (string)seletedItem.Header;
                checkedListBox.Items.Clear();

                this.SelectTable(sender, true);
                refreshDisplayDbTb();
                PropertiesManageData.Content = "Tab. Properties";
            }
            else if (true/*treeViewDatabases.SelectedNode.Level != 0*/)
            {
                int[] toCheck;
                switch (dbVar)
                {
                    case "mysql":
                        actualTable = "columns";
                        actualDatabase = "INFORMATION_SCHEMA";
                        userWhere.Text = $"table_name = '{(string)selectedItem.Header}' and table_schema='{(string)parentItem.Header}';";
                        toCheck = new int[] { 3, 5, 6, 8, 9, 16, 18 };
                        break;
                    case "postgres":
                        actualTable = "INFORMATION_SCHEMA.columns";
                        actualDatabase = (string)parentItem.Header;
                        userWhere.Text = $"table_name = '{(string)selectedItem.Header}';";
                        toCheck = new int[] { 3, 5, 6, 7, 10, 43 };
                        break;
                    default:
                        throw new Exception("Switch not implemented at PropertiesManageData_Click ");
                }

                refreshDisplayDbTb();
                /*TreeNode[] nodes = new TreeNode[1];
                nodes[0] = new TreeNode(actualTable);
                TreeNode parent = new TreeNode(actualDatabase, nodes); //setting the parent */
                userLimit.Text = "";

                this.SelectTable(sender, true, userWhere.Text);

                /*foreach (int i in toCheck)
                    checkedListBox.SetItemChecked(i, true); */
                RefreshReadData_Click(sender, e);

                PropertiesManageData.Content = "Manage Data";

                //NpgsqlDataAdapter da = db.PostGresdataGridAdapterl;
            }

        }

        private void treeViewDatabases_BeforeSelect(object sender, RoutedPropertyChangedEventArgs<object> oe)
        {
            TreeViewItem newNode;
            TreeViewItem newParent;
            TreeViewItem oldNode;
            try
            {
                newNode = (TreeViewItem)oe.NewValue;
                 newParent = (TreeViewItem)newNode.Parent;
                 oldNode = (TreeViewItem)oe.OldValue;
            }
            catch(Exception e)
            {

                Console.WriteLine($"Central -> treeViewDatabases_BeforeSelect -> Error at try {e.Message}");
                return;
            }


            string header = (string)newNode.Header;
            Console.WriteLine($"Select node " + header);

            if (newNode.Parent == treeViewDatabases) actualDatabase = header;
            else
            {
                actualDatabase = (string)newParent.Header;
                actualTable = header;

                userLimit.Text = "";
                userWhere.Text = "";

                //this.RenderTableProperties(sender, e.Node);

                PropertiesManageData.Content = "Tab. Properties";
                this.SelectTable(sender, true, userWhere.Text);
            }
            refreshDisplayDbTb();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExecuteQuery(object sender, MouseEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(qInp.Text)) return;

            string result = "";
            List<List<string>> output = db.Select(qInp.Text);
            foreach (List<string> list in output)
            {
                foreach (string s in list)
                    result += s + " ";
                result += Environment.NewLine;
            }

            if (result == "") result = "Success !";
            qRez.Text = result;

            if (checkAutoselect.Checked)
                this.RefreshDatabasesTree();

        }


        private void refreshDisplayDbTb()
        {
            dispDatabase.Text = actualDatabase ;
            dispTable.Text = actualTable;
        }
    }


    public class dbField
    {
        public string name { get; set; }
        public bool isChecked { get; set; }

        public dbField(string name, bool isChecked)
        {
            this.name = name;
            this.isChecked = isChecked;

        }

    }
}
