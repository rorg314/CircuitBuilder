using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum BuildMode { Wire, Component}

public class BuildController : MonoBehaviour {

    public static BuildController instance;

    [HideInInspector]
    public EntityName entityNameToBuild;
    [HideInInspector]
    public Dir dirToBuild;
    [HideInInspector]
    public Entity entityUnderMouse;

    private void Awake() {
        if(instance == null) {
            instance = this;
        }
        else { 
            Debug.LogWarning("More than one BuildController!"); 
        }
        
        entityNameToBuild = EntityName.None;
        entityUnderMouse = null;

        TileController.instance.cbMouseEnterNewTile += setEntityUnderMouse;
    }

    // Triggered by cbMouseEnterNewTile - sets the entity under mouse
    public void setEntityUnderMouse(Tile tile) {

        if(tile.installedEntity != null) {
            entityUnderMouse = tile.installedEntity;

        }
        else {
            entityUnderMouse = null;
        }


    }

    // The current prototype to be built
    public Entity protoToBuild;

    // Set the current entity to build when click LMB

    public void setEntityToBuild(string name) {
        // Unregister the old callbacks (to avoid double registering)
        TileController.instance.cbMouseEnterNewTile -= previewSelectedOnTile;
        TileController.instance.cbMouseEnterNewTile -= buildSelectedOnTile;
        TileController.instance.cbMouseEnterNewTile -= removeMouseoverEntity;
        InputController.Instance.Rpressed -= cyclePreviewBuildDir;

        // Reset dir to build
        dirToBuild = Dir.N;
        
        entityNameToBuild = (EntityName)Enum.Parse(typeof(EntityName), name);

        protoToBuild = getPrototype(entityNameToBuild);
        
        if (entityNameToBuild != EntityName.None) {
            // Register the callback if entity is not none
            TileController.instance.cbMouseEnterNewTile += previewSelectedOnTile;
            // Register the callback to rotate the preview
            InputController.Instance.Rpressed += cyclePreviewBuildDir;
        }

    }

    // Triggered by cbMouseEnterNewTile if LMB held
    public void buildSelectedOnTile(Tile rootTile) {

        // Make sure any remaining previews are removed
        removeOldPreview();

        

        canBuild = tryPlaceEntity(protoToBuild, rootTile, dirToBuild);

        Entity entity = placeEntity(protoToBuild, rootTile, dirToBuild, canBuild, false);

        if (canBuild) {
            // Build will have completed successfully, add to master lists
            MasterController.instance.allEntities.Add(entity);
            if (entity.entityType == EntityType.Component) {
                MasterController.instance.allComponentEntities.Add(entity);

            }
            else {
                
                MasterController.instance.allWireEntities.Add(entity);

                // Create the wirepiece for this wire

            }
            // Create the circuit for this entity, and merge with surrounding
            // Create a new circuit for this entity (only when building) - will merge with any surrounding circuits if found
            entity.circuit = new Circuit(entity);
            

        }
        // Trying to build but cant - 
        if (!canBuild) {

            

        }
    }

    // Display a preview of the entity to build on the selected tile (before click)
    // Triggered by cbMouseEnterNewTile
    public void previewSelectedOnTile(Tile rootTile) {

        removeOldPreview();

        previewTile = rootTile;

        Entity proto = getPrototype(entityNameToBuild);

        canBuild = tryPlaceEntity(proto, rootTile, dirToBuild);

        previewEntity = placeEntity(proto, rootTile, dirToBuild, canBuild, true);

    }
    // The tile to display the preview (need reference so when cycling build dir the preview updates)
    public Tile previewTile = null;
    // The preview entity (must be nullified when starting a build)
    public Entity previewEntity = null;
    // True if can build
    public bool canBuild = false;
    
    public void removeOldPreview() {
        
        
        if(previewEntity != null) {
            EntitySpriteController.instance.onEntityRemoved(previewEntity);
            previewEntity = null;
        }

    }

    public void removeMouseoverEntity(Tile rootTile) {

        if(entityUnderMouse != null) {
            EntitySpriteController.instance.onEntityRemoved(entityUnderMouse);
        }

    }

    public void cyclePreviewBuildDir() {

        dirToBuild = nextDirClockwise(dirToBuild);

        previewSelectedOnTile(previewTile);

    }

    public bool tryPlaceEntity(Entity proto, Tile rootTile, Dir dir) {


        if (checkAllTiles(proto, rootTile, dir) == false) {
            return false;
        }
        else { return true; }

    }

    public Entity getPrototype(EntityName name) {
        Dictionary<EntityName, Entity> protoDict = EntitySpriteController.instance.entityPrototypes;

        Entity proto = null;

        if (protoDict.ContainsKey(name)) {
            proto = protoDict[name];
            return proto;
        }
        else { Debug.LogError("No such entity exists in dict!"); return null; }
    }


    // Place the entity on the rootTile 
    public Entity placeEntity(Entity proto, Tile rootTile, Dir dir, bool canBuild, bool isGhost) {
        Entity entity;

        
        if (canBuild == false) {
            //Debug.Log("Invalid placement!");
            // Preview a red ghost sprite 
            entity = proto.placeEntityInstance(rootTile, proto, true, true, dir);
            // Set the preview entity as the placed entity to ensure removal when build dragging
            previewEntity = entity;
        }
        else {
            // Placement is valid
            entity = proto.placeEntityInstance(rootTile, proto, isGhost, false, dir);
        }
        
        EntitySpriteController.instance.onEntityCreated(entity);

        return entity;
    }

    public Dir nextDirClockwise(Dir dir) {
        switch (dir) {
            case Dir.N:
                return Dir.E;
            case Dir.E:
                return Dir.S;
            case Dir.S:
                return Dir.W;
            case Dir.W:
                return Dir.N;
            default:
                return Dir.Null;
        }
    }

    public Dir nextDirAClockwise(Dir dir) {
        switch (dir) {
            case Dir.N:
                return Dir.W;
            case Dir.E:
                return Dir.N;
            case Dir.S:
                return Dir.E;
            case Dir.W:
                return Dir.S;
            default:
                return Dir.Null;
        }
    }


    public int angleFromDir(Dir dir) {
        switch (dir) {
            case Dir.N:
                return 0;
            case Dir.E:
                return -270;
            case Dir.S:
                return -180;
            case Dir.W:
                return -90;
            default:
                return 0;
        }
    }
    // How many 90 degree rotations should be applied 
    public int numberOfRotations(Dir dir) {
        switch (dir) {
            case Dir.N:
                return 0;
            case Dir.E:
                return 3;
            case Dir.S:
                return 2;
            case Dir.W:
                return 1;
            default:
                return 0;
        }
    }

    public int rotationsToReset(Dir dir) {
        switch (dir) {
            case Dir.N:
                return 0;
            case Dir.E:
                return 1;
            case Dir.S:
                return 2;
            case Dir.W:
                return 3;
            default:
                return 0;
        }
    }

    public Dir oppositeDir(Dir dir) {
        switch (dir) {
            case Dir.N:
                return Dir.S;
            case Dir.E:
                return Dir.W;
            case Dir.S:
                return Dir.N;
            case Dir.W:
                return Dir.E;
            default:
                return Dir.Null;
        }


    }


    ////////////////////////////////////////////////////////////////////////////////////////
    ///                                  VALIDATE BUILDS
    ////////////////////////////////////////////////////////////////////////////////////////
    // First check only checks root tile
    public bool checkRootTile(Tile rootTile) {

        if(rootTile.installedEntity == null) {
            return true;
        }
        if (rootTile.installedEntity != null) {

            if (rootTile.installedEntity.isGhost) {
                return true;
            }
            else {
                Debug.Log("Can't build!");
                return false;
            }
        }

        return false;
    }

    // Check all tiles covered by this entity (needs a prototype)
    public bool checkAllTiles(Entity prototype, Tile rootTile, Dir dir) {
        // Rotate prototype to ensure tiles are calculated correctly
        int number = numberOfRotations(dir);

        // Temporarily rotate the prototype
        for (int i = 0; i < number; i++) {
            EntitySpriteController.instance.rotateEntityOffsets(prototype);
        }

        List<Tile> coveredTiles = prototype.getCoveredTiles(rootTile);
        

        foreach(Tile t in coveredTiles) {
            if(t.installedEntity != null && t.installedEntity.isGhost == false) {
                for (int i = 0; i < rotationsToReset(prototype.buildDir); i++) {
                    EntitySpriteController.instance.rotateEntityOffsets(prototype);
                }
                canBuild = false;
                return false;
            }
        }

        // Make sure the prototype is returned to the default rotation (super hacky, probably a better way)
        for (int i = 0; i < rotationsToReset(prototype.buildDir); i++) {
            EntitySpriteController.instance.rotateEntityOffsets(prototype);
        }

        // No covered tiles have installed entities
        canBuild = true;
        return true;
    }



}
