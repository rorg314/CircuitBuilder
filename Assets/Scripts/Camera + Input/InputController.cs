using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class InputController : MonoBehaviour{

    public static InputController Instance { get; protected set; }

    //Defines whether touch or keyboard input
    bool touchInput;
    //Defines if in free camera mode
    public bool cameraMode { get; set; }
   
    Vector3 panDirection;

    float zoom;

    public event Action Rpressed;
    
    public bool breakk;

    private void Start() {
        Instance = this;

        touchInput = false;
        cameraMode = true;
        panning = false;
        zooming = false;
        panDirection = Vector3.zero;

        Rpressed += testR;
    }

    public void testR() {
        Debug.Log("R press");
    }


    public void InputUpdate() {
        //Process KBM input
        if (touchInput == false) {

            UpdateCamera();

            processLMB();
            processRMB();

            processR();

            if (Input.GetKeyDown("f")) {
                breakk = true;
            }
            if (Input.GetKeyUp("f")) {
                breakk = false;
            }
        }

        //Process touch input
        if (touchInput == true) {





        }

    }

    public void UpdateCamera() {


        //Default is camera mode
        if (cameraMode) {

            //Process WASD for panDirection
            panDirection = processWASD();
            //Send to CameraController
            if (panning) {
                CameraController.instance.PanCamera(panDirection);
            }

            //Process scroll for zoom
            zoom = processScroll();
            //Send to controller
            if (zooming) {
                CameraController.instance.ZoomCamera(zoom);
            }

            ////Process QE input for rotation (returns 0 if off, 1-Q, 2-E)
            //QEindex = processQE();
            //GFindex = processGF();
            //if (rotating) {
            //    CameraController.instance.TurnCamera(QEindex);
            //    CameraController.instance.TurnRFCamera(GFindex);
            //}

        }

        //Selected some object - sets mode to focus mode

    }



    public void processR() {

        if (Input.GetKeyDown("r")) {
            
            Rpressed?.Invoke();
        }

    }


    
    public void processLMB() {

        //Do nothing if over UI
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        if (Input.GetMouseButtonDown(0)) {
            // LMB clicked down

            // Set the previewTile as null to avoid the new entity being removed when exiting the drag
            // (preview callback would get set which then calls removeOldPreview on the first clicked tile)
            BuildController.instance.removeOldPreview();
            
            BuildController.instance.previewEntity = null;
            
            // Tile that was clicked
            Tile clickedTile = TileController.instance.getTileUnderMouse();

            //Debug.Log(clickedTile.getThisTilePositon());

            // Build the currently selected entity on the clicked tile
            BuildController.instance.buildSelectedOnTile(clickedTile);

            // Unregister the preview callback and replace with build
            TileController.instance.cbMouseEnterNewTile -= BuildController.instance.previewSelectedOnTile;
            // Register the callback to build when entering a new tile
            TileController.instance.cbMouseEnterNewTile += BuildController.instance.buildSelectedOnTile;
            
           
        }

        if (Input.GetMouseButtonUp(0)) {
            // Unregister build 
            TileController.instance.cbMouseEnterNewTile -= BuildController.instance.buildSelectedOnTile;
            // Register preview
            TileController.instance.cbMouseEnterNewTile += BuildController.instance.previewSelectedOnTile;
        }


    }

    public void processRMB() {

        //Do nothing if over UI
        if (EventSystem.current.IsPointerOverGameObject()) { return; }

        if (Input.GetMouseButtonDown(1)) {

            // Tile that was clicked
            Tile clickedTile = TileController.instance.getTileUnderMouse();
            // Remove clicked entity if not null
            if (BuildController.instance.entityUnderMouse != null) {
                EntitySpriteController.instance.onEntityRemoved(BuildController.instance.entityUnderMouse);
            }
            
            // Register the callback to remove entity when entering a new tile
            TileController.instance.cbMouseEnterNewTile += BuildController.instance.removeMouseoverEntity;

        }

        if (Input.GetMouseButtonUp(1)) {
            // Unregister remove 
            TileController.instance.cbMouseEnterNewTile -= BuildController.instance.removeMouseoverEntity;
            
        }


    }

    bool panning;
    private Vector3 processWASD() {
        //Basis directions in camera plane w/s z-axis, a/d x-axis
        Vector3 w = new Vector3(0, 0, 1);
        Vector3 a = new Vector3(-1, 0, 0);
        Vector3 s = new Vector3(0, 0, -1);
        Vector3 d = new Vector3(1, 0, 0);

        //Process WASD input - convert to panDirection -> send to CameraController
        //Add direction when key down
        if (Input.GetKey("w")) { panning = true; panDirection += w; }
        if (Input.GetKey("a")) { panning = true; panDirection += a; }
        if (Input.GetKey("s")) { panning = true; panDirection += s; }
        if (Input.GetKey("d")) { panning = true; panDirection += d; }
        //Subtract direction when key up
        if (Input.GetKeyUp("w") || Input.GetKeyUp("s")) { panning = false; panDirection.y = 0; }
        if (Input.GetKeyUp("a") || Input.GetKeyUp("d")) { panning = false; panDirection.x = 0; }

        //Get overall pan direction
        panDirection = panDirection.normalized * 0.1f;

        return panDirection;
    }


    //For zooming with scroll wheel
    bool zooming;
    private float processScroll() {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        float scrollSpeed = 5f;


        float zoom = scroll * scrollSpeed * Time.deltaTime * 50;
        //zoom = Mathf.Clamp(zoom, minY, maxY);

        if (scroll == 0) { zooming = false; return 0f; }
        else {
            zooming = true;
            return zoom;
        }
    }



}
