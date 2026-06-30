using FlowNode.app.command;

using FlowNode.node;

using System;

using System.ComponentModel;

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

            propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;



            this.Controls.Add(propertyGrid);

        }



        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)

        {

            if (currentNode == null || editor == null || e.ChangedItem == null)

                return;



            var sheet = propertyGrid.SelectedObject as NodePropertySheet;

            if (sheet == null)

                return;



            PropertyDescriptor matched = FindPropertyDescriptor(sheet, e.ChangedItem.Label);

            if (matched == null)

            {

                editor.InvalidateNode(currentNode);

                return;

            }



            object oldValue = e.OldValue;

            object newValue = matched.GetValue(sheet);



            ICommand command;
            if (currentNode is GetObjectNode getNode && matched.Name == "Value")
            {
                command = new UpdateDataObjectCommand(
                    editor.NodeManager,
                    getNode.VariableName,
                    newValue,
                    getNode.VariableType);
            }
            else if (currentNode.findPin(matched.Name) is Pin pin
                     && pin.direction == PinDirection.Input
                     && pin.pinType == PinType.Data)
            {
                command = new SetPinDataCommand(currentNode, matched.Name, oldValue, newValue);
            }
            else
            {
                command = new SetNodePropertyCommand(currentNode, matched.Name, oldValue, newValue);
            }

            editor.CommandManager.ExecuteCommand(command);
            editor.NodeManager.SyncGetObjectOutputPins();
            editor.InvalidateNode(currentNode);

        }



        private static PropertyDescriptor FindPropertyDescriptor(NodePropertySheet sheet, string label)

        {

            foreach (PropertyDescriptor pd in sheet.GetProperties())

            {

                if (pd.Name == label || pd.DisplayName == label)

                    return pd;

            }



            return null;

        }



        public void ShowProperties(NodeBase node)

        {

            currentNode = node;

            propertyGrid.SelectedObject = new NodePropertySheet(node, editor.NodeManager);

        }



        public void ClearProperties()

        {

            currentNode = null;

            propertyGrid.SelectedObject = null;

        }

    }

}


