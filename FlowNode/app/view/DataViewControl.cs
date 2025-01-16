using System;
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
        private ListView listView;
        private Button addButton;
        private Button closeButton;

        public DataViewControl(NodeManager nodeManager, CommandManager commandManager)
        {
            this.nodeManager = nodeManager;
            this.commandManager = commandManager;
            InitializeComponents();
            UpdateListView();
        }

        private void InitializeComponents()
        {
            Text = "Data Object Manager";
            Size = new Size(300, 400);

            // 创建 TableLayoutPanel
            var tableLayoutPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true
            };
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70F)); // ListView 占 70%
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // 按钮占固定高度
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F)); // 关闭按钮占固定高度

            // 创建 ListView
            listView = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill
            };

            listView.Columns.Add("Key", 50);
            listView.Columns.Add("Value", 100);
            listView.Columns.Add("Type", 50);

            // 添加右键菜单
            var contextMenu = new ContextMenuStrip();
            var deleteMenuItem = new ToolStripMenuItem("Delete");
            deleteMenuItem.Click += DeleteMenuItem_Click;
            contextMenu.Items.Add(deleteMenuItem);
            listView.ContextMenuStrip = contextMenu;

            // 创建按钮
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

            // 将控件添加到 TableLayoutPanel
            tableLayoutPanel.Controls.Add(listView, 0, 0);
            tableLayoutPanel.Controls.Add(addButton, 0, 1);
            tableLayoutPanel.Controls.Add(closeButton, 0, 2);

            // 将 TableLayoutPanel 添加到 UserControl
            Controls.Add(tableLayoutPanel);
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

        private void AddButton_Click(object sender, EventArgs e)
        {
            using (var form = new DataObjectEditForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var command = new AddDataObjectCommand(
                            nodeManager, 
                            form.ObjectKey, 
                            form.ObjectValue, 
                            form.ObjectType
                        );
                        commandManager.ExecuteCommand(command);
                        UpdateListView();
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
                var command = new RemoveDataObjectCommand(nodeManager, key);
                commandManager.ExecuteCommand(command);
                UpdateListView();
            }
        }
    }

    // 用于添加/编辑数据对象的对话框
    public class DataObjectEditForm : Form
    {
        private TextBox keyTextBox;
        private TextBox valueTextBox;
        private ComboBox typeComboBox;
        private Button okButton;
        private Button cancelButton;

        public string ObjectKey => keyTextBox.Text;
        public object ObjectValue => Convert.ChangeType(valueTextBox.Text, ObjectType);
        public Type ObjectType => Type.GetType($"System.{typeComboBox.SelectedItem}");

        public DataObjectEditForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "Add Data Object";
            Size = new Size(300, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var keyLabel = new Label { Text = "Key:", Left = 10, Top = 20 };
            keyTextBox = new TextBox { Left = 100, Top = 20, Width = 180 };

            var valueLabel = new Label { Text = "Value:", Left = 10, Top = 50 };
            valueTextBox = new TextBox { Left = 100, Top = 50, Width = 180 };

            var typeLabel = new Label { Text = "Type:", Left = 10, Top = 80 };
            typeComboBox = new ComboBox { Left = 100, Top = 80, Width = 180 };
            typeComboBox.Items.AddRange(new object[] { "String", "Int32", "Double", "Boolean" });
            typeComboBox.SelectedIndex = 0;

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 100,
                Top = 120
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Left = 200,
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