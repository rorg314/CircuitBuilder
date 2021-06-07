using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EntitySpriteController : MonoBehaviour {

    public static EntitySpriteController instance;

    //Prototypes
    public Dictionary<EntityName, Entity> entityPrototypes;

    //Sprites
    public Dictionary<string, Sprite> entityNameSpriteMap;
    public Dictionary<Entity, GameObject> entityGameObjectMap;

    
    public event Action<Entity> cbOnEntityChanged;



    private void Awake() {
        instance = this;

        entityNameSpriteMap = new Dictionary<string, Sprite>();
        loadWireSprites();
        loadComponentSprites();
        entityGameObjectMap = new Dictionary<Entity, GameObject>();

        RegisterEntityPrototypes();
    }



    //Load all wire sprites
    void loadWireSprites() {

        Sprite[] wireSprites = Resources.LoadAll<Sprite>("Sprites/Wires");

        foreach (Sprite s in wireSprites) {
            //Debug.Log(s.name);

            entityNameSpriteMap.Add(s.name, s);
        }

    }
    //Load all component sprites
    void loadComponentSprites() {

        Sprite[] componentSprites = Resources.LoadAll<Sprite>("Sprites/Components");

        foreach (Sprite s in componentSprites) {
            //Debug.Log(s.name);

            entityNameSpriteMap.Add(s.name, s);
        }

    }


    public void onEntityCreated(Entity entity) {
        
        
        // Create game object
        GameObject entity_go = new GameObject();
        entityGameObjectMap.Add(entity, entity_go);
        entity.entity_go = entity_go;

        // Set scale
        entity_go.transform.localScale = new Vector3(TileController.instance.cellSize, TileController.instance.cellSize, 1f);

        // Set name
        entity_go.name = entity.ToString();
        
        // Rotate to be flat on plane
        entity_go.transform.Rotate(90, 0, 0);

        // Transform (pivot is centre)
        
        entity_go.transform.position = entity.rootTile.getTileWorldPositon() + new Vector3(entity.Xspan, 0, entity.Zspan)* 0.5f * TileController.instance.cellSize;


        // If component, rotate based on build direction
        if (entity.entityType == EntityType.Component) {

            int number = BuildController.instance.numberOfRotations(entity.buildDir);

            for (int i = 0; i < number; i++) {
                entity = rotateEntityObject(entity);
            }

        }
        

        if (entity.cantBuild == false) {
            entity.registerInstalledTiles();
        }

        //Add sprite component
        SpriteRenderer sr = entity_go.AddComponent<SpriteRenderer>();
        List<Tile> neighbours = new List<Tile>();
        // Update neighbours
        if (entity.entityType == EntityType.WirePiece) {
            neighbours = entity.rootTile.getNeighbouringTiles();
        }
        if(entity.entityType == EntityType.Component) {
            neighbours = entity.getSurroundingTiles(true);
        }
        if (neighbours != null) {
            updateNeighbourWires(neighbours);
        }
        cbOnEntityChanged += onEntityChanged;

        cbOnEntityChanged?.Invoke(entity);
    }

    public void onEntityChanged(Entity entity) {
        SpriteRenderer sr = null;
        Sprite sprite = null;
        
        // Get the sprite renderer
        if (entityGameObjectMap.ContainsKey(entity)) {
            
            sr = entityGameObjectMap[entity].GetComponent<SpriteRenderer>();

        }
        else {
            Debug.LogWarning("Trying to change entity not in dict!?" + entity.entityName);
            return;
        }
        
        if (entity.entityType == EntityType.WirePiece) {
            // Calculate the wire sprite based on neighbours
            sprite = getWireSpriteToRender(entity);
                        
        }

        if(entity.entityType == EntityType.Component) {
            if (entityNameSpriteMap.ContainsKey(entity.entityName.ToString())) {

                sprite = entityNameSpriteMap[entity.entityName.ToString()];
                
                // Battery has polarity built into sprite
                if(entity.entityName != EntityName.Battery) {
                    UiController.instance.showPolarity(entity);
                }
                
            }
            else {
                Debug.LogWarning("Component sprite not in dict");
            }

        }

        // Set the sprite
        sr.sprite = sprite;

        sr.sortingLayerName = "Entities";
        sr.sortingOrder = 0;

        if (entity.isGhost) {
            // Set the alpha to half
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.5f);
        }
        else {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
        }

        if (entity.cantBuild) {
            sr.color = new Color(1, 0, 0, 0.5f);
        }

    }

    public void onEntityRemoved(Entity entity) {

        //Debug.Log("Removing entity" + entity.ToString());

        UiController.instance.hidePolarity(entity);

        // Uninstall the entity only if it was buildable/built 
        if (entity.cantBuild == false) {
            entity.unregisterInstalledTiles();
        }

        if (entity.entityType == EntityType.WirePiece) {
            // Update neighbour wires
            List<Tile> neighbours = entity.rootTile.getNeighbouringTiles();
            updateNeighbourWires(neighbours);
        }

        Destroy(entity.entity_go);
    }

    //Get correct wire based on neighbours
    public Sprite getWireSpriteToRender(Entity wireEnt) {
        string neighbourString = getNeighbourWiresCode(wireEnt.rootTile);
        wireEnt.neighbourWiresString = neighbourString;
        string wireName = null;

        wireName = wireEnt.entityName + "_" + neighbourString;
        if (entityNameSpriteMap.ContainsKey(wireName)) {
            Sprite wireSprite = entityNameSpriteMap[wireName];
            return wireSprite;
        }
        else { Debug.Log("No such wire sprite with name: " + wireName); return null; }

    }

    public Entity rotateEntityObject(Entity entity) {

        

        // Rotate the tile offsets and swap spans
        rotateEntityOffsets(entity);

        // Rotate the terminal dirs
        entity.posDir = BuildController.instance.nextDirAClockwise(entity.posDir);
        entity.negDir = BuildController.instance.nextDirAClockwise(entity.negDir);
        // Rotate and translate the game object (if not a prototype)

        if (entity.entity_go != null) {
            entity.entity_go.transform.RotateAround(entity.rootTile.getTileWorldPositon(), Vector3.up, -90);
            entity.entity_go.transform.Translate(entity.Xspan * TileController.instance.cellSize, 0, 0, Space.World);
        }
        
        
        return entity;
    }
    
    public void rotateEntityOffsets(Entity entity) {
        // Set the rotation direction on the instance
        entity.buildDir = BuildController.instance.nextDirClockwise(entity.buildDir);

        // Swap XZ spans
        int oldZspan = entity.Zspan;
        int oldXspan = entity.Xspan;

        entity.Xspan = oldZspan;
        entity.Zspan = oldXspan;

        // Rotate all tile offsets (pivots around old root)
        List<Vector3Int> newOffsets = new List<Vector3Int>();

        Vector3Int oldPos = entity.posTileOffset;
        Vector3Int oldNeg = entity.negTileOffset;

        foreach (Vector3Int oldOffset in entity.allTileOffsets) {

            Vector3Int newOffset = new Vector3Int(-oldOffset.z, 0, oldOffset.x);

            //Translate by -1 in X to account for pivot around corner
            newOffset += new Vector3Int(-1, 0, 0);

            //Translate by new xSpan (keeps root tile in same place and always bottom left corner)
            newOffset += new Vector3Int(entity.Xspan, 0, 0);

            newOffsets.Add(newOffset);

            // Check if this offset corresponds to terminal, and thus change the new terminal offset
            if(oldOffset == oldPos) {
                entity.posTileOffset = newOffset;
            }
            if(oldOffset == oldNeg) {
                entity.negTileOffset = newOffset;
            }
        }

        entity.allTileOffsets = newOffsets;
    }

    

    // Update all the neighbouring wires 
    public void updateNeighbourWires(List<Tile> neighbours) {
        
        foreach(Tile t in neighbours) {
            if(t == null) {
                // Ignore null tiles
                continue;
            }

            if(t.installedEntity == null) {
                continue;
            }

            if(t.installedEntity.entityType == EntityType.WirePiece) {

                onEntityChanged(t.installedEntity);

            }

        }

    }


    public string getNeighbourWiresCode(Tile rootTile) {
        //Index codes for NESW
        int N = 1000;
        int E = 0100;
        int S = 0010;
        int W = 0001;
        int[] ints = { N, E, S, W };

        List<Tile> neighbourTiles = rootTile.getNeighbouringTiles();

        int neighbourCode = 0;
        for (int i = 0; i < 4; i++) {
            Tile t = neighbourTiles[i]; //NESW
            
            if(t == null) {
                // Ignore null tiles
                continue;
            }
            
            if (t.installedEntity != null ) {
                if (t.installedEntity.entityType == EntityType.WirePiece) {
                    //Found a neighbouring wire tile, record tiles in NESW order
                    neighbourCode += ints[i];
                }
                if(t.installedEntity.entityType == EntityType.Component) {
                    Vector3Int rootCoords = t.installedEntity.rootTile.getTileCoordinates();
                    // Check pos terminal (probably want to streamline this)
                    if (t.getTileCoordinates() == rootCoords + t.installedEntity.posTileOffset) {
                        // Tile is pos terminal - check orientation from this tile matches terminal dir (opposite)
                        Dir dir = TileController.instance.getNeighbourTileOrientation(rootTile, t);
                        if(BuildController.instance.oppositeDir(dir) == t.installedEntity.posDir) {
                            neighbourCode += ints[i];
                        }
                    }
                    // Check neg terminal
                    if (t.getTileCoordinates() == rootCoords + t.installedEntity.negTileOffset) {
                        // Tile is neg terminal - check orientation from this tile matches terminal dir (opposite)
                        Dir dir = TileController.instance.getNeighbourTileOrientation(rootTile, t);
                        if (BuildController.instance.oppositeDir(dir) == t.installedEntity.negDir) {
                            neighbourCode += ints[i];
                        }
                    }

                }
            }
            else { neighbourCode += 0; }
        }
        string neighbourString = null;
        if (neighbourCode != 0) {
            neighbourString = TileController.instance.quadToNESWmap[neighbourCode];
        }
        if (neighbourCode == 0) { neighbourString = "E"; }

        return neighbourString;

    }



    public void RegisterEntityPrototypes() {

        entityPrototypes = new Dictionary<EntityName, Entity>();


        entityPrototypes.Add(EntityName.SimpleWire, new Entity(
            EntityType.WirePiece,
            EntityName.SimpleWire,
            1,                          //width
            1,                          //height
            0f                          //resistance - zero for wires
            
            ));

        entityPrototypes.Add(EntityName.SimpleResistor, new Entity(
            EntityType.Component,
            EntityName.SimpleResistor,
            2,                          //width
            1,                          //height
            3f,                         //resistance
            new Vector3Int(1, 0, 0),    //posTileOffset
            Vector3Int.zero,            //negTileOffset
            Dir.E,                      //posDir
            Dir.W                       //negDir
            ));

        entityPrototypes.Add(EntityName.Battery, new Entity(
            EntityType.Component,
            EntityName.Battery,
            2,                          //width
            1,                          //height
            0f,                         //resistance - zero for battery
            new Vector3Int(1, 0, 0),    //posTileOffset
            Vector3Int.zero,            //negTileOffset
            Dir.E,                      //posDir
            Dir.W,                      //negDir
            12f                         //battery voltage
            ));

        entityPrototypes.Add(EntityName.RectRot, new Entity(
            EntityType.Component,
            EntityName.RectRot,
            4,                          //width
            3,                          //height
            5f,                         //resistance
            new Vector3Int(1, 0, 2),    //posTileOffset
            new Vector3Int(2, 0, 0),    //negTileOffset
            Dir.N,                      //posDir
            Dir.S                       //negDir
            ));

        entityPrototypes.Add(EntityName.SquareRot, new Entity(
            EntityType.Component,
            EntityName.SquareRot,
            3,                          //width
            3,                          //height
            5f,                         //resistance
            new Vector3Int(1, 0, 2),    //posTileOffset
            new Vector3Int(2, 0, 0),    //negTileOffset
            Dir.N,                      //posDir
            Dir.S                       //negDir
            ));

    }
}
