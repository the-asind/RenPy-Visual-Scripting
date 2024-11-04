using System.Windows;
using System.Windows.Controls;
using RenPy_VisualScripting.Models;
using RenPy_VisualScripting.ViewModels;
using RenPy_VisualScripting.Views;
using TreeViewItem = System.Windows.Controls.TreeViewItem;

namespace RenPy_VisualScripting;

/// <summary>
/// Interaction logic for MainView.xaml
/// </summary>
public partial class MainView : Window
{
    private string _filePath = null!;

    public MainView() 
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        DataContext = viewModel;
        viewModel.FileOpened += ViewModel_FileOpened;
        LoadChoiceTreeAsync();
    }

    private void ViewModel_FileOpened(object sender, string filePath)
    {
        _filePath = filePath;
    }

    private async void LoadChoiceTreeAsync()
    {
        var parser = new RenPyParser();
        var choiceTree = await parser.ParseAsync("example.rpy");
        DisplayLabelTabs(choiceTree);
    }

    private void DisplayLabelTabs(ChoiceNode root)
    {
        LabelTabControl.Items.Clear();

        // Create INIT tab first
        var initTab = new TabItem
        {
            Header = "INIT",
            Content = new TreeView()
        };
        var initTreeView = (TreeView)initTab.Content;
        foreach (var action in root.Actions)
        {
            initTreeView.Items.Add(new TreeViewItem { Header = action });
        }
        LabelTabControl.Items.Add(initTab);

        // Create tabs for each label
        foreach (var labelNode in root.Children)
        {
            var tabItem = new TabItem
            {
                Header = labelNode.LabelName,
                Content = new TreeView()
            };
            
            var treeView = (TreeView)tabItem.Content;
            BuildTreeForLabel(treeView.Items, labelNode);
            
            LabelTabControl.Items.Add(tabItem);
        }
    }

    private void BuildTreeForLabel(ItemCollection items, ChoiceNode node)
    {
        // Add actions as items
        foreach (var action in node.Actions)
        {
            items.Add(new TreeViewItem { Header = action });
        }

        // Add children (if-statements and menu blocks)
        foreach (var child in node.Children)
        {
            var childItem = new TreeViewItem { Header = child.LabelName };
            items.Add(childItem);

            // Recursively build the tree for this child
            BuildTreeForLabel(childItem.Items, child);
        }
    }

    private void Block_Click(object sender, RoutedEventArgs e)
    {
        var block = (ChoiceNode)((FrameworkElement)sender).DataContext;
        var editorWindow = new CodeEditorView(_filePath, block.StartLine, block.EndLine);
        editorWindow.Show();
    }
}