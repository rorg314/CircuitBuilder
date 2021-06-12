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
    public List<Tile> allTiles;
    

    

    // A circuit is equivalent to a closed node graph with directed edges 
    // Any newly created entity spawns a new circuit - will be merged into other circuits 
    

    // Constructor for single entity circuits
    public Circuit(Entity entity) {

        // Create a new circuit containing this entity

        this.juncs = new List<WireJunction>();
        this.segments = new List<CircuitSegment>();
        this.allTiles = new List<Tile>();

        // Create a new circuit - will be merged with others if found 
        
        this.allTiles.Add(entity.rootTile);
        entity.circuit = this;

        if (entity.entityType == EntityType.WirePiece) {
            // Create a single piece long wire segment and add to circuit
            CircuitSegment seg = new CircuitSegment(entity);
            segments.Add(seg);
            // Set reference on entity to this segment
            entity.circSegment = seg;
            // Set references to parent circuit on segment (already set on entity)
            seg.parentCircuit = this;
        }


        Debug.Log("Number of circuits before: " + CircuitController.instance.allCircuits.Count);

        // Check for neighbouring circuits and append if appropriate
        List<Circuit> neighbourCircs = checkForNeighbourCircuits(entity);


        if (neighbourCircs.Count == 2) {
            // Need to join two circuits - append to one then join
            //appendCircuit(neighbourCircs[0], entity);
            //entity.circuit = neighbourCircs[0];

            Circuit baseCirc = appendCircuit(neighbourCircs[0], this, entity);
            
            CircuitController.instance.joinCircuits(baseCirc, neighbourCircs[1], entity.rootTile);
        }

        else if (neighbourCircs.Count == 1) {
            // Append to the existing circuit
            //appendCircuit(neighbourCircs[0], entity);

            appendCircuit(neighbourCircs[0], this, entity);
            
        }

        else if (neighbourCircs.Count == 0) {
            // Created a new standalone circuit
            CircuitController.instance.allCircuits.Add(this);

            CircuitController.instance.triggerCircuitChanged(this);
        }

        Debug.Log("Number of circuits after: " + CircuitController.instance.allCircuits.Count);
        

        

    }
    // Use to append a single entity to this existing circuit
    public Circuit appendCircuit(Circuit baseCirc, Circuit appendCirc, Entity entity) {

        // Get the tile in the base that this entity is appending to (returns first found)
        Tile baseTile = CircuitController.instance.getBaseTile(baseCirc, entity.rootTile);

        // Join the circuits
        CircuitController.instance.joinCircuits(baseCirc, appendCirc, baseTile);
        // Add the reference to the base circuit on the entity
        entity.circuit = baseCirc;
        // Add the appended tile to the base circ
        baseCirc.allTiles.Add(entity.rootTile);

        CircuitController.instance.triggerCircuitChanged(baseCirc);

        return baseCirc;
    }

    // Check if a newly created circuit should be added to any surrounding circuits
    public List<Circuit> checkForNeighbourCircuits(Entity entity) {
        // New circ will contain one entity only
        //Entity entity = newCirc.entities[0];

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

        // The debug arrow object for this segment
        public GameObject debugArrow;

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
                this.startTile = entity.posTerminal.terminalTile;
                this.endTile = entity.negTerminal.terminalTile;
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

            // Tiles neighbouring the appended piece 
            List<Tile> neighbours = entity.rootTile.getNeighbouringTiles();

            foreach(Tile neighbour in neighbours) {

                if (startTile == neighbour) {
                    // Appending to start of wire 
                    startTile = entity.rootTile;
                    allSegmentTiles.Add(entity.rootTile);
                    entity.circSegment = this;
                    this.length = getSegmentLength();
                    return;
                }
                else if (endTile == neighbour) {
                    // Appending to start of wire 
                    endTile = entity.rootTile;
                    allSegmentTiles.Add(entity.rootTile);
                    entity.circSegment = this;
                    this.length = getSegmentLength();
                    return;
                }
                
                continue;

            }
            
            
            // Segment was not joining on the start/end - must create junction instead





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

