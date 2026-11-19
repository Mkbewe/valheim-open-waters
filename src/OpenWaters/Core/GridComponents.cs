using System.Collections.Generic;

namespace OpenWaters
{
    internal static class GridComponents
    {
        public static int[] Label(bool[] land, int size, out int count)
        {
            int[] labels = new int[land.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = -1;
            }

            Stack<int> stack = new Stack<int>();
            count = 0;
            for (int start = 0; start < land.Length; start++)
            {
                if (!land[start] || labels[start] >= 0)
                {
                    continue;
                }

                int label = count;
                labels[start] = label;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int index = stack.Pop();
                    int x = index % size;
                    int y = index / size;
                    PushIfLand(land, labels, stack, size, x - 1, y, label);
                    PushIfLand(land, labels, stack, size, x + 1, y, label);
                    PushIfLand(land, labels, stack, size, x, y - 1, label);
                    PushIfLand(land, labels, stack, size, x, y + 1, label);
                }
                count++;
            }

            return labels;
        }

        private static void PushIfLand(
            bool[] land,
            int[] labels,
            Stack<int> stack,
            int size,
            int x,
            int y,
            int label)
        {
            if (x < 0 || x >= size || y < 0 || y >= size)
            {
                return;
            }
            int index = y * size + x;
            if (!land[index] || labels[index] >= 0)
            {
                return;
            }
            labels[index] = label;
            stack.Push(index);
        }
    }
}
