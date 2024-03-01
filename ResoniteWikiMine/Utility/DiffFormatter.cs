using System.Text;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace ResoniteWikiMine.Utility;

public static class DiffFormatter
{
    private const int ContextLines = 3;
    private const int ContextLinesDouble = ContextLines * 2;

    /// <summary>
    /// Generate a unidiff-like diff from two texts.
    /// </summary>
    public static string GenerateDiff(string oldText, string newText)
    {
        var diffBuilder = new InlineDiffBuilder();
        var model = diffBuilder.BuildDiffModel(oldText, newText);

        if (!model.HasDifferences)
            return "";

        var sb = new StringBuilder();

        sb.AppendLine("--- old");
        sb.AppendLine("+++ new");

        var oldLineIndex = 0;
        var newLineIndex = 0;
        // Go over ranges and find chunks to put in the diff
        DiffMarker? chunkStart = null;
        DiffMarker? chunkEnd = null;
        int i;
        for (i = 0; i < model.Lines.Count; i++)
        {
            var piece = model.Lines[i];

            switch (piece.Type)
            {
                case ChangeType.Inserted:
                    newLineIndex += 1;
                    chunkStart ??= MakeDiffMarker();
                    chunkEnd = MakeDiffMarker();
                    break;
                case ChangeType.Deleted:
                    oldLineIndex += 1;
                    chunkStart ??= MakeDiffMarker();
                    chunkEnd = MakeDiffMarker();
                    break;

                default:
                    newLineIndex += 1;
                    oldLineIndex += 1;
                    // Unmodified
                    if (chunkEnd is { } chunkEndVal && i - ContextLinesDouble > chunkEndVal.DiffLineIndex)
                    {
                        WriteChunk();
                    }
                    break;
            }
        }

        if (chunkStart != null)
            WriteChunk();

        return sb.ToString();

        void WriteChunk()
        {
            var oldStart = chunkStart!.Value.OldLineIndex - ContextLines;
            var newStart = chunkStart!.Value.NewLineIndex - ContextLines;
            var oldLength = chunkEnd!.Value.OldLineIndex - oldStart + ContextLines;
            var newLength = chunkEnd!.Value.NewLineIndex - newStart + ContextLines;
            sb.AppendLine($"@@ -{oldStart},{oldLength} +{newStart},{newLength} @@");

            var contextStart = Math.Max(chunkStart.Value.DiffLineIndex - ContextLines, 0);
            var contextEnd = Math.Min(chunkEnd.Value.DiffLineIndex + ContextLines + 1, model.Lines.Count);

            for (var j = contextStart; j < contextEnd; j++)
            {
                var line = model.Lines[j];
                var prefix = line.Type switch
                {
                    ChangeType.Deleted => '-',
                    ChangeType.Inserted => '+',
                    _ => ' '
                };
                sb.Append(prefix);
                sb.AppendLine(line.Text);
            }

            chunkStart = default;
            chunkEnd = default;
        }

        DiffMarker MakeDiffMarker()
        {
            return new DiffMarker(i, oldLineIndex, newLineIndex);
        }
    }

    private record struct DiffMarker(int DiffLineIndex, int OldLineIndex, int NewLineIndex);
}