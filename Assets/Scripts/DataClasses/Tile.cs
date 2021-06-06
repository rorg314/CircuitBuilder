using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile {

    public TileGrid<Tile> tileGrid;
    //Grid coordinates of this tile
    public int x;
    public int z;

    //Ground sprite to be rendered on this tile
    Sprite tileGroundSprite;

    //Generic object installed on this tile (may be installed accross multiple tiles)
    public Entity installedEntity { get; set; }

    // Defines this tile as a wire junction
    public Circuit.WireJunction wireJunction;
    // The wire segment that contains this tile 
    public Circuit.WireSegment wireSegment;


    public Tile(TileGrid<Tile> grid, int x, int z) {
        this.tileGrid = grid;
        this.x = x;
        this.z = z;
    }


    public Vector3 getTileWorldPositon() {
        return tileGrid.GetWorldPosition(this.x, this.z);
    }

    public Vector3Int getTileCoordinates() {
        return new Vector3Int(this.x, 0, this.z);
    }

    public void triggerTileChanged() {

        tileGrid.TriggerGridObjectChanged(x, z);

    }

    //Returns array of tiles in NESW order (no diagonal)
    public Tile[] getNeighbouringTiles() {
        //North tile:
        Tile northTile = tileGrid.GetGridObject(this.x, this.z + 1);
        Tile eastTile = tileGrid.GetGridObject(this.x + 1, this.z);
        Tile southTile = tileGrid.GetGridObject(this.x, this.z - 1);
        Tile westTile = tileGrid.GetGridObject(this.x - 1, this.z);

        return new Tile[] { northTile, eastTile, southTile, westTile };

    }

    public override string ToString() {
        
        if(this.installedEntity != null) {
            return this.installedEntity.entityName.ToString() + " - " + this.x.ToString() + ", " + this.z.ToString();
        }

        return this.x.ToString() + ", " + this.z.ToString();
    }

}
