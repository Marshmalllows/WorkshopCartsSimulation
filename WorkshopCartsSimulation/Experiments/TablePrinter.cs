namespace WorkshopCartsSimulation.Experiments;

internal static class TablePrinter
{
    public static void Print(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var widths = headers.Select((header, column) =>
            Math.Max(header.Length, rows.Count == 0 ? 0 : rows.Max(row => row[column].Length))).ToArray();

        Console.WriteLine(FormatRow(headers, widths));
        Console.WriteLine(string.Join("-+-", widths.Select(width => new string('-', width))));
        foreach (var row in rows)
            Console.WriteLine(FormatRow(row, widths));
    }

    private static string FormatRow(IReadOnlyList<string> cells, IReadOnlyList<int> widths) =>
        string.Join(" | ", cells.Select((cell, column) => column == 0 ? cell.PadRight(widths[column]) : cell.PadLeft(widths[column])));
}
