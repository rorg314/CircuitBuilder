using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum EntityType { WirePiece, Component, Misc }
// None represents the null value of EntityName
public enum EntityName { None, SimpleWire, SimpleResistor, Battery, RectRot, SquareRot }

public enum Charge { Pos, Neg}

public enum Dir { N, E, S, W, Null} //Direction the component (pins) are facing 
public class Entity {
    // Wire, component etc
    public EntityType entityType;
    // Specific entity name
    public EntityName entityName;

    //////////////////////// TILE INFO ////////////////////////

    //Acts as pivot, always in bottom left corner
    public Tile rootTile { get; set; }
    //Defines the positive and negative terminal locations of this entity 
    //public Vector3Int posTileOffset;
    //public Vector3Int negTileOffset;

    public Terminal posTerminal;
    public Terminal negTerminal;
    public Terminal[] terminals;


    // Dir pos terminal is facing
    public Dir posDir;
    // Dir neg terminal is facing
    public Dir negDir;
    // Offsets for all covered tiles
    public List<Vector3Int> allTileOffsets;

    // The x and z widths of this component (swap when rotating)
    public int Xspan { get; set; }
    public int Zspan { get; set; }
    
    public List<Tile> installedTiles;

    //////////////////////// BUILDING ////////////////////////

    // Direction this entity (components only) is facing (default is N) 
    public Dir buildDir;

    public string neighbourWiresString;
    
    public bool isConnected;

    public Sprite sprite;
    public GameObject entity_go;
    public List<GameObject> terminalList_go;

    // True if this entity should be a ghost
    public bool isGhost;
    // True if the entity can't be built (for displaying red sprite)
    public bool cantBuild;

    //////////////////////// PHYSICS ////////////////////////

    // Resistance of the entity - determined on creation
    public float resistance;

    //Voltage drop accross the entity (calculated for components, fixed for battery)
    public float voltage;

    // Current flowing through this entity
    public float current;

    // Circuit segment that contains this entity 
    public Circuit.Segment circSeg;
    // If this entity is a junction
    public Circuit.Junction circJunc;
    // The arrow showing the direction of the segment (single tile)
    public GameObject segArrow;
    

    public class Terminal {
        // Entity this terminal is attached to 
        public Entity entity;
        // Terminal charge
        public Charge charge;
        // Terminal offset from the entity root
        public Vector3Int terminalOffset;
        // Tile of the terminal
        public Tile terminalTile;
        // Tile that will cause a wire to connect
        public Tile connectTile;
        // Terminal direction
        public Dir terminalDir;

        public Terminal(Entity entity, Charge charge, Vector3Int terminalOffset, Dir terminalDir) {
            this.entity = entity;
            this.charge = charge;
            this.terminalOffset = terminalOffset;
            this.terminalDir = terminalDir;

            
        }

        // Copy constructor for building instance from prototype
        public Terminal(Entity newEntity, Terminal other) {
            this.entity = newEntity;
            this.charge = other.charge;
            this.terminalOffset = other.terminalOffset;
            this.terminalDir = other.terminalDir;


        }

        

        public void setTerminalTiles() {

            terminalTile = TileController.instance.tileGrid.GetGridObject(entity.rootTile.getTileCoordinates() + terminalOffset);
            connectTile = TileController.instance.getAdjacentTileInDir(terminalTile, terminalDir);
        }


    }
    



    // Constructor without terminal info (for wires)
    public Entity(EntityType type, EntityName name, int X, int Z, float resistance) {
        this.entityType = type;
        this.entityName = name;
        this.Xspan = X;
        this.Zspan = Z;
        this.resistance = resistance;
        this.buildDir = Dir.N;



        this.allTileOffsets = this.getTileOffsets();

    }
    // Terminal info constructor for components
    public Entity(EntityType type, EntityName name, int X, int Z, float resistance, Vector3Int posT, Vector3Int negT, Dir posDir, Dir negDir) {
        this.entityType = type;
        this.entityName = name;
        this.Xspan = X;
        this.Zspan = Z;
        this.resistance = resistance;

        // Terminals //

        posTerminal = new Terminal(this, Charge.Pos, posT, posDir);
        negTerminal = new Terminal(this, Charge.Neg, negT, negDir);
        terminals = new Terminal[] { posTerminal, negTerminal };
        
        //this.posTileOffset = posT;
        //this.negTileOffset = negT;
        //this.posDir = posDir;
        //this.negDir = negDir;


        // Set default build dir
        this.buildDir = Dir.N;
        
        // Calculate all tile offsets
        this.allTileOffsets = this.getTileOffsets();

    }
    // For batteries
    public Entity(EntityType type, EntityName name, int X, int Z, float resistance, Vector3Int posT, Vector3Int negT, Dir posDir, Dir negDir, float volts) {
        this.entityType = type;
        this.entityName = name;
        this.Xspan = X;
        this.Zspan = Z;
        this.resistance = resistance;

        posTerminal = new Terminal(this, Charge.Pos, posT, posDir);
        negTerminal = new Terminal(this, Charge.Neg, negT, negDir);
        terminals = new Terminal[]{ posTerminal, negTerminal };

        //this.posTileOffset = posT;
        //this.negTileOffset = negT;
        //this.posDir = posDir;
        //this.negDir = negDir;


        this.buildDir = Dir.N;
        this.voltage = volts;


        this.allTileOffsets = this.getTileOffsets();

    }

    // Copy constructor 
    public Entity(Entity other) {
        this.resistance = other.resistance;
        this.Xspan = other.Xspan;
        this.Zspan = other.Zspan;
        this.entityType = other.entityType;
        this.entityName = other.entityName;
        
        //if(other.entityType == EntityType.Component) {
        //    this.posTileOffset = other.posTileOffset;
        //    this.negTileOffset = other.negTileOffset;
        //    this.posDir = other.posDir;
        //    this.negDir = other.negDir;
        //}

        if(other.entityType == EntityType.Component) {

            this.posTerminal = new Terminal(this, other.posTerminal);
            this.negTerminal = new Terminal(this, other.negTerminal);
            this.terminals = new Terminal[] { this.posTerminal, this.negTerminal};

        }
        
        this.allTileOffsets = other.allTileOffsets;
        // Must create new list to avoid reference persisting to old list on prototype
        this.terminalList_go = new List<GameObject>();
                
        this.buildDir = other.buildDir;
        
        this.neighbourWiresString = other.neighbourWiresString;
    }
    
    virtual public Entity Clone() {
        return new Entity(this);
    }

    

   public Entity placeEntityInstance(Tile rootTile, Entity proto, bool isGhost, bool cantBuild, Dir buildDir) {

        Entity entity = proto.Clone();

        entity.rootTile = rootTile;

        // Set terminal tile info
        if(entity.entityType == EntityType.Component) {
            foreach (Terminal term in entity.terminals) {
                term.setTerminalTiles();
            }
        }
        
        entity.isGhost = isGhost;
        entity.cantBuild = cantBuild;
        entity.buildDir = buildDir;

        return entity;
    }

   

    // Get the offsets for all covered grid points
    public List<Vector3Int> getTileOffsets() {

        List<Vector3Int> offsets = new List<Vector3Int>();

        for (int i = 0; i < this.Xspan; i++) {
            for (int j = 0; j < this.Zspan; j++) {

                offsets.Add( new Vector3Int(i, 0, j));
                
            }
        }
        return offsets;
    }

    // Overload for specifying rootTile
    public List<Tile> getCoveredTiles(Tile rootTile) {

        List<Tile> coveredTiles = new List<Tile>();

        Vector3Int rootPosXZ = rootTile.getTileCoordinates();
        
        foreach (Vector3Int offset in this.allTileOffsets) {

            Vector3Int tileXZ = rootPosXZ + offset;
            Tile tile = TileController.instance.tileGrid.GetGridObject(tileXZ);
            coveredTiles.Add(tile);
            
        }

        return coveredTiles;
    }

    // Gets surrounding tiles for an entity (double radius to include two rows of tiles)
    public List<Tile> getSurroundingTiles(bool doubleRadius) {

        List<Tile> tiles = new List<Tile>();
        Vector3Int rootCoords = rootTile.getTileCoordinates();
        
        // Go along entire bottom and top row (starting from -1 to get diag corners)
        for (int i = -1; i <= Xspan; i++) {

            Vector3Int bottomCoord = rootCoords + new Vector3Int(i, 0, - 1);
            Vector3Int topCoord = rootCoords + new Vector3Int(i, 0, Zspan );

            if (doubleRadius) {

                Vector3Int nextBottomCoord = rootCoords + new Vector3Int(i, 0, -2);
                Vector3Int nextTopCoord = rootCoords + new Vector3Int(i, 0, Zspan + 1);
                tiles.Add(TileController.instance.tileGrid.GetGridObject(nextBottomCoord));
                tiles.Add(TileController.instance.tileGrid.GetGridObject(nextTopCoord));

            }

            tiles.Add(TileController.instance.tileGrid.GetGridObject(bottomCoord));
            tiles.Add(TileController.instance.tileGrid.GetGridObject(topCoord));
        }
        // Fill in remaining side rows (diag corners already added)
        for (int j = 0; j < Zspan; j++) {
            Vector3Int leftCoord = rootCoords + new Vector3Int(- 1, 0, j);
            Vector3Int rightCoord = rootCoords + new Vector3Int(Xspan , 0, j);
            tiles.Add(TileController.instance.tileGrid.GetGridObject(leftCoord));
            tiles.Add(TileController.instance.tileGrid.GetGridObject(rightCoord));

            if (doubleRadius) {
                Vector3Int nextLeftCoord = rootCoords + new Vector3Int(-2, 0, j);
                Vector3Int nextRightCoord = rootCoords + new Vector3Int(Xspan + 1, 0, j);
                tiles.Add(TileController.instance.tileGrid.GetGridObject(nextLeftCoord));
                tiles.Add(TileController.instance.tileGrid.GetGridObject(nextRightCoord));

            }

        }

        return tiles;
    }

    


    public void registerInstalledTiles() {

        List<Tile> tiles = this.getCoveredTiles(this.rootTile);

        foreach(Tile t in tiles) {

            t.installedEntity = this;
            t.triggerTileChanged();

        }

    }

    public void unregisterInstalledTiles() {

        List<Tile> tiles = this.getCoveredTiles(this.rootTile);

        foreach (Tile t in tiles) {

            t.installedEntity = null;
            t.triggerTileChanged();

        }

    }

    public override string ToString() {
        return entityName.ToString() + "_(" + rootTile.x.ToString() + ", " + rootTile.z.ToString() + ")";
    }

    

}
