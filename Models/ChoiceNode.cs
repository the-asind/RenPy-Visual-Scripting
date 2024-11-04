namespace RenPy_VisualScripting.Models;

/// <summary>
/// Class for representing selection tree nodes and node types
/// </summary>
/// <param></param>
/// <returns></returns>
public class ChoiceNode
{
    public string LabelName { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public List<ChoiceNode> Children { get; set; } = [];
    public List<string> Actions { get; set; } = [];
    public ChoiceNodeType NodeType { get; set; }
}

public enum ChoiceNodeType
{
    Action,
    LabelBlock,
    IfBlock,
    ElseBlock,
    MenuBlock,
    ChoiceBlock
}