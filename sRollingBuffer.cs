namespace SlideDrum.sInputSystem
{
    public class sRollingBuffer<T>
    {
        private readonly T[] buffer;
        private int index = 0;
        private int count = 0;
        public int Count => count;
        public int Capacity => buffer.Length;
        public bool full => count == Capacity;
        public T this[int i] => Get(i);
        public sRollingBuffer(int capacity)
        {
            buffer = new T[capacity];
        }
        public void Add(T item)
        {
            buffer[index] = item;

            index = (index + 1) % buffer.Length;

            if (count < buffer.Length)
                count++;
        }

        // 0 = newest
        // 1 = previous
        // 2 = older
        public T Get(int index)
        {
            if (index >= count || index < 0)
                return default;

            int item = (this.index - 1 - index + buffer.Length) % buffer.Length;

            return buffer[item];
        }
        public T Oldest()
        {
            if (count == 0)
                return default;
            int item = index % buffer.Length;
            return buffer[item];
        }
        public T Newest()
        {
            if (count == 0)
                return default;
            return buffer[0];
        }
    }
}
