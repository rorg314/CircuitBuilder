using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum SegmentType { Wire, Component, Junction, Null};


public class Circuit {

    // Junctions in the circuit
    public List<Junction> juncs;
    // Wire segments
    public List<Segment> segments;
    // All tiles in circuit
    public List<Tile> allTilesInCircuit;
    // List of all debug objects in for this circuit
    public List<GameObject> allDebugObjects;



    // A circuit is equivalent to a closed node graph with directed edges 
    // Any newly created entity spawns a new circuit - will be merged into other circuits 


    // Constructor for single entity circuits
    public Circuit(Entity entity) {

        // Create a new circuit containing this entity

        this.juncs = new List<Junction>();
        this.segments = new List<Segment>();
        this.allTilesInCircuit = new List<Tile>();

        // Create a new circuit - will be merged with others if found 

        this.allTilesInCircuit.Add(entity.rootTile);

        // Check for neighbouring circuits and append if appropriate
        List<Circuit> neighbourCircs = checkForNeighbourCircuits(entity, this);

        // Check special case for making a new 4-way junction
        if (neighbourCircs.Count == 4) {
            // Create new junction from this entity and use to create a new circuit (this circuit discarded)
            Junction junc = new Junction(entity.rootTile);
            Circuit newCirc = new Circuit(junc);

        }
        else {
            // Proceed with this circuit creation
            CircuitController.instance.allCircuits.Add(this);

            // Create a segment and add to circuit
            Segment seg = new Segment(entity);
            segments.Add(seg);
            // Set reference on entity to this segment
            entity.circSeg = seg;
            // Set references to parent circuit on segment (already set on entity)
            seg.circuit = this;

            Debug.Log("Number of circuits before: " + CircuitController.instance.allCircuits.Count);

            
            if(neighbourCircs.Count == 1) {
                CircuitController.instance.joinCircuits(this, neighbourCircs[0], entity.rootTile);
            }
            else if(neighbourCircs.Count > 1) {
                // Join to the first found neighbour
                CircuitController.instance.joinCircuits(this, neighbourCircs[0], entity.rootTile);

                // Recursively check for new neighbours and join as appropriate
                List<Circuit> newNeighbours = checkForNeighbourCircuits(entity, this);
                if(newNeighbours.Count > 0) {
                    CircuitController.instance.joinCircuits(this, newNeighbours[0], entity.rootTile);
                }

            }

            else if (neighbourCircs.Count == 0) {
                // Created a new standalone circuit
                CircuitController.instance.triggerCircuitChanged(this);
            }

            Debug.Log("Number of circuits after: " + CircuitController.instance.allCircuits.Count);
        }
    }

    
    

    // For constructing with existing segment
    public Circuit(Segment segment) {

        segment.circuit = this;

        this.juncs = new List<Junction>();
        this.segments = new List<Segment>();
        this.allTilesInCircuit = new List<Tile>();

        this.allTilesInCircuit.AddRange(segment.allSegmentTiles);
        this.segments.Add(segment);

        CircuitController.instance.allCircuits.Add(this);

        // Check for neighbour circuits on either end of wiresegment
        if (segment.segmentType == SegmentType.Wire) {

            List<Tile> endNeighbours = segment.endTile.getNeighbouringTiles();
            List<Circuit> endCircs = CircuitController.instance.getNeighbourCircuitsExcludingThis(endNeighbours, this);

            List<Tile> startNeighbours = segment.startTile.getNeighbouringTiles();
            List<Circuit> startCircs = CircuitController.instance.getNeighbourCircuitsExcludingThis(startNeighbours, this);

            
            if (endCircs.Count > 0) {

                for (int i = 0; i < endCircs.Count; i++) {

                    Tile baseTile = TileController.instance.getCommonTile(endNeighbours, endCircs[i].allTilesInCircuit);
                    CircuitController.instance.joinCircuits(this, endCircs[i], baseTile);

                }

            }

            if (startCircs.Count > 0) {

                for (int i = 0; i < startCircs.Count; i++) {

                    Tile baseTile = TileController.instance.getCommonTile(startNeighbours, startCircs[i].allTilesInCircuit);
                    CircuitController.instance.joinCircuits(this, startCircs[i], baseTile);

                }

            }

            if(startCircs.Count == 0 && endCircs.Count == 0) {
                // Created a standalone circuit
                CircuitController.instance.triggerCircuitChanged(this);
            }

        }


    }


    // Construct with junction - merges surrounding circuits 
    public Circuit(Junction junc) {

        junc.circuit = this;

        this.juncs = new List<Junction>();
        this.segments = new List<Segment>();
        this.allTilesInCircuit = new List<Tile>();

        this.juncs.Add(junc);
        this.allTilesInCircuit.Add(junc.juncTile);

        CircuitController.instance.allCircuits.Add(this);

        // Merge with any surrounding circuits
        List<Tile> neighbours = junc.juncTile.getNeighbouringTiles();
        List<Circuit> neighbourCircs = CircuitController.instance.getNeighbourCircuitsExcludingThis(neighbours, this);
        // Join any found circuits to this base
        foreach (Circuit circ in neighbourCircs) {
            
            CircuitController.instance.joinCircuits(this, circ, junc.juncTile);
            
        }
        //CircuitController.instance.triggerCircuitChanged(this);

    }

    // Check if a newly created circuit should be added to any surrounding circuits
    public List<Circuit> checkForNeighbourCircuits(Entity entity, Circuit thisCircuit) {

        // Check if the newly created circuit is surrounded by any existing circuits
        List<Tile> neighbours = new List<Tile>();
        if (entity.entityType == EntityType.Component) {
            // Only want touching surrounded tiles so doubleRadius = false
            neighbours = entity.getSurroundingTiles(false);

        }
        else if (entity.entityType == EntityType.WirePiece) {

            neighbours = entity.rootTile.getNeighbouringTiles();
        }
        List<Circuit> neighbourCircs = CircuitController.instance.getNeighbourCircuitsExcludingThis(neighbours, thisCircuit);

        return neighbourCircs;


    }



    // Segment spans from start to end tile - can be next to a junction tile
    // Segment is graph edge
    public class Segment {
        // Circuit this wire segment belongs to
        public Circuit circuit;

        public SegmentType segmentType;

        // Length of the wiresegment in tiles
        public int length;
        // Start and end tiles
        public Tile startTile;
        public Tile endTile;
        // All tiles in segment - ordered from start to end tile 
        public List<Tile> allSegmentTiles;

        // List of connected segments 
        List<Segment> connectedSegments;

        // The debug arrow object for this segment
        public GameObject debugArrow;
        public int debugColourIndex;

        // PHYSICS //
        // Voltage accross segment ends
        public float startVoltage;
        public float endVoltage;
        // Current through this segment
        public float current;

        // Construct a single circuit segment 
        // PARENT CIRCUIT MUST BE SET OUTSIDE CONSTRUCTOR
        public Segment(Entity entity) {

            this.allSegmentTiles = new List<Tile>();
            this.connectedSegments = new List<Segment>();

            if (entity.entityType == EntityType.WirePiece) {
                // Segment is directed based on current flow - from start (pos) to end (neg) tile - negative current flows from end to start
                this.startTile = entity.rootTile;
                this.endTile = entity.rootTile;
                // Segment length
                this.length = this.getSegmentLength();
                allSegmentTiles.Add(startTile);
                entity.circSeg = this;
                this.segmentType = SegmentType.Wire;
            }

            if (entity.entityType == EntityType.Component) {
                this.startTile = entity.posTerminal.terminalTile;
                this.endTile = entity.negTerminal.terminalTile;
                this.length = 1;
                allSegmentTiles.AddRange(entity.installedTiles);

                entity.circSeg = this;
                this.segmentType = SegmentType.Component;
            }

            // Segment either ends on component or at junction

        }

        // Overload for constructing wire type segment with list of tiles -
        // PARENT CIRCUIT MUST BE SET OUTSIDE CONSTRUCTOR
        public Segment(List<Tile> allSegmentTiles) {

            this.allSegmentTiles = allSegmentTiles;
            this.connectedSegments = new List<Segment>();
            this.startTile = allSegmentTiles[0];
            this.endTile = allSegmentTiles[allSegmentTiles.Count - 1];
            this.length = getSegmentLength();
            // Must be a wire segment if given list of tiles
            this.segmentType = SegmentType.Wire;

            foreach (Tile t in allSegmentTiles) {
                if (t.installedEntity != null) {
                    t.installedEntity.circSeg = this;
                }
            }

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
    public class Junction {
        // Circuit this junction is part of - SET OUTSIDE CONSTRUCTOR
        public Circuit circuit;
        // Segments flowing into this junction (ending at this junction)
        public List<Segment> inSegs;
        // Segs flowing out - starting at this junc
        public List<Segment> outSegs;
        // Total current flowing through this junction
        public float totalCurrent;
        // Maps each segment to the respective current 
        public Dictionary<Segment, float> juncSegmentCurrentDict;
        // Junction tile
        public Tile juncTile;

        public GameObject juncDebugObject;


        public Junction(Tile juncTile) {

            this.juncTile = juncTile;

            if(juncTile.installedEntity != null) {

                juncTile.installedEntity.circSeg = null;
                juncTile.installedEntity.circJunc = this;

            }

            setJunctionSegments();

        }

        // Sets the junction segments from neighbouring tiles
        public void setJunctionSegments() {

            // Segments flowing into this junction (ending at this junction)
            inSegs = new List<Segment>();
            // Segs flowing out - starting at this junc
            outSegs = new List<Segment>();

            List<Tile> neighbours = juncTile.getNeighbouringTiles();

            foreach (Tile t in neighbours) {

                if (t.installedEntity != null && t.installedEntity.circSeg != null) {
                    // Seg flowing into junction
                    if (t.installedEntity.circSeg.endTile == t) {
                        inSegs.Add(t.installedEntity.circSeg);
                    }
                    else if (t.installedEntity.circSeg.startTile == t) {
                        outSegs.Add(t.installedEntity.circSeg);
                    }
                }

            }
        }


    }

    // Defines a closed loop in the circuit - contains segments, junctions and components in the loop
    public class CircuitLoop {

        // All segments within this loop
        List<Segment> loopSegments;
        // All junctions this loop covers
        List<Junction> loopJunctions;
        // All components within this loop
        List<Entity> loopComponents;

    }

}
