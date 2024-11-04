using System.IO;

namespace RenPy_VisualScripting.Models;

/// <summary>
///     Asynchronous parser of RenPy code using recursive descent to build a choice tree
/// </summary>
/// <param></param>
/// <returns></returns>
public class RenPyParser
{
    public async Task<ChoiceNode> ParseAsync(string scriptPath)
    {
        var scriptLines = await File.ReadAllLinesAsync(scriptPath);
        return ParseLabels(scriptLines);
    }

    private ChoiceNode ParseLabels(string[] lines)
    {
        var rootNode = new ChoiceNode { LabelName = "INIT", StartLine = 0 };
        var isFirstLabelAppear = false;
        var index = 0;
        while (index < lines.Length)
        {
            var line = lines[index];
            if (IsLabel(line, out var labelName))
            {
                if (!isFirstLabelAppear)
                {
                    isFirstLabelAppear = true;
                    rootNode.Children.Add(new ChoiceNode
                    {
                        LabelName = "INIT", 
                        StartLine = 0, 
                        EndLine = index-1, 
                        NodeType = ChoiceNodeType.LabelBlock
                    });
                    
                }

                var labelNode = new ChoiceNode { LabelName = labelName, StartLine = index, NodeType = ChoiceNodeType.LabelBlock};
                index++;
                var labelChildNode = new ChoiceNode { StartLine = index, NodeType = ChoiceNodeType.Action };
                
                while (ParseBlock(lines, ref index, 1, labelChildNode))
                {
                    labelChildNode.LabelName = lines[labelChildNode.StartLine].Trim();
                    labelNode.Children.Add(labelChildNode);
                    labelChildNode.StartLine = index;
                    index++;
                    labelChildNode = new ChoiceNode { StartLine = index, NodeType = ChoiceNodeType.Action };
                }
                labelNode.EndLine = index - 1;
                rootNode.Children.Add(labelNode);
            }
            else
            {
                // Collect lines before the first label
                index++;
            }
        }

        return rootNode; //TODO: fix it
    }

    private bool ParseBlock(string[] lines, ref int index, int indentLevel, ChoiceNode currentNode)
    {
        while (index < lines.Length)
        {
            
            string line(int _index) => lines[_index];
            var currentIndent = GetIndentLevel(line(index));

            if (string.IsNullOrWhiteSpace(line(index)))
            {
                index++;
                continue;
            }

            if (currentIndent < indentLevel)
            {
                index--;
                currentNode.EndLine = index;
                return false;
            }
            
            if (!IsAStatement(line(index).Trim()))
            {
                index++;
                continue;
            }

            if (currentNode.StartLine != index)
            {
                index--;
                currentNode.EndLine = index;
                return true;
            }

            if (IsIfStatement(line(index).Trim()))
            {
                // Parse If-statement lineS
                ParseStatement(lines, ref index, currentNode, currentIndent, ChoiceNodeType.IfBlock);
                index++; // TODO: Check if this is necessary
                while (IsElifStatement(line(index).Trim()))
                {
                    ParseStatement(lines, ref index, currentNode, currentIndent, ChoiceNodeType.IfBlock);

                    if (!IsElseStatement(line(index).Trim())) break;
                    ParseStatement(lines, ref index, currentNode, currentIndent, ChoiceNodeType.ElseBlock);

                    break;
                }

                if (!IsElseStatement(line(index).Trim())) break;
                ParseStatement(lines, ref index, currentNode, currentIndent, ChoiceNodeType.ElseBlock);
                index--;
                currentNode.EndLine = index;
                return true;
            }

            if (IsMenuStatement(line(index).Trim()))
            {
                currentNode.NodeType = ChoiceNodeType.MenuBlock;
                currentNode.EndLine = index;
                index++;
                ParseMenuBlock(lines, ref index, currentIndent+1, currentNode);
                index--;
                currentNode.EndLine = index;
                return true;
            }
        }
        index--;
        currentNode.EndLine = index;
        return false;
    }

    private void ParseStatement(string[] lines, ref int index, ChoiceNode currentNode, int currentIndent,
        ChoiceNodeType nodeType)
    {
        ChoiceNode statementNode;
        currentNode.NodeType = nodeType;
        currentNode.EndLine = index;
        index++;
        statementNode = new ChoiceNode { StartLine = index, NodeType = ChoiceNodeType.Action };
        while (ParseBlock(lines, ref index, currentIndent+1, statementNode))
        {
            statementNode.LabelName = lines[statementNode.StartLine].Trim();
            currentNode.Children.Add(statementNode);
            statementNode.StartLine = index;
            index++;
        }
    }

    private void ParseMenuBlock(string[] lines, ref int index, int indentLevel, ChoiceNode menuNode)
    {
        while (index < lines.Length)
        {
            var line = lines[index];
            var currentIndent = GetIndentLevel(line);

            if (string.IsNullOrWhiteSpace(line))
            {
                index++;
                continue;
            }

            if (currentIndent < indentLevel)
                return;

            line = line.Trim();

            if (line.EndsWith($":"))
            {
                var choiceText = line.TrimEnd(':').Trim();
                var choiceNode = new ChoiceNode { LabelName = choiceText, StartLine = index, NodeType = ChoiceNodeType.ChoiceBlock };
                index++;
                ParseStatement(lines, ref index, choiceNode, currentIndent, ChoiceNodeType.Action);
                menuNode.Children.Add(choiceNode);
            }
            else
            {
                index++;
            }
        }
    }

    private bool IsLabel(string line, out string labelName)
    {
        line = line.Trim();
        if (line.StartsWith("label ") && line.EndsWith(":"))
        {
            labelName = line.Substring(6, line.Length - 7).Trim();
            return true;
        }

        labelName = null;
        return false;
    }

    private bool IsIfStatement(string line)
    {
        return line.TrimStart().StartsWith("if ") && line.EndsWith(":");
    }

    private bool IsElseStatement(string line)
    {
        return line.TrimStart().StartsWith("else") && line.EndsWith(":");
    }

    private bool IsElifStatement(string line)
    {
        return line.TrimStart().StartsWith("elif ") && line.EndsWith(":");
    }

    private bool IsAStatement(string line)
    {
        var trimmedLine = line.TrimStart();
        return trimmedLine.StartsWith("if ") || trimmedLine.StartsWith("elif ") || trimmedLine.StartsWith("else") ||
               trimmedLine.StartsWith("menu");
    }

    private bool IsMenuStatement(string line)
    {
        var trimmedLine = line.TrimStart();
        return trimmedLine.StartsWith("menu") && trimmedLine.EndsWith(":");
    }


    private static int GetIndentLevel(string line)
    {
        var indent = 0;
        var tabScore = 0;
        foreach (var c in line)
            switch (c)
            {
                case '\t':
                    tabScore = 0;
                    indent++;
                    break;

                case ' ':
                    tabScore++;
                    if (tabScore == 4)
                    {
                        indent++;
                        tabScore = 0;
                    }

                    break;

                default:
                    return indent;
            }

        return indent;
    }
}