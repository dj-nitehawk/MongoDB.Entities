namespace MongoDB.Entities;

sealed class Levenshtein
{
    readonly string _storedValue;
    readonly int[] _costs;

    public Levenshtein(string value)
    {
        _storedValue = value.ToLower();
        _costs = new int[_storedValue.Length];
    }

    public int DistanceFrom(string value)
    {
        value = value.ToLower();

        if (_costs.Length == 0)
            return value.Length;

        for (var i = 0; i < _costs.Length;)
            _costs[i] = ++i;

        for (var i = 0; i < value.Length; i++)
        {
            var cost = i;
            var addationCost = i;

            var value1Char = value[i];

            for (var j = 0; j < _storedValue.Length; j++)
            {
                var insertionCost = cost;

                cost = addationCost;

                addationCost = _costs[j];

                if (value1Char != _storedValue[j])
                {
                    if (insertionCost < cost)
                        cost = insertionCost;

                    if (addationCost < cost)
                        cost = addationCost;

                    ++cost;
                }

                _costs[j] = cost;
            }
        }

        return _costs[^1];
    }
}