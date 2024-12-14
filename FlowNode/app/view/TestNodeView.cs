using System;
using System.Drawing;
using System.Windows.Forms;
using FlowNode.node;

namespace FlowNode.app.view
{
    public class TestNodeView : NodeView
    {
        private NodeLabel titleLabel;
        private NodeTextBox inputTextBox;
        private NodeButton executeButton;
        private NodeCheckBox enableCheckBox;
        private NodeComboBox optionsComboBox;
        private NodeProgressBar progressBar;
        private NodeLabel statusLabel;

        public TestNodeView(NodeBase node, Point location) : base(node, location)
        {

        }

        protected override void InitializeControls()
        {
            // 创建并设置各个控件
            titleLabel = new NodeLabel(this, "titleLabel", "111111111Test Node")
            {
                Bounds = new Rectangle(10, 30, 180, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            inputTextBox = new NodeTextBox(this, "inputTextBox", "Enter text...")
            {
                Bounds = new Rectangle(10, 60, 180, 25)
            };

            executeButton = new NodeButton(this, "executeButton", "Execute")
            {
                Bounds = new Rectangle(10, 95, 180, 25)
            };

            enableCheckBox = new NodeCheckBox(this, "enableCheckBox", "Enable Processing")
            {
                Bounds = new Rectangle(10, 130, 180, 20)
            };

            optionsComboBox = new NodeComboBox(this, "optionsComboBox")
            {
                Bounds = new Rectangle(10, 160, 180, 25)
            };
            optionsComboBox.Items.AddRange(new[] { "Option 1", "Option 2", "Option 3" });

            progressBar = new NodeProgressBar(this, "progressBar")
            {
                Bounds = new Rectangle(10, 195, 180, 15)
            };

            statusLabel = new NodeLabel(this, "statusLabel", "Ready")
            {
                Bounds = new Rectangle(10, 220, 180, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 添加所有控件
            Controls.Add(titleLabel);
            Controls.Add(inputTextBox);
            Controls.Add(executeButton);
            Controls.Add(enableCheckBox);
            Controls.Add(optionsComboBox);
            Controls.Add(progressBar);
            Controls.Add(statusLabel);

            // 调整节点大小以适应所有控��
            Bounds = new Rectangle(Bounds.X, Bounds.Y, 200, 250);
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            // 设置按钮点击事件
            executeButton.Click += (s, e) =>
            {
                if (!enableCheckBox.Checked)
                {
                    statusLabel.Text = "Processing is disabled";
                    return;
                }

                statusLabel.Text = "Processing...";
                SimulateProgress();
            };

            // 设置文本框变化事件
            inputTextBox.TextChanged += (s, text) =>
            {
                titleLabel.Text = string.IsNullOrEmpty(text) ? "Test Node" : text;
            };

            // 设置复选框变化事件
            enableCheckBox.CheckedChanged += (s, isChecked) =>
            {
                executeButton.Enabled = isChecked;
                if (!isChecked)
                {
                    statusLabel.Text = "Disabled";
                }
                else
                {
                    statusLabel.Text = "Ready";
                }
            };

            // 设置下拉框选择变化事件
            optionsComboBox.SelectedIndexChanged += (s, index) =>
            {
                if (index >= 0)
                {
                    statusLabel.Text = $"Selected: {optionsComboBox.SelectedItem}";
                }
            };
        }

        private async void SimulateProgress()
        {
            executeButton.Enabled = false;
            progressBar.Value = 0;

            for (int i = 0; i <= 100; i++)
            {
                progressBar.Value = i;
                await System.Threading.Tasks.Task.Delay(50);
            }

            statusLabel.Text = "Completed!";
            executeButton.Enabled = true;
        }

    }
} 