using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Circuit {
    
    // Junctions in the circuit
    public List<WireJunction> juncs;
    // Wire segments
    public List<WireSegment> segments;
    // All entities in circuit
    public List<Entity> entities;
    

    // Entities that comprise this wire 
    public List<Entity> wireEntities;

    // A circuit is equivalent to a closed node graph with directed edges 
    // Any newly created entity spawns a new circuit - will be merged into other circuits 
    public Circuit(List<WireSegment> segments, List<WireJunction> junctions, List<Entity> components) { 
        
        
        this.juncs = junctions;
        this.segments = segments;
        this.entities = components;

    }

    // Constructor for single entity circuits
    public Circuit(Entity entity) {

        



        // Create a new circuit containing this entity

        this.juncs = new List<WireJunction>();
        this.segments = new List<WireSegment>();
        this.entities = new List<Entity>();


        this.entities.Add(entity);

        if(entity.entityType == EntityType.WirePiece) {
            // Create a single piece long wire segment and add to circuit
            WireSegment seg = new WireSegment(entity.rootTile);
            segments.Add(seg);
        }


    }

    // Check if a newly created circuit should be added to any surrounding circuits
    public void checkForNeighbourCircuits(Circuit newCirc) {
        // New circ will contain one entity only
        Entity entity = newCirc.entities[0];

        // Check if the newly created circuit is surrounded by any existing circuits
        Tile[] neighbours = { };
        if (entity.entityType == EntityType.Component) {
            // Only want touching surrounded tiles so doubleRadius = false
            neighbours = entity.getSurroundingTiles(false);

        }
        else if (entity.entityType == EntityType.WirePiece) {

            neighbours = entity.rootTile.getNeighbouringTiles();
        }

        foreach (Tile t in neighbours) {

            if (t.installedEntity != null && t.installedEntity.circuit != null) {
                // Merge the circuits - add this construction to the already constructed circuit
                CircuitController.instance.mergeCircuits(t.installedEntity.circuit, this);

            }

        }

    }

    // Segment spans from start to end tile - either could be junction
    // Wire segment is graph edge
    public class WireSegment {
        // Circuit this wire segment belongs to
        public Circuit parentCircuit;
        
        // Length of the wiresegment in tiles
        public int length;
        // Start and end tiles
        public Tile startTile;
        public Tile endTile;
        // All tiles
        public List<Tile> allSegmentTiles;

        // PHYSICS //
        // Voltage accross wire ends - only changed when end meets a component -> out wire has lower voltage
        public float voltage;
        // Current through this wire segment
        public float current;

        // Construct a single wire segment piece
        public WireSegment(Tile tile) {

            // Segment is directed based on current flow - from start (pos) to end (neg) tile  
            this.startTile = tile;
            this.endTile = tile;
            // Segment length
            this.length = this.getSegmentLength();

            this.allSegmentTiles = new List<Tile>();

            allSegmentTiles.Add(startTile);


            // Check neighbour tiles for other segments and join if found
            Tile[] neighbours = tile.getNeighbouringTiles();

            foreach(Tile t in neighbours) {
                if(t.installedEntity != null && t.installedEntity.entityType == EntityType.WirePiece) {
                    // Found a neighbouring wire piece, join this segment to it

                }

            }



            // Segment either ends on component or at junction

        }

        public int getSegmentLength() {

            Vector3Int start = startTile.getTileCoordinates();
            Vector3Int end = endTile.getTileCoordinates();

            Vector3Int diff = end - start;

            return Mathf.Abs(diff.x) + Mathf.Abs(diff.z);

        }
        
    }

    

    // Defines a junction between (at most four) wire segments
    // Equivalent to a graph node
    public class WireJunction {

        // Segments flowing into this junction (ending at this junction)
        public List<WireSegment> inSegs;
        // Segs flowing out - starting at this junc
        public List<WireSegment> outSegs;
        // Total current flowing through this junction
        public float totalCurrent;
        // Maps each segment to the respective current 
        public Dictionary<WireSegment, float> juncSegmentCurrentDict;


    }

    // Defines a closed loop in the circuit - contains segments, junctions and components in the loop
    public class WireLoop {

        // All segments within this loop
        List<WireSegment> loopSegments;
        // All junctions this loop covers
        List<WireJunction> loopJunctions;
        // All components within this loop
        List<Entity> loopComponents;

    }

    // Merge two segments
    public WireSegment mergeSegments(WireSegment seg1, WireSegment seg2) {

        
        //if(seg1.startTile == seg2.startTile) {
        //    joinTile = seg1.startTile;
        //}
        //if(seg1.startTile == seg2.endTile) {
        //    joinTile = seg1.startTile;
        //}
        //if(seg1.endTile == seg2.startTile) {
        //    joinTile = seg1.endTile;
        //}
        //if(seg1.endTile == seg2.endTile) {
        //    joinTile = seg1.endTile;
        //}



    }

    public Tile findSegmentIntersection(WireSegment seg1, WireSegment seg2) {


    }










    


    


    



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

