using System.Globalization;
using System.Text;

// This is a presentation helper for this sample's sequential graph, not another
// orchestrator. It uses the same stage order as WorkflowBuilder and literal
// observed text; the model neither chooses edges nor generates the diagram.
internal static class WorkflowDiagram
{
    internal static string Render(IReadOnlyList<(string Name, string Role, string Output)> stages)
    {
        if (stages.Count == 0)
            throw new ArgumentException("A workflow diagram needs at least one stage.", nameof(stages));

        var diagram = new StringBuilder("flowchart TD\n");
        for (int i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            diagram.AppendLine($"  step{i}[\"{Escape(stage.Name)}<br/>{Escape(stage.Role)}<br/>Output: {Escape(Preview(stage.Output))}\"]");
        }
        for (int i = 1; i < stages.Count; i++)
            diagram.AppendLine($"  step{i - 1} --> step{i}");
        return diagram.ToString();
    }

    internal static string Preview(string text)
    {
        // Whitespace normalization keeps streamed newlines and Markdown readable
        // in a small node. This is a verbatim excerpt, not an LLM summary: omitted
        // qualifications remain available in the unchanged full console stream.
        string normalized = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
            return "(no text output observed)";

        // Count Unicode text elements, not UTF-16 code units, to avoid splitting
        // emoji or combining accents. The 240-character budget includes "...".
        int[] boundaries = StringInfo.ParseCombiningCharacters(normalized);
        if (boundaries.Length <= 240)
            return normalized;

        string prefix = normalized[..boundaries[237]];
        if (normalized[boundaries[237]] != ' ' && !prefix.EndsWith(' '))
        {
            int lastSpace = prefix.LastIndexOf(' ');
            if (lastSpace > 0)
                prefix = prefix[..lastSpace];
        }
        return prefix.TrimEnd() + "...";
    }

    private static string Escape(string text)
    {
        // Model text is untrusted diagram data. Mermaid's numeric entities keep
        // quotes, brackets, HTML, backticks and directives inside the label.
        // Stable step IDs never contain model output; only our <br/> is markup.
        var escaped = new StringBuilder();
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune) || rune.Value is ' ' or '.' or ',' or ':' or '-' or '!' or '?')
                escaped.Append(rune.ToString());
            else
                escaped.Append('#').Append(rune.Value.ToString(CultureInfo.InvariantCulture)).Append(';');
        }
        return escaped.ToString();
    }
}
