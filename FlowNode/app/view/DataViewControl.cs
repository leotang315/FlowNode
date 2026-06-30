using System;
using System.Globalization;
using System.Windows.Forms;
using System.Drawing;
using FlowNode.node;
using FlowNode.app.command;

namespace FlowNode
{
    public partial class DataViewControl : UserControl
    {
        private readonly NodeManager nodeManager;
        private readonly CommandManager commandManager;
        private readonly Action onDataChanged;
        public ListView listView;
        private Button addButton;
        private Button closeButton;

        public DataViewControl(
            NodeManager nodeManager,
            CommandManager commandManager,
            Action onDataChanged = null)
        {
            this.nodeManager = nodeManager;
            this.commandManager = commandManager;
            this.onDataChanged = onDataChanged;
            InitializeComponents();
            UpdateListView();
        }

        private void InitializeComponents()
        {
            Text = "Data Object Manager";
            Size = new Size(200, 200);

            var tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true
            };
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            listView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill
            };

            listView.Columns.Add("Name", 50);
            listView.Columns.Add("Value", 100);
            listView.Columns.Add("Type", 50);

            listView.DoubleClick += (s, e) => EditSelectedItem();

            var contextMenu = new ContextMenuStrip();
            var editMenuItem = new ToolStripMenuItem("Edit");
            editMenuItem.Click += (s, e) => EditSelectedItem();
            contextMenu.Items.Add(editMenuItem);
            var deleteMenuItem = new ToolStripMenuItem("Delete");
            deleteMenuItem.Click += DeleteMenuItem_Click;
            contextMenu.Items.Add(deleteMenuItem);
            listView.ContextMenuStrip = contextMenu;

            addButton = new Button
            {
                Text = "Add New",
                Dock = DockStyle.Fill
            };
            addButton.Click += AddButton_Click;

            closeButton = new Button
            {
                Text = "Close",
                Dock = DockStyle.Fill,
                DialogResult = DialogResult.OK
            };

            tableLayoutPanel.Controls.Add(listView, 0, 0);
            tableLayoutPanel.Controls.Add(addButton, 0, 1);
            tableLayoutPanel.Controls.Add(closeButton, 0, 2);

            Controls.Add(tableLayoutPanel);
        }

        public void RefreshList()
        {
            UpdateListView();
        }

        private void NotifyDataChanged()
        {
            nodeManager.SyncGetObjectOutputPins();
            onDataChanged?.Invoke();
        }

        private void UpdateListView()
        {
            listView.Items.Clear();
            foreach (var key in nodeManager.GetAllDataObjectKeys())
            {
                var value = nodeManager.GetDataObject(key);
                var type = nodeManager.GetDataObjectType(key);

                var item = new ListViewItem(key);
                item.SubItems.Add(value?.ToString() ?? "null");
                item.SubItems.Add(type?.Name ?? "unknown");
                listView.Items.Add(item);
            }
        }

        private void EditSelectedItem()
        {
            if (listView.SelectedItems.Count == 0)
                return;

            var key = listView.SelectedItems[0].Text;
            var type = nodeManager.GetDataObjectType(key);
            if (type == null)
                return;

            var value = nodeManager.GetDataObject(key);
            using (var form = new DataEditForm(key, value, type, editMode: true))
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    commandManager.ExecuteCommand(new UpdateDataObjectCommand(
                        nodeManager, key, form.ObjectValue, type));
                    UpdateListView();
                    NotifyDataChanged();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating data object: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            using (var form = new DataEditForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        commandManager.ExecuteCommand(new AddDataObjectCommand(
                            nodeManager,
                            form.ObjectKey,
                            form.ObjectValue,
                            form.ObjectType));
                        UpdateListView();
                        NotifyDataChanged();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error adding data object: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DeleteMenuItem_Click(object sender, EventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                var key = listView.SelectedItems[0].Text;
                commandManager.ExecuteCommand(new RemoveDataObjectCommand(nodeManager, key));
                UpdateListView();
                NotifyDataChanged();
            }
        }
    }

    public class DataEditForm : Form
    {
        private readonly bool editMode;
        private readonly Type objectType;
        private TextBox keyTextBox;
        private TextBox valueTextBox;
        private ComboBox typeComboBox;
        private Button okButton;
        private Button cancelButton;

        public string ObjectKey => keyTextBox.Text;

        public object ObjectValue
        {
            get
            {
                var type = editMode ? objectType : ObjectType;
                return Convert.ChangeType(valueTextBox.Text, type, CultureInfo.InvariantCulture);
            }
        }

        public Type ObjectType => Type.GetType($"System.{typeComboBox.SelectedItem}");

        public DataEditForm()
        {
            editMode = false;
            InitializeComponents();
        }

        public DataEditForm(string key, object value, Type type, bool editMode)
        {
            this.editMode = editMode;
            objectType = type;
            InitializeComponents();
            Text = editMode ? "Edit Data Object" : "Add Data Object";
            keyTextBox.Text = key;
            valueTextBox.Text = value != null
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : string.Empty;
            SelectType(type);
            keyTextBox.ReadOnly = editMode;
            typeComboBox.Enabled = !editMode;
        }

        private void SelectType(Type type)
        {
            var name = type?.Name ?? "String";
            for (int i = 0; i < typeComboBox.Items.Count; i++)
            {
                if (string.Equals(typeComboBox.Items[i]?.ToString(), name, StringComparison.Ordinal))
                {
                    typeComboBox.SelectedIndex = i;
                    return;
                }
            }

            typeComboBox.SelectedIndex = 0;
        }

        private void InitializeComponents()
        {
            Text = "Add Data Object";
            Size = new Size(320, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var keyLabel = new Label { Text = "Name:", Left = 10, Top = 20, Width = 50 };
            keyTextBox = new TextBox { Left = 70, Top = 20, Width = 220 };

            var valueLabel = new Label { Text = "Value:", Left = 10, Top = 50, Width = 50 };
            valueTextBox = new TextBox { Left = 70, Top = 50, Width = 220 };

            var typeLabel = new Label { Text = "Type:", Left = 10, Top = 80, Width = 50 };
            typeComboBox = new ComboBox { Left = 70, Top = 80, Width = 220 };
            typeComboBox.Items.AddRange(new object[] { "String", "Int32", "Double", "Boolean" });
            typeComboBox.SelectedIndex = 0;

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 130,
                Top = 120
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Left = 210,
                Top = 120
            };

            Controls.AddRange(new Control[] {
                keyLabel, keyTextBox,
                valueLabel, valueTextBox,
                typeLabel, typeComboBox,
                okButton, cancelButton
            });

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}
