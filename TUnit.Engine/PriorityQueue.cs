#if !NET

using C5;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Engine;

class PriorityQueue<TElement, TPriority>
{
    struct Element : IComparer<Element>
    {
        public TElement Value;
        public TPriority Priority;

        public Element(TElement value, TPriority priority)
        {
            Value = value;
            Priority = priority;
        }
        public int Compare(Element x, Element y)
        {
            return Comparer<TPriority>.Default.Compare(x.Priority, y.Priority);
        }
    }

    private IPriorityQueue<Element> list;

    public PriorityQueue()
    {
        this.list = new C5.IntervalHeap<Element>();
    }

    public void Enqueue(TElement item, TPriority priority)
    {
        list.Add(new Element(item, priority));
    }

    public bool TryDequeue([MaybeNullWhen(false)] out TElement item, [MaybeNullWhen(false)] out TPriority priority)
    {
        if (list.Count > 0)
        {
            Element element = list.DeleteMin();
            item = element.Value;
            priority = element.Priority;
            return true;
        }

        item = default;
        priority = default;
        return false;
    }
}

#endif
