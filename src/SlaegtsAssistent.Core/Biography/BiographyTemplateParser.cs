namespace SlaegtsAssistent.Core.Biography;

public sealed class BiographyTemplateParser
{
    public BiographyTemplate Parse(string source, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        var root = new List<BiographyTemplateNode>();
        var stack = new Stack<(string Kind, string Path, List<BiographyTemplateNode> Children, int Line, int Column)>();
        var position = 0;
        while (position < source.Length)
        {
            var open = source.IndexOf("{{", position, StringComparison.Ordinal);
            if (open < 0)
            {
                AddNode(root, stack, new TextTemplateNode(source[position..]));
                break;
            }

            if (open > position)
            {
                AddNode(root, stack, new TextTemplateNode(source[position..open]));
            }

            var close = source.IndexOf("}}", open + 2, StringComparison.Ordinal);
            if (close < 0)
            {
                throw Error("Skabelonudtryk mangler `}}`.", source, open, filePath);
            }

            var expression = source[(open + 2)..close].Trim();
            var (line, column) = GetLocation(source, open);
            if (expression.StartsWith("#if ", StringComparison.Ordinal))
            {
                var path = ValidatePath(expression[4..].Trim(), source, open, filePath);
                stack.Push(("if", path, [], line, column));
            }
            else if (expression.StartsWith("#each ", StringComparison.Ordinal))
            {
                var path = ValidatePath(expression[6..].Trim(), source, open, filePath);
                stack.Push(("each", path, [], line, column));
            }
            else if (expression is "/if" or "/each")
            {
                var kind = expression[1..];
                if (stack.Count == 0 || stack.Peek().Kind != kind)
                {
                    throw Error($"Uventet lukket blok `{expression}`.", source, open, filePath);
                }

                var block = stack.Pop();
                BiographyTemplateNode node = kind == "if"
                    ? new IfTemplateNode(block.Path, block.Children, block.Line, block.Column)
                    : new EachTemplateNode(block.Path, block.Children, block.Line, block.Column);
                AddNode(root, stack, node);
            }
            else if (expression.StartsWith("#", StringComparison.Ordinal))
            {
                throw Error("Ukendt skabelonblok.", source, open, filePath);
            }
            else
            {
                AddNode(
                    root,
                    stack,
                    new VariableTemplateNode(
                        ValidatePath(expression, source, open, filePath),
                        line,
                        column));
            }

            position = close + 2;
        }

        if (stack.Count > 0)
        {
            var block = stack.Peek();
            throw new BiographyTemplateException(
                $"Skabelonblokken `{block.Kind}` mangler en afslutning.",
                filePath,
                block.Line,
                block.Column);
        }

        var template = new BiographyTemplate(source, root, BiographyTemplateContract.CurrentVersion);
        BiographyTemplateContract.Validate(template, filePath);
        return template;
    }

    private static void AddNode(
        ICollection<BiographyTemplateNode> root,
        Stack<(string Kind, string Path, List<BiographyTemplateNode> Children, int Line, int Column)> stack,
        BiographyTemplateNode node)
    {
        if (stack.Count == 0)
        {
            root.Add(node);
        }
        else
        {
            stack.Peek().Children.Add(node);
        }
    }

    private static string ValidatePath(string path, string source, int position, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(path)
            || path.Any(character => !(char.IsLetterOrDigit(character) || character is '.' or '_' or '-')))
        {
            throw Error("Skabelonfeltet er ugyldigt.", source, position, filePath);
        }

        return path;
    }

    private static BiographyTemplateException Error(
        string message,
        string source,
        int position,
        string? filePath)
    {
        var (line, column) = GetLocation(source, position);
        return new BiographyTemplateException(message, filePath, line, column);
    }

    private static (int Line, int Column) GetLocation(string source, int position)
    {
        var line = 1;
        var lastLineBreak = -1;
        for (var index = 0; index < position; index++)
        {
            if (source[index] == '\n')
            {
                line++;
                lastLineBreak = index;
            }
        }

        return (line, position - lastLineBreak);
    }
}
