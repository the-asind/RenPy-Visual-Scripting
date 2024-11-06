using System.IO;
using System.Text;
using System.Windows;

namespace RenPy_VisualScripting.Models
{
    /// <summary>
    ///     Asynchronous parser of RenPy code using recursive descent to build a choice tree
    /// </summary>
    public class RenPyParser
    {
        private List<string> lines;
        
        /// <summary>
        /// Parses the RenPy script asynchronously and returns the root choice node.
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
        /// Parses the labels in the script lines and builds the choice tree.
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

            return rootNode; //TODO: fix it
        }

        /// <summary>
        /// Parses a block of lines with a given indentation level and updates the current node.
        /// </summary>
        /// <param name="index">The current index in the lines array.</param>
        /// <param name="indentLevel">The expected indentation level.</param>
        /// <param name="currentNode">The current choice node being parsed.</param>
        /// <returns>True if a new statement is encountered, otherwise false.</returns>
        private bool ParseBlock(ref int index, int indentLevel, ChoiceNode currentNode)
        {
            while (index < lines.Count)
            {
                var currentIndent = GetIndentLevel(Line(index));

                if (string.IsNullOrWhiteSpace(Line(index)))
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

                if (!IsAStatement(Line(index).Trim()))
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

                if (IsIfStatement(Line(index).Trim()))
                {
                    // Parse If-statement lines
                    ParseStatement(ref index, currentNode, currentIndent, ChoiceNodeType.IfBlock); 
                    return true;
                }
                if (IsElifStatement(Line(index).Trim()))
                {
                    ParseStatement(ref index, currentNode, currentIndent, ChoiceNodeType.IfBlock);
                    return true;
                }

                if (IsElseStatement(Line(index).Trim()))
                {
                    ParseStatement(ref index, currentNode, currentIndent, ChoiceNodeType.ElseBlock);
                    return true;
                }

                if (IsMenuStatement(Line(index).Trim()))
                {
                    currentNode.NodeType = ChoiceNodeType.MenuBlock;
                    currentNode.EndLine = index;
                    index++;
                    ParseMenuBlock(ref index, currentIndent + 1, currentNode);
                    index--;
                    currentNode.EndLine = index;
                    return true;
                }

                continue;

                string Line(int index)
                {
                    return lines[index];
                }
            }

            index--;
            currentNode.EndLine = index;
            return false;
        }

        /// <summary>
        /// Parses a statement block and updates the current node.
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
            // ParseBlock returns true only if it encounters a new statement
            while (true)
            {
                var temp = (ParseBlock(ref index, currentIndent + 1, statementNode));
                statementNode.LabelName = GetLabelName(statementNode);
                currentNode.Children.Add(statementNode);
                statementNode.StartLine = index;
                
                if (!temp)
                    break;
                index++;
                statementNode = new ChoiceNode { StartLine = index, NodeType = ChoiceNodeType.Action };
            }
        }

        /// <summary>
        /// Parses a menu block and updates the menu node.
        /// </summary>
        /// <param name="index">The current index in the lines array.</param>
        /// <param name="indentLevel">The expected indentation level.</param>
        /// <param name="menuNode">The menu choice node being parsed.</param>
        private void ParseMenuBlock(ref int index, int indentLevel, ChoiceNode menuNode)
        {
            while (index < lines.Count)
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

                if (line.EndsWith(":"))
                {
                    var choiceText = line.TrimEnd(':').Trim();
                    var choiceNode = new ChoiceNode
                        { LabelName = choiceText, StartLine = index, NodeType = ChoiceNodeType.ChoiceBlock };
                    index++;
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
        /// Determines if a line is a label and extracts the label name.
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
        /// Determines if a line is an if statement.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>True if the line is an if statement, otherwise false.</returns>
        private bool IsIfStatement(string line)
        {
            return line.TrimStart().StartsWith("if ") && line.EndsWith(":");
        }

        /// <summary>
        /// Determines if a line is an else statement.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>True if the line is an else statement, otherwise false.</returns>
        private bool IsElseStatement(string line)
        {
            return line.TrimStart().StartsWith("else") && line.EndsWith(":");
        }

        /// <summary>
        /// Determines if a line is an elif statement.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>True if the line is an elif statement, otherwise false.</returns>
        private bool IsElifStatement(string line)
        {
            return line.TrimStart().StartsWith("elif ") && line.EndsWith(":");
        }

        /// <summary>
        /// Determines if a line is a statement.
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
        /// Determines if a line is a menu statement.
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>True if the line is a menu statement, otherwise false.</returns>
        private bool IsMenuStatement(string line)
        {
            var trimmedLine = line.TrimStart();
            return trimmedLine.StartsWith("menu") && trimmedLine.EndsWith(":");
        }

        /// <summary>
        /// Gets the indentation level of a line.
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
            if (lines[node.StartLine].EndsWith(":"))
                return lines[node.StartLine].Substring(0, lines[node.StartLine].Length - 1).Trim();
            var label = new StringBuilder();
            for (var i = node.StartLine; i <= node.EndLine; i++)
            {
                label.Append(lines[i].Trim());
                label.Append('\n');
            }

            return label.ToString();
        }
    }
}