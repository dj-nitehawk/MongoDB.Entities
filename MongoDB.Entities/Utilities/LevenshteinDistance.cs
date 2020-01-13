namespace MongoDB.Entities
{
    internal class Levenshtein
    {
        private readonly string storedValue;
        private readonly int[] costs;

        public Levenshtein(string value)
        {
            storedValue = value.ToLower();
            costs = new int[storedValue.Length];
        }

        public int DistanceFrom(string value)
        {
            value = value.ToLower();

            if (costs.Length == 0)
            {
                return value.Length;
            }

            for (int i = 0; i < costs.Length;)
            {
                costs[i] = ++i;
            }

            for (int i = 0; i < value.Length; i++)
            {
                int cost = i;
                int addationCost = i;

                char value1Char = value[i];

                for (int j = 0; j < storedValue.Length; j++)
                {
                    int insertionCost = cost;

                    cost = addationCost;

                    addationCost = costs[j];

                    if (value1Char != storedValue[j])
                    {
                        if (insertionCost < cost)
                        {
                            cost = insertionCost;
                        }

                        if (addationCost < cost)
                        {
                            cost = addationCost;
                        }

                        ++cost;
                    }

                    costs[j] = cost;
                }
            }

            return costs[costs.Length - 1];
        }
    }
}
