﻿using Latios;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct GridTag : IComponentData { }

public struct Grid : ICollectionComponent
{
    private NativeHashMap<int2, float2> nodePositions;

    private NativeMultiHashMap<int2, Entity> objects;

    public readonly NativeArray<int2> neighbors;

    public int NodeCount { get; private set; }

    public Grid(int nodeCount)
    {
        NodeCount = nodeCount;

        nodePositions = new NativeHashMap<int2, float2>(NodeCount, Allocator.Persistent);
        objects = new NativeMultiHashMap<int2, Entity>(NodeCount, Allocator.Persistent);


        neighbors = new NativeArray<int2>(HexTileNeighbors.Neighbors, Allocator.Persistent);
    }

    public JobHandle Dispose(JobHandle inputDeps)
    {
        var disposeDependencies = new NativeArray<JobHandle>(4, Allocator.Temp)
        {
            [0] = objects.Dispose(inputDeps),
            [1] = nodePositions.Dispose(inputDeps),
            [2] = neighbors.Dispose(inputDeps)
        };

        return JobHandle.CombineDependencies(disposeDependencies);
    }

    public Type AssociatedComponentType => typeof(GridTag);

    public NativeHashMap<int2, float2>.Enumerator GetAllNodePositions()
    {
        return nodePositions.GetEnumerator();
    }

    public float3 GetNodePosition(int2 nodeIndex)
    {
        var nodePos = nodePositions[nodeIndex];

        var position = new float3(nodePos.x, 0, nodePos.y);

        return position;
    }

    public void SetNodePosition(int2 index, float2 position)
    {
        nodePositions[index] = position;
    }

    public int2 GetNeighborNodeFromDirection(int2 currentNode, AxialDirections direction)
    {
        var dir = neighbors[(int)direction];

        var nextNode = currentNode + dir;

        return nextNode;
    }

    public void SetGridObject(int2 nodeIndex, Entity gridObject)
    {
        if (CompareObjects(nodeIndex, gridObject))
            return;

        objects.Add(nodeIndex, gridObject);
    }

    public Entity GetGridObject(int2 nodeIndex)
    {
        var gridObject = Entity.Null;

        var iterator = objects.GetValuesForKey(nodeIndex);

        if (HasGridOject(nodeIndex))
        {
            iterator.MoveNext();
            gridObject = iterator.Current;
        }

        return gridObject;
    }

    public void RemoveGridObject(int2 nodeIndex, Entity gridObject)
    {
        objects.Remove(nodeIndex, gridObject);
    }

    public bool HasGridOject(int2 nodeIndex)
    {
        var count = objects.CountValuesForKey(nodeIndex);

        return count > 0;
    }

    public bool HasNode(int2 nodeIndex)
    {
        return nodePositions.TryGetValue(nodeIndex, out var _); ;
    }

    public bool IsWalkable(int2 nodeIndex)
    {
        return HasNode(nodeIndex) && !HasGridOject(nodeIndex);
    }

    public void FindGridObjects(int2 start, int findRange, NativeList<int2> gridObjectsInRange)
    {
        var nodesInRange = HexTileNeighbors.CalculateTilesCount(findRange);

        var queue = new NativeQueue<int2>(Allocator.Temp);
        var visited = new NativeHashSet<int2>(nodesInRange, Allocator.Temp);

        queue.Enqueue(start);
        visited.Add(start);

        while (visited.Count() <= nodesInRange)
        {
            var currentNode = queue.Dequeue();

            for (int i = 0; i < neighbors.Length; i++)
            {
                var neighborNode = HexTileNeighbors.GetNeighborNode(currentNode, neighbors[i]);

                if (!visited.Add(neighborNode))
                {
                    continue;
                }

                queue.Enqueue(neighborNode);

                if (!HasGridOject(neighborNode))
                    continue;

                gridObjectsInRange.Add(neighborNode);
            }
        }

        visited.Dispose();
        queue.Dispose();
    }

    private bool CompareObjects(int2 objectANode, Entity ojbectB)
    {
        var objectsOnTile = objects.GetValuesForKey(objectANode);

        foreach (var item in objectsOnTile)
        {
            var objectsAreSame = item == ojbectB;

            if (objectsAreSame)
                return true;
        }

        return false;
    }
}

public struct TileGridData : ICollectionComponent
{
    private NativeHashMap<int2, Entity> nodeToTile;

    public TileGridData(int nodeCount)
    {
        nodeToTile = new NativeHashMap<int2, Entity>(nodeCount, Allocator.Persistent);
    }

    public Type AssociatedComponentType => typeof(GridTag);

    public JobHandle Dispose(JobHandle inputDeps)
    {
        return nodeToTile.Dispose(inputDeps);
    }

    public void InitTile(int2 nodeIndex, Entity tile)
    {
        nodeToTile.Add(nodeIndex, tile);
    }

    public Entity GetTile(int2 nodeIndex)
    {
        return nodeToTile.TryGetValue(nodeIndex, out var tile) ? tile : Entity.Null;
    }
}