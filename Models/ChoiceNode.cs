namespace RenPy_VisualScripting.Models;

/// <summary>
/// Class for representing selection tree nodes and node types
/// </summary>
/// <param></param>
/// <returns></returns>
public class ChoiceNode
{
    public string LabelName { get; set; } = "ERROR";
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public List<ChoiceNode> Children { get; set; } = [];
    public ChoiceNodeType NodeType { get; set; }
    public ChoiceNode? FalseBranch { get; set; }  // Hook for the false branch of an if block
}

public enum ChoiceNodeType
{
    Action, // default block type
    LabelBlock, // block that contains a label
    IfBlock, // block that contains an if statement and have two outputs (true and false)
    ElseBlock, // block that contains an else statement (false output of an if block)
    MenuBlock, // block that contains a menu statement and have multiple outputs
    MenuOption // option of a menu block
}