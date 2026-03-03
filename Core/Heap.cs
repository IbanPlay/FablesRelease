namespace CalamityFables.Core
{
    //Sebastian lague beloved
    public class Heap<T> where T : IHeapItem<T>
    {
        private T[] items;
        private int currentItemCount;
        private readonly int sizeBuffer;

        public Heap(int maxHeapSize = 1000, int resizeBuffer = 100)
        {
            items = new T[maxHeapSize];
            sizeBuffer = resizeBuffer;
        }

        public void Add(T item)
        {
            if (currentItemCount >= items.Length)
                Resize();

            item.HeapIndex = currentItemCount;
            items[currentItemCount] = item;
            SortUp(item);
            currentItemCount++;
        }

        public T PopFirst()
        {
            T firstItem = items[0];
            currentItemCount--;
            items[0] = items[currentItemCount];
            items[0].HeapIndex = 0;
            SortDown(items[0]);

            return firstItem;
        }

        public int Count => currentItemCount;

        public bool Contains(T item) => Equals(items[item.HeapIndex], item);

        public void UpdateItem(T item, bool sortDown = false)
        {
            SortUp(item);
            if (sortDown)
                SortDown(item);
        }

        public void SortUp(T item)
        {
            int parentIndex = (item.HeapIndex - 1) / 2;
            while (true)
            {
                if (parentIndex == item.HeapIndex)
                    break;

                T parentItem = items[parentIndex];
                if (item.CompareTo(parentItem) > 0)
                    Swap(item, parentItem);

                else
                    break;

                parentIndex = (item.HeapIndex - 1) / 2;
            }
        }

        public void SortDown(T item)
        {
            while (true)
            {
                int childIndexA = item.HeapIndex * 2 + 1;
                int childIndexB = item.HeapIndex * 2 + 2;
                int swapIndex = 0;

                if (childIndexA < currentItemCount)
                {
                    swapIndex = childIndexA;
                    if (childIndexB < currentItemCount && items[childIndexA].CompareTo(items[childIndexB]) < 0)
                        swapIndex = childIndexB;

                    if (item.CompareTo(items[swapIndex]) < 0)
                        Swap(item, items[swapIndex]);
                    else
                        break;
                }
                else
                    break;
            }
        }

        public void Swap(T itemA, T itemB)
        {
            items[itemA.HeapIndex] = itemB;
            items[itemB.HeapIndex] = itemA;
            int itemAIndex = itemA.HeapIndex;
            itemA.HeapIndex = itemB.HeapIndex;
            itemB.HeapIndex = itemAIndex;
        }

        private void Resize()
        {
            T[] newItems = new T[items.Length + sizeBuffer];
            for (int i = 0; i < items.Length; i++)
                newItems[i] = items[i];

            items = newItems;
        }

        public bool TryFind(Func<T, bool> predicate, out T find)
        {
            find = default(T);
            if (Count == 0)
                return false;

            find = items.FirstOrDefault(predicate);
            return find != null;
        }
    }

    public interface IHeapItem<T> : IComparable<T>
    {
        int HeapIndex {
            get;
            set;
        }
    }
}
