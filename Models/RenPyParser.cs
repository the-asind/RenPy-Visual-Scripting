using System.IO;
using System.Text;

namespace RenPy_VisualScripting.Models;

/// <summary>
///     Asynchronous parser of RenPy code using recursive descent to build a choice tree
/// </summary>
public class RenPyParser
{
    private List<string> lines;

    /// <summary>
    ///     Parses the RenPy script asynchronously and returns the root choice node.
    /// </summary>
    /// <param name="scriptPath">The path to the RenPy script file.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the root choice node.</returns>
    public async Task<ChoiceNode> ParseAsync(string scriptPath)
    {
        var scriptLines = await File.ReadAllLinesAsync(scriptPath);
        lines = scriptLines.ToList();
        return ParseLabels();
    }

    /// <summary>
    ///     Parses the labels in the script lines and builds the choice tree.
    /// </summary>
    /// <returns>The root choice node.</returns>
    private ChoiceNode ParseLabels()
    {
        var rootNode = new ChoiceNode { LabelName = "root", StartLine = 0 };
        var isFirstLabelAppear = false;
        var index = 0;
        while (index < lines.Count)
        {
            var line = lines[index];
            if (IsLabel(line, out var labelName))
            {
                if (!isFirstLabelAppear)
                {
                    isFirstLabelAppear = true;
                    var temp = new ChoiceNode
                    {
                        LabelName = "INIT",
                        StartLine = 0,
                        EndLine = index - 1,
                        NodeType = ChoiceNodeType.LabelBlock
                    };
                    temp.Children.Add(new ChoiceNode
                    {
                        LabelName = GetLabelName(temp), StartLine = 0, EndLine = index - 1,
                        NodeType = ChoiceNodeType.Action
                    });
                    rootNode.Children.Add(temp);
                }

                var labelNode = new ChoiceNode
                    { LabelName = labelName, StartLine = index, NodeType = ChoiceNodeType.LabelBlock };
                index++;
                var labelChildNode = new ChoiceNode { StartLine = index, NodeType = ChoiceNodeType.Action };

                while (ParseBlock(ref index, 1, labelChildNode))
                {
                    labelChildNode.LabelName = GetLabelName(labelChildNode);
                    labelNode.Children.Add(labelChildNode);
                    index++;
                    labelChildNode = new ChoiceNode { StartLine = index, NodeType = ChoiceNodeType.Action };
                }

                labelChildNode.LabelName = GetLabelName(labelChildNode);
                labelNode.Children.Add(labelChildNode);
                labelChildNode.StartLine = index;

                labelNode.EndLine = index - 1;
                rootNode.Children.Add(labelNode);
            }
            else
            {
                // Collect lines before the first label
                index++;
            }
        }

        return rootNode;
    }

    /// <summary>
    ///     Parses a block of lines with a given indentation level and updates the current node.
    /// </summary>
    /// <param name="index">The current index in the lines array.</param>
    /// <param name="indentLevel">The expected indentation level.</param>
    /// <param name="currentNode">The current choice node being parsed.</param>
    /// <returns>True if a new statement is encountered, otherwise false.</returns>
    private bool ParseBlock(ref int index, int indentLevel, ChoiceNode currentNode)
    {
        while (index < lines.Count)
        {
            var currentLine = lines[index];
            var currentIndent = GetIndentLevel(currentLine);

            if (string.IsNullOrWhiteSpace(currentLine))
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

            if (!IsAStatement(currentLine.Trim()))
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

            var trimmedLine = currentLine.Trim();

            if (IsIfStatement(trimmedLine))
            {
                ParseStatement(ref index, currentNode, currentIndent, ChoiceNodeType.IfBlock);
                return true;
            }

            if (IsMenuStatement(trimmedLine))
            {
                ParseMenuBlock(ref index, currentNode, currentIndent);
                return true;
            }

            // Other statements can be handled here
            index++;
        }

        index--;
        currentNode.EndLine = index;
        return false;
    }

    /// <summary>
    ///     Parses a statement block and updates the current node.
    /// </summary>
    /// <param name="index">The current index in the lines array.</param>
    /// <param name="currentNode">The current choice node being parsed.</param>
    /// <param name="currentIndent">The current indentation level.</param>
    /// <param name="nodeType">The type of the node being parsed.</param>
    private void ParseStatement(ref int index, ChoiceNode currentNode, int currentIndent,
        ChoiceNodeType nodeType)
    {
        currentNode.NodeType = nodeType;
        currentNode.EndLine = index;
        index++;
        var statementNode = new ChoiceNode { StartLine = index, NodeType = ChoiceNodeType.Action };

        // Parse the 'true' branch
        while (true)
        {
            var temp = ParseBlock(ref index, currentIndent + 1, statementNode);

            // TODO: по всей видимости, если в if есть menu, после которого заканчивается label, то цикл не прерывается. Прим. ln.7443-7581
            statementNode.LabelName = GetLabelName(statementNode);
            currentNode.Children.Add(statementNode);
            if (!temp)
                break;
            index++;
            statementNode = new ChoiceNode { StartLine = index, NodeType = ChoiceNodeType.Action };
        }

        statementNode.EndLine = index;

        // Check for 'elif' or 'else' at the same indentation level
        while (index + 1 < lines.Count)
        {
            index++;
            var nextLine = lines[index];
            var nextIndent = GetIndentLevel(nextLine);
            var nextLineTrimmed = nextLine.Trim();

            if (string.IsNullOrWhiteSpace(nextLine))
                continue;

            if (nextIndent != currentIndent)
            {
                index--;
                break;
            }

            if (IsElifStatement(nextLineTrimmed))
            {
                // Parse 'elif' as FalseBranch
                var falseBranchNode = new ChoiceNode { StartLine = index };
                ParseStatement(ref index, falseBranchNode, currentIndent, ChoiceNodeType.IfBlock);
                if (falseBranchNode == null) return;
                falseBranchNode.LabelName = GetLabelName(falseBranchNode);
                currentNode.FalseBranch = falseBranchNode;
                return;
            }

            if (IsElseStatement(nextLineTrimmed))
            {
                // Parse 'else' as FalseBranch
                var falseBranchNode = new ChoiceNode { StartLine = index };
                ParseStatement(ref index, falseBranchNode, currentIndent, ChoiceNodeType.ElseBlock);
                if (falseBranchNode == null) return;
                falseBranchNode.LabelName = GetLabelName(falseBranchNode);
                currentNode.FalseBranch = falseBranchNode;
                return;
            }

            index--;
            break;
        }
    }

    /// <summary>
    ///     Parses a menu block and updates the menu node.
    /// </summary>
    /// <param name="index">The current index in the lines array.</param>
    /// <param name="indentLevel">The expected indentation level.</param>
    /// <param name="menuNode">The menu choice node being parsed.</param>
    private void ParseMenuBlock(ref int index, ChoiceNode menuNode, int indentLevel)
    {
        // сперва мы встречаем строку "menu:"
        menuNode.StartLine = index;
        menuNode.EndLine = index;
        menuNode.NodeType = ChoiceNodeType.MenuBlock;
        index++;
        while (index < lines.Count)
        {
            var line = lines[index];
            var currentIndent = GetIndentLevel(line);

            if (string.IsNullOrWhiteSpace(line))
            {
                index++;
                continue;
            }

            // если встречаем строку с отступом меньше, то выходим
            if (currentIndent <= indentLevel)
            {
                index--;
                return;
            }


            // если строка с двойным отступом или больше, то выходим из меню

            line = line.Trim();
            if (index > 7542) index = index;
            // затем мы должны предположить отступ +1 и найти все варианты выбора
            if (line.StartsWith('\"') && line.EndsWith(':'))
            {
                var choiceNode = new ChoiceNode
                {
                    LabelName = line.TrimEnd(':').Trim(), StartLine = index,
                    NodeType = ChoiceNodeType.MenuOption
                };
                ParseStatement(ref index, choiceNode, currentIndent, ChoiceNodeType.Action);
                menuNode.Children.Add(choiceNode);
            }
            else
            {
                index++;
            }
        }
    }

    /// <summary>
    ///     Determines if a line is a label and extracts the label name.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <param name="labelName">The extracted label name.</param>
    /// <returns>True if the line is a label, otherwise false.</returns>
    private static bool IsLabel(string line, out string labelName)
    {
        line = line.Trim();
        if (line.StartsWith("label ") && line.EndsWith(":"))
        {
            labelName = line.Substring(6, line.Length - 7).Trim();
            return true;
        }

        labelName = null!;
        return false;
    }

    /// <summary>
    ///     Determines if a line is an if statement.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>True if the line is an if statement, otherwise false.</returns>
    private bool IsIfStatement(string line)
    {
        return line.TrimStart().StartsWith("if ") && line.EndsWith(":");
    }

    /// <summary>
    ///     Determines if a line is an else statement.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>True if the line is an else statement, otherwise false.</returns>
    private bool IsElseStatement(string line)
    {
        return line.TrimStart().StartsWith("else") && line.EndsWith(":");
    }

    /// <summary>
    ///     Determines if a line is an elif statement.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>True if the line is an elif statement, otherwise false.</returns>
    private bool IsElifStatement(string line)
    {
        return line.TrimStart().StartsWith("elif ") && line.EndsWith(":");
    }

    /// <summary>
    ///     Determines if a line is a statement.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>True if the line is a statement, otherwise false.</returns>
    private bool IsAStatement(string line)
    {
        var trimmedLine = line.TrimStart();
        return trimmedLine.StartsWith("if ") || trimmedLine.StartsWith("elif ") || trimmedLine.StartsWith("else") ||
               trimmedLine.StartsWith("menu");
    }

    /// <summary>
    ///     Determines if a line is a menu statement.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>True if the line is a menu statement, otherwise false.</returns>
    private bool IsMenuStatement(string line)
    {
        var trimmedLine = line.TrimStart();
        return trimmedLine.StartsWith("menu") && trimmedLine.EndsWith(":");
    }

    /// <summary>
    ///     Gets the indentation level of a line.
    /// </summary>
    /// <param name="line">The line to check.</param>
    /// <returns>The indentation level.</returns>
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

    private string GetLabelName(ChoiceNode node)
{
    // if in first line there was a ":" at end of line
    if (IsAStatement(lines[node.StartLine]) && lines[node.StartLine].EndsWith(':'))
        return lines[node.StartLine][..(lines[node.StartLine].Length - 1)].Trim();

    var label = new StringBuilder();
    var totalLines = node.EndLine - node.StartLine + 1;

    if (totalLines > 14)
    {
        // Append first 6 non-empty lines
        var appendedLines = 0;
        for (var i = node.StartLine; i <= node.EndLine && appendedLines < 6; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            label.Append(lines[i].Trim());
            label.Append('\n');
            appendedLines++;
        }

        // Append the placeholder line
        label.Append("<...>\n");

        // Append last 6 non-empty lines
        appendedLines = 0;
        for (var i = node.EndLine; i >= node.StartLine && appendedLines < 6; i--)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            label.Append(lines[i].Trim());
            label.Append('\n');
            appendedLines++;
        }
    }
    else
    {
        for (var i = node.StartLine; i <= node.EndLine; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            label.Append(lines[i].Trim());
            label.Append('\n');
        }
    }

    return label.ToString();
}
}