using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class CircuitController : MonoBehaviour {

    public static CircuitController instance;

    public List<Circuit> allCircuits;

    // Show debug vectors on circuit segments
    [HideInInspector]
    public bool showDebug;
    
    public Sprite debugArrowSprite;

    private Color[] debugColours = { Color.white, Color.red, Color.blue, Color.green, Color.cyan, Color.magenta, Color.black, Color.yellow };
    private int lastColourIndex = 0;

    public event Action<Circuit> cbOnCircuitChanged;

    // Start is called before the first frame update
    void Start() {

        instance = this;
        allCircuits = new List<Circuit>();
        showDebug = true;

        if (showDebug) {

            cbOnCircuitChanged += drawCircuitDebug;

        }

    }

    public void triggerCircuitChanged(Circuit circ) {
        
        cbOnCircuitChanged?.Invoke(circ);

    }

    public void drawCircuitDebug(Circuit circ) {

        foreach(Circuit.CircuitSegment seg in circ.segments) {
            // Destroy any old arrow if existing
            if(seg.debugArrow != null) {
                Destroy(seg.debugArrow);
            }

            Vector3 dir = seg.endTile.getTileWorldPositon() - seg.startTile.getTileWorldPositon();
            Vector3 startPos = seg.startTile.getTileWorldPositon() + TileController.instance.tileCentreOffset;
            
            
            GameObject segArrow = new GameObject();
            
            // Scale arrow based on length of dir (arrow initially points in x dir only, then rotate to be lying along dir)
            segArrow.transform.localScale = new Vector3(dir.magnitude, TileController.instance.cellSize, 0);
            segArrow.transform.position = startPos + dir * 0.5f;
            
            float rot = CalculateAngle(Vector3.right, dir);
            segArrow.transform.Rotate(-90, rot, 0, Space.Self);

            seg.debugArrow = segArrow;

            SpriteRenderer sr = segArrow.AddComponent<SpriteRenderer>();
            sr.sprite = debugArrowSprite;
            sr.color = debugColours[(lastColourIndex + 1) % (debugColours.Length-1)];
            lastColourIndex++;
            sr.sortingLayerName = "UI";
            sr.sortingOrder = 0;
            
            
        }

    }

    // Util for getting 360 angle between vectors
    public static float CalculateAngle(Vector3 from, Vector3 to) {

        return Quaternion.FromToRotation(Vector3.up, to - from).eulerAngles.y;

    }

    public List<Entity> getNeighbourComponentEntities(List<Tile> neighbourTiles) {

        List<Entity> neighbourComponents = new List<Entity>();
        foreach (Tile t in neighbourTiles) {
            // Check if neighbour contains any wiresegments
            if (t.installedEntity != null && t.installedEntity.entityType == EntityType.Component) {
                neighbourComponents.Add(t.installedEntity);
            }
        }

        return neighbourComponents;

    }

    public List<Circuit.CircuitSegment> getNeighbourCircuitSegments(List<Tile> neighbourTiles) {

        List<Circuit.CircuitSegment> neighbourSegments = new List<Circuit.CircuitSegment>();
        foreach (Tile t in neighbourTiles) {
            // Check if neighbour contains any wiresegments
            if (t.installedEntity != null && t.installedEntity.circSeg != null) {
                neighbourSegments.Add(t.installedEntity.circSeg);
            }
        }

        return neighbourSegments;
    }

    public List<Circuit> getNeighbourCircuits(List<Tile> neighbourTiles) {

        List<Circuit> neighbourCircs = new List<Circuit>();
        foreach (Tile t in neighbourTiles) {
            if (t.installedEntity != null && t.installedEntity.circSeg.circuit != null) {
                // Append to neighbour circs
                neighbourCircs.Add(t.installedEntity.circSeg.circuit);
            }
        }

        return neighbourCircs;

    }

    public List<Circuit> getNeighbourCircuitsExcludingThis(List<Tile> neighbourTiles, Circuit thisCirc) {

        List<Circuit> neighbourCircs = new List<Circuit>();
        foreach (Tile t in neighbourTiles) {
            if (t.installedEntity != null && t.installedEntity.circSeg != null && t.installedEntity.circSeg.circuit != thisCirc) {
                // Append to neighbour circs
                neighbourCircs.Add(t.installedEntity.circSeg.circuit);
            }
        }

        return neighbourCircs;

    }
    // Find the tile in the base that this is adjacent to this append tile
    public Tile getBaseTile(Circuit baseCirc, Tile appendTile) {

        return TileController.instance.getCommonTile(appendTile.getNeighbouringTiles(), baseCirc.allTilesInCircuit);

    }

    // Find the tile in the joining circuit that connects to the base tile
    public Tile getJoinTile(Circuit baseCirc, Circuit joinCirc, Tile baseTile) {

        List<Tile> baseNeighbours = baseTile.getNeighbouringTiles();

        return TileController.instance.getCommonTile(baseNeighbours, joinCirc.allTilesInCircuit);

    }

    // Use to append a single entity to this existing circuit
    public Circuit appendCircuit(Circuit baseCirc, Circuit appendCirc, Entity entity) {

        // Get the tile in the base that this entity is appending to (returns first found)
        Tile baseTile = CircuitController.instance.getBaseTile(baseCirc, entity.rootTile);

        // Join the circuits
        joinCircuits(baseCirc, appendCirc, baseTile);
        
        // Add the appended tile to the base circ
        baseCirc.allTilesInCircuit.Add(entity.rootTile);

        triggerCircuitChanged(baseCirc);

        return baseCirc;
    }

    public Circuit joinCircuits(Circuit baseCirc, Circuit joinCirc, Tile baseTile) {

        Tile joinTile = getJoinTile(baseCirc, joinCirc, baseTile);

        SegmentType baseType = baseTile.installedEntity.circSeg.segmentType;
        SegmentType joinType = joinTile.installedEntity.circSeg.segmentType;

        // If base/join is wire segment
        if (baseType == SegmentType.Wire && joinType == SegmentType.Wire) {
            
            // Joining wireseg to wireseg
            joinSegments(baseTile.installedEntity.circSeg, joinTile.installedEntity.circSeg, baseTile, joinTile);

        }

        // Change the circuit ref on the joined circuit segments to the base circuit
        foreach(Tile t in joinCirc.allTilesInCircuit) {
         
            t.installedEntity.circSeg.circuit = baseCirc;

        }

        allCircuits.Remove(joinCirc);
        return baseCirc;

    }

    // Segments must be NESW neighbours to join - joinTile will have just been appended to the base circuit
    public void joinSegments(Circuit.CircuitSegment baseSeg, Circuit.CircuitSegment joinSeg, Tile baseTile, Tile joinTile) {
        
        // Destroy any debug preview on the join seg
        Destroy(joinSeg.debugArrow);
        // Remove the join seg from its circuit master list 
        joinSeg.circuit.segments.Remove(joinSeg);
        // Replace the reference on all joinSegment tiles with the base segment
        foreach(Tile t in joinSeg.allSegmentTiles) {
            t.installedEntity.circSeg = baseSeg;
        }


        // Add all tiles to the base seg
        //baseSeg.allSegmentTiles.AddRange(joinSeg.allSegmentTiles);

        if (baseSeg.endTile == baseTile) {
            // Joining to end of base

            if(joinTile == joinSeg.startTile) {
                // Joining start to end of base, new end is end of join seg
                baseSeg.endTile = joinSeg.endTile;
                // Add range to end of list of base seg
                baseSeg.allSegmentTiles.AddRange(joinSeg.allSegmentTiles);
            }
            else {
                // Joining end to end of base - join start is new base end 
                baseSeg.endTile = joinSeg.startTile;
                // Add the joined range in reverse order
                joinSeg.allSegmentTiles.Reverse();
                baseSeg.allSegmentTiles.AddRange(joinSeg.allSegmentTiles);
            }

        }
        else {
            // Joining to start of base
            if (joinTile == joinSeg.startTile) {
                // Joining start to start of base, new start is end of join seg
                baseSeg.startTile = joinSeg.endTile;
                // Reverse the joined tiles and add to beginning of base range
                joinSeg.allSegmentTiles.Reverse();
                baseSeg.allSegmentTiles.InsertRange(0, joinSeg.allSegmentTiles);
            }
            else {
                // Joining end to start of base 
                baseSeg.startTile = joinSeg.startTile;
                // Add range to beginning of base range
                baseSeg.allSegmentTiles.InsertRange(0, joinSeg.allSegmentTiles);
            }

        }

        baseSeg.length = baseSeg.getSegmentLength();

        triggerCircuitChanged(baseSeg.circuit);

    }

    public void splitCircuit(Circuit baseCirc, Tile splitTile) {





    }

    public void splitSegment(Circuit.CircuitSegment baseSeg, Tile splitTile) {

        baseSeg.circuit.allTilesInCircuit.Remove(splitTile);

        int splitIndex = baseSeg.allSegmentTiles.IndexOf(splitTile);
        int baseCount = baseSeg.allSegmentTiles.Count;
        
        // Deep copy the range to split off
        List<Tile> newSegRange = new List<Tile>();
        
        for (int i = splitIndex + 1; i < baseCount; i++) {
            newSegRange.Add(baseSeg.allSegmentTiles[i]);
            // Remove from the allTilesInCircuit list (avoids looping through twice)
            baseSeg.circuit.allTilesInCircuit.Remove(baseSeg.allSegmentTiles[i]);
            // Remove circuit references on entities in split segment
            
            baseSeg.allSegmentTiles[i].installedEntity.circSeg = null;
        }
            
        // Remove the split off range from the base segment 
        baseSeg.allSegmentTiles.RemoveRange(splitIndex, baseCount - splitIndex);
        
        // Make the new base segment end on splitIndex - 1 (last tile in base list)
        baseSeg.endTile = baseSeg.allSegmentTiles[baseSeg.allSegmentTiles.Count - 1];

        // Make a new segment that start on splitIndex + 1 and ends at the old baseEnd
        Circuit.CircuitSegment newSeg = new Circuit.CircuitSegment(newSegRange);

        // Make a new circuit from the split segment (will merge back to the original circuit if connected elsewhere)
        Circuit newCirc = new Circuit(newSeg);

        triggerCircuitChanged(baseSeg.circuit);
        triggerCircuitChanged(newCirc);

    }




    public void removeEntityFromCircuit(Entity entity) {

        Tile removedTile = entity.rootTile;
        Circuit.CircuitSegment baseSeg = entity.circSeg;

        if (entity.entityType == EntityType.WirePiece) {

            

            // Entity was removed from start - set new start tile
            if (baseSeg.startTile == entity.rootTile) {
                baseSeg.allSegmentTiles.Remove(entity.rootTile);
                baseSeg.startTile = getNearestTileInSegment(removedTile, baseSeg);
                entity.circSeg = null;
                
            }
            // Entity removed from end
            else if (baseSeg.endTile == entity.rootTile) {
                entity.circSeg.allSegmentTiles.Remove(entity.rootTile);
                baseSeg.endTile = getNearestTileInSegment(removedTile, baseSeg);
                entity.circSeg = null;
                
            }
            // Entity being removed from middle - must split circuit
            else {
                entity.circSeg = null;
                
                splitSegment(baseSeg, entity.rootTile);
                
            }

            triggerCircuitChanged(baseSeg.circuit);

        }




    }

    public Tile getNearestTileInSegment(Tile baseTile, Circuit.CircuitSegment seg) {

        return TileController.instance.getCommonTile(baseTile.getNeighbouringTiles(), seg.allSegmentTiles);

    }

}
