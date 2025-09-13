using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 6;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private float tileSize = 1.0f;
    
    private Tile[,] tiles;
    
    public int Width => width;
    public int Height => height;
    public Tile[,] Tiles => tiles;
    
    private void Start()
    {
        GenerateGrid();
    }
    
    public void GenerateGrid()
    {
        if (tilePrefab == null)
        {
            CreateDefaultTilePrefab();
        }
        
        tiles = new Tile[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x * tileSize, 0, y * tileSize);
                GameObject tileObject = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                
                Tile tile = tileObject.GetComponent<Tile>();
                if (tile == null)
                {
                    tile = tileObject.AddComponent<Tile>();
                }
                
                tile.Initialize(x, y);
                tiles[x, y] = tile;
            }
        }
        
        Debug.Log($"Grid generated: {width}x{height} tiles");
    }
    
    private void CreateDefaultTilePrefab()
    {
        GameObject defaultTile = GameObject.CreatePrimitive(PrimitiveType.Plane);
        defaultTile.transform.localScale = new Vector3(0.1f, 1, 0.1f);
        defaultTile.GetComponent<Renderer>().material.color = Color.green;
        tilePrefab = defaultTile;
    }
    
    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return null;
            
        return tiles[x, y];
    }
    
    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
    
    public bool CanPlaceUnitAt(int x, int y)
    {
        Tile tile = GetTile(x, y);
        return tile != null && tile.CanPlaceUnit();
    }
    
    public bool PlaceUnitAt(int x, int y, Unit unit)
    {
        Tile tile = GetTile(x, y);
        if (tile == null) return false;
        
        return tile.PlaceUnit(unit);
    }
    
    public void RemoveUnitAt(int x, int y)
    {
        Tile tile = GetTile(x, y);
        tile?.RemoveUnit();
    }
}