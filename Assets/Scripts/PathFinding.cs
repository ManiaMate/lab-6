using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class Node
{
    public Vector2Int position;
    public bool canGoThrough;
    public Node parentNode;

    public float gCost = int.MaxValue;
    public float hCost = int.MaxValue;
    public float fCost = int.MaxValue;

    public Node(Vector2Int Position, bool CanGoThrough)
    {
        position = Position;
        canGoThrough = CanGoThrough;
        gCost = int.MaxValue;
        hCost = int.MaxValue;
        fCost = int.MaxValue;
    }

    public override string ToString()
    {
        return $"Node(Position: {position}, CanGoThrough: {canGoThrough}, gCost: {gCost}, hCost: {hCost}, fCost: {fCost})";
    }

    public override bool Equals(object obj)
    {
        if (obj is Node other)
            return position == other.position; 
        return false;
    }
}

public class PathFinding : MonoBehaviour
{
    public Vector2Int gridSize;
    public Tilemap tilemap;
    public Tilemap pipTilemap;
    public TileBase pipTile;
    public TileBase finishTile;
    public TileBase unsearchedTile;
    public TileBase searchedTile;
    public TileBase obstacleTile;
    public TileBase shortestPathTile;
    public Node[,] grid;

    public Node startNode;
    public Node endNode;
    public List<Node> finalPath = new List<Node>();

    // Start is called before the first frame update
    void Start()
    {
        CreateInitialGrid();
        SetStartAndEnd();
        print(startNode);
        print(endNode);

        StartCoroutine(FindPathway());
    }

    public List<Node> RetracePath(Node current)
    {
        if (current == startNode)
        {
            tilemap.SetTile(new Vector3Int(startNode.position.x, startNode.position.y, 0), shortestPathTile);
            return finalPath;
        }
        tilemap.SetTile(new Vector3Int(current.position.x, current.position.y, 0), shortestPathTile);
        return RetracePath(current.parentNode);
    }

    // A* implementaiton
    IEnumerator FindPathway()
    {
        // Start with only startNode
        List<Node> openList = new List<Node> { startNode };
        // searched nodes
        List<Node> closedList = new List<Node> {};
        Node previousNode = startNode;

        startNode.gCost = 0;
        startNode.hCost = Vector2Int.Distance(startNode.position, endNode.position);
        startNode.fCost = startNode.gCost + startNode.hCost;

        while (openList.Count > 0)
        {
            Node current = GetLowFCostNode(openList);

            openList.Remove(current);
            closedList.Add(current);
            tilemap.SetTile(new Vector3Int(current.position.x, current.position.y, 0), searchedTile);
            if (previousNode != null)
            {
                pipTilemap.SetTile(new Vector3Int(previousNode.position.x, previousNode.position.y, 0), null);
            }
            pipTilemap.SetTile(new Vector3Int(current.position.x, current.position.y, 0), pipTile);

            previousNode = current;
            yield return new WaitForSeconds(0.5f);

            if (current == endNode)
            {
                Debug.Log("Path found");
                RetracePath(endNode);
                yield break;
            }

            List<Node> neighbors = GetNeighbors(current);
            foreach (Node neighbor in neighbors)
            {
                if (neighbor.canGoThrough == false || closedList.Contains(neighbor)) 
                {
                    continue;
                }

                float movementUpdateCost = 0;
                if ((neighbor.position.x == current.position.x -1 && neighbor.position.y == current.position.y-1) ||
                    (neighbor.position.x == current.position.x-1 && neighbor.position.y == current.position.y+1) ||
                    (neighbor.position.x == current.position.x+1 && neighbor.position.y == current.position.y-1) ||
                    (neighbor.position.x == current.position.x+1 && neighbor.position.y == current.position.y+1))
                {
                    // Diagonal movement neighbor
                    movementUpdateCost = 1.4f;
                }
                else
                {
                    // Cardinal movement neighbor
                    movementUpdateCost = 1.0f;
                }

                if (movementUpdateCost + current.gCost < neighbor.gCost || openList.Contains(neighbor) == false)
                {
                    neighbor.hCost = Vector2Int.Distance(neighbor.position, endNode.position);
                    neighbor.gCost = current.gCost + movementUpdateCost;
                    neighbor.fCost = neighbor.hCost + neighbor.gCost;
                    neighbor.parentNode = current;

                    if (openList.Contains(neighbor) == false)
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }
        yield break;
    }

    Node GetLowFCostNode(List<Node> nodeList)
    {
        float lowest_f = int.MaxValue;
        Node finalNode = null;
        foreach(Node nod in nodeList)
        {
            if (nod.fCost < lowest_f ||  (nod.fCost == finalNode.fCost && nod.hCost < finalNode.hCost))
            {
                lowest_f = nod.fCost;
                finalNode = nod;
            }
        }
        return finalNode;
    }

    void SetStartAndEnd()
    {
        List<Node> allNodesGoThrough = new List<Node>();

        foreach(Node nod in grid)
        {
            if (nod.canGoThrough == true)
            {
                allNodesGoThrough.Add(nod);
            }
        }
        int minDistance = (gridSize.x / 2) + 1;
        startNode = allNodesGoThrough[Random.Range(0, allNodesGoThrough.Count)];
        endNode = allNodesGoThrough[Random.Range(0, allNodesGoThrough.Count)];

        while (startNode == endNode || (Mathf.Abs(startNode.position.x - endNode.position.x) + Mathf.Abs(startNode.position.y - endNode.position.y) < minDistance))
        {
            startNode = allNodesGoThrough[Random.Range(0, allNodesGoThrough.Count)];
            endNode = allNodesGoThrough[Random.Range(0, allNodesGoThrough.Count)];
        }

        pipTilemap.SetTile(new Vector3Int(startNode.position.x, startNode.position.y, 0), pipTile);
        pipTilemap.SetTile(new Vector3Int(endNode.position.x, endNode.position.y, 0), finishTile);

        int obstacleCount = 0;
        while (obstacleCount < gridSize.x + 5)
        {
            Node randomTile = allNodesGoThrough[Random.Range(0, allNodesGoThrough.Count)];
            if (randomTile == startNode || randomTile == endNode)
            {
                continue;
            }
            else
            {
                tilemap.SetTile(new Vector3Int(randomTile.position.x, randomTile.position.y, 0), obstacleTile);
                randomTile.canGoThrough = false;
                obstacleCount += 1;
            }
        }
    }

    void CreateInitialGrid()
    {
        grid = new Node[gridSize.x, gridSize.y];
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), unsearchedTile);
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                bool canGoThrough = true;
                grid[x, y] = new Node(new Vector2Int(x, y), canGoThrough);
            }
        }
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        int x = node.position.x;
        int y = node.position.y;

        // Include diagonal directions
        Vector2Int[] directions = {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, -1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(1, 1) 
        };

        foreach (var dir in directions)
        {
            Vector2Int neighborPos = new Vector2Int(x + dir.x, y + dir.y);
            if (neighborPos.x >= 0 && neighborPos.x < gridSize.x &&
                neighborPos.y >= 0 && neighborPos.y < gridSize.y)
            {
                neighbors.Add(grid[neighborPos.x, neighborPos.y]);
            }
        }
        return neighbors;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
