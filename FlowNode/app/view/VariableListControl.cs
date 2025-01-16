using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FlowNode
{
    public class VariableListControl : UserControl
    {
        private List<VariableItem> items = new List<VariableItem>();
        private int selectedIndex = -1;
        private VScrollBar vScrollBar;
        private Button addButton;

        public VariableListControl()
        {
            this.DoubleBuffered = true;
            this.MouseClick += VariableListControl_MouseClick;

            vScrollBar = new VScrollBar
            {
                Dock = DockStyle.Right,
                Minimum = 0,
                Maximum = 0,
                SmallChange = 1,
                LargeChange = 1,
                Visible = false
            };
            vScrollBar.Scroll += (s, e) => UpdateLabels();
            this.Controls.Add(vScrollBar);

            addButton = new Button
            {
                Text = "+",
                Size = new Size(30, 30),
                Location = new Point(this.Width - 40, 5),
                BackColor = Color.Gray,
                ForeColor = Color.White
            };
            addButton.Click += AddButton_Click;
            this.Controls.Add(addButton);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            AddItem("NewVar", "Type", Color.LightGray);
        }

        public void AddItem(string name, string type, Color color)
        {
            items.Add(new VariableItem { Name = name, Type = type, Color = color });
            UpdateScrollBar();
            CreateLabels();
        }

        private void UpdateScrollBar()
        {
            int visibleItems = (this.Height - 40) / 30;
            vScrollBar.Maximum = Math.Max(0, items.Count - visibleItems);
            vScrollBar.Visible = items.Count > visibleItems;
        }

        private void CreateLabels()
        {
            this.Controls.Clear();
            this.Controls.Add(vScrollBar);
            this.Controls.Add(addButton);

            int y = 40;
            int startIndex = vScrollBar.Value;

            for (int i = startIndex; i < items.Count && y < this.Height; i++)
            {
                var item = items[i];
                var label = new Label
                {
                    Text = $"{item.Name} - {item.Type}",
                    ForeColor = Color.White,
                    BackColor = item.Color,
                    Location = new Point(10, y),
                    Size = new Size(this.Width - vScrollBar.Width - 20, 25)
                };
                label.Click += (s, e) => SelectItem(i);
                this.Controls.Add(label);
                y += 30;
            }
        }

        private void UpdateLabels()
        {
            CreateLabels();
        }

        private void SelectItem(int index)
        {
            selectedIndex = index;
            // Handle item selection logic here
        }

        private void VariableListControl_MouseClick(object sender, MouseEventArgs e)
        {

        }

        // Existing StartEdit and EndEdit methods...
    }

    public class VariableItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Color Color { get; set; }
    }
}
