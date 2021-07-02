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

    }

    public void triggerCircuitChanged(Circuit circ) {
        
        cbOnCircuitChanged?.Invoke(circ);

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
            if (t.installedEntity != null && t.installedEntity.circSeg != null) {
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
                // Skip if already added
                if (neighbourCircs.Contains(t.installedEntity.circSeg.circuit)) {
                    continue;
                }
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
    public Tile getJoinTile(Circuit joinCirc, Tile baseTile) {

        List<Tile> baseNeighbours = baseTile.getNeighbouringTiles();

        return TileController.instance.getCommonTile(baseNeighbours, joinCirc.allTilesInCircuit);

    }
    
    // Join two circuits 
    public Circuit joinCircuits(Circuit baseCirc, Circuit joinCirc, Tile baseTile) {

        destroyDebugObjects(joinCirc);

        Tile joinTile = getJoinTile(joinCirc, baseTile);
        SegmentType baseType = SegmentType.Null;
        SegmentType joinType = SegmentType.Null;
        
        if(baseTile.installedEntity.circJunc != null) {
            baseType = SegmentType.Junction;
        }
        if(joinTile.installedEntity.circJunc != null) {
            joinType = SegmentType.Junction;
        }
        
        if (baseTile.installedEntity.circSeg != null) {
            baseType = baseTile.installedEntity.circSeg.segmentType;
        }
        if (joinTile.installedEntity.circSeg != null) {
            joinType = joinTile.installedEntity.circSeg.segmentType;
        }
        

        // If base/join is wire segment
        if (baseType == SegmentType.Wire && joinType == SegmentType.Wire) {

            Circuit.Segment baseSeg = baseTile.installedEntity.circSeg;
            Circuit.Segment joinSeg = joinTile.installedEntity.circSeg;

            // Check if joining segment starts/ends together - or if creating junction
            if ((baseTile == baseSeg.startTile || baseTile == baseSeg.endTile) && (joinTile == joinSeg.startTile || joinTile == joinSeg.endTile)) {
                // Joining wireseg to wireseg
                joinSegments(baseSeg, joinSeg, baseTile, joinTile);

                //// Add any junctions
                //foreach (Circuit.Junction junc in joinCirc.juncs) {
                //    if (baseCirc.juncs.Contains(junc) == false) {
                //        baseCirc.juncs.Add(junc);
                //    }
                //}

                //// Add any segments not directly joined
                //baseCirc.segments.AddRange(joinCirc.segments);
                //// Change reference on all added segments 
                //foreach(Circuit.Segment seg in baseCirc.segments) {
                //    foreach(Tile t in seg.allSegmentTiles) {
                //        t.installedEntity.circSeg.circuit = baseCirc;
                //    }
                //}

                transferSegsAndJuncs(baseCirc, joinCirc);


                triggerCircuitChanged(baseCirc);
            }
            else {
                // Creating a junction
                createJunction(baseSeg, joinSeg, baseTile, joinTile);
            }


            return baseCirc;

        }

        if(baseType == SegmentType.Junction && joinType == SegmentType.Wire) {
            // Base junction is having segments joined to it - only when constructing a new junction
            

            // Add the joined allTiles to the base circuit
            baseCirc.allTilesInCircuit.AddRange(joinCirc.allTilesInCircuit);


            updateCircuitReferences(baseCirc, joinCirc);
            //// Change the circuit ref on the joined tiles to the base circuit
            //foreach (Tile t in joinCirc.allTilesInCircuit) {
            //    if (t.installedEntity != null) {
                    
            //        if (t.installedEntity.circSeg != null) {
            //            t.installedEntity.circSeg.circuit = baseCirc;
            //        }
            //        else if (t.installedEntity.circJunc != null) {
            //            t.installedEntity.circJunc.circuit = baseCirc;
            //        }
            //    }
            //    else {
            //        Debug.LogError("Tile in circuit had no installed entity! - Tile: " + t.ToString());
            //    }
            //}

            

            allCircuits.Remove(joinCirc);


            return baseCirc;

        }
        if(baseType == SegmentType.Wire && joinType == SegmentType.Junction) {

            // Base segment is having a junction joined to it (built segment next to existing junction)
            
            // Add the joined allTiles to the base circuit
            baseCirc.allTilesInCircuit.AddRange(joinCirc.allTilesInCircuit);

            transferSegsAndJuncs(baseCirc, joinCirc);

            updateCircuitReferences(baseCirc, joinCirc);
            
            allCircuits.Remove(joinCirc);


            return baseCirc;
        }

        // Joining component/wire
        if ((baseType == SegmentType.Wire && joinType == SegmentType.Component) || (baseType == SegmentType.Component && joinType == SegmentType.Wire)) {




        }

        return null;

    }

    public void transferSegsAndJuncs(Circuit baseCirc, Circuit joinCirc) {

        // Add any junctions
        foreach (Circuit.Junction junc in joinCirc.juncs) {
            if (baseCirc.juncs.Contains(junc) == false) {
                baseCirc.juncs.Add(junc);
            }
        }

        // Add any segments not directly joined
        baseCirc.segments.AddRange(joinCirc.segments);
        // Change reference on all added segments 
        foreach (Circuit.Segment seg in baseCirc.segments) {
            foreach (Tile t in seg.allSegmentTiles) {
                t.installedEntity.circSeg.circuit = baseCirc;
            }
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
        //// Change the circuit ref on the joined tiles to the base circuit
        //foreach (Tile t in joinSeg.allSegmentTiles) {
        //    t.installedEntity.circSeg.circuit = baseSeg.circuit;
        //}

        // Add the joined allTiles to the base circuit
        baseSeg.circuit.allTilesInCircuit.AddRange(joinSeg.circuit.allTilesInCircuit);

        allCircuits.Remove(joinSeg.circuit);
            
        // Remove debug objects from the old joining circuit
        //destroyDebugObjects(joinSeg.circuit);

        baseSeg.length = baseSeg.getSegmentLength();


    }


    public void createJunction(Circuit.Segment baseSeg, Circuit.Segment joinSeg, Tile baseTile, Tile joinTile) {



        if (baseTile == baseSeg.endTile || baseTile == baseSeg.startTile) {
            // baseTile is end being joined into the middle of another segment (joinTile is not start/end of joinSeg)
            if (joinTile.installedEntity != null) {
                removeEntityFromCircuit(joinTile.installedEntity);
            }
            else {
                Debug.LogError("Trying to create junction with no installed entity!");
            }

            // Join seg becomes standalone circuit segment - should not join to anything until junc created

            //Circuit joinCircSeg = new Circuit(baseSeg);


            Circuit.Junction junc = new Circuit.Junction(joinTile);
            Circuit juncCirc = new Circuit(junc);

        }
        else {
            // baseTile is in the middle of the base segment - joinTile start/end of joinSeg
            if (baseTile.installedEntity != null) {
                removeEntityFromCircuit(baseTile.installedEntity);
            }
            else {
                Debug.LogError("Trying to create junction with no installed entity!");
            }

            // Join seg becomes standalone circuit segment - should not join to anything until junc created

            //Circuit joinCircSeg = new Circuit(joinSeg);


            Circuit.Junction junc = new Circuit.Junction(baseTile);
            Circuit juncCirc = new Circuit(junc);

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

    
    public void removeEntityFromCircuit(Entity entity) {

        Tile removedTile = entity.rootTile;
        Circuit.Segment baseSeg = entity.circSeg;

        // Remove any old debug objects (from entire circuit)
        //if(entity.circSeg != null && entity.circSeg.circuit != null) {
        //    foreach(GameObject go in entity.circSeg.circuit.allDebugObjects) {
        //        Destroy(go);
        //    }
        //}

        if (entity.entityType == EntityType.WirePiece) {

            // Entity was removed from start - set new start tile
            if (baseSeg.startTile == entity.rootTile) {
                baseSeg.allSegmentTiles.Remove(entity.rootTile);
                baseSeg.startTile = getNearestTileInSegment(removedTile, baseSeg);
                entity.circSeg = null;
                triggerCircuitChanged(baseSeg.circuit);
            }
            // Entity removed from end
            else if (baseSeg.endTile == entity.rootTile) {
                entity.circSeg.allSegmentTiles.Remove(entity.rootTile);
                baseSeg.endTile = getNearestTileInSegment(removedTile, baseSeg);
                entity.circSeg = null;
                triggerCircuitChanged(baseSeg.circuit);
            }
            // Entity being removed from middle - must split circuit
            else {
                entity.circSeg = null;

                splitSegment(baseSeg, entity.rootTile);

            }

            

        }
    }

    public Tile getNearestTileInSegment(Tile baseTile, Circuit.Segment seg) {

        return TileController.instance.getCommonTile(baseTile.getNeighbouringTiles(), seg.allSegmentTiles);

    }

    

}
