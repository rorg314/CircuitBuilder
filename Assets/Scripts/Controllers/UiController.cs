using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiController : MonoBehaviour {
    public static UiController instance;

    
    public Dictionary<string, Sprite> uiSpritesMap;


    // Start is called before the first frame update
    void Start() {
        
        if(instance == null) {
            instance = this;
        }

        uiSpritesMap = new Dictionary<string, Sprite>();

        loadUISprites();

        // Initisliase the tile cursor (prevTile set to zero at start)
        initTileCursor(TileController.instance.zeroTile);

        TileController.instance.cbMouseEnterNewTile += moveTileCursor;

        MasterController master = MasterController.instance;

    }

    //Load all component sprites
    void loadUISprites() {

        Sprite[] UISprites = Resources.LoadAll<Sprite>("Sprites/UI");

        foreach (Sprite s in UISprites) {
            //Debug.Log(s.name);

            uiSpritesMap.Add(s.name, s);
        }

    }


    //////////////////////////////////////////////////////////////////////////////////////////
    ///                                      TILE CURSOR
    //////////////////////////////////////////////////////////////////////////////////////////

    // Tile to display the hover cursor
    Tile hoverTile;
    // If a tile was selected
    Tile selectedTile;
    // Previous mouse position (start at zero)
    //Tile prevTile = TileController.instance.tileGrid.GetGridObject(Vector3.zero);



    // Create the initial cursor at the starting prevTile = zero (to be moved)
    GameObject tileCursor_go;
    public void initTileCursor(Tile prevTile) {

        tileCursor_go = new GameObject();
        // Set local scale based on grid cellSize (local y corresponds to global z here since rotated by 90)
        tileCursor_go.transform.localScale = new Vector3(TileController.instance.cellSize, TileController.instance.cellSize, 1f);

        tileCursor_go.transform.position = prevTile.getTileWorldPositon() + TileController.instance.tileCentreOffset;
        tileCursor_go.transform.Rotate(new Vector3(90, 0, 0), Space.Self);
        SpriteRenderer sr = tileCursor_go.AddComponent<SpriteRenderer>();
        sr.sprite = uiSpritesMap["selectorCursor"];

        sr.sortingLayerName = "UI";

    }

    // Move the tile cursor 
    public void moveTileCursor(Tile newTile) {

        tileCursor_go.transform.position = newTile.getTileWorldPositon() + TileController.instance.tileCentreOffset;

    }

    //////////////////////////////////////////////////////////////////////////////////////////
    ///                                POLARITY DISPLAY
    //////////////////////////////////////////////////////////////////////////////////////////

    public void hidePolarity(Entity entity) {

        // Destroy the old polarity objects if existing
        if (entity.terminalList_go.Count > 0) {
            //Debug.Log("Removing terminals " + entity.ToString());
            foreach (GameObject obj in entity.terminalList_go) {
                Destroy(obj);
            }
            entity.terminalList_go.Clear();
        }

    }


    // Show the polarity sprite on the respective terminals
    public void showPolarity(Entity entity) {
        // Remove any old polarity sprites
        hidePolarity(entity);

        // Create the new terminal sprite objects - loop creates pos (0) and then neg (1)
        string[] names = {"pos", "neg"};

        for (int i = 0; i < 2; i++) {

            GameObject terminal_go = new GameObject();
            terminal_go.name = entity.ToString() + "_" + names[i];
            terminal_go.transform.localScale = new Vector3(TileController.instance.cellSize, TileController.instance.cellSize, 1f);

            if (i == 0) {
                terminal_go.transform.position = entity.posTerminal.terminalTile.getTileWorldPositon() + TileController.instance.tileCentreOffset;
            }
            else { 
                terminal_go.transform.position = entity.negTerminal.terminalTile.getTileWorldPositon() + TileController.instance.tileCentreOffset;
            }
            
            terminal_go.transform.Rotate(90, 0, 0, Space.Self);
            
            SpriteRenderer sr = terminal_go.AddComponent<SpriteRenderer>();
            sr.sprite = uiSpritesMap[names[i]];

            entity.terminalList_go.Add(terminal_go);

            sr.sortingLayerName = "UI";
            sr.sortingOrder = 0;
        }
        

    }
    
    
    
    
    
    public void displayAllComponentPolarity() {



    }


}
