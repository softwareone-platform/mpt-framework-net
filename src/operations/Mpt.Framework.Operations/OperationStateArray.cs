namespace Mpt.Framework.Operations;

internal class OperationStateArray
{
    private readonly byte[] _data;
    private readonly int _length;

    public int Length => _length;

    public OperationStateArray(byte[] data, int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");

        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < (length + 3) / 4)
            throw new ArgumentOutOfRangeException(nameof(data), "Byte array is too small for the specified length.");

        _data = data;
        _length = length;
    }

    public OperationStateArray(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");

        _data = new byte[(length + 3) / 4];
        _length = length;
    }

    public void Set(int index, ItemState state)
    {
        if (index < 0 || index >= _length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be within array length");

        var byteIndex = index / 4;
        var bitOffset = index % 4 * 2;
        _data[byteIndex] &= (byte)~(0b11 << bitOffset);
        _data[byteIndex] |= (byte)(((byte)state & 0b11) << bitOffset);
    }

    public ItemState Get(int index)
    {
        if (index < 0 || index >= _length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be within array length");

        var byteIndex = index / 4;
        var bitOffset = index % 4 * 2;
        return (ItemState)(_data[byteIndex] >> bitOffset & 0b11);
    }

    public byte[] Data => _data;

    /// <summary>
    /// Returns a dictionary with counts of each state, iterating bytes directly.
    /// </summary>
    public Dictionary<ItemState, int> GetCounters()
    {
        int pending = 0, succeeded = 0, failed = 0;

        var fullBytes = _length / 4;
        var remainingItems = _length % 4;

        for (var i = 0; i < fullBytes; i++)
        {
            var b = _data[i];
            for (var j = 0; j < 4; j++)
                Increment((ItemState)(b >> j * 2 & 0b11));
        }

        if (remainingItems > 0)
        {
            var b = _data[fullBytes];
            for (var j = 0; j < remainingItems; j++)
                Increment((ItemState)(b >> j * 2 & 0b11));
        }

        return new Dictionary<ItemState, int>
        {
            [ItemState.Pending] = pending,
            [ItemState.Succeeded] = succeeded,
            [ItemState.Failed] = failed
        };

        void Increment(ItemState state)
        {
            switch (state)
            {
                case ItemState.Pending: pending++; break;
                case ItemState.Succeeded: succeeded++; break;
                case ItemState.Failed: failed++; break;
            }
        }
    }
}

public enum ItemState : byte
{
    Pending = 0,
    Succeeded = 1,
    Failed = 2
}
