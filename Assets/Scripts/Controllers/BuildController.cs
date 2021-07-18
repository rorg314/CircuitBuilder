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

        canPlace = tryPlaceEntity(protoToBuild, rootTile, dirToBuild);

        previewCircuit = testCircuitPlacement(protoToBuild, rootTile);

        if (canPlace && previewCircuit != null) {
            validBuild = true;
        }
        else {
            validBuild = false;
        }

        Entity entity = placeEntitySprite(protoToBuild, rootTile, dirToBuild, validBuild, false);


        if (validBuild) {

            // Replace the prototype in the circuit with the actual entity
            CircuitController.instance.createCircuit(entity, false);

            CircuitController.instance.allCircuits.Remove(previewCircuit);

            // Build will have completed successfully, add to master lists
            MasterController.instance.allEntities.Add(entity);
            
            if (entity.entityType == EntityType.Component) {
                MasterController.instance.allComponentEntities.Add(entity);

            }
            else {
                
                MasterController.instance.allWireEntities.Add(entity);

            }
            // Create the circuit for this entity, and merge with surrounding
            // Create a new circuit for this entity (only when building) - will merge with any surrounding circuits if found
            //Circuit circ = new Circuit(entity);
            //CircuitController.instance.allCircuits.Add(circ);
            //Circuit circ = CircuitController.instance.createCircuit(entity, false);
        }
        // Trying to build but cant - 
        if (!canPlace) {
            
            // Do nothing
            return;

        }
    }

    // Display a preview of the entity to build on the selected tile (before click)
    // Triggered by cbMouseEnterNewTile
    public void previewSelectedOnTile(Tile rootTile) {

        removeOldPreview();

        previewTile = rootTile;

        Entity proto = getPrototype(entityNameToBuild);

        canPlace = tryPlaceEntity(proto, rootTile, dirToBuild);

        previewCircuit = testCircuitPlacement(proto, rootTile);

        if (canPlace && previewCircuit != null) {
            validBuild = true;
        }
        else {
            validBuild = false;
        }

        previewEntity = placeEntitySprite(proto, rootTile, dirToBuild, validBuild, true);

    }
    // The tile to display the preview (need reference so when cycling build dir the preview updates)
    public Tile previewTile = null;
    // The preview entity (must be nullified when starting a build)
    public Entity previewEntity = null;
    // True if can build
    public bool canPlace = false;
    // Not null if the circuit was valid
    public Circuit previewCircuit = null;
    // True if this is a valid build
    bool validBuild = false;
    
    public void removeOldPreview() {
        
        
        if(previewEntity != null) {

            // Check if preview circuit has no tiles (preview entity already removed)
            if(previewCircuit.allTilesInCircuit.Count == 0) {
                CircuitController.instance.allCircuits.Remove(previewCircuit);
            }

            CircuitController.instance.removeEntityFromCircuit(previewEntity);
            EntitySpriteController.instance.onEntityRemoved(previewEntity);
            previewEntity = null;
            validBuild = false;
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
        
        return true;
    }

    // Test if this entity would create a valid circuit
    public Circuit testCircuitPlacement(Entity proto, Tile rootTile) {

        // Temporarily set the root tile on the prototype - MUST remove after
        proto.rootTile = rootTile;
        // Temporarily install the proto on the root tile (return false if already have installed entity)
        if(rootTile.installedEntity == null) {
            rootTile.installedEntity = proto;
        }
        else {
            return null;
        }
        Circuit testCircuit = CircuitController.instance.createCircuit(proto, true);
        
        if (testCircuit != null) {

            // Created a valid circuit

            // Remove the test prototype
            CircuitController.instance.removeEntityFromCircuit(proto);

            //Remove all references added to the proto
            proto.rootTile = null;
            rootTile.installedEntity = null;

            return testCircuit;

        }
        else {

            // Remove the test prototype
            CircuitController.instance.removeEntityFromCircuit(proto);

            //Remove all references added to the proto
            proto.rootTile = null;
            rootTile.installedEntity = null;
            return null;
        }




        
        //Circuit testCircuit = new Circuit(proto, true);

        //if (testCircuit.validCircuit) {
            
        //    // The testCircuit has become the circuit that was tested joining to
            
        //    // Add to allCircuits iff contains more tiles than the preview only
        //    List<Tile> previewTiles = proto.getCoveredTiles(rootTile);
        //    foreach (Tile t in testCircuit.allTilesInCircuit) {
        //        if(previewTiles.Contains(t) == false) {
        //            // Circuit contains tiles that are not part of the preview only - is a previously constructed valid circuit
        //            if (CircuitController.instance.allCircuits.Contains(testCircuit) == false) {
        //                CircuitController.instance.allCircuits.Add(testCircuit);
        //            }
        //        }
        //    }
        //    // If circuit placement was valid, remove the proto from circuit and return true
        //    CircuitController.instance.removeEntityFromCircuit(proto);
            
        //    // Remove all references added to the proto
        //    proto.rootTile = null;
        //    rootTile.installedEntity = null;
            
        //    return true;
        //}
        //else {
        //    // Circuit will not have been fully created
        //    CircuitController.instance.removeEntityFromCircuit(proto);
        //    // The testCircuit has become the old circuit that was tested joining to 
        //    CircuitController.instance.allCircuits.Add(testCircuit);
        //    proto.rootTile = null;
        //    rootTile.installedEntity = null;
        //    return false;
        //}


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
    public Entity placeEntitySprite(Entity proto, Tile rootTile, Dir dir, bool canBuild, bool isGhost) {
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
                canPlace = false;
                return false;
            }
        }

        // Make sure the prototype is returned to the default rotation (super hacky, probably a better way)
        for (int i = 0; i < rotationsToReset(prototype.buildDir); i++) {
            EntitySpriteController.instance.rotateEntityOffsets(prototype);
        }

        // No covered tiles have installed entities
        canPlace = true;
        return true;
    }



}
