using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum SegmentType { Wire, Component, Junction};

public class Circuit {
    
    // Junctions in the circuit
    public List<WireJunction> juncs;
    // Wire segments
    public List<CircuitSegment> segments;
    // All entities in circuit
    public List<Entity> entities;
    

    

    // A circuit is equivalent to a closed node graph with directed edges 
    // Any newly created entity spawns a new circuit - will be merged into other circuits 
    public Circuit(List<CircuitSegment> segments, List<WireJunction> junctions, List<Entity> components) { 
        
        this.juncs = junctions;
        this.segments = segments;
        this.entities = components;

    }

    // Constructor for single entity circuits
    public Circuit(Entity entity) {

        // Create a new circuit containing this entity

        this.juncs = new List<WireJunction>();
        this.segments = new List<CircuitSegment>();
        this.entities = new List<Entity>();

        this.entities.Add(entity);

        Debug.Log("Number of circuits before: " + CircuitController.instance.allCircuits.Count);

        // Check for neighbouring circuits and append if appropriate
        List<Circuit> neighbourCircs = checkForNeighbourCircuits(this);


        if (neighbourCircs.Count > 1) {
            // Need to merge circuits

        }

        else if (neighbourCircs.Count == 1) {
            // Append to the existing circuit
            neighbourCircs[0].appendCircuit(entity);
        }

        else if (neighbourCircs.Count == 0) {
            // Create a new circuit
            CircuitController.instance.allCircuits.Add(this);
            
            if (entity.entityType == EntityType.WirePiece) {
                // Create a single piece long wire segment and add to circuit
                CircuitSegment seg = new CircuitSegment(entity);
                segments.Add(seg);
                // Set reference on entity to this segment
                entity.circSegment = seg;
                // Set references to parent circuit on segment (already set on entity)
                seg.parentCircuit = this;
            }
        }

        Debug.Log("Number of circuits after: " + CircuitController.instance.allCircuits.Count);


    }
    // Use to append a single entity to this existing circuit
    public void appendCircuit(Entity entity) {
        

        // Append a single wire piece to the circuit
        if (entity.entityType == EntityType.WirePiece) {
            List<Tile> neighbours = entity.rootTile.getNeighbouringTiles();

            List<CircuitSegment> neighbourSegments = CircuitController.instance.getNeighbourCircuitSegments(neighbours);

            if(neighbourSegments.Count == 0) {
                // Did not find any neighbour segments - check if wire adjacent to component
                List<Entity> neighbourComponents = CircuitController.instance.getNeighbourComponentEntities(neighbours);
                if(neighbourComponents.Count == 0) {
                    Debug.LogWarning("Found no nearby circuits to append to!");
                }
                if(neighbourComponents.Count == 1) {
                    // Create and add the wire segment to the circuit - only if circuit of found component matches the circuit appending to
                    if(neighbourComponents[0].circuit == this) {
                        CircuitSegment seg = new CircuitSegment(entity);
                    }
                }
                if(neighbourComponents.Count > 1) {
                    // Append to one then merge
                }

            }
            if(neighbourSegments.Count == 1) {
                // Append the single wire piece
                neighbourSegments[0].appendWirePiece(entity);
            }

        }

        

    }

    // Check if a newly created circuit should be added to any surrounding circuits
    public List<Circuit> checkForNeighbourCircuits(Circuit newCirc) {
        // New circ will contain one entity only
        Entity entity = newCirc.entities[0];

        // Check if the newly created circuit is surrounded by any existing circuits
        List<Tile> neighbours = new List<Tile>();
        if (entity.entityType == EntityType.Component) {
            // Only want touching surrounded tiles so doubleRadius = false
            neighbours = entity.getSurroundingTiles(false);

        }
        else if (entity.entityType == EntityType.WirePiece) {

            neighbours = entity.rootTile.getNeighbouringTiles();
        }
        List<Circuit> neighbourCircs = CircuitController.instance.getNeighbourCircuits(neighbours);

        return neighbourCircs;
        

    }

    // Segment spans from start to end tile - can be next to a junction tile
    // Segment is graph edge
    public class CircuitSegment {
        // Circuit this wire segment belongs to
        public Circuit parentCircuit;

        public SegmentType segmentType;
        
        // Length of the wiresegment in tiles
        public int length;
        // Start and end tiles
        public Tile startTile;
        public Tile endTile;
        // All tiles
        public List<Tile> allSegmentTiles;

        // PHYSICS //
        // Voltage accross segment ends
        public float startVoltage;
        public float endVoltage;
        // Current through this segment
        public float current;

        // Construct a single circuit segment - circuit reference set outside constructor
        public CircuitSegment(Entity entity) {

            this.allSegmentTiles = new List<Tile>();

            if (entity.entityType == EntityType.WirePiece) {
                // Segment is directed based on current flow - from start (pos) to end (neg) tile - negative current flows from end to start
                this.startTile = entity.rootTile;
                this.endTile = entity.rootTile;
                // Segment length
                this.length = this.getSegmentLength();
                allSegmentTiles.Add(startTile);
                entity.circSegment = this;
            }
            
            if(entity.entityType == EntityType.Component) {
                this.startTile = TileController.instance.tileGrid.GetGridObject(entity.rootTile.getTileCoordinates() + entity.posTileOffset);
                this.endTile = TileController.instance.tileGrid.GetGridObject(entity.rootTile.getTileCoordinates() + entity.negTileOffset);
                this.length = 1;
                allSegmentTiles.AddRange(entity.installedTiles);

                entity.circSegment = this;
            }


            // Segment either ends on component or at junction

        }

        public int getSegmentLength() {

            Vector3Int start = startTile.getTileCoordinates();
            Vector3Int end = endTile.getTileCoordinates();

            Vector3Int diff = end - start;

            return Mathf.Abs(diff.x) + Mathf.Abs(diff.z);

        }

        // Add a single wire piece to the start or end of a segment (entity must be wire)
        public void appendWirePiece(Entity entity) {

            //if(entity.entityType != EntityType.WirePiece) {
            //    Debug.LogError("Trying to add a non wire entity to wire segment! -- " + entity.ToString());
            //}

            

            if (startTile == entity.rootTile) {
                // Appending to start of wire 
                startTile = entity.rootTile;
                allSegmentTiles.Add(entity.rootTile);
            }
            else if (endTile == entity.rootTile) {
                // Appending to start of wire 
                endTile = entity.rootTile;
                allSegmentTiles.Add(entity.rootTile);
            }
            else {
                // Segment was not joining on the start/end - must create junction instead

            }
            entity.circSegment = this;
            this.length = getSegmentLength();
            

        }

        public void appendComponent(Entity entity) {




        }
        
    }

    

    // Defines a junction between (at most four) wire segments
    // Equivalent to a graph node
    public class WireJunction {

        // Segments flowing into this junction (ending at this junction)
        public List<CircuitSegment> inSegs;
        // Segs flowing out - starting at this junc
        public List<CircuitSegment> outSegs;
        // Total current flowing through this junction
        public float totalCurrent;
        // Maps each segment to the respective current 
        public Dictionary<CircuitSegment, float> juncSegmentCurrentDict;


    }

    // Defines a closed loop in the circuit - contains segments, junctions and components in the loop
    public class CircuitLoop {

        // All segments within this loop
        List<CircuitSegment> loopSegments;
        // All junctions this loop covers
        List<WireJunction> loopJunctions;
        // All components within this loop
        List<Entity> loopComponents;

    }

    // Merge two segments
    //public WireSegment mergeSegments(WireSegment seg1, WireSegment seg2) {

        
    //    //if(seg1.startTile == seg2.startTile) {
    //    //    joinTile = seg1.startTile;
    //    //}
    //    //if(seg1.startTile == seg2.endTile) {
    //    //    joinTile = seg1.startTile;
    //    //}
    //    //if(seg1.endTile == seg2.startTile) {
    //    //    joinTile = seg1.endTile;
    //    //}
    //    //if(seg1.endTile == seg2.endTile) {
    //    //    joinTile = seg1.endTile;
    //    //}



    //}

    //public Tile findSegmentIntersection(WireSegment seg1, WireSegment seg2) {


    //}










    


    


    



}



//Old
//public class WirePiece {

//    public Tile tile;
    
//    public string neighbours;

//    public Entity wirePieceEntity;

//    public WirePiece(Tile t) {

//        this.tile = t;
        

//    }
//}

