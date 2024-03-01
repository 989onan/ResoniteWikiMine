namespace ResoniteWikiMine.Utility;

public static class LinqExtensions
{
    public static IEnumerable<T> Flatten<T>(T root, Func<T, IEnumerable<T>> selectChildren)
    {
        var stack = new Queue<T>();
        stack.Enqueue(root);

        while (stack.TryDequeue(out var element))
        {
            yield return element;

            foreach (var child in selectChildren(element))
            {
                stack.Enqueue(child);
            }
        }
    }
}