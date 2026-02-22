using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GeneratedGrid
{
    public int width;
    public int height;
    public bool[,] floor;
    public bool[,] wall;

    public GeneratedGrid(int w, int h)
    {
        width = w;
        height = h;
        floor = new bool[w, h];
        wall = new bool[w, h];
    }
}

public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private Vector2 roomSizeWorldUnits = new Vector2(30, 30);
    [SerializeField] private float cellSize = 1f;

    [Header("Walkers")]
    [Range(0f, 1f)][SerializeField] private float chanceChangeDir = 0.5f;
    [Range(0f, 1f)][SerializeField] private float chanceSpawn = 0.05f;
    [Range(0f, 1f)][SerializeField] private float chanceDestroy = 0.05f;
    [SerializeField] private int maxWalkers = 10;
    [Range(0.05f, 0.8f)][SerializeField] private float percentToFill = 0.2f;
    [SerializeField] private int maxIterations = 100000;

    private struct Walker
    {
        public Vector2Int pos;
        public Vector2Int dir;
    }

    public GeneratedGrid Generate()
    {
        int height = Mathf.RoundToInt(roomSizeWorldUnits.x / cellSize);
        int width = Mathf.RoundToInt(roomSizeWorldUnits.y / cellSize);

        var grid = new GeneratedGrid(width, height);

        var walkers = new List<Walker>();
        walkers.Add(new Walker
        {
            pos = new Vector2Int(width / 2, height / 2),
            dir = RandomDir()
        });

        int iterations = 0;
        while (iterations++ < maxIterations)
        {
            // paint floors
            for (int i = 0; i < walkers.Count; i++)
                grid.floor[walkers[i].pos.x, walkers[i].pos.y] = true;

            // destroy walker
            for (int i = 0; i < walkers.Count; i++)
            {
                if (walkers.Count <= 1) break;
                if (Random.value < chanceDestroy)
                {
                    walkers.RemoveAt(i);
                    break;
                }
            }

            // change direction
            for (int i = 0; i < walkers.Count; i++)
            {
                if (Random.value < chanceChangeDir)
                {
                    var w = walkers[i];
                    w.dir = RandomDir();
                    walkers[i] = w;
                }
            }

            // spawn new walkers
            int checks = walkers.Count;
            for (int i = 0; i < checks; i++)
            {
                if (walkers.Count >= maxWalkers) break;
                if (Random.value < chanceSpawn)
                {
                    walkers.Add(new Walker
                    {
                        pos = walkers[i].pos,
                        dir = RandomDir()
                    });
                }
            }

            // move & clamp
            for (int i = 0; i < walkers.Count; i++)
            {
                var w = walkers[i];
                w.pos += w.dir;

                w.pos.x = Mathf.Clamp(w.pos.x, 1, width - 2);
                w.pos.y = Mathf.Clamp(w.pos.y, 1, height - 2);

                walkers[i] = w;
            }

            if ((float)CountFloors(grid) / (width * height) >= percentToFill)
                break;
        }

        BuildWalls(grid);
        RemoveSingleWalls(grid);
        FillVoidWithWalls(grid);

        return grid;
    }

    private int CountFloors(GeneratedGrid g)
    {
        int c = 0;
        for (int x = 0; x < g.width; x++)
            for (int y = 0; y < g.height; y++)
                if (g.floor[x, y]) c++;
        return c;
    }

    private void BuildWalls(GeneratedGrid g)
    {
        for (int x = 1; x < g.width - 1; x++)
        {
            for (int y = 1; y < g.height - 1; y++)
            {
                if (!g.floor[x, y]) continue;

                TryWall(g, x + 1, y);
                TryWall(g, x - 1, y);
                TryWall(g, x, y + 1);
                TryWall(g, x, y - 1);
            }
        }
    }

    private void TryWall(GeneratedGrid g, int x, int y)
    {
        if (!g.floor[x, y])
            g.wall[x, y] = true;
    }

    private void RemoveSingleWalls(GeneratedGrid g)
    {
        for (int x = 1; x < g.width - 1; x++)
        {
            for (int y = 1; y < g.height - 1; y++)
            {
                if (!g.wall[x, y]) continue;

                bool surroundedByFloors =
                    g.floor[x + 1, y] &&
                    g.floor[x - 1, y] &&
                    g.floor[x, y + 1] &&
                    g.floor[x, y - 1];

                if (surroundedByFloors)
                    g.wall[x, y] = false;
            }
        }
    }

    private void FillVoidWithWalls(GeneratedGrid g)
    {
        for (int x = 0; x < g.width; x++)
        {
            for (int y = 0; y < g.height; y++)
            {
                if (!g.floor[x, y] && !g.wall[x, y])
                {
                    g.wall[x, y] = true;
                }
            }
        }
    }


    private Vector2Int RandomDir()
    {
        int r = Random.Range(0, 4);
        return r switch
        {
            0 => Vector2Int.up,
            1 => Vector2Int.right,
            2 => Vector2Int.down,
            _ => Vector2Int.left
        };
    }
}
