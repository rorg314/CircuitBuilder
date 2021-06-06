using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MasterController : MonoBehaviour {

    public static MasterController instance { get; protected set; }

    //////////////////////////////////////////////////////////////////////////////////////////
    ///                              MASTER REFERENCE LISTS
    //////////////////////////////////////////////////////////////////////////////////////////

    public List<Entity> allEntities;
    public List<Entity> allWireEntities;
    public List<Entity> allComponentEntities;



    // Start is called before the first frame update
    void Awake(){

        instance = this;

        allEntities = new List<Entity>();
        allWireEntities = new List<Entity>();
        allComponentEntities = new List<Entity>();
        
    }

    
    // Time between tileUnderMouse checks 
    float tileMouseCheckTime = 1/20;
    // Counts up to 1/tileCheckFreq (seconds) then resets
    float tileMouseCheckTimer = 0;

    // Update is called once per frame
    void Update() {

        // Update input controller every frame
        InputController.Instance.InputUpdate();


        // Increment the timer
        tileMouseCheckTimer += Time.deltaTime;



        if(tileMouseCheckTimer > tileMouseCheckTime) {

            TileController.instance.checkForNewTileUnderMouse();

        }


    }
}
