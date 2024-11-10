using System.Collections.ObjectModel;
using RenPy_VisualScripting.Models;
using System.ComponentModel;
using System.Windows.Controls;

namespace RenPy_VisualScripting.ViewModels;

public class ChoiceNodeViewModel : INotifyPropertyChanged
{
    public string LabelName { get; }
    public ChoiceNodeType NodeType { get; }
    public ChoiceNodeViewModel? Parent { get; }
    public ObservableCollection<ChoiceNodeViewModel> Children { get; }

    public double NodeWidth { get; }
    public Orientation ChildrenOrientation { get; }

    public ChoiceNodeViewModel TrueBranch { get; }
    public ChoiceNodeViewModel FalseBranch { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ChoiceNodeViewModel(ChoiceNode node, ChoiceNodeViewModel? parent = null)
    {
        LabelName = node.LabelName;
        NodeType = node.NodeType;
        if (parent != null)
        {
            Parent = parent;

            // Set NodeWidth to half of parent's width or default value
            if (parent.NodeType is ChoiceNodeType.ElseBlock or ChoiceNodeType.MenuOption or ChoiceNodeType.LabelBlock)
                NodeWidth = parent.NodeWidth;
            else
                NodeWidth = parent.NodeWidth / 2;
        }
        else
        {
            Parent = null;
            NodeWidth = 2500;
        }

        if (NodeType == ChoiceNodeType.IfBlock)
        {
            // For IfBlock, set orientation and initialize branches
            ChildrenOrientation = Orientation.Horizontal;

            // Initialize TrueBranch with the first child
            if (node.Children.Count > 0)
            {
                TrueBranch = new ChoiceNodeViewModel(node.Children[0], this);
            }

            // Initialize FalseBranch if it exists
            if (node.FalseBranch != null)
            {
                FalseBranch = new ChoiceNodeViewModel(node.FalseBranch, this);
            }
        }
        else
        {
            // Set orientation based on node type
            if (NodeType == ChoiceNodeType.IfBlock || NodeType == ChoiceNodeType.MenuBlock)
            {
                ChildrenOrientation = Orientation.Horizontal;
            }
            else
            {
                ChildrenOrientation = Orientation.Vertical;
            }

            Children = new ObservableCollection<ChoiceNodeViewModel>();

            foreach (var childNode in node.Children)
            {
                Children.Add(new ChoiceNodeViewModel(childNode, this));
            }
        }
    }
}
