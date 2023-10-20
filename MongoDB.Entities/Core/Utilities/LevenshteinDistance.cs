namespace MongoDB.Entities;

class Levenshtein
{
    readonly string storedValue;
    readonly int[] costs;

    public Levenshtein(string value)
    {
        storedValue = value.ToLower();
        costs = new int[storedValue.Length];
    }

    public int DistanceFrom(string value)
    {
        value = value.ToLower();

        if (costs.Length == 0)
            return value.Length;

        for (var i = 0; i < costs.Length;)
            costs[i] = ++i;

        for (var i = 0; i < value.Length; i++)
        {
            var cost = i;
            var addationCost = i;

            var value1Char = value[i];

            for (var j = 0; j < storedValue.Length; j++)
            {
                var insertionCost = cost;

                cost = addationCost;

                addationCost = costs[j];

                if (value1Char != storedValue[j])
                {
                    if (insertionCost < cost)
                        cost = insertionCost;

                    if (addationCost < cost)
                        cost = addationCost;

                    ++cost;
                }

                costs[j] = cost;
            }
        }

        return costs[^1];
    }
}