using System.Diagnostics.CodeAnalysis;

#if !NET

namespace TUnit.Engine
{
    public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
    {
        private readonly List<Element> _elements = [];

        private struct Element(TElement value, TPriority priority)
        {
            public readonly TElement Value = value;
            public readonly TPriority Priority = priority;
        }

        public int Count => _elements.Count;

        public void Enqueue(TElement value, TPriority priority)
        {
            var element = new Element(value, priority);
            _elements.Add(element);
            HeapifyUp(_elements.Count - 1);
        }

        public TElement Dequeue()
        {
            if (_elements.Count == 0)
            {
                throw new InvalidOperationException("The priority queue is empty.");
            }

            var element = _elements[0];
            _elements[0] = _elements[^1];
            _elements.RemoveAt(_elements.Count - 1);
            HeapifyDown(0);

            return element.Value;
        }

        public bool TryDequeue([NotNullWhen(true)] out TElement? value, [NotNullWhen(true)] out TPriority? priority)
        {
            if (_elements.Count == 0)
            {
                value = default(TElement);
                priority = default(TPriority);
                return false;
            }

            var element = _elements[0];
            _elements[0] = _elements[^1];
            _elements.RemoveAt(_elements.Count - 1);
            HeapifyDown(0);

            value = element.Value!;
            priority = element.Priority;
            return true;
        }

        public TElement Peek()
        {
            if (_elements.Count == 0)
            {
                throw new InvalidOperationException("The priority queue is empty.");
            }

            return _elements[0].Value;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                var parentIndex = (index - 1) / 2;
                if (_elements[index].Priority.CompareTo(_elements[parentIndex].Priority) >= 0)
                {
                    break;
                }

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void HeapifyDown(int index)
        {
            while (index < _elements.Count / 2)
            {
                var leftChildIndex = 2 * index + 1;
                var rightChildIndex = 2 * index + 2;
                var smallestChildIndex = leftChildIndex;

                if (rightChildIndex < _elements.Count && _elements[rightChildIndex].Priority.CompareTo(_elements[leftChildIndex].Priority) < 0)
                {
                    smallestChildIndex = rightChildIndex;
                }

                if (_elements[index].Priority.CompareTo(_elements[smallestChildIndex].Priority) <= 0)
                {
                    break;
                }

                Swap(index, smallestChildIndex);
                index = smallestChildIndex;
            }
        }

        private void Swap(int index1, int index2)
        {
            (_elements[index1], _elements[index2]) = (_elements[index2], _elements[index1]);
        }
    }
}

#endif
