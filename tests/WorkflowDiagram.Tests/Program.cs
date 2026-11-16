using System.Globalization;

// A dependency-free check of the presentation boundary; no Foundry credentials
// or model calls are needed to exercise truncation and untrusted label handling.
Assert(WorkflowDiagram.Preview(" \r\n\t") == "(no text output observed)", "Empty output must be explicit.");
Assert(WorkflowDiagram.Preview("First\r\n second\t third") == "First second third", "Normalize whitespace.");
Assert(WorkflowDiagram.Preview(new string('a', 240)) == new string('a', 240), "Keep exactly 240 elements.");
Assert(WorkflowDiagram.Preview(new string('a', 241)) == new string('a', 237) + "...", "Bound long unbroken words.");
Assert(WorkflowDiagram.Preview(new string('a', 237) + " tail") == new string('a', 237) + "...",
    "Keep a complete word that fits the preview budget.");

string words = string.Join(" ", Enumerable.Repeat("recommendation", 30));
string wordPreview = WorkflowDiagram.Preview(words);
Assert(wordPreview.EndsWith("recommendation...", StringComparison.Ordinal), "Prefer whole words.");
Assert(StringInfo.ParseCombiningCharacters(wordPreview).Length <= 240, "Include ellipsis in the limit.");
Assert(words.StartsWith(wordPreview[..^3], StringComparison.Ordinal), "Keep a literal excerpt.");

foreach (string element in new[] { "\U0001F600", "e\u0301", "\U0001F469\u200D\U0001F4BB" })
{
    string preview = WorkflowDiagram.Preview(string.Concat(Enumerable.Repeat(element, 241)));
    Assert(preview == string.Concat(Enumerable.Repeat(element, 237)) + "...", "Preserve Unicode text elements.");
}

string unsafeText = "\" ] --> evil[\"<script>alert('x')</script> & #quot; ` %%{init: {}}%%\nnext";
string diagram = WorkflowDiagram.Render(
[
    ("Triage", "Diagnose", unsafeText),
    ("Review", "Check safety", "Reviewed text"),
    ("Reply", "Write answer", "")
]);
Assert(diagram.StartsWith("flowchart TD\n", StringComparison.Ordinal), "Use Mermaid flowchart syntax.");
Assert(diagram.Contains("step0 --> step1") && diagram.Contains("step1 --> step2"), "Preserve configured order.");
Assert(diagram.Split("-->", StringSplitOptions.None).Length == 3, "Model text must not add edges.");
Assert(!diagram.Contains("<script>") && !diagram.Contains("%%{") && !diagram.Contains("#quot;"),
    "Escape HTML, directives and pre-existing entities.");
Assert(diagram.Contains("#34;") && diagram.Contains("#60;") && diagram.Contains("#35;"), "Use Mermaid entities.");
Assert(diagram.Contains("Output: Reviewed text"), "Include observed output.");
Assert(diagram.Contains("#40;no text output observed#41;"), "Keep missing output distinct.");
Assert(WorkflowDiagram.Render([("Only", "Role", "Text")]).Split("-->", StringSplitOptions.None).Length == 1,
    "A single stage has no invented edges.");
try
{
    WorkflowDiagram.Render([]);
    throw new InvalidOperationException("An empty workflow must be rejected.");
}
catch (ArgumentException)
{
}

Console.WriteLine("Workflow diagram checks passed.");
Console.Write(diagram);

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
