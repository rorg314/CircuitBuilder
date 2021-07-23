using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    public static CameraController instance;

    public Camera mainCamera;


    private void Awake() {
        if (instance == null) { instance = this; }
        else { Debug.LogError("More than one cameracontroller instance"); }
        
        mainCamera = Camera.main;
        
        //Move camera to centre of grid
        mainCamera.transform.position = TileController.instance.centreTile.getTileWorldPositon();
        // Move upwards 
        mainCamera.transform.position += new Vector3(0, 50, 0);
    }

    //Min and max heights
    float minY = 0.25f;
    float maxY = 250f;
    //Base camera rotation angle - set xrot to 90 for topdown view
    Vector3 baseAngles = new Vector3(70f, 0f, 0f);
    Vector3 currentAngles = new Vector3(0f, 0f, 0f);
    //Height below which camera starts rotating - set zero to disable
    float turnHeight = 0f;
    //Max amount the camera will turn to
    float maxTurnAngle = 20f;
    float zoomSpeed = 7f;
    public void ZoomCamera(float zoom) {

        //Global pos of camera (global zoom)
        Vector3 pos = mainCamera.transform.position;
        pos.y -= zoom * zoomSpeed;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        if (pos.y < turnHeight) {

            float percent = 1 - pos.y / turnHeight;
            float turnAmount = maxTurnAngle * percent;
            currentAngles.x = baseAngles.x - turnAmount;

        }
        else { currentAngles.x = baseAngles.x; }

        mainCamera.transform.position = pos;
        mainCamera.transform.rotation = Quaternion.Euler(currentAngles);
    }

    float panSpeed = 10f;
    Vector3 localTranslate;
    public void PanCamera(Vector3 dir) {

        //dir = Quaternion.Euler(currentAngles) * dir;
        localTranslate = Vector3.ProjectOnPlane(dir, Vector3.up);

        //Pan camera using rotation adjusted transform
        mainCamera.transform.Translate(localTranslate * panSpeed * Time.deltaTime * 50, Space.World);

    }
}
