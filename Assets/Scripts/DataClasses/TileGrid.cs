using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CodeMonkey.Utils;

public class TileGrid<TGridObject> {

    //On grid value changed event (passes event arguments x, z of grid point)
    public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;
    //Event arguments (x, z position of changed grid point)
    public class OnGridValueChangedEventArgs : EventArgs {
        public int x;
        public int z;
    }

    private int width;
    private int height;
    private TGridObject[,] gridArray;
    private float cellSize;
    private Vector3 originPosition;

    public GameObject tileGridParent { get; set; }

    bool debug = true;
    //Grid constructor, requires func to specify type of TGridObject
    public TileGrid(GameObject parent, int width, int height, float cellSize, Vector3 originPosition, Func<TileGrid<TGridObject>, int, int, TGridObject> createGridObject) {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        this.tileGridParent = parent;
        gridArray = new TGridObject[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int z = 0; z < gridArray.GetLength(1); z++) {
                
                gridArray[x, z] = createGridObject(this, x, z);

            }
        }

        if (debug) {

            TextMesh[,] debugTextArray = new TextMesh[width, height];
            
            for (int x = 0; x < gridArray.GetLength(0); x++) {
                for (int z = 0; z < gridArray.GetLength(1); z++) {
                    debugTextArray[x, z] = UtilsClass.CreateWorldText(gridArray[x, z]?.ToString(), tileGridParent.transform, GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * .5f, 15, Color.white, TextAnchor.MiddleCenter);
                    debugTextArray[x, z].transform.Rotate(90, 0, 0, Space.Self);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 100f);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 100f);

                }
            }
            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

            OnGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) => { //updateNeighbours(eventArgs.x, eventArgs.z); };
                debugTextArray[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z]?.ToString(); 
            };
        }
    }

    public void updateNeighbours(int x, int z) {
        //Debug.Log(x + ", " + z);

    }
    

    public int GetWidth() {
        return width;
    }

    public int GetHeight() {
        return height;
    }

    public float GetCellSize() {
        return cellSize;
    }

    //Method to return world position of each grid point 
    public Vector3 GetWorldPosition(int x, int z) {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }
    public Vector3 GetWorldPosition(Vector3Int coords) {

        return GetWorldPosition(coords.x, coords.z);

    }

    public void GetXZ(Vector3 worldPosition, out int x, out int z) {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
    }
    //Check if x, z coordinates are valid
    public void TriggerGridObjectChanged(int x, int z) {
        if (x >= 0 && z >= 0 && x < width && z < height) {
            OnGridValueChanged?.Invoke(this, new OnGridValueChangedEventArgs { x = x, z = z });
        }
    }

    public void SetGridObject(int x, int z, TGridObject obj) {
        if (x >= 0 && z >= 0 && x < width && z < height) {
            gridArray[x, z] = obj;
            //debugTextArray[x, z].text = gridArray[x, z].ToString();
            OnGridValueChanged?.Invoke(this, new OnGridValueChangedEventArgs { x = x, z = z });
        }
    }

    public void SetGridObject(Vector3 worldPosition, TGridObject obj) {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        SetGridObject(x, z, obj);
    }

    public TGridObject GetGridObject(int x, int z) {
        if (x >= 0 && z >= 0 && x < width && z < height) {
            return gridArray[x, z];
        }
        else {
            return default(TGridObject);
        }
    }

    public TGridObject GetGridObject(Vector3Int coords) {

        return GetGridObject(coords.x, coords.z);
    }

    public TGridObject GetGridObject(Vector3 worldPosition) {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        return GetGridObject(x, z);
    }
}
