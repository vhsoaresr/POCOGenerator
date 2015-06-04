﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using POCOGenerator.Controls;
using POCOGenerator.DomainServices;
using POCOGenerator.Entities;
using POCOGenerator.Extenders;
using POCOGenerator.Properties;


namespace POCOGenerator
{
	public partial class Main : Form
	{
		private const int TopCount = 10;

		#region Properties

		private BindingList<ConnectionItem> ConnectionItems
		{
			get { return (BindingList<ConnectionItem>) connectionBindingSource.DataSource; }
			set { connectionBindingSource.DataSource = value; }
		}

		private string SelectedConnectionString
		{
			get
			{
				var connectionItem = (ConnectionItem) connectionBindingSource.Current;
				return connectionItem != null ? connectionItem.ConnectionString : null;
			}
			set
			{
				var connectionItem = connectionBindingSource.List.OfType<ConnectionItem>().FirstOrDefault(c => c.ConnectionString == value);
				var i = connectionBindingSource.IndexOf(connectionItem);
				if (i >= 0)
				{
					connectionBindingSource.Position = i;
				}
				else if (connectionBindingSource.Count > 0)
				{
					cboConnection.SelectedIndex = 0;
				}
			}
		}

		#endregion

		public Main()
		{
			InitializeComponent();
			tabResult.TabPages.Clear();
		}
		

		private void AddConnection()
		{
			var f = new Connection();
			var dialogResult = f.ShowDialog();
			if (dialogResult == DialogResult.OK)
			{
				var connection = new ConnectionItem { ConnectionString = f.ConnectionString };
				ConnectionItems.Add(connection);
				SelectedConnectionString = f.ConnectionString;
				SaveSettings();
				LoadTables();
			}
		}

		private void EditConnection()
		{
			var connection = ConnectionItems.FirstOrDefault(c => c.ConnectionString == cboConnection.SelectedValue.ToString());
			if (connection == null)
			{
				MessageBox.Show("There is no connection to edit", "Edit", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			var f = new Connection { ConnectionString = connection.ConnectionString };
			var dialogResult = f.ShowDialog();
			if (dialogResult == DialogResult.OK)
			{
				connection.ConnectionString = f.ConnectionString;
				// How to refresh displayed data???
				SaveSettings();
				LoadTables();
			}
		}

		private void RemoveConnection()
		{
			var connection = ConnectionItems.FirstOrDefault(c => c.ConnectionString == cboConnection.SelectedValue.ToString());
			if (connection == null)
			{
				MessageBox.Show("There is no connection to remove", "Remove", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			ConnectionItems.Remove(connection);
			SaveSettings();
			LoadTables();
		}

		private void LoadTables()
		{
			if (SelectedConnectionString.IsSpecified())
			{
				cboTableName.DataSource = SqlParser.GetTableNames(SelectedConnectionString);
			}
			else
			{
				cboTableName.DataSource = null;
			}
		}

		private void Generate(string sql)
		{
			try
			{
				Cursor = Cursors.WaitCursor;

				var adoHandler = new SqlParser(SelectedConnectionString, sql);
				resultItemBindingSource.DataSource = adoHandler.ResultItems;

				tabResult.TabPages.Clear();
				foreach (var resultItem in adoHandler.ResultItems)
				{
					var tabPage = new TabPage { Text = resultItem.EntityName, Margin = new Padding(0) };
					tabResult.TabPages.Add(tabPage);

					var content = new ResultContent { Dock = DockStyle.Fill };
					content.Initiate(resultItem);
					tabPage.Controls.Add(content);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("GenerateClass", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void AdjustSql()
		{
			var sql = txtSqlView.Text.Trim();
			txtSqlView.Text = sql.Replace("SELECT *", string.Format("SELECT TOP {0} *", TopCount));
		}

		private void GetSettings()
		{
			var settings = SettingsHandler.Get();
			ConnectionItems = settings.ConnectionStrings;
			if (ConnectionItems.Any(c => c.ConnectionString == settings.SelectedConnection))
			{
				SelectedConnectionString = settings.SelectedConnection;
				LoadTables();
			}

			txtSqlView.Text = settings.SqlView;
			txtSqlProcedure.Text = settings.SqlProcedure;

			if (Environment.UserName == "jal")
			{
				txtSqlProcedure.Text = "EXEC usp_ExtExportGetTasks @ServerId=0, @ExternalSystemId=2, @MaxProcessingTimeInMinutes=5  AS Task, Customer, Activity, Helper";
			}
		}

		private void SaveSettings()
		{
			var settings = new Settings
			{
				ConnectionStrings = ConnectionItems,
				SelectedConnection = SelectedConnectionString,
				SqlView = txtSqlView.Text,
				SqlProcedure = txtSqlProcedure.Text
			};
			SettingsHandler.Save(settings);
		}


		#region Event Handlers

		private void Main_Load(object sender, EventArgs e)
		{
			GetSettings();
		}

		private void btnAdd_Click(object sender, EventArgs e)
		{
			AddConnection();
		}

		private void btnEdit_Click(object sender, EventArgs e)
		{
			EditConnection();
		}

		private void btnRemove_Click(object sender, EventArgs e)
		{
			RemoveConnection();
		}

		private void connectionBindingSource_CurrentChanged(object sender, EventArgs e)
		{
			LoadTables();
		}

		private void btnGenerateTable_Click(object sender, EventArgs e)
		{
			var sql = string.Format("SELECT TOP {0} * FROM {1}", TopCount, cboTableName.Text);
			SaveSettings();
			Generate(sql);
		}

		private void btnGenerateView_Click(object sender, EventArgs e)
		{
			SaveSettings();
			AdjustSql();
			Generate(txtSqlView.Text);
		}

		private void btnGenerate_Click(object sender, EventArgs e)
		{
			SaveSettings();
			Generate(txtSqlProcedure.Text);
		}

		#endregion
	}
}