﻿using Latios;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class AttackNodeViewSystem : SubSystem
{
    protected override void OnUpdate()
    {
        var grid = sceneBlackboardEntity.GetCollectionComponent<Grid>(true);

        if (!TryGetSingletonEntity<Hover>(out var hoverTile))
            return;

        Entities.ForEach((ref AttackNodeView attackNodeView, in AttackNodeData attackNodeData) =>
        {
            var hoverNode = GetComponent<IndexInGrid>(hoverTile).value;

            if (!grid.HasUnit(hoverNode))
            {
                EntityManager.DestroyEntity(attackNodeView.attackTileEntity);
                return;
            }

            var newAttackNode = attackNodeData.index;

            if (!attackNodeView.attackNode.Equals(newAttackNode))
            {
                attackNodeView.attackNode = newAttackNode;
                EntityManager.DestroyEntity(attackNodeView.attackTileEntity);

                attackNodeView.attackTileEntity = EntityManager.Instantiate(attackNodeView.attackTilePrefab);

                var pos = grid[attackNodeView.attackNode];
                EntityManager.SetComponentData(attackNodeView.attackTileEntity, new Translation { Value = new float3(pos.x, 0.3f, pos.y) });
            }

        }).WithStructuralChanges().Run();
    }
}