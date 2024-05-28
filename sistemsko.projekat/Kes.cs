using ProjekatSP;
using System;
using System.Collections.Generic;

public class LRUCache
{
    private static readonly int _capacity = 10;
    private static readonly TimeSpan _timeToLive = new TimeSpan(3600);
    private static readonly Dictionary<string, LinkedListNode<(string key, IQAir value, DateTime timestamp)>> _cacheMap = new Dictionary<string, LinkedListNode<(string key, IQAir value, DateTime timestamp)>>();
    private static readonly LinkedList<( string key, IQAir value, DateTime timestamp)> _lruList = new LinkedList<(string key, IQAir value, DateTime timestamp)>();


    public static bool Contains(string key)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            if (DateTime.Now - node.Value.timestamp > _timeToLive)
            {
                // Entry has expired
                RemoveNode(node);
                return false;
            }

            return true;
        }

        return false;
    }
    public static IQAir Get(string key)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            if (DateTime.Now - node.Value.timestamp > _timeToLive)
            {
                // Entry has expired
                RemoveNode(node);
                return default(IQAir);
            }

            _lruList.Remove(node);
            _lruList.AddFirst(node);
            return node.Value.value;
        }
        return default(IQAir);
    }

    public static void Put(string key, IQAir value)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            _lruList.Remove(node);
        }
        else
        {
            if (_cacheMap.Count >= _capacity)
            {
                RemoveExpiredOrLRU();
            }
        }

        var newNode = new LinkedListNode<(string key, IQAir value, DateTime timestamp)>((key, value, DateTime.Now));
        _lruList.AddFirst(newNode);
        _cacheMap[key] = newNode;
    }

    private static void RemoveNode(LinkedListNode<(string key, IQAir value, DateTime timestamp)> node)
    {
        _lruList.Remove(node);
        _cacheMap.Remove(node.Value.key);
    }

    private static void RemoveExpiredOrLRU()
    {
        var currentNode = _lruList.Last;
        while (currentNode != null && DateTime.Now - currentNode.Value.timestamp > _timeToLive)
        {
            var previousNode = currentNode.Previous;
            RemoveNode(currentNode);
            currentNode = previousNode;
        }

        if (currentNode != null && _cacheMap.Count >= _capacity)
        {
            RemoveNode(currentNode);
        }
    }
}
