using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using Npgsql;
using System.Data.Common;

namespace UDBM__Useless_DataBase_Manager_
{
    public partial class MainForm : Form
    {
        dynamic db;
        string dbVar, actualDatabase, actualTable;
        string[] highlightKeyWords;

        DataSet ManageDataSet;


        // Type conn, comm, dread;
        // DBConnect<conn. , comm, dread> db;

        public MainForm()
        {
            InitializeComponent();
            string host = LogInForm.ehost.Text;
            string user = LogInForm.euser.Text;
            string pass = LogInForm.epass.Text;
            switch (LogInForm.edbvar.SelectedItem.ToString())
            {
                case "MySQL":
                    Console.WriteLine("MainForm() switch works at MySQL");
                    db = new DBConnect<MySqlConnection, MySqlCommand, MySqlDataReader>(host, user, pass, "mysql");
                    dbVar = "mysql";
                    break;
                case "PostgeSQL":
                    Console.WriteLine("MainForm() switch works at PostgeSQL");
                    db = new DBConnect<NpgsqlConnection, NpgsqlCommand, NpgsqlDataReader>(host, user, pass, "postgres");
                    dbVar = "postgres";
                    break;
                default:
                    MessageBox.Show("Main.cs Constructor switch not implemented");
                    break;
            }


            highlightKeyWords = ("select use insert create values table database column alter change drop and or abs sum avg * as null int char" +
                                  "varchar text blob decimal float where limit day month year desc delete trunc truncate transaction rollback" +
                                  "commit floor ceiling from if in isnull is like join schema with union all inner outter right left not").Split(' ');

            Console.Write("Construct() construction finished");
        }
        private void ListDatabases()
        {
            try
            {
                List<List<string>> listdb = null;

                switch (dbVar)
                {
                    case "mysql":
                        Console.WriteLine("ListDatabses: Select databases for Mysql");
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
                        MessageBox.Show(db.dbVar + " database switch not implemented at private void ListDatabases");
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
                    List<TreeNode> tableNodes = new List<TreeNode>();
                    Console.WriteLine("Process database " + dbname);

                    foreach (List<string> tbnames in listtb)
                    {
                        if (tbnames == null) continue;
                        string tbname = tbnames[0];
                        Console.WriteLine("Set tabele: " + tbname);
                        tableNodes.Add(new TreeNode(tbname));
                    }
                    TreeNode dbNode;
                    dbNode = new TreeNode(dbname, tableNodes.ToArray());
                    treeView1.Nodes.Add(dbNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSaveGridData_Click(object sender, EventArgs e)
        {
            updateDbFromDataGrid(ManageDataSet);
        }

        /* private void TabPropApplyChanges_Click(object sender, EventArgs e)
         {
             updateDbFromDataGrid(PropsDataSet);
         }*/

        public void updateDbFromDataGrid(DataSet data)
        {
            try
            {
                switch (dbVar)
                {
                    case "mysql":
                        db.MySqldataGridAdaptaer.Update(data, data.Tables[0].TableName);
                        break;
                    case "postgres":
                        db.PostGresdataGridAdapterl.Update(data, data.Tables[0].TableName);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "UDBM:MySQL Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        public void SelectTable(object sender, TreeNode selectedNode, string condition = "true")
        {
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.Parent != selectedNode.Parent || treeView1.SelectedNode != selectedNode)
                checkedListBox1.Items.Clear();
            string dbname = actualDatabase;
            string tbname = actualTable;
            string sqlSelectCols = "";

            foreach (string cheSel in checkedListBox1.CheckedItems)
            {
                sqlSelectCols += cheSel + ",";
            }
            if (!String.IsNullOrWhiteSpace(sqlSelectCols)) sqlSelectCols = sqlSelectCols.Remove(sqlSelectCols.Length - 1);
            else sqlSelectCols = "*";
            Console.WriteLine("Selection: " + sqlSelectCols);

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
                    MessageBox.Show("Switch not implemented at Main.cs SelectTable");
                    throw new Exception("Switch not implemented at Main.cs SelectTable");
            }
            Console.WriteLine("SelectTable: exec querry: " + query);
            DataSet tableData = db.GetDataSet(query, tbname);
            if (tableData != null)
            {
                ManageDataGrid.DataSource = tableData;
                tableData.Tables[0].TableName = "tab";
                ManageDataGrid.DataMember = "tab";
                ManageDataSet = db.dataGridSet;
            }

            if (checkedListBox1.Items.Count < ManageDataGrid.Columns.Count)
            {
                checkedListBox1.Items.Clear();
                foreach (DataGridViewColumn column in ManageDataGrid.Columns)
                    checkedListBox1.Items.Add(column.HeaderText);
            }

            if (ManageDataGrid.Columns[0].HeaderText.ToLower() == "id")
                ManageDataGrid.Columns[0].Width = 50;



        }
        private void Main_Load(object sender, EventArgs e)
        {
            this.Owner.Hide();

            Console.WriteLine("Main_Load: start ListDatabases();");
            ListDatabases();
            Console.WriteLine("Select Databases load finished");
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Owner.Show();
        }

        private void RefreshDatabasesTree()
        {
            TreeNode selectedNode = treeView1.SelectedNode;
            treeView1.Nodes.Clear();
            this.ListDatabases();

            treeView1.SelectedNode = treeView1.Nodes[0];
            treeView1.Select();

            checkedListBox1.Items.Clear();
            ManageDataGrid.DataSource = null;
            // TablePropDataGrid.DataSource = null;
            tPropName.Text = "";

            userLimit.Text = "";
            userWhere.Text = "";

            selected_db_tb.Text = "Database: Table:";

            if (treeView1.SelectedNode.Level > 0)
                treeView1.SelectedNode.Parent.Expand();

        }

        private void RefreshReadData_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode.Level != 0)
                this.SelectTable(sender, treeView1.SelectedNode, userWhere.Text);
        }
        private void PropertiesManageData_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode.Level != 0)
            {
                int[] toCheck;
                switch (dbVar)
                {

                    case "postgres":
                        actualTable = "INFORMATION_SCHEMA.columns";
                        userWhere.Text = "table_name = '" + treeView1.SelectedNode.Text + "';";
                        toCheck = new int[] { 3, 5, 6, 7, 10, 43 };
                        break;
                    default:
                        throw new Exception("Switch not implemented at PropertiesManageData_Click ");
                }


                refreshDisplayDbTb();
                TreeNode[] nodes = new TreeNode[1];
                nodes[0] = new TreeNode(actualTable);
                TreeNode parent = new TreeNode(treeView1.SelectedNode.Parent.Text, nodes); //setting the parent
                userLimit.Text = "";

                this.SelectTable(sender, nodes[0], userWhere.Text);



                foreach (int i in toCheck)
                    checkedListBox1.SetItemChecked(i, true);
                RefreshReadData_Click(sender, e);

                //NpgsqlDataAdapter da = db.PostGresdataGridAdapterl;
            }

        }

        /*private void RenderTableProperties(object sender, TreeNode node)
        {
            try
            {
                tPropName.Text = node.Text;
                string query;
                switch (dbVar)
                {
                    case "mysql":
                        query = "DESC " + node.Parent.Text + "." + node.Text;
                        break;
                    case "postgres":
                        query = "select column_name, data_type, character_maximum_length from INFORMATION_SCHEMA.COLUMNS where table_name = '" + node.Text + "';";
                        break;
                    default:
                        throw new Exception("Switch not implmented at main.cs RenderTable properties");
                }
                DataSet tableData = db.GetDataSet(query);
               
                TablePropDataGrid.DataSource = tableData;
                TablePropDataGrid.DataMember = tableData.Tables[0].TableName;
                PropsDataSet = db.dataGridSet;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Database Error");
            }
        } */

        private void treeView1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            Console.WriteLine("Select node " + e.Node.Text + " of Level: " + e.Node.Level);

            if (e.Node.Level == 0) actualDatabase = e.Node.Text;
            if (e.Node.Level == 1)
            {
                actualDatabase = e.Node.Parent.Text;
                actualTable = e.Node.Text;

                userLimit.Text = "";
                userWhere.Text = "";

                //this.RenderTableProperties(sender, e.Node);
                this.SelectTable(sender, e.Node, userWhere.Text);
            }
            refreshDisplayDbTb();
        }

        private void refreshDisplayDbTb()
        {
            selected_db_tb.Text = "Database: " + actualDatabase + ", Table: " + actualTable;
        }

        private void textBox1_MouseDoubleClick(object sender, MouseEventArgs e)
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


        private void logOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.FormClosed -= SelectDatabaseForm_FormClosed;
            this.Owner.Show();
            this.Close();
        }

        private void createotherOneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Owner.Show();
            this.Owner.BringToFront();
        }

        private void SelectDatabaseForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.FormClosed -= SelectDatabaseForm_FormClosed;
            this.Owner.Show();
            this.Close();
        }

        private void buttonNewQuery(object sender, EventArgs e)
        {
            if (toolStripStatusLabel1.Text == "Unsaved")
                if (MessageBox.Show("Do you want to save the current querry? ", "UDBM: Mysql", MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.No)
                    this.button1_Click_1(sender, e);
            qRez.Text = "";
            qInp.Text = "";
            queryName.Text = "";
            toolStripStatusLabel1.Text = "";
            toolStripStatusLabel2.Text = "";
        }

        private void EveClearOutput(object sender, EventArgs e)
        {
            qRez.Text = "";
        }

        private void tPropSave_Click(object sender, EventArgs e)
        {
            if (tPropName.Text == treeView1.SelectedNode.Text) return;
            if (String.IsNullOrWhiteSpace(tPropName.Text)) return;

            try
            {
                List<List<string>> rez = db.Select("ALTER TABLE " + treeView1.SelectedNode.Text + " RENAME TO " + tPropName.Text);
                this.RefreshDatabasesTree();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                string querry = "DROP TABLE " + treeView1.SelectedNode.Text + ";";
                if (MessageBox.Show("Are you sure you want to execute ? \n " + querry, "UDBM Error", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
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

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.RefreshDatabasesTree();
        }

        private void bSaveQuery_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = queryName.Text;
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                toolStripStatusLabel2.Text = saveFileDialog1.FileName;
                queryName.Text = saveFileDialog1.FileName.Split('\\').Last();
                try
                {
                    qInp.SaveFile(toolStripStatusLabel2.Text, RichTextBoxStreamType.PlainText);
                    toolStripStatusLabel1.Text = "Saved";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    toolStripStatusLabel1.Text = "Unsaved";
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (toolStripStatusLabel2.Text != "")
            {
                qInp.SaveFile(toolStripStatusLabel2.Text, RichTextBoxStreamType.PlainText);
                toolStripStatusLabel1.Text = "Saved";
            }
            else
                this.bSaveQuery_Click(sender, e);
        }

        private void tPropDelAll_Click(object sender, EventArgs e)
        {
            try
            {
                string querry = "delete from " + treeView1.SelectedNode.Text + " ;";
                if (MessageBox.Show("Are you sure you want to execute ? \n " + querry, "UDBM:MySQL", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
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

        private void tPropTrunc_Click(object sender, EventArgs e)
        {
            try
            {
                string querry = "Truncate TABLE " + treeView1.SelectedNode.Text + " ;";
                if (MessageBox.Show("Are you sure you want to execute ? \n " + querry, "UDBM:MySQL", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
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

        private void dataGridView1_DoubleClick(object sender, EventArgs e)
        {

        }

        private void DiscardChangesReadData_Click(object sender, EventArgs e)
        {
            SelectTable(sender, treeView1.SelectedNode);
        }

        private void qInp_TextChanged(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Unsaved";

            if (highlightTimer.Enabled == false)
                highlightTimer.Enabled = true;
        }
        private void highlightTimer_Tick(object sender, EventArgs e)
        {

            int selStart = qInp.SelectionStart;
            qInp.Enabled = false;
            qInp.SelectAll();
            qInp.SelectionColor = Color.Black;
            qInp.SelectionFont = new Font(qInp.Font, FontStyle.Regular);
            qInp.Select(qInp.Text.Length, 0);

            foreach (string s in highlightKeyWords)
                CheckKeyword(s, Color.Purple, 0);
            highlightTimer.Enabled = false;

            qInp.Enabled = true;
            qInp.Select();
            qInp.SelectionStart = selStart;
        }

        private void bOpenQuery_Click(object sender, EventArgs e)
        {
            if (toolStripStatusLabel1.Text == "Unsaved")
                if (MessageBox.Show("Do you want to save the current querry? ", "UDBM: Mysql", MessageBoxButtons.YesNo) != System.Windows.Forms.DialogResult.No)
                    this.button1_Click_1(sender, e);
            qRez.Text = "";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                qInp.LoadFile(openFileDialog1.FileName, RichTextBoxStreamType.PlainText);
                queryName.Text = openFileDialog1.FileName.Split('\\').Last();
                toolStripStatusLabel2.Text = openFileDialog1.FileName;
            }
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show(e.Exception.Message, "UDBM:MySQL Error at row " + e.RowIndex + " column: " + e.ColumnIndex, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void userLimit_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsDigit(e.KeyChar) && !Char.IsControl(e.KeyChar)) e.Handled = true;
        }

        private void CheckKeyword(string word, Color color, int startIndex)
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
                        qInp.SelectionFont = new Font(qInp.Font, FontStyle.Bold);

                        qInp.Select(selectStart, 0);
                        qInp.SelectionColor = Color.Black;
                        qInp.SelectionFont = new Font(qInp.Font, FontStyle.Regular);
                    }
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked == true)
            {
                DiscardChangesManageData.Enabled = false;
                ApplyChangesManageData.Enabled = false;
                ManageDataGrid.Validated += btnSaveGridData_Click;
            }
            else
            {
                DiscardChangesManageData.Enabled = true;
                ApplyChangesManageData.Enabled = true;
                ManageDataGrid.Validated -= btnSaveGridData_Click;
            }
        }

        /* private void PropTabAutoApply_CheckedChanged(object sender, EventArgs e)
         {
             if (((CheckBox)sender).Checked == true)
             {
                 TabPropApplyChanges.Enabled = false;
                 TabPropdiscard.Enabled = false;
                 TablePropDataGrid.Validated += TabPropApplyChanges_Click;
             }
             else
             {
                 TabPropApplyChanges.Enabled = true;
                 TabPropdiscard.Enabled = true;
                 TablePropDataGrid.Validated -= TabPropApplyChanges_Click;
             }
         }*/
    }
}

/*
To do:
 * 
 * Mai multe baze de date
 * Editarea proprietatilor tabelului (ceva similar cu view/edit data)
 * Lucru cu scheme
 * De permis de introdus orice querry in Manage data
 * panel-ul cu baze de date in dreapta - resiseable
 * Careva probleme cu auto apply
 * refactoring la denmiri
 * rename pentru Postgre
 * panelul din stanga cu copacul sa fac resizebale
*/
