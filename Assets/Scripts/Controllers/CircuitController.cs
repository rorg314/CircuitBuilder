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
            if (t.installedEntity != null && t.installedEntity.circSegment != null) {
                neighbourSegments.Add(t.installedEntity.circSegment);
            }
        }

        return neighbourSegments;
    }

    public List<Circuit> getNeighbourCircuits(List<Tile> neighbourTiles) {

        List<Circuit> neighbourCircs = new List<Circuit>();
        foreach (Tile t in neighbourTiles) {
            if (t.installedEntity != null && t.installedEntity.circuit != null) {
                // Append to neighbour circs
                neighbourCircs.Add(t.installedEntity.circuit);
            }
        }

        return neighbourCircs;

    }
    // Find the tile in the base that this is adjacent to this append tile
    public Tile getBaseTile(Circuit baseCirc, Tile appendTile) {

        return TileController.instance.getCommonTile(appendTile.getNeighbouringTiles(), baseCirc.allTiles);

    }

    // Find the tile in the joining circuit that connects to the base tile
    public Tile getJoinTile(Circuit baseCirc, Circuit joinCirc, Tile baseTile) {

        List<Tile> baseNeighbours = baseTile.getNeighbouringTiles();

        return TileController.instance.getCommonTile(baseNeighbours, joinCirc.allTiles);

    }

    public Circuit joinCircuits(Circuit baseCirc, Circuit joinCirc, Tile baseTile) {

        Tile joinTile = getJoinTile(baseCirc, joinCirc, baseTile);

        SegmentType baseType = baseTile.installedEntity.circSegment.segmentType;
        SegmentType joinType = joinTile.installedEntity.circSegment.segmentType;

        // If base/join is wire segment
        if (baseType == SegmentType.Wire && joinType == SegmentType.Wire) {
            
            // Joining wireseg to wireseg
            joinSegments(baseTile.installedEntity.circSegment, joinTile.installedEntity.circSegment, baseTile, joinTile);

        }



        return baseCirc;

    }

    // Segments must be NESW neighbours to join - joinTile will have just been appended to the base circuit
    public void joinSegments(Circuit.CircuitSegment baseSeg, Circuit.CircuitSegment joinSeg, Tile baseTile, Tile joinTile) {
        //Tile joinTile;
        //// Get the joining tile of the join segment (start or end)
        //List<Tile> endNeighbours = joinSeg.startTile.getNeighbouringTiles();
        ////List<Tile> startNeighbours = joinSeg.endTile.getNeighbouringTiles();
        
        //if(TileController.instance.getCommonTile(new List<Tile> { baseTile }, endNeighbours) != null) {
        //    // Joining using the end of joinSeg
        //    joinTile = joinSeg.endTile;
        //}
        //else {
        //    // Joining the start
        //    joinTile = joinSeg.startTile;
        //}


        // Destroy any debug preview on the join seg
        Destroy(joinSeg.debugArrow);
        // Remove the join seg from its circuit master list 
        joinSeg.parentCircuit.segments.Remove(joinSeg);
        // Replace the reference on all joinSegment tiles with the base segment
        foreach(Tile t in joinSeg.allSegmentTiles) {

            t.installedEntity.circSegment = baseSeg;

        }


        // Add all tiles to the base seg
        baseSeg.allSegmentTiles.AddRange(joinSeg.allSegmentTiles);

        if (baseSeg.endTile == baseTile) {
            // Joining to end of base

            if(joinTile == joinSeg.startTile) {
                // Joining start to end of base, new end is end of join seg
                baseSeg.endTile = joinSeg.endTile;
            }
            else {
                // Joining end to end of base 
                baseSeg.endTile = joinSeg.startTile;
            }


        }
        else {
            // Joining to start of base
            if (joinTile == joinSeg.startTile) {
                // Joining start to start of base, new start is end of join seg
                baseSeg.startTile = joinSeg.endTile;
            }
            else {
                // Joining end to start of base 
                baseSeg.startTile = joinSeg.startTile;
            }

        }

        baseSeg.length = baseSeg.getSegmentLength();

        triggerCircuitChanged(baseSeg.parentCircuit);


    }



    // Merge circ will be single entity circ, the end point of a wire or a connecting component -
    // mergeTile will be the tile that connects the two circuits to be merged (to avoid looping through entire merge circuit)
    public Circuit mergeCircuits(Circuit baseCirc, Circuit mergeCirc, Tile mergeTile) {

        //baseCirc.entities.AddRange(mergeCirc.entities);
        
        
        // Join any connected wire segments
        foreach(Circuit.CircuitSegment baseSeg in baseCirc.segments) {

            foreach(Circuit.CircuitSegment joinSeg in mergeCirc.segments) {
                // Check if segments overlap or are adjacent 
                foreach(Tile baseTile in baseSeg.allSegmentTiles) {

                }

            }


        }
        
        
        baseCirc.segments.AddRange(mergeCirc.segments);
        baseCirc.juncs.AddRange(mergeCirc.juncs);

        return baseCirc;
    }







}
