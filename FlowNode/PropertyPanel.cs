using FlowNode.node;
using System;
using System.Windows.Forms;

namespace FlowNode
{
    public class PropertyPanel : UserControl
    {
        private PropertyGrid propertyGrid;
        private NodeEditor editor;
        private NodeBase currentNode;

        public PropertyPanel(NodeEditor editor)
        {
            this.editor = editor;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                ToolbarVisible = false
            };

            this.Controls.Add(propertyGrid);
        }

        public void ShowProperties(NodeBase node)
        {
            currentNode = node;
            propertyGrid.SelectedObject = node;
        }

        public void ClearProperties()
        {
            currentNode = null;
            propertyGrid.SelectedObject = null;
        }
    }
} 