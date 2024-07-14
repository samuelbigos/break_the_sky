using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot.Collections;

public partial class StructPool<T> where T: SteeringManager.IPoolable
{
    /*
     * StructPool is a simple wrapper over an array of structs that allows re-use without moving around elements
     * in memory. Users of StructPool need to handle default elements.
     * 
     * _available - Maintains a stack of free elements (via index).
     * _span - Returns the size of the memory span that includes all elements. This isn't the total number of elements
     * because the array can be fragmented by removing elements in the middle.
     * 
     * * When an item is added, we get the next available index and add to that location.
     * * When an item is removed, that element is set back to default. Note: an element removed could be in the middle
     * of the array, so we can't just reduce _span or Span wouldn't return a complete list.
     */
    
    public int Count => _pool.Length - _available.Count;
    public int Span => _span;

    private int _span;
    private T[] _pool;
    private Stack<int> _available = new();

    public StructPool(int capacity)
    {
        _pool = new T[capacity];
        
        // reverse add indices to _available so we pop the lowest indices first.
        for (int i = capacity - 1; i >= 0; i--)
        {
            _available.Push(i);
        }
    }

    public Span<T> AsSpan()
    {
        return _pool.AsSpan(0, _span);
    }

    public int Add(T item)
    {
        int index = _available.Pop();
        _pool[index] = item;
        _span = Mathf.Max(_span, index + 1);
        return index;
    }

    public void Remove(int i)
    {
        _available.Push(i);
        _pool[i] = default;
        
        // if we removed the right-most element of _pool, we can reduce _span until we hit a non-empty element.
        while (_span > 0 && _pool[_span - 1].Empty())
        {
            _span--;
        }
    }
}
