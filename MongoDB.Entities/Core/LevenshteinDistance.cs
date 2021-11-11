namespace MongoDB.Entities;

internal class Levenshtein
{
    private readonly string _storedValue;
    private readonly int[] _costs;

    public Levenshtein(string value)
    {
        _storedValue = value.ToLower();
        _costs = new int[_storedValue.Length];
    }

    public int DistanceFrom(string value)
    {
        value = value.ToLower();

        if (_costs.Length == 0)
        {
            return value.Length;
        }

        for (int i = 0; i < _costs.Length;)
        {
            _costs[i] = ++i;
        }

        for (int i = 0; i < value.Length; i++)
        {
            int cost = i;
            int addationCost = i;

            char value1Char = value[i];

            for (int j = 0; j < _storedValue.Length; j++)
            {
                int insertionCost = cost;

                cost = addationCost;

                addationCost = _costs[j];

                if (value1Char != _storedValue[j])
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

                _costs[j] = cost;
            }
        }

        return _costs[_costs.Length - 1];
    }
}
