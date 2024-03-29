﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using DBDiff.Schema;
using DBDiff.Schema.Model;
using DBDiff.Schema.Attributes;
using DBDiff.Schema.SQLServer.Generates.Model;

namespace DBDiff.Front
{
    public partial class SchemaTreeView : UserControl
    {
        private ISchemaBase databaseSource;
        private ISchemaBase databaseDestination;

        public delegate void SchemaHandler(string ObjectFullName);
        public event SchemaHandler OnSelectItem;

        public SchemaTreeView()
        {
            InitializeComponent();
        }

        public ISchemaBase DatabaseDestination
        {
            get { return databaseDestination; }
            set { databaseDestination = value; }
        }

        public ISchemaBase DatabaseSource
        {
            get { return databaseSource; }
            set
            {
                databaseSource = value;
                if (value != null)
                {
                    RebuildSchemaTree();
                }
            }
        }

        private void ReadPropertys(Type item, TreeNodeCollection nodes, ISchemaBase schema)
        {
            PropertyInfo[] pi = item.GetProperties();
            nodes.Clear();
            foreach (PropertyInfo p in pi)
            {
                object[] attrs = p.GetCustomAttributes(typeof(ShowItemAttribute), true);
                if (attrs.Length > 0)
                {
                    ShowItemAttribute show = (ShowItemAttribute)attrs[0];
                    TreeNode node = nodes.Add(p.Name, show.Name);
                    node.ImageKey = "Folder";
                    ReadPropertyDetail(node, p, schema, show);
                }
            }
        }

        public static IList<T> ConvertToListOf<T>(IList iList)
        {
            IList<T> result = new List<T>();
            foreach (T value in iList)
                result.Add(value);
            return result;
        }        
        private void ReadPropertyDetail(TreeNode node, PropertyInfo p, ISchemaBase schema, ShowItemAttribute attr)
        {
            List<SQLServerSchemaBase> items = SchemaTreeView.ConvertToListOf<SQLServerSchemaBase>(p.GetValue(schema, null) as IList) as List<SQLServerSchemaBase>;
           
            //Calculate how many are alter, create, drop, so we can display an easy number. Use Linq, simple and easy.
            int alterCount, createCount, dropCount;

            alterCount = (from a in items
                          where a.Status == Enums.ObjectStatusType.AlterStatus || a.Status == Enums.ObjectStatusType.AlterBodyStatus
                          select a).Count();

            createCount = (from a in items
                           where a.Status == Enums.ObjectStatusType.CreateStatus
                           select a).Count();

            dropCount = (from a in items
                         where a.Status == Enums.ObjectStatusType.DropStatus
                         select a).Count();

            node.Text = string.Format("{0} - {1} Items (A{2}, C{3}, D{4})", node.Text, items.Count, alterCount, createCount, dropCount);

            node.Nodes.Clear();
            foreach (ISchemaBase item in items)
            {
                if (CanNodeAdd(item))
                {
                    TreeNode subnode = node.Nodes.Add((attr.IsFullName ? item.FullName : item.Name));
                    if (item.Status == Enums.ObjectStatusType.DropStatus)
                        subnode.ForeColor = Color.Red;
                    else if (item.Status == Enums.ObjectStatusType.CreateStatus)
                        subnode.ForeColor = Color.Green;
                    else if ((item.HasState(Enums.ObjectStatusType.AlterStatus)) || (item.HasState(Enums.ObjectStatusType.RebuildStatus)) || (item.HasState(Enums.ObjectStatusType.DisabledStatus)))
                        subnode.ForeColor = Color.Blue;
                    subnode.Tag = item;
                    subnode.ImageKey = attr.Image;
                    subnode.SelectedImageKey = attr.Image;
                }
            }
        }

        private void RebuildSchemaTree()
        {
            treeView1.Visible = false;
            treeView1.Nodes.Clear();
            TreeNode databaseNode = treeView1.Nodes.Add(databaseSource.Name);
            ReadPropertys(databaseSource.GetType(), databaseNode.Nodes, databaseSource);
            treeView1.Sort();
            databaseNode.ImageKey = "Database";
            databaseNode.Expand();
            treeView1.Visible = true;
        }

        private Boolean CanNodeAdd(ISchemaBase item)
        {
            if ((item.Status == Enums.ObjectStatusType.DropStatus) && (ShowMissingObjects))
                return true;
            if ((item.Status == Enums.ObjectStatusType.CreateStatus) && (ShowNewObjects))
                return true;
            if ((item.Status == Enums.ObjectStatusType.AlterStatus) && (ShowDiferentObjects))
                return true;
            if ((item.Status == Enums.ObjectStatusType.RebuildStatus) && (ShowDiferentObjects))
                return true;
            if ((item.Status == Enums.ObjectStatusType.DisabledStatus) && (ShowDiferentObjects))
                return true;
            if ((item.Status == Enums.ObjectStatusType.UpdateStatus) && (ShowDiferentObjects))
                return true;
            if ((item.Status == Enums.ObjectStatusType.OriginalStatus) && (ShowExistingObjects))
                return true;

            return false;
        }

        public Boolean ShowExistingObjects
        {
            get { return chkShowExistingObjects.Checked; }
            set { chkShowExistingObjects.Checked = value; }
        }

        public Boolean ShowNewObjects
        {
            get { return chkNew.Checked; }
            set { chkNew.Checked = value; }
        }

        public Boolean ShowMissingObjects
        {
            get { return chkOld.Checked; }
            set { chkOld.Checked = value; }
        }

        public Boolean ShowDiferentObjects
        {
            get { return chkDiferent.Checked; }
            set { chkDiferent.Checked = value; }
        }

        private void chkShowExistingObjects_CheckedChanged(object sender, EventArgs e)
        {
            RebuildSchemaTree();
        }

        private void chkDiferent_CheckedChanged(object sender, EventArgs e)
        {
            RebuildSchemaTree();
        }

        private void chkOld_CheckedChanged(object sender, EventArgs e)
        {
            RebuildSchemaTree();
        }

        private void chkNew_CheckedChanged(object sender, EventArgs e)
        {
            RebuildSchemaTree();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ISchemaBase item = ((ISchemaBase)e.Node.Tag);
            if (item != null)
            {
                if (item.ObjectType == Enums.ObjectType.Table)
                    ReadPropertys(item.GetType(), e.Node.Nodes, item);
                if (OnSelectItem != null) OnSelectItem(item.FullName);
            }
        }
    }
}
