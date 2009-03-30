﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TsvnTfsProvider.Forms
{
	public partial class IssuesBrowser : Form
	{
		private readonly List<MyWorkItem> associatedWorkItems = new List<MyWorkItem>();
		private readonly ListViewColumnSorter listViewColumnSorter;
		private readonly TfsProviderOptions options;
		private TeamFoundationServer tfs;
		private WorkItemStore workItemStore;

		public IssuesBrowser(string parameters, string comment)
		{
			InitializeComponent();
			Comment = comment;
			options = TfsOptionsSerializer.Deserialize(parameters);

			int idColumnIndex = 2;
			foreach (ColumnHeader header in listViewIssues.Columns)
			{
				if (header.Name == "ID")
				{
					idColumnIndex = header.Index;
					break;
				}
			}
			listViewColumnSorter = new ListViewColumnSorter(idColumnIndex);
			listViewIssues.ListViewItemSorter = listViewColumnSorter;
		}

		public string Comment { get { return commentBox.Text; } private set { commentBox.Text = value; } }

		public string AssociatedWorkItems
		{
			get
			{
				var result = new StringBuilder();
				foreach (var workItem in associatedWorkItems) result.AppendFormat("{0} {1}: {2}\n", workItem.type, workItem.id, workItem.title);
				return result.ToString();
			}
		}

		private WorkItemStore ConnectToTfs()
		{
			tfs = TeamFoundationServerFactory.GetServer(options.ServerName);
			tfs.EnsureAuthenticated();
			return (WorkItemStore) tfs.GetService(typeof (WorkItemStore));
		}

		private IEnumerable<MyWorkItem> GetWorkItems(TfsQuery query)
		{
			var myWorkItems = new List<MyWorkItem>();
			var context = new Dictionary<string, string> {{"project", query.Query.Project.Name}};
			WorkItemCollection wiCollection = workItemStore.Query(query.Query.QueryText, context);
			foreach (WorkItem workItem in wiCollection)
				myWorkItems.Add(new MyWorkItem {id = workItem.Id, state = workItem.State, title = workItem.Title, type = workItem.Type.Name});
			return myWorkItems;
		}

		private void PopulateComboBoxWithSavedQueries(ComboBox comboBox)
		{
			Project project = workItemStore.Projects[options.ProjectName];
			StoredQueryCollection storedQueries = project.StoredQueries;
			foreach (StoredQuery query in storedQueries)
				comboBox.Items.Add(new TfsQuery(query));
			if (comboBox.Items.Count > 0) comboBox.SelectedIndex = 0;
		}

		private void MyIssuesForm_Load(object sender, EventArgs e)
		{
			workItemStore = ConnectToTfs();
			PopulateComboBoxWithSavedQueries(queryComboBox);
		}

		private void PopulateWorkItemsList(ListView listView, TfsQuery query)
		{
			listView.Items.Clear();
			IEnumerable<MyWorkItem> workItems = GetWorkItems(query);
			foreach (var workItem in workItems)
			{
				var lvi = new ListViewItem {Text = "", Tag = workItem,};
				lvi.SubItems.Add(workItem.type);
				lvi.SubItems.Add(workItem.id.ToString());
				lvi.SubItems.Add(workItem.state);
				lvi.SubItems.Add(workItem.title);
				listView.Items.Add(lvi);
			}
			foreach (ColumnHeader column in listViewIssues.Columns) column.Width = -1;
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			foreach (ListViewItem lvi in listViewIssues.Items)
				if (lvi.Checked) associatedWorkItems.Add((MyWorkItem) lvi.Tag);
		}

		private void queryComboBox_SelectedValueChanged(object sender, EventArgs e)
		{
			PopulateWorkItemsList(listViewIssues, (TfsQuery) queryComboBox.SelectedItem);
		}

		private void listViewIssues_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (e.Column == listViewColumnSorter.SortColumn)
				listViewColumnSorter.InvertOrder();
			else
			{
				listViewColumnSorter.SortColumn = e.Column;
				listViewColumnSorter.Order = SortOrder.Ascending;
			}
			listViewIssues.Sort();
		}
	}

	internal class TfsQuery
	{
		public TfsQuery(StoredQuery query)
		{
			Query = query;
			Name = query.Name;
		}

		private string Name { get; set; }
		public StoredQuery Query { get; private set; }

		public override string ToString()
		{
			return Name;
		}
	}

	public struct MyWorkItem
	{
		public int id;
		public string state;
		public string title;
		public string type;
	}
}