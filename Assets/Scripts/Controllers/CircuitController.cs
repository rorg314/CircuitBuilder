using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircuitController : MonoBehaviour {

    public static CircuitController instance;


    // Start is called before the first frame update
    void Start() {

        instance = this;

    }


    // Use to append a single circuit entity to an existing circuit
    public Circuit appendCircuit(Circuit baseCirc, Entity entity) {

        // Append a single wire piece to the circuit
        if(entity.entityType == EntityType.WirePiece) {
            Tile[] neighbours = entity.rootTile.getNeighbouringTiles();

            foreach(Tile t in neighbours) {
                // Check if neighbour contains any wiresegments
                if(t.wireSegment != null) {

                    

                }

            }

        }
        

    }



    // Merge circ will be single entity circ, the end point of a wire or a connecting component -
    // mergeTile will be the tile that connects the two circuits to be merged (to avoid looping through entire merge circuit)
    public Circuit mergeCircuits(Circuit baseCirc, Circuit mergeCirc, Tile mergeTile) {

        baseCirc.entities.AddRange(mergeCirc.entities);
        
        
        // Join any connected wire segments
        foreach(Circuit.WireSegment baseSeg in baseCirc.segments) {

            foreach(Circuit.WireSegment joinSeg in mergeCirc.segments) {
                // Check if segments overlap or are adjacent 
                foreach(Tile baseTile in baseSeg.allSegmentTiles)

            }


        }
        
        
        baseCirc.segments.AddRange(mergeCirc.segments);
        baseCirc.juncs.AddRange(mergeCirc.juncs);

    }







}
