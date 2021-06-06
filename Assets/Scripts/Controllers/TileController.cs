using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



public class TileController : MonoBehaviour {

    public static TileController instance;

    public TileGrid<Tile> tileGrid { get; set; }

    public Vector3 tileCentreOffset;

    // Invoked when mouse enters a new tile 
    public event Action<Tile> cbMouseEnterNewTile;

    // Tile at grid origin (located at (0, 0))
    public Tile zeroTile;
    // Old tile used when checking for new tile under mouse
    public Tile oldTile;
    // Centre tile (roughly)
    public Tile centreTile;

    public float cellSize = 10f;

    private void Awake() {
        instance = this;
        
        
        buildQuadNESWmap();

        int gridWidth = 25;
        int gridHeight = 25;
        //float cellSize = 10f;
        tileCentreOffset = new Vector3(cellSize, 0, cellSize) * 0.5f;
        
        
        tileGrid = new TileGrid<Tile>(gridWidth, gridHeight, cellSize, Vector3.zero, (TileGrid<Tile> g, int x, int z) => new Tile(g, x, z));

        zeroTile = new Tile(tileGrid, 0, 0);
        oldTile = zeroTile;
        centreTile = tileGrid.GetGridObject(Mathf.FloorToInt(gridWidth / 2), Mathf.FloorToInt(gridHeight / 2));
        
    }

    //Get (x, 0, z) coordinates of mouse position on ground plane (uses a raycast)
    public Vector3 getGroundMousePosition() {
        Vector3 hitPoint;

        hitPoint = getRaycastHitGroundPoint();

        return hitPoint;
    }

    public Tile getTileUnderMouse() {
        Vector3 position = getGroundMousePosition();
        Tile tile = tileGrid.GetGridObject(position);
        return tile;
    }

    // Will trigger cbMouseEnterNewTile when mouse moves into new tile
   
    public void checkForNewTileUnderMouse() {

        Tile checkTile = getTileUnderMouse();

        if(checkTile == null) {
            checkTile = zeroTile;
        }

        if(checkTile != oldTile) {
            //Moved out of the old tile - trigger the callback with the new tile
            cbMouseEnterNewTile?.Invoke(checkTile);
            //Debug.Log("callbak trig");
            oldTile = checkTile;
            //Also return the new tile 
            //return checkTile;
        }

        //Still in same tile - just return
        return; //oldTile;
    }

    public RaycastHit raycastFromScreen() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit rayHit;
        Physics.Raycast(ray, out rayHit);
        return rayHit;
    }

    public Vector3 getRaycastHitPoint() {
        RaycastHit rayHit = raycastFromScreen();
        Vector3 hit_point = rayHit.point;
        
        return hit_point;
    }

    public Transform[] getRaycastHitColliders() {
        RaycastHit rayHit = raycastFromScreen();
        if (rayHit.collider != null) {
            Transform[] hitParentTransforms = rayHit.collider.GetComponentsInParent<Transform>();
            return hitParentTransforms;
        }
        return null;
    }

    //Only get the point at which the raycast hits the ground plane collider (in layer index 8 - GroundPlane)
    public Vector3 getRaycastHitGroundPoint() {

        int layerIndex = LayerMask.GetMask("GroundPlane");
        //Debug.Log(layerIndex);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit rayHit;
        Physics.Raycast(ray, out rayHit, 1000f, layerIndex);
        Vector3 hit_point = rayHit.point;
        //Debug.Log(hit_point);

        return hit_point;
    }

    // Get the direction from startTile to endTile (NESW neighbours - at most 1 coord difference) 
    public Dir getNeighbourTileOrientation(Tile startTile, Tile endTile) {
        Dir dir = Dir.Null;
        Vector3Int startCoords = startTile.getTileCoordinates();
        Vector3Int endCoords = endTile.getTileCoordinates();

        Vector3Int dirVec = endCoords - startCoords;

        if(dirVec.x == 1) { dir = Dir.E; }
        if(dirVec.x == -1) { dir = Dir.W; }
        if (dirVec.z == 1) { dir = Dir.N; }
        if (dirVec.z == -1) { dir = Dir.S; }

        return dir;
    }

    











    public Dictionary<int, string> quadToNESWmap;
    public Dictionary<string, int> NESWtoQuadMap;

    public void buildQuadNESWmap() {
        int N = 1000;
        int E = 0100;
        int S = 0010;
        int W = 0001;
        int[] ints = { N, E, S, W };
        string[] names = { "N", "E", "S", "W" };

        quadToNESWmap = new Dictionary<int, string>();
        NESWtoQuadMap = new Dictionary<string, int>();

        for (int i = 0; i < 4; i++) {
            quadToNESWmap.Add(ints[i], names[i]);
            NESWtoQuadMap.Add(names[i], ints[i]);

        }
        //NN(unused), NE, NS, NW 
        for (int i = 1; i < 4; i++) {
            quadToNESWmap.Add(ints[0] + ints[i], "N" + names[i]);

        }

        quadToNESWmap.Add(E + S, "ES");
        NESWtoQuadMap.Add("ES", E + S);
        quadToNESWmap.Add(E + W, "EW");
        NESWtoQuadMap.Add("EW", E + W);
        quadToNESWmap.Add(S + W, "SW");
        NESWtoQuadMap.Add("SW", S + W);
        quadToNESWmap.Add(N + E + S, "NES");
        NESWtoQuadMap.Add("NES", N + E + S);
        quadToNESWmap.Add(N + E + W, "NEW");
        NESWtoQuadMap.Add("NEW", N + E + W);
        quadToNESWmap.Add(N + S + W, "NSW");
        NESWtoQuadMap.Add("NSW", N + S + W);
        quadToNESWmap.Add(E + S + W, "ESW");
        NESWtoQuadMap.Add("ESW", E + S + W);
        quadToNESWmap.Add(N + E + S + W, "NESW");
        NESWtoQuadMap.Add("NESW", N + E + S + W);

    }


}
