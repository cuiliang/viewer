﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Properties;
using Viewer.UI.Forms;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    [Export(typeof(IAttributeView))]
    public partial class AttributeTableView : WindowView, IAttributeView
    {
        /// <summary>
        /// Background color of an attribute which is not set in all entities in selection
        /// </summary>
        private readonly Color _globalBackColor = Color.AliceBlue;

        /// <summary>
        /// Background color of a read only attribute
        /// </summary>
        private readonly Color _readOnlyBackColor = Color.LightGray;
        
        private const int TypeColumnIndex = 2;
        
        public AttributeTableView()
        {
            InitializeComponent();

            // add types column
            var typeColumn = GridView.Columns[TypeColumnIndex] as DataGridViewComboBoxColumn;
            typeColumn.DataSource = Enum.GetValues(typeof(AttributeType));
            typeColumn.ValueType = typeof(AttributeType);
        }

        #region View interface

        public event EventHandler SaveAttributes;
        public event EventHandler<AttributeChangedEventArgs> AttributeChanged;
        public event EventHandler<AttributeDeletedEventArgs> AttributeDeleted;
        public event EventHandler<SortEventArgs> SortAttributes;
        public event EventHandler<FilterEventArgs> FilterAttributes;
        
        public List<AttributeGroup> Attributes { get; set; } = new List<AttributeGroup>();

        private AttributeViewType _viewType;
        public AttributeViewType ViewType
        {
            get => _viewType;
            set
            {
                _viewType = value;
                Icon = value == AttributeViewType.Exif
                    ? Resources.ExifComponentIcon
                    : Resources.AttributesComponentIcon;
            }
        }

        private bool _suspendUpdateEvent = false;

        public void UpdateAttributes()
        {
            _suspendUpdateEvent = true;
            SuspendLayout();
            try
            {
                GridView.Rows.Clear();
                foreach (var attr in Attributes)
                {
                    var row = CreateAttributeView(attr);
                    GridView.Rows.Add(row);
                }
            }
            finally
            {
                ResumeLayout();
                _suspendUpdateEvent = false;
            }
        }

        public void UpdateAttribute(int index)
        {
            if (index < 0 || index >= Attributes.Count)
                return;

            var row = CreateAttributeView(Attributes[index]);
            _suspendUpdateEvent = true;
            try
            {
                if (index < GridView.Rows.Count)
                    GridView.Rows.RemoveAt(index);
                GridView.Rows.Insert(index, row);
            }
            finally
            {
                _suspendUpdateEvent = false;
            }
        }

        public void AttributeNameIsNotUnique(string name)
        {
            MessageBox.Show(
                string.Format(Resources.DuplicateAttributeName_Message, name),
                Resources.DuplicateAttributeName_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public void AttributeNameIsEmpty()
        {
            MessageBox.Show(
                Resources.AttributeNameEmpty_Message, 
                Resources.AttributeNameEmpty_Label, 
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        #endregion

        private class RowAttributeVisitor : IValueVisitor
        {
            private DataGridViewRow _row;

            public RowAttributeVisitor(DataGridViewRow row)
            {
                _row = row;
            }

            public void Visit(IntValue attr)
            {
                _row.Cells.Add(new DataGridViewTextBoxCell
                {
                    ValueType = typeof(int),
                    Value = attr.Value
                });
                AddTypeColumn(AttributeType.Int);
            }

            public void Visit(RealValue attr)
            {
                _row.Cells.Add(new DataGridViewTextBoxCell
                {
                    ValueType = typeof(double),
                    Value = attr.Value
                });
                AddTypeColumn(AttributeType.Double);
            }

            public void Visit(StringValue attr)
            {
                _row.Cells.Add(new DataGridViewTextBoxCell
                {
                    ValueType = typeof(string),
                    Value = attr.Value
                });
                AddTypeColumn(AttributeType.String);
            }

            public void Visit(DateTimeValue attr)
            {
                _row.Cells.Add(new DateTimeCell { Value = attr.Value });
                AddTypeColumn(AttributeType.DateTime);
            }

            public void Visit(ImageValue attr)
            {
            }

            private void AddTypeColumn(AttributeType type)
            {
                _row.Cells.Add(new DataGridViewComboBoxCell
                {
                    DataSource = Enum.GetValues(typeof(AttributeType)),
                    ValueType = typeof(AttributeType),
                    Value = type,
                });
            }
        }

        private DataGridViewRow CreateAttributeView(AttributeGroup attr)
        {
            var row = new DataGridViewRow { Tag = attr };
            row.Cells.Add(new DataGridViewTextBoxCell { ValueType = typeof(string), Value = attr.Data.Name });

            if (attr.IsMixed)
            {
                var mixedValueCell = new DataGridViewTextBoxCell
                {
                    Value = "mixed value",
                    ValueType = typeof(string),
                    Style = {ForeColor = Color.Gray}
                };

                row.Cells.Add(mixedValueCell);
            }
            else
            {
                attr.Data.Value.Accept(new RowAttributeVisitor(row));

                if (!attr.IsGlobal)
                {
                    row.DefaultCellStyle.BackColor = _globalBackColor;
                }
            }

            // disable editing if the attribute is readonly
            if ((attr.Data.Flags & AttributeFlags.ReadOnly) != 0)
            {
                row.ReadOnly = true;
                row.DefaultCellStyle.BackColor = _readOnlyBackColor;
            }

            return row;
        }
        
        private Attribute TryParseRow(DataGridViewRow row)
        {
            var name = row.Cells[0].Value as string;
            var value = row.Cells[1].Value;
            var type = row.Cells[2].Value as AttributeType?;
            
            switch (type)
            {
                case AttributeType.Int:
                    return new Attribute(name, new IntValue(value as int?));
                case AttributeType.Double:
                    return new Attribute(name, new RealValue(value as double?));
                case AttributeType.String:
                    return new Attribute(name, new StringValue(value as string));
                case AttributeType.DateTime:
                    return new Attribute(name, new DateTimeValue(value as DateTime?));
                case null:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void GridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _suspendUpdateEvent)
                return;

            var row = GridView.Rows[e.RowIndex];
            var newValue = TryParseRow(row);
            if (newValue == null)
                return;
            
            AttributeChanged?.Invoke(sender, new AttributeChangedEventArgs
            {
                Index = e.RowIndex,
                OldValue = Attributes[e.RowIndex],
                NewValue = new AttributeGroup { IsMixed = false, Data = newValue }
            });
        }

        private void GridView_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells[TypeColumnIndex].Value = AttributeType.String;
        }

        private void GridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                SaveButton_Click(sender, e);
            }
            else if (e.Control && e.KeyCode == Keys.F)
            {
                SearchTextBox.Focus();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                var deleted = new List<int>();
                foreach (DataGridViewCell cell in GridView.SelectedCells)
                {
                    deleted.Add(cell.RowIndex);
                }

                AttributeDeleted?.Invoke(sender, new AttributeDeletedEventArgs
                {
                    Deleted = deleted
                });
            }
        }

        private void GridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            SortColumn column = SortColumn.Name;
            switch (e.ColumnIndex)
            {
                case 0:
                    column = SortColumn.Name;
                    break;
                case 1:
                    column = SortColumn.Value;
                    break;
                case 2:
                    column = SortColumn.Type;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e));
            }

            SortAttributes?.Invoke(sender, new SortEventArgs
            {
                Column = column
            });
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveAttributes?.Invoke(sender, e);
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            FilterAttributes?.Invoke(sender, new FilterEventArgs
            {
                FilterText = SearchTextBox.Text
            });
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                SearchTextBox.Text = ""; // reset the filter
            }
        }

        protected override string GetPersistString()
        {
            return base.GetPersistString() + ";" + (ViewType == AttributeViewType.Exif ? "exif" : "attributes");
        }
    }
}
