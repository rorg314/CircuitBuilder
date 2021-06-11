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
            Vector3 dir = seg.endTile.getTileWorldPositon() - seg.startTile.getTileWorldPositon();
            Vector3 startPos = seg.startTile.getTileWorldPositon() + TileController.instance.tileCentreOffset;
            //Debug.DrawRay(seg.startTile.getTileWorldPositon() + TileController.instance.tileCentreOffset, dir , Color.red, Mathf.Infinity);
            //Debug.Log("drew");
            //DrawArrow.ForDebug(startPos, dir, Color.red);

            GameObject segArrow = new GameObject();

            seg.debugArrow = segArrow;

            SpriteRenderer sr = segArrow.AddComponent<SpriteRenderer>();
            sr.sprite = debugArrowSprite;
            
        }

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



    // Merge circ will be single entity circ, the end point of a wire or a connecting component -
    // mergeTile will be the tile that connects the two circuits to be merged (to avoid looping through entire merge circuit)
    public Circuit mergeCircuits(Circuit baseCirc, Circuit mergeCirc, Tile mergeTile) {

        baseCirc.entities.AddRange(mergeCirc.entities);
        
        
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
