using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;




public class CircuitController : MonoBehaviour {

    

    public static CircuitController instance;

    public List<Circuit> allCircuits;

    public int incrementalCircNumber;

    // Show debug vectors on circuit segments
    [HideInInspector]
    public bool showDebug;
    
    public Sprite debugArrowEndsSprite;
    public Sprite debugArrowSprite;
    public Sprite debugJuncSprite;

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
        incrementalCircNumber = 0;

    }

    public void triggerCircuitChanged(Circuit circ) {
        
        cbOnCircuitChanged?.Invoke(circ);

    }

    public void updateAllCircuits() {

        foreach (Circuit circ in allCircuits) {
            destroyDebugObjects(circ);
            triggerCircuitChanged(circ);
        }

    }

    public void toggleDebug() {

        showDebug = !showDebug;

        if (showDebug) {
            cbOnCircuitChanged += drawCircuitDebug;
        }
        else {
            cbOnCircuitChanged -= drawCircuitDebug;
        }

        updateAllCircuits();
    }

    public void destroyDebugObjects(Circuit circ) {

        if(circ.allDebugObjects != null) {

            foreach(GameObject go in circ.allDebugObjects) {

                Destroy(go);

            }
            circ.allDebugObjects.Clear();
        }


    }

    public void drawCircuitDebug(Circuit circ) {

        if(showDebug == false) {
            return;
        }



        if(circ.allDebugObjects == null) {
            circ.allDebugObjects = new List<GameObject>();
        }
        else {
            destroyDebugObjects(circ);
        }


        foreach(Circuit.Segment seg in circ.segments) {
            seg.debugColourIndex = (lastColourIndex + 1) % (debugColours.Length - 1);
            lastColourIndex++;
            
            //drawSegmentEndsDebug(seg);

            drawSegmentTileArrows(seg);
            
        }
        foreach(Circuit.Junction junc in circ.juncs) {
            if(junc.juncDebugObject != null) {
                Destroy(junc.juncDebugObject);
            }
            if(junc.juncTile.installedEntity.circSeg != null && junc.juncTile.installedEntity.circSeg.debugArrow != null) {
                Destroy(junc.juncTile.installedEntity.circSeg.debugArrow);
            }

            GameObject juncDebug = new GameObject();

            junc.juncDebugObject = juncDebug;

            juncDebug.transform.localScale = new Vector3(TileController.instance.cellSize, TileController.instance.cellSize, 0);
            juncDebug.transform.position = junc.juncTile.getTileWorldPositon() + TileController.instance.tileCentreOffset;
            juncDebug.transform.Rotate(-90, 0, 0, Space.Self);
            juncDebug.name = "junc " + junc.juncTile.ToString();


            SpriteRenderer sr = juncDebug.AddComponent<SpriteRenderer>();
            sr.sprite = debugJuncSprite;
            sr.color = debugColours[(lastColourIndex + 1) % (debugColours.Length - 1)];
            lastColourIndex++;

            
            sr.sortingLayerName = "UI";
            sr.sortingOrder = 0;

            circ.allDebugObjects.Add(juncDebug);
        }

    }
    
    public void drawSegmentTileArrows(Circuit.Segment seg) {


        // Loop through tiles in order from start to finish - segment must have > 1 tile
        if (seg.allSegmentTiles.Count > 1) {
            for (int i = 0; i < seg.allSegmentTiles.Count; i++) {
                Tile tile = seg.allSegmentTiles[i];
                Dir dir = Dir.N;
                // Arrow should point towards next tile in segment
                if (i < seg.allSegmentTiles.Count - 1) {
                    dir = TileController.instance.getNeighbourTileOrientation(tile, seg.allSegmentTiles[i + 1]);
                }
                else {
                    dir = BuildController.instance.oppositeDir(TileController.instance.getNeighbourTileOrientation(tile, seg.allSegmentTiles[i - 1]));
                }
                // Arrow initialises pointing N
                float rot = BuildController.instance.angleFromDir(dir);

                // Remove any old arrow
                //if(tile.installedEntity != null && tile.installedEntity.segArrow != null) {
                //    Destroy(tile.installedEntity.segArrow);
                //}

                GameObject arrow = new GameObject();
                tile.installedEntity.segArrow = arrow;

                arrow.transform.position = tile.getTileWorldPositon() + TileController.instance.tileCentreOffset;
                arrow.transform.localScale = new Vector3(TileController.instance.cellSize, TileController.instance.cellSize, 1f);
                arrow.transform.Rotate(90, rot, 0, Space.Self);

                arrow.name = "seg " + tile.ToString();

                SpriteRenderer sr = arrow.AddComponent<SpriteRenderer>();
                sr.sprite = debugArrowSprite;
                sr.sortingLayerName = "UI";
                sr.sortingOrder = 0;
                sr.color = debugColours[seg.debugColourIndex];

                seg.circuit.allDebugObjects.Add(arrow);
            }
        }

    }
    public void drawSegmentEndsDebug(Circuit.Segment seg) {

        // Destroy any old arrow if existing
        //if (seg.debugArrow != null) {
        //    Destroy(seg.debugArrow);
        //}

        Vector3 dir = seg.endTile.getTileWorldPositon() - seg.startTile.getTileWorldPositon();
        Vector3 startPos = seg.startTile.getTileWorldPositon() + TileController.instance.tileCentreOffset;


        GameObject segArrow = new GameObject();

        // Scale arrow based on length of dir (arrow initially points in x dir only, then rotate to be lying along dir)
        segArrow.transform.localScale = new Vector3(dir.magnitude, TileController.instance.cellSize, 0);
        segArrow.transform.position = startPos + dir * 0.5f;

        
        float rot = Quaternion.FromToRotation(Vector3.right, dir).eulerAngles.y;
        segArrow.transform.Rotate(-90, 0, 0, Space.Self);
        segArrow.transform.Rotate(0, rot, 0, Space.World);

        seg.debugArrow = segArrow;

        SpriteRenderer sr = segArrow.AddComponent<SpriteRenderer>();
        sr.sprite = debugArrowEndsSprite;
        //sr.color = debugColours[(lastColourIndex + 1) % (debugColours.Length - 1)];
        //lastColourIndex++;
        
        sr.sortingLayerName = "UI";
        sr.sortingOrder = 0;
        sr.color = debugColours[seg.debugColourIndex];

        seg.circuit.allDebugObjects.Add(segArrow);
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

    public List<Circuit.Segment> getNeighbourCircuitSegments(List<Tile> neighbourTiles) {

        List<Circuit.Segment> neighbourSegments = new List<Circuit.Segment>();
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
            if (t.installedEntity != null && (t.installedEntity.circSeg != null || t.installedEntity.circJunc != null)) {
                Circuit circ;
                // Get the reference to the parent circuit(could probably streamline this?)
                if (t.installedEntity.circSeg != null) {
                    circ = t.installedEntity.circSeg.circuit;
                }
                else {
                    circ = t.installedEntity.circJunc.circuit;
                }
                //// Skip if already added
                //if (neighbourCircs.Contains(circ)) {
                //    continue;
                //}
                // Append to neighbour circs
                neighbourCircs.Add(circ);
            }
        }

        return neighbourCircs;

    }

    public List<Circuit> getNeighbourCircuitsExcludingThis(List<Tile> neighbourTiles, Circuit thisCirc) {

        List<Circuit> neighbourCircs = new List<Circuit>();
        
        foreach (Tile t in neighbourTiles) {
            if (t.installedEntity != null && (t.installedEntity.circSeg != null || t.installedEntity.circJunc != null))  {
                Circuit circ;
                // Get the reference to the parent circuit (could probably streamline this?)
                if (t.installedEntity.circSeg != null) {
                    circ = t.installedEntity.circSeg.circuit;
                }
                else {
                    circ = t.installedEntity.circJunc.circuit;
                }
                // Skip if the circuit is thisCirc
                if(circ == thisCirc) {
                    continue;
                }
                
                // Skip if already added
                if (neighbourCircs.Contains(circ)) {
                    continue;
                }
                // Append to neighbour circs
                neighbourCircs.Add(circ);
            }
        }

        return neighbourCircs;
    }

    // Find the tile in the base that this is adjacent to this append tile
    public Tile getBaseTile(Circuit baseCirc, Tile appendTile) {

        return TileController.instance.getCommonTile(appendTile.getNeighbouringTiles(), baseCirc.allTilesInCircuit);

    }

    // Find the tile in the joining circuit that connects to the base tile
    public Tile getJoinTile(Circuit joinCirc, Tile baseTile) {

        List<Tile> baseNeighbours = baseTile.getNeighbouringTiles();

        return TileController.instance.getCommonTile(baseNeighbours, joinCirc.allTilesInCircuit);

    }
    
    public bool checkValidSegmentJoin(Circuit.Segment baseSeg, Circuit.Segment joinSeg) {

        // Check if this join would form a looped segment
        
        if(baseSeg == joinSeg) {
            return false;
        }


        // Check if joining end/end start/start start/end of another segment
        if((baseSeg.endTile == joinSeg.endTile) || (baseSeg.startTile == joinSeg.endTile) || (baseSeg.endTile == joinSeg.startTile) || (baseSeg.startTile == joinSeg.startTile)) {
            // Do not allow if these are the same segment
            if(baseSeg == joinSeg) {
                return false;
            }
            
            // Check if joining two segment ends that both meet the same junction
            foreach(Circuit.Junction junc in baseSeg.connectedJuncs) {
                if (joinSeg.connectedJuncs.Contains(junc)) {
                    return false;
                }
            }
        }

        return true;
    }


    public Circuit createCircuit(Entity entity, bool testCircuit=false) {
        Debug.Log("Number of circuits before: " + CircuitController.instance.allCircuits.Count);
        
        Circuit circ = new Circuit(entity);
        allCircuits.Add(circ);
        
        circ.circName = incrementalCircNumber.ToString();
        incrementalCircNumber++;
       
        // Check for neighbours and join if possible
        List<Circuit> neighbours = circ.checkForNeighbourCircuits(entity, circ, true);

        // Check special case for making a new 4-way junction
        if (neighbours.Count == 4) {
            // Create new junction from this entity and use to create a new circuit (this circuit discarded)
            Circuit.Junction junc = new Circuit.Junction(entity.rootTile);
            Circuit newCirc = new Circuit(junc);
            Debug.Log("Number of circuits after: " + CircuitController.instance.allCircuits.Count);
            return newCirc;
        }
        else if (neighbours.Count == 0) {
            
            // Created a new standalone circuit
            triggerCircuitChanged(circ);
            Debug.Log("Number of circuits after: " + CircuitController.instance.allCircuits.Count);
            return circ;
        }
        else {
            Circuit newBaseCircuit = joinToAllNeighbours(circ, neighbours, entity);
            if (newBaseCircuit != null) {

                // Joined to all neighbours successfully 
                Debug.Log("Number of circuits after: " + CircuitController.instance.allCircuits.Count);
                return newBaseCircuit;
            }
            else {

                // Did not create a valid circuit 
                removeEntityFromCircuit(entity);
                Debug.Log("Number of circuits after: " + CircuitController.instance.allCircuits.Count);
                return null;

            }
        }
        
    }


    public Circuit joinToAllNeighbours(Circuit baseCircuit, List<Circuit> neighbours, Entity entity) {

        

        // Try and join to the first neighbour circuit
        Circuit newBase = tryJoinCircuits(baseCircuit, neighbours[0], entity.rootTile);
        
        if (newBase != null) {
            
            // Join was successful - continue to join to new neighbours
            List<Circuit> newNeighbours = newBase.checkForNeighbourCircuits(entity, newBase, true);

            while (newNeighbours.Count > 0) {
                newBase = tryJoinCircuits(newBase, newNeighbours[0], entity.rootTile);

                newNeighbours = newBase.checkForNeighbourCircuits(entity, newBase, true);
                
            }
            // Found no other neighbour circuits - check if trying to join to the same circuit and do not allow
            if (newBase.checkForNeighbourCircuits(entity, newBase, false).Count > 1) {

                // Only allow if the neighbours are in same segment and tiles in sequence (new entity has joined two segments together)
                // OR if joining to a junction
                if(entity.circSeg != null) {

                    Tile baseTile = entity.rootTile;
                    List<Tile> neighbourTiles = baseTile.getNeighbouringTiles();
                    // Check if joining to a junction and thus allow
                    foreach(Tile t in neighbourTiles) {
                        if(t.installedEntity != null) {
                            if(t.installedEntity.circJunc != null) {
                                return newBase;
                            }
                        }
                    }
                    
                    int baseIndex = entity.circSeg.allSegmentTiles.IndexOf(baseTile);
                    if(neighbourTiles.Contains(entity.circSeg.allSegmentTiles[baseIndex + 1]) && neighbourTiles.Contains(entity.circSeg.allSegmentTiles[baseIndex - 1])) {
                        return newBase;
                    }

                }
                
                return null;

            }

            // All joined successfully
            return newBase;
        }

        return null;
    }

    // Attempt the join - returns true if join successful 
    public Circuit tryJoinCircuits(Circuit baseCirc, Circuit joinCirc, Tile baseTile) {

        destroyDebugObjects(joinCirc);

        Tile joinTile = getJoinTile(joinCirc, baseTile);

        (SegmentType, SegmentType) types = getJoinTypes(baseTile, joinTile);
        SegmentType baseType = types.Item1;
        SegmentType joinType = types.Item2;

        // If base/join is wire segment
        if (baseType == SegmentType.Wire && joinType == SegmentType.Wire) {

            Circuit.Segment baseSeg = baseTile.installedEntity.circSeg;
            Circuit.Segment joinSeg = joinTile.installedEntity.circSeg;

            // Check if joining segment starts/ends together - or if creating junction
            if ((baseTile == baseSeg.startTile || baseTile == baseSeg.endTile) && (joinTile == joinSeg.startTile || joinTile == joinSeg.endTile)) {
                // Joining wireseg to wireseg
                if (checkValidSegmentJoin(baseSeg, joinSeg) == false) {
                    Debug.LogWarning("Cannot join these segments!");
                    // Return without joining
                    return null;
                }
                
                joinSegments(baseSeg, joinSeg, baseTile, joinTile);
                    
                transferSegsAndJuncs(baseCirc, joinCirc);

                updateCircuitReferences(baseCirc, joinCirc);

                triggerCircuitChanged(baseCirc);
                    
                return baseCirc;
                
            }
            else {
                // Creating a junction
                Circuit juncCirc = createJunction(baseSeg, joinSeg, baseTile, joinTile);
                // Return the new junction circuit 
                return juncCirc;
            }
        }
        // Joining segment and a junction
        if ((baseType == SegmentType.Junction && joinType == SegmentType.Wire) || (baseType == SegmentType.Wire && joinType == SegmentType.Junction)) {

            // Add the joined allTiles to the base circuit
            baseCirc.allTilesInCircuit.AddRange(joinCirc.allTilesInCircuit);

            // Transfer all segments and junctions to new base circuit
            transferSegsAndJuncs(baseCirc, joinCirc);

            // Update all references on join circuit to new base circuit
            updateCircuitReferences(baseCirc, joinCirc);

            allCircuits.Remove(joinCirc);

            triggerCircuitChanged(baseCirc);

            return baseCirc;
        }

        // Joining component/wire
        if ((baseType == SegmentType.Wire && joinType == SegmentType.Component) || (baseType == SegmentType.Component && joinType == SegmentType.Wire)) {



        }

        return null;
    }

    public (SegmentType, SegmentType) getJoinTypes(Tile baseTile, Tile joinTile) {

        
        SegmentType baseType = SegmentType.Null;
        SegmentType joinType = SegmentType.Null;

        if (baseTile.installedEntity.circJunc != null) {
            baseType = SegmentType.Junction;
        }
        if (joinTile.installedEntity.circJunc != null) {
            joinType = SegmentType.Junction;
        }

        if (baseTile.installedEntity.circSeg != null) {
            baseType = baseTile.installedEntity.circSeg.segmentType;
        }
        if (joinTile.installedEntity.circSeg != null) {
            joinType = joinTile.installedEntity.circSeg.segmentType;
        }

        return (baseType, joinType);

    }


    public void transferSegsAndJuncs(Circuit baseCirc, Circuit joinCirc) {

        // Add any junctions
        foreach (Circuit.Junction junc in joinCirc.juncs) {
            
            if (baseCirc.juncs.Contains(junc) == false) {
                baseCirc.juncs.Add(junc);
            }
        }

        // Add any segments not directly joined
        //baseCirc.segments.AddRange(joinCirc.segments);
        foreach(Circuit.Segment seg in joinCirc.segments) {

            if(baseCirc.segments.Contains(seg) == false) {
                baseCirc.segments.Add(seg);
            }
        }

        // Transfer any debug objects
        if(joinCirc.allDebugObjects != null && joinCirc.allDebugObjects.Count > 0) {

            baseCirc.allDebugObjects.AddRange(joinCirc.allDebugObjects);

        }

    }

    public void updateCircuitReferences(Circuit baseCirc, Circuit joinCirc) {

        // Change the circuit ref on the joined tiles to the base circuit
        foreach (Tile t in joinCirc.allTilesInCircuit) {
            if (t.installedEntity != null) {

                if (t.installedEntity.circSeg != null) {
                    t.installedEntity.circSeg.circuit = baseCirc;
                }
                else if (t.installedEntity.circJunc != null) {
                    t.installedEntity.circJunc.circuit = baseCirc;
                }
            }
            else {
                Debug.LogError("Tile in circuit had no installed entity! - Tile: " + t.ToString());
            }
        }

    }

    // Segments must be NESW neighbours to join - joinTile will have just been appended to the base circuit
    public void joinSegments(Circuit.Segment baseSeg, Circuit.Segment joinSeg, Tile baseTile, Tile joinTile) {

        // Remove the join seg from join circuit master list (to avoid re adding)
        joinSeg.circuit.segments.Remove(joinSeg);

        if (baseSeg.endTile == baseTile) {
            // Joining to end of base 
            if (joinTile == joinSeg.startTile) {
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
        else if (baseSeg.startTile == baseTile) {
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


        // Replace the reference on all joinSegment tiles with the base segment
        foreach (Tile t in joinSeg.allSegmentTiles) {
            t.installedEntity.circSeg = baseSeg;
        }

        // Add the joined allTiles to the base circuit
        baseSeg.circuit.allTilesInCircuit.AddRange(joinSeg.circuit.allTilesInCircuit);

        allCircuits.Remove(joinSeg.circuit);

        baseSeg.length = baseSeg.getSegmentLength();

    }


    public Circuit createJunction(Circuit.Segment baseSeg, Circuit.Segment joinSeg, Tile baseTile, Tile joinTile) {
        
        if (baseTile == baseSeg.endTile || baseTile == baseSeg.startTile) {
            // baseTile is end being joined into the middle of another segment (joinTile is not start/end of joinSeg)
            if (joinTile.installedEntity != null) {
                removeEntityFromCircuit(joinTile.installedEntity);
            }
            else {
                Debug.LogError("Trying to create junction with no installed entity!");
            }

            Circuit.Junction junc = new Circuit.Junction(joinTile);
            Circuit juncCirc = new Circuit(junc);
            return juncCirc;
        }
        else {
            // baseTile is in the middle of the base segment - joinTile start/end of joinSeg
            if (baseTile.installedEntity != null) {
                removeEntityFromCircuit(baseTile.installedEntity);
            }
            else {
                Debug.LogError("Trying to create junction with no installed entity!");
            }


            Circuit.Junction junc = new Circuit.Junction(baseTile);
            Circuit juncCirc = new Circuit(junc);
            return juncCirc;
        }
        
    }

    

    public void splitSegment(Circuit.Segment baseSeg, Tile splitTile) {

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
        Circuit.Segment newSeg = new Circuit.Segment(newSegRange);

        // Make a new circuit from the split segment (will merge back to the original circuit if connected elsewhere)
        Circuit newCirc = new Circuit(newSeg);

        

        triggerCircuitChanged(baseSeg.circuit);
        triggerCircuitChanged(newCirc);

    }


    public void recalculateAllJunctions(Circuit circuit) {

        foreach(Circuit.Junction junc in circuit.juncs) {

            junc.setJunctionSegments();

        }

    }

    public void checkForRendundantNeighbourJunction(Tile removedTile) {

        // Check if any neighbours have a junction that needs to be removed
        List<Tile> neighbours = removedTile.getNeighbouringTiles();
        foreach (Tile t in neighbours) {

            if(t.installedEntity != null && t.installedEntity.circJunc != null) {
                Circuit.Junction junc = t.installedEntity.circJunc;
                if(junc.inSegs.Count + junc.outSegs.Count == 2) {

                    // Junction is redundant - remove and replace with segment
                    removeEntityFromCircuit(t.installedEntity);
                    // Create single segment piece 
                    createCircuit(t.installedEntity);

                }

            }

        }

    }
    
    public void removeEntityFromCircuit(Entity entity, bool test=false) {

        Tile removedTile = entity.rootTile;
        
        Circuit.Segment baseSeg = entity.circSeg;
        Circuit.Junction baseJunc = entity.circJunc;
        Circuit baseCirc = null;
        if(baseSeg != null) {
            baseCirc = baseSeg.circuit;
        }
        else if(baseJunc != null) {
            baseCirc = baseJunc.circuit;
        }

        if (baseSeg != null) {
            // Removing a segment 
            // Entity was removed from start - set new start tile
            if (baseSeg.startTile == removedTile) {
                checkForDisconnectingJunction(entity.rootTile, baseSeg);
                
                baseSeg.circuit.allTilesInCircuit.Remove(removedTile);

                removeTileFromSegmentEnds(baseSeg, removedTile);

                entity.circSeg = null;
                
                //triggerCircuitChanged(baseCirc);
            }
            // Entity removed from end
            else if (baseSeg.endTile == removedTile) {
                checkForDisconnectingJunction(entity.rootTile, baseSeg);

                baseSeg.circuit.allTilesInCircuit.Remove(removedTile);

                removeTileFromSegmentEnds(baseSeg, removedTile);

                entity.circSeg = null;
                
                //triggerCircuitChanged(baseCirc);
            }
            // Entity being removed from middle - must split circuit
            else {
                entity.circSeg = null;

                splitSegment(baseSeg, entity.rootTile);

            }
        }
        if(baseJunc != null) {
            // Removing a junction - remove all connected segments and recreate 

            // Remove the junction
            baseJunc.circuit.juncs.Remove(baseJunc);
            baseJunc.circuit.allTilesInCircuit.Remove(baseJunc.juncTile);
            baseJunc.juncTile.installedEntity.circJunc = null;

            // If number of connected segs is same as entire circuit - must remove the original circuit before recreating original segments

            if(baseJunc.circuit.segments.Count == baseJunc.inSegs.Count + baseJunc.outSegs.Count) {

                allCircuits.Remove(baseJunc.circuit);

            }


            // Remove all connected segments and recreate as separate circuits (will rejoin if connected elsewhere)
            foreach (Circuit.Segment seg in baseJunc.inSegs) {
                // Disconnect each segment and recreate (removed tile is junction so won't be removed from segment)
                removeSegmentFromJunction(baseJunc.circuit, baseJunc, seg, removedTile);
            }
            foreach (Circuit.Segment seg in baseJunc.outSegs) {
                // Disconnect each segment and recreate (removed tile is junction so won't be removed from segment)
                removeSegmentFromJunction(baseJunc.circuit, baseJunc, seg, removedTile);
            }

            baseJunc.circuit = null;

        }
        
        
        // Recalculate all junctions in the base circ (OPTIMISE)
        if (baseCirc != null) {
            recalculateAllJunctions(baseCirc);
        }
        // Check if any neighbouring junctions are now redundant
        checkForRendundantNeighbourJunction(removedTile);

        // Trigger circuit changed
        if (baseCirc != null) {
            triggerCircuitChanged(baseCirc);
        }
    }

    // Check if removing this entity needs to disconnect that segment from a junction
    public void checkForDisconnectingJunction(Tile removedTile, Circuit.Segment segment) {

        List<Tile> neighbours = removedTile.getNeighbouringTiles();

        foreach(Tile t in neighbours) {
            if(t.installedEntity != null) {

                if(t.installedEntity.circJunc != null) {

                    removeSegmentFromJunction(t.installedEntity.circJunc.circuit, t.installedEntity.circJunc, segment, removedTile);

                }
            }
        }
    }

    // Disconnect a segment from a junction 
    public void removeSegmentFromJunction(Circuit baseCirc, Circuit.Junction baseJunc, Circuit.Segment segment, Tile removedTile) {

        // Remove the segment
        baseCirc.segments.Remove(segment);

        // Remove all tiles in segment from circuit
        foreach(Tile t in segment.allSegmentTiles) {
            baseCirc.allTilesInCircuit.Remove(t);   
        }

        // Disconnect this segment from all connected juncs
        foreach (Circuit.Junction junc in segment.connectedJuncs) {

            if (junc.inSegs.Contains(segment)) {
                junc.inSegs.Remove(segment);
            }
            if (junc.outSegs.Contains(segment)) {
                junc.outSegs.Remove(segment);
            }
            
        }
        // Clear connected juncs, will be reset once new segment is created
        segment.connectedJuncs.Clear();
        
        // If the removed tile was in the segment, remove from the segment before recreating
        if (segment.allSegmentTiles.Contains(removedTile)) {

            removeTileFromSegmentEnds(segment, removedTile);
            
        }
        if(segment.allSegmentTiles.Count > 0) {
            new Circuit(segment);
        }

    }

    public Tile getNearestTileInSegment(Tile baseTile, Circuit.Segment seg) {

        return TileController.instance.getCommonTile(baseTile.getNeighbouringTiles(), seg.allSegmentTiles);

    }

    public void removeTileFromSegmentEnds(Circuit.Segment seg, Tile removedTile) {

        seg.allSegmentTiles.Remove(removedTile);

        if(seg.endTile == removedTile) {

            seg.endTile = getNearestTileInSegment(removedTile, seg);

        }
        if(seg.startTile == removedTile) {

            seg.startTile = getNearestTileInSegment(removedTile, seg);

        }


    }

    

}
