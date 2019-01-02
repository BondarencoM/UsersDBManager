using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data.SqlClient;

namespace UDBM
{
    
    public partial class Central : Window
    {
        dynamic db;
        string dbVar, actualDatabase, actualTable;
        string[] highlightKeyWords;

        DataTable ManageDataSet;

        System.Windows.Forms.SaveFileDialog saveFileDialog1;
        System.Windows.Forms.Timer highlightTimer;
        System.Windows.Forms.OpenFileDialog openFileDialog1;

        public Central()
        {


            InitializeComponent();
            highlightKeyWords = ("select use insert create values table database column alter change drop and or abs sum avg * as null int char" +
                                  "varchar text blob decimal float where limit day month year desc delete trunc truncate transaction rollback" +
                                  "commit floor ceiling from if in isnull is like join schema with union all inner outter right left not").Split(' ');
        }

        public Central(string host, string user, string pass, string type)
        {
            Gestures.Load();

            InitializeComponent();

            highlightKeyWords = ("select use insert create values table database column alter change drop and or abs sum avg * as null int char" +
                                  "varchar text blob decimal float where limit day month year desc delete trunc truncate transaction rollback" +
                                  "commit floor ceiling from if in isnull is like join schema with union all inner outter right left not").Split(' ');
            saveFileDialog1 = new System.Windows.Forms.SaveFileDialog()
            { AddExtension=true, DefaultExt=".sql", Filter = "Sql file (*.sql)|*.sql|Text File (*.txt)|*.txt|All files (*.*)|*.*" };
           
            openFileDialog1 = new System.Windows.Forms.OpenFileDialog()
            { CheckFileExists=true, Filter = "Sql file (*.sql)|*.sql|Text File (*.txt)|*.txt|All files (*.*)|*.*" };

            highlightTimer = new System.Windows.Forms.Timer();
            highlightTimer.Tick += highlightTimer_Tick;      

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
                case "Microsoft SQL Server":
                    Console.WriteLine("MainForm() switch works at MS SQL Server");
                    db = new DBConnect<SqlConnection, SqlCommand, SqlDataReader>(host, user, pass, "sqlserver");
                    dbVar = "sqlserver";
                    break;
                default:
                    MessageBox.Show("Main.cs Constructor switch not implemented type = " + type);
                    break;
            };
           
        }

        #region General
        private void Main_Load(object sender, EventArgs e)
        {
            this.Owner.Hide();

            Console.WriteLine("Main_Load -> Start ListDatabases();");
            ListDatabases();
            Console.WriteLine("Main_Load -> Databases Listed");
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
                        break;
                    case "sqlserver":
                        listdb = db.Select("select name from sys.databases;");
                        Console.WriteLine("ListDatabses: Selected databases for MS SQL Server");
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
                        case "sqlserver":
                            db.usedb(dbname);
                            listtb = db.Select($"SELECT TABLE_NAME FROM {dbname}.INFORMATION_SCHEMA.TABLES WHERE  TABLE_SCHEMA = 'dbo' and TABLE_TYPE = 'BASE TABLE'");
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

        private void RefreshDatabasesTree()
        {
            TreeViewItem selectedNode = (TreeViewItem)treeViewDatabases.SelectedItem;
            treeViewDatabases.Items.Clear();
            this.ListDatabases();

            var tvi = treeViewDatabases.ItemContainerGenerator.ContainerFromItem(treeViewDatabases.Items[0]) as TreeViewItem;
            if (tvi != null) tvi.IsSelected = true;

            checkedListBox.Items.Clear();

            ManageDataGrid.DataContext = null;

            tPropName.Text = "";

            userLimit.Text = "250";
            userWhere.Text = "";

            dispDatabase.Text = "";
            dispTable.Text = "";

            // if (((TreeViewItem)treeViewDatabases.SelectedItem).Parent != treeViewDatabases)
            //     ((TreeViewItem)((TreeViewItem)treeViewDatabases.SelectedItem).Parent).IsExpanded = true;





        }

        public void SelectTable(object sender, bool doRefresh = false, string condition = "")
        {
            /*  if (treeViewDatabases.SelectedItem == null || 
                 (TreeViewItem)((TreeViewItem)treeViewDatabases.SelectedItem).Parent != selectedNode.Parent ||
                 treeViewDatabases.SelectedItem != selectedNode)
                 checkedListBox.Items.Clear(); */
            if (doRefresh) checkedListBox.Items.Clear();

            string dbname = actualDatabase;
            string tbname = actualTable;
            string sqlSelectCols = "";

            foreach (string col in checkedListBox.CheckedItems)
            {
                sqlSelectCols += col + ",";
            }
            if (!String.IsNullOrWhiteSpace(sqlSelectCols)) sqlSelectCols = sqlSelectCols.Remove(sqlSelectCols.Length - 1);
            else sqlSelectCols = "*";
            Console.WriteLine("ListDatabases -> Selection: " + sqlSelectCols);

            if (!String.IsNullOrWhiteSpace(condition)) condition = "Where " + condition + " ";

            string limit = "";
            if (!String.IsNullOrWhiteSpace(userLimit.Text)) limit = "LIMIT " + userLimit.Text;

            string query;

            switch (dbVar)
            {
                case "mysql":
                    query = $"Select {sqlSelectCols} from {dbname}.{tbname} {condition} {limit} ;";
                    break;
                case "postgres":
                    query = $"Select {sqlSelectCols} from {tbname} {condition} {limit} ;";
                    break;
                case "sqlserver":
                    db.usedb(dbname);
                    limit = "";
                    if (!String.IsNullOrWhiteSpace(userLimit.Text))
                        limit = $"TOP({userLimit.Text})";
                    query = $"Select {limit} {sqlSelectCols} from {tbname} {condition} ;"; 
                    break;
                default:
                    MessageBox.Show("ListDatabases -> Switch not implemented at SelectTable dbVar = " + dbVar);
                    throw new Exception("Switch not implemented at Main.cs SelectTable");
            }
            Console.WriteLine("ListDatabases -> SelectTable: exec querry: " + query);
            DataTable tableData = db.GetDataSet(query, tbname);
            if (tableData != null)
            {
                ManageDataGrid.DataContext = tableData;
                ManageDataSet = tableData;

                //tableData.
            }


            if (checkedListBox.Items.Count < ManageDataGrid.Columns.Count)
            {
                checkedListBox.Items.Clear();
                foreach (DataGridColumn column in ManageDataGrid.Columns)
                    checkedListBox.Items.Add((string)column.Header);

            }

            foreach (DataGridColumn column in ManageDataGrid.Columns)
            {
                column.Width = new DataGridLength(1.0, DataGridLengthUnitType.Auto);
            }


            /* if ( ManageDataGrid.Columns.Count!=0 && ((string)ManageDataGrid.Columns[0].Header).ToLower() == "id")
                 ManageDataGrid.Columns[0].Width = 50;*/
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Owner.Show();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // event ay be removed at GridSplitter_IsMouseCaptureWithinChanged()
            if (WindowState == WindowState.Maximized) 
                MainGrid.ColumnDefinitions[0].Width = new GridLength(2.0, GridUnitType.Star);
            else
                MainGrid.ColumnDefinitions[0].Width = new GridLength(4.0, GridUnitType.Star);
        }

        private void GridSplitter_IsMouseCaptureWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.StateChanged -= Window_StateChanged;
        }

        private void refreshDisplayDbTb()
        {
            dispDatabase.Text = actualDatabase;
            dispTable.Text = actualTable;
        }

        #endregion

        #region Manage Data
        private void RefreshReadData_Click(object sender, EventArgs e)
        {
            TreeViewItem sel = (TreeViewItem)treeViewDatabases.SelectedItem;

            if (sel.Parent != (DependencyObject)treeViewDatabases)
                this.SelectTable(sender, false, userWhere.Text);
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
                    case "sqlserver":
                        db.SqlDataGridAdapter.Update(data);
                        break;
                    default:
                        throw new Exception("Switch not implemented at void updateDbFromDataGrid(DataSet data) dbVar = " + dbVar);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "UDBM: Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSaveGridData_Click(object sender, EventArgs e)
        {
            updateDbFromDataGrid(ManageDataSet);
        }

        private void PropertiesManageData_Click(object sender, EventArgs e)
        {
            TreeViewItem selectedItem;
            TreeViewItem parentItem;
            try
            {
                selectedItem = (TreeViewItem)treeViewDatabases.SelectedItem;
                parentItem = (TreeViewItem)selectedItem.Parent;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Eror at Try Catch at PropertiesManageData_Click. Message: " + ex.Message);
                return;
            }
            if ((string)PropertiesManageData.Content == "Manage Data")
            {
                TreeViewItem seletedItem = (TreeViewItem)treeViewDatabases.SelectedItem;

                actualDatabase = (string)((TreeViewItem)seletedItem.Parent).Header;
                actualTable = (string)seletedItem.Header;
                checkedListBox.Items.Clear();

                this.SelectTable(sender, true);

                userWhere.Text = "";

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
                        userWhere.Text = $"table_name = '{(string)selectedItem.Header}' and table_schema='{(string)parentItem.Header}'";
                        toCheck = new int[] { 3, 5, 6, 8, 9, 16, 18 };
                        break;
                    case "postgres":
                        actualTable = "INFORMATION_SCHEMA.columns";
                        actualDatabase = (string)parentItem.Header;
                        userWhere.Text = $" table_name = '{(string)selectedItem.Header}';";
                        toCheck = new int[] { 3, 5, 6, 7, 10, 43 };
                        break;
                    case "sqlserver":
                        actualTable = "INFORMATION_SCHEMA.columns";
                        actualDatabase = (string)parentItem.Header;
                        userWhere.Text = $" table_catalog = '{(string)parentItem.Header}' and table_name = '{(string)selectedItem.Header}';";
                        toCheck = new int[] { 3, 5, 6, 7, 10 };
                        break;
                    default:
                        throw new Exception("Switch not implemented at PropertiesManageData_Click ");
                }

                refreshDisplayDbTb();
                /*TreeNode[] nodes = new TreeNode[1];
                nodes[0] = new TreeNode(actualTable);
                TreeNode parent = new TreeNode(actualDatabase, nodes); //setting the parent */
                userLimit.Text = "250";
                

                this.SelectTable(sender, true, userWhere.Text);

                foreach (int i in toCheck)
                    checkedListBox.SetItemChecked(i, true);
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
            catch (Exception e)
            {

                Console.WriteLine($"Central -> treeViewDatabases_BeforeSelect -> Error at try, Message: {e.Message}");
                return;
            }


            string header = (string)newNode.Header;
            Console.WriteLine($"Select node " + header);

            if (newNode.Parent == treeViewDatabases) actualDatabase = header;
            else
            {
                actualDatabase = (string)newParent.Header;
                actualTable = header;

                userLimit.Text = "250";
                userWhere.Text = "";

                //this.RenderTableProperties(sender, e.Node);

                PropertiesManageData.Content = "Tab. Properties";
                this.SelectTable(sender, true, userWhere.Text);
            }
            refreshDisplayDbTb();
        }

        private void DiscardChangesReadData_Click(object sender, EventArgs e)
        {
            SelectTable(sender, true);
        }

        private void PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }
        
        private void checkBox1_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (DiscardChangesManageData == null) return;
            if (((CheckBox)sender).IsChecked == true)
            {
                DiscardChangesManageData.IsEnabled = false;
                ApplyChangesManageData.IsEnabled = false;
                ManageDataGrid.RowEditEnding += btnSaveGridData_Click;
            }
            else
            {
                DiscardChangesManageData.IsEnabled = true;
                ApplyChangesManageData.IsEnabled = true;
                ManageDataGrid.RowEditEnding -= btnSaveGridData_Click;
            }
        }

        private void btnSaveGridData_Click(object sender, RoutedEventArgs e)
        {
            updateDbFromDataGrid(ManageDataSet);
        }

        #endregion

        #region Execute Querry

        private void ExecuteQuery(object sender, RoutedEventArgs e)
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

            if (checkAutoselect.IsChecked == true)
                this.RefreshDatabasesTree();

        }

        private void buttonNewQuery(object sender, RoutedEventArgs e)
        {
            if ((string)toolStripStatusLabel1.Content == "Unsaved")
                if (MessageBox.Show("Do you want to save the current querry? ", "UDBM: Mysql", MessageBoxButton.YesNo) != MessageBoxResult.No)
                    this.button1_Click_1(sender, e);
            qRez.Text = "";
            qInp.Text = "";
            queryName.Text = "";
            toolStripStatusLabel1.Content = "";
            toolStripStatusLabel2.Content = "";
        }

        private void EveClearOutput(object sender, RoutedEventArgs e)
        {
            qRez.Text = "";
        }

        private void bSaveQuery_Click(object sender, RoutedEventArgs e)
        {
            saveFileDialog1.FileName = queryName.Text;
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                toolStripStatusLabel2.Content = saveFileDialog1.FileName;
                queryName.Text = saveFileDialog1.FileName.Split('\\').Last();
                try
                {
                    qInp.SaveFile((string)toolStripStatusLabel2.Content, System.Windows.Forms.RichTextBoxStreamType.PlainText);
                    toolStripStatusLabel1.Content = "Saved";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    toolStripStatusLabel1.Content = "Unsaved";
                }
            }
        }

        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace((string)toolStripStatusLabel2.Content))
            {
                qInp.SaveFile((string)toolStripStatusLabel2.Content, System.Windows.Forms.RichTextBoxStreamType.PlainText);
                toolStripStatusLabel1.Content = "Saved";
            }
            else
                this.bSaveQuery_Click(sender, e);
        }

        private void bOpenQuery_Click(object sender, RoutedEventArgs e)
        {
            if ((string)toolStripStatusLabel1.Content == "Unsaved")
                if (MessageBox.Show("Do you want to save the current querry? ", "UDBM: Mysql", MessageBoxButton.YesNo) != MessageBoxResult.No)
                    this.button1_Click_1(sender, e);
            qRez.Text = "";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                qInp.LoadFile(openFileDialog1.FileName, System.Windows.Forms.RichTextBoxStreamType.PlainText);
                queryName.Text = openFileDialog1.FileName.Split('\\').Last();
                toolStripStatusLabel2.Content = openFileDialog1.FileName;
            }
        }


        private void qInp_TextChanged(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Content = "Unsaved";

            if (highlightTimer.Enabled == false)
                highlightTimer.Enabled = true;
        }

        private void highlightTimer_Tick(object sender, EventArgs e)
        {

            int selStart = qInp.SelectionStart;
            qInp.Enabled = false;
            qInp.SelectAll();
            qInp.SelectionColor = System.Drawing.Color.Black;
            qInp.SelectionFont = new System.Drawing.Font(qInp.Font, System.Drawing.FontStyle.Regular);
            qInp.Select(qInp.Text.Length, 0);

            foreach (string s in highlightKeyWords)
                CheckKeyword(s, System.Drawing.Color.Purple, 0);
            highlightTimer.Enabled = false;

            qInp.Enabled = true;
            qInp.Focus();
            qInp.SelectionStart = selStart;
        }

        private void CheckKeyword(string word, System.Drawing.Color color, int startIndex)
        {
            if (qInp.Text.ToLower().Contains(word))
            {
                int index = -1;
                int selectStart = qInp.SelectionStart;

                while ((index = qInp.Text.ToLower().IndexOf(word, (index + 1))) != -1)
                {

                    int endSel = index + word.Length;
                    bool isKeyWord = true;

                    if (endSel < qInp.Text.Length && !Char.IsWhiteSpace(qInp.Text[endSel])) isKeyWord = false;
                    if (index - 1 > 0 && !Char.IsWhiteSpace(qInp.Text[index - 1])) isKeyWord = false;

                    if (isKeyWord)
                    {
                        qInp.Select((index), word.Length);
                        qInp.SelectionColor = color;
                        qInp.SelectionFont = new System.Drawing.Font(qInp.Font, System.Drawing.FontStyle.Bold);

                        qInp.Select(selectStart, 0);
                        qInp.SelectionColor = System.Drawing.Color.Black;
                        qInp.SelectionFont = new System.Drawing.Font(qInp.Font, System.Drawing.FontStyle.Regular);
                    }
                }
            }
        }
        #endregion

        #region Properties
        private void tPropSave_Click(object sender, RoutedEventArgs e)
        {
            string selecteTable = (string)((TreeViewItem)treeViewDatabases.SelectedItem).Header;
            if ((string)tPropName.Text == selecteTable) return;
            if (String.IsNullOrWhiteSpace(tPropName.Text)) return;

            try
            {
                List<List<string>> rez = db.Select("ALTER TABLE " + selecteTable + " RENAME TO " + tPropName.Text);
                this.RefreshDatabasesTree();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnRenameCancel_Click(object sender, RoutedEventArgs e)
        {
            tPropName.Text = "";
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            string selecteTable = (string)((TreeViewItem)treeViewDatabases.SelectedItem).Header;
            try
            {
                string querry = $"DROP TABLE  {selecteTable} ;";
                if (MessageBox.Show("Are you sure you want to execute ? \n " + querry, "UDBM Error", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    List<List<string>> rez = db.Select(querry);
                    this.RefreshDatabasesTree();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void tPropDelAll_Click(object sender, RoutedEventArgs e)
        {
            string selecteTable = (string)((TreeViewItem)treeViewDatabases.SelectedItem).Header;
            try
            {
                string querry = $"delete from {selecteTable} ;";
                if (MessageBox.Show("Are you sure you want to execute ? \n " + querry, "UDBM:MySQL", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    List<List<string>> rez = db.Select(querry);
                    this.RefreshDatabasesTree();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void tPropTrunc_Click(object sender, RoutedEventArgs e)
        {
            string selecteTable = (string)((TreeViewItem)treeViewDatabases.SelectedItem).Header;
            try
            {
                string querry = $"Truncate TABLE {selecteTable} ;";
                if (MessageBox.Show("Are you sure you want to execute ? \n " + querry, "UDBM:MySQL", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    List<List<string>> rez = db.Select(querry);
                    this.RefreshDatabasesTree();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Menu strip
        private void logOutToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void createotherOneToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            
            this.Owner.Show();
            this.WindowState = WindowState.Minimized;
        }

        private void SelectDatabaseForm_FormClosed(object sender, RoutedEventArgs e)
        {
            //this.Owner.Show();
            System.Windows.Application.Current.Shutdown();
        }

        private void refreshToolStripMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.RefreshDatabasesTree();
        }

        private void ManageDataGrid_Error(object sender, ValidationErrorEventArgs e)
        {
            MessageBox.Show(e.Error.Exception.Message, "UDBM:Data grid error ", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Hotkeys
        public void eveGoToTab (int tab)
        {
            Console.WriteLine($"Moving to tab nr {tab}");
           // WorkingArea.SelectedIndex = tab;
            WorkingArea.SelectedItem = WorkingArea.Items[tab];
        }

        public void eveGoToTab1 (object sender, RoutedEventArgs e)
        {
            eveGoToTab(0);
        }

        private void CommandBinding_CanExecute_True(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExecuteQuery_CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            if (WorkingArea.SelectedIndex == 1) e.CanExecute = true;
            else e.CanExecute = false;
        }

        private void qInp_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.F5)
                ExecuteQuery(sender, null);
            else if (e.KeyCode == System.Windows.Forms.Keys.N && e.Control == true)
                buttonNewQuery(sender, null);
            else if (e.KeyCode == System.Windows.Forms.Keys.S && e.Control == true)
                button1_Click_1(sender, null);
            else if (e.KeyCode == System.Windows.Forms.Keys.S && e.Control == true && e.Shift == true)
                bSaveQuery_Click(sender, null);
            else if (e.KeyCode == System.Windows.Forms.Keys.O && e.Control == true)
                bOpenQuery_Click(sender, null);

        }

        public void eveGoToTab2(object sender, RoutedEventArgs e)
        {
            eveGoToTab(1);
        }

        public void eveGoToTab3(object sender, RoutedEventArgs e)
        {
            eveGoToTab(2);
        }



        #endregion

    }
    public static class Gestures
    {
        //tabs
        public static RoutedCommand gestGoToTab1 = new RoutedCommand();
        public static RoutedCommand gestGoToTab2 = new RoutedCommand();
        public static RoutedCommand gestGoToTab3 = new RoutedCommand();
        //Manage Data
        public static RoutedCommand gestRefresh = new RoutedCommand();
        public static RoutedCommand gestProperties = new RoutedCommand();
        public static RoutedCommand gestApplyChanges = new RoutedCommand();
        //Execute querry
        public static RoutedCommand gestExecuteQuery = new RoutedCommand();
        public static RoutedCommand gestNewQuery = new RoutedCommand();
        public static RoutedCommand gestSaveQuery = new RoutedCommand();
        public static RoutedCommand gestSaveAsQuery = new RoutedCommand();
        public static RoutedCommand gestOpeneQuery = new RoutedCommand();
        //other
        //public static RoutedCommand gestPrefs = new RoutedCommand();
        public static void Load()
        {
            //tabs
            gestGoToTab1.InputGestures.Add(new KeyGesture(Key.D1, ModifierKeys.Control));
            gestGoToTab1.InputGestures.Add(new KeyGesture(Key.M, ModifierKeys.Control));
            gestGoToTab2.InputGestures.Add(new KeyGesture(Key.D2, ModifierKeys.Control));
            gestGoToTab2.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control));
            gestGoToTab3.InputGestures.Add(new KeyGesture(Key.D3, ModifierKeys.Control));
            gestGoToTab3.InputGestures.Add(new KeyGesture(Key.T, ModifierKeys.Control));
            //Manage Data
            gestRefresh.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));
            gestProperties.InputGestures.Add(new KeyGesture(Key.P, ModifierKeys.Control));
            gestApplyChanges.InputGestures.Add(new KeyGesture(Key.Enter, ModifierKeys.Control));
            //Execute Query
            gestExecuteQuery.InputGestures.Add(new KeyGesture(Key.F5));
            gestNewQuery.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
            gestSaveQuery.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            gestSaveAsQuery.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));
            gestOpeneQuery.InputGestures.Add(new KeyGesture(Key.O, ModifierKeys.Control));
            //other
            
                }

    }
}

/*
To do:
 * Rename only working for the last db
 * sortarea
 * Default limit
 * After tabel properties "where" field doesn't clear
 * Mai multe baze de date
 *  Oracle
 *  MS Server
 * Lucru cu scheme
 * De permis de introdus orice querry in Manage data
 * Careva probleme cu auto apply
 * rename pentru Postgre
 * Create another one z-index la fora de logare
 * refactoring la denmiri
 * Preferences
        modul de fit la datagrid
        default limit
 * DB-s expand all
 *
 * 
*/
