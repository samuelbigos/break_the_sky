using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot.Collections;

public class StructPool<T> where T: struct
{
    public int Count => _count;

    private int _count;
    private T[] _pool;
    private List<int> _available = new List<int>();

    public StructPool(int capacity)
    {
        _pool = new T[capacity];
        for (int i = 0; i < capacity; i++)
        {
            _available.Add(i);
        }
    }

    public Span<T> AsSpan()
    {
        return _pool.AsSpan(0, _count);
    }

    public int Add(T item)
    {
        int index = _available[0];
        _available.RemoveAt(0);
        _pool[index] = item;
        _count = Mathf.Max(_count, index + 1);
        return index;
    }

    public void Remove(int i)
    {
        _available.Add(i);
        _pool[i] = default;
    }
}
