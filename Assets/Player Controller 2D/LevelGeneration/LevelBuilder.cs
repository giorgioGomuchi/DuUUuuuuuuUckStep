using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelBuilder : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap wallTilemap;

    [Header("Tiles")]
    [SerializeField] private TileBase groundTile;
    [SerializeField] private TileBase wallTile;

    public List<Vector2> Build(GeneratedGrid grid)
    {

        Debug.Log("Building tiles...");

        groundTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();

        var walkables = new List<Vector2>();

        for (int x = 0; x < grid.width; x++)
        {
            for (int y = 0; y < grid.height; y++)
            {
                var cell = new Vector3Int(x, y, 0);

                if (grid.floor[x, y])
                {
                    groundTilemap.SetTile(cell, groundTile);
                    walkables.Add(groundTilemap.GetCellCenterWorld(cell));
                }
                else if (grid.wall[x, y])
                {
                    wallTilemap.SetTile(cell, wallTile);
                }
            }
        }

        return walkables;
    }

    public void ClearLevel()
    {
        //TODO CLEAR LEVEL
    }

}
