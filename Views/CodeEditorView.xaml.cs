using System.IO;
using System.Windows;

namespace RenPy_VisualScripting.Views
{
    public partial class CodeEditorView : Window
    {
        private int _startLine;
        private int _endLine;
        private string _filePath;
        private string[]? _allLines;

        public CodeEditorView(string filePath, int startLine, int endLine)
        {
            InitializeComponent();
            _filePath = filePath;
            _startLine = startLine;
            _endLine = endLine;
            LoadCodeSegment();
        }

        private void LoadCodeSegment()
        {
            _allLines = File.ReadAllLines(_filePath);
            if (_startLine - 1 < 0 || _endLine > _allLines.Length) return;
            if (_startLine - 1 < 0 || _startLine - 1 > _allLines.Length || _endLine < 0 ||
                _endLine > _allLines.Length) return;
            var codeSegment = string.Join("\n", _allLines[(_startLine - 1).._endLine]);
            CodeEditor.Text = codeSegment;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var modifiedCode = CodeEditor.Text.Split('\n');
            for (int i = _startLine - 1, j = 0; i < _endLine; i++, j++) 
                _allLines[i] = modifiedCode[j];
            File.WriteAllLines(_filePath, _allLines);
            this.Close();
        }
    }
}
