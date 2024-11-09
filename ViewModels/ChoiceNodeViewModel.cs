using System.Collections.ObjectModel;
using RenPy_VisualScripting.Models;
using System.ComponentModel;
using System.Windows.Controls;

namespace RenPy_VisualScripting.ViewModels;

public class ChoiceNodeViewModel : INotifyPropertyChanged
{
    public string LabelName { get; }
    public ChoiceNodeType NodeType { get; }
    public ChoiceNodeViewModel Parent { get; }
    public ObservableCollection<ChoiceNodeViewModel> Children { get; }

    public double NodeWidth { get; }
    public Orientation ChildrenOrientation { get; }

    public event PropertyChangedEventHandler PropertyChanged;

    public ChoiceNodeViewModel(ChoiceNode node, ChoiceNodeViewModel parent = null)
    {
        LabelName = node.LabelName;
        NodeType = node.NodeType;
        Parent = parent;

        // Set NodeWidth to half of parent's width or default value
        NodeWidth = parent != null ? parent.NodeWidth / 2 : 2000;

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
