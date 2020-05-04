using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Linq;

[System.Serializable]
public class GameData
{
    public string openContainerId = "";
    public string playerRoomId = "";
    public ItemData[] items = null;
}

[RequireComponent( typeof( AudioSource ) )]
public class ControlManager : MonoBehaviour
{
    static public ControlManager instance = null;

    [SerializeField] private bool m_debugIds = false;
    [SerializeField] private bool m_debugClicks = false;

    [Header( "Game Settings" )]
    [SerializeField] private bool m_skipSystemRooms = false;
    [SerializeField] private bool m_verbose = true;

    [Header( "Sprite Layering" )]
    [SerializeField] private int m_sortingOrderTakeable = 50;
    [SerializeField] private int m_sortingOrderDragging = 100;

    [Header( "Special Rooms" )]
    [SerializeField] private Room m_nowhereRoom = null;
    [SerializeField] private Room m_inventoryRoom = null;
    [SerializeField] private Room m_containerRoom = null;
    [SerializeField] private Room m_systemStartRoom = null;
    public Room GameStartRoom;

    [Header( "Interaction Rectangles" )]
    [SerializeField] private Rect m_inventoryRect = new Rect();
    [SerializeField] private Rect m_roomRect = new Rect();

    [Header( "Mouse Tweaks" )]
    [SerializeField] private float m_dragThreshold = 0.1f;
    [SerializeField] private float m_doubleClickThresholdSec = 0.3f;

    [Header( "UI" )]
    [SerializeField] private Button m_backButton = null;
    [SerializeField] private TextMeshProUGUI m_containerLabel = null;
    [SerializeField] private GameObject m_gameOverHud = null;
    [SerializeField] private Image m_helpUi = null;

    [Header( "Audio" )]
    [SerializeField] float m_musicTransitionTimeSec = 5.0f;

    [Header( "Menus" )]
    [SerializeField] GameObject m_menu = null;
    [SerializeField] GameObject m_helpMenu = null;

    public float DragThreshold { get { return m_dragThreshold; } }
    public float DoubleClickThresholdSec { get { return m_doubleClickThresholdSec; } }

    public bool ShouldEndGame { get; set; }
    public Room GameOverRoom { private get; set; }
    public Room NowhereRoom {  get { return m_nowhereRoom; } }

    public int SaveSlot { private get; set; }

    public int SortingOrderTakeable { get { return m_sortingOrderTakeable; } }
    public int SortingOrderDragging { get { return m_sortingOrderDragging; } }

    public bool DebugIds { get { return m_debugIds; } }
    public bool Verbose { get { return m_verbose; } set { m_verbose = value; } }

    private int m_turnCount = 0;

    private int TopSortingOrder {
        get {
            SpriteRenderer topSr = null;
            foreach ( var item in m_itemList ) {
                if ( item.location != Room.Current || item == Item.Selected )
                    continue;

                var curSr = item.GetComponent<SpriteRenderer>();
                if ( topSr == null || curSr.sortingOrder > topSr.sortingOrder )
                    topSr = curSr;
            }

            if ( topSr == null ) return 1;
            return topSr.sortingOrder + 1;
        }
    }

    public void AdvanceTurn() {
        ++m_turnCount;
    }

    public void SortOnTop( Item a_item ) {
        a_item.GetComponent<SpriteRenderer>().sortingOrder = TopSortingOrder + 1;
    }

    private List<Item> m_itemList = new List<Item>();
    private List<Room> m_roomList = new List<Room>();

    private int m_sortingOrderTop = 50;

    public bool BackEnabled { set { m_backButton.interactable = value; } }

    private List<Timer> m_timerList = new List<Timer>();

    public Timer AddTimer(float a_timeSec, List<ItemCombination> a_actionList ) {
        var timer = new Timer() {
            ActionList = a_actionList,
            Time = a_timeSec
        };

        m_timerList.Add( timer );

        return timer;
    }

    public void RemoveTimer(Timer a_timer ) {
        m_timerList.Remove( a_timer );
    }

    public bool AddToInventory( Item a_item ) {
        return m_inventoryRoom.AddItem( a_item );
    }

    private bool CheckDropCombine( Item a_item ) {
        Debug.Log( "Check drop combine for " + a_item.name );
        var underMouse = ItemsUnderMouse;
        if ( underMouse.Count < 2 ) return false;

        if ( underMouse[0] == a_item ) underMouse[1].ActionCombine( a_item );
        else if ( underMouse[1] == a_item ) underMouse[0].ActionCombine( a_item );
        return true;
    }

    private bool DropInRoom( Item a_item ) {
        var originalRoom = a_item.location;
        var successfulDrop = Room.Current.AddItem( a_item );
        if ( successfulDrop )
            MessageWindow.instance.ShowMessage( "You drop " + a_item.name + " in the room." );

        return successfulDrop;
    }

    private bool DropInInventory( Item a_item ) {
        if ( a_item.location == m_inventoryRoom ) return false;

        var successfulDrop = AddToInventory( a_item );
        if ( successfulDrop ) 
            MessageWindow.instance.ShowMessage( "You take " + a_item.name + "." );

        return successfulDrop;
    }

    private bool DropInContainer( Item a_item ) {
        if ( a_item.location == m_containerRoom ) return false;

        // don't drop a container into itself
        if ( a_item == m_currentOpenItem ) {
            MessageWindow.instance.ShowMessage( "You can't drop a container into itself!" );
            return false;
        }

        // for now, don't drop any container into any container (interface limitation)
        if ( a_item.IsContainer ) {
            MessageWindow.instance.ShowMessage( a_item.name + " doesn't fit." );
            return false;
        }

        var successfulDrop = m_containerRoom.AddItem( a_item );
        if ( successfulDrop )
            MessageWindow.instance.ShowMessage( "You put " + a_item.name + " into " + m_currentOpenItem.name + "." );

        return successfulDrop;
    }

    public bool DropItem( Item a_item ) {
        var successfulDrop = false;

        if ( CheckDropCombine( a_item ) ) return false;

        if ( m_inventoryRect.Contains( a_item.transform.position ) ) {
            if ( m_currentOpenItem != null && a_item.transform.position.y < m_inventoryRect.center.y )
                successfulDrop = DropInContainer( a_item );
            else successfulDrop = DropInInventory( a_item );
        }

        if ( m_roomRect.Contains( a_item.transform.position ) ) {
            successfulDrop = DropInRoom( a_item );
        }

        return successfulDrop;
    }

    public void GoBack() {
        MessageWindow.instance.Clear();
        Room.Current.GoBack();
    }

    public void HideActiveRooms() {
        foreach ( var room in FindObjectsOfType<Room>() ) {
            if ( room.IsContainer ) continue;
            room.gameObject.SetActive( false );
        }

        if ( m_inventoryRoom != null )
            m_inventoryRoom.gameObject.SetActive( true );
    }

    public Item FindItemById( string a_id ) {
        if ( string.IsNullOrEmpty( a_id ) ) return null;
        foreach ( var item in m_itemList ) {
            if ( item.GetComponent<UniqueId>().uniqueId == a_id )
                return item;
        }
        return null;
    }

    public Room FindRoomById( string a_id ) {
        if ( string.IsNullOrEmpty( a_id ) ) return null;
        foreach ( var room in m_roomList ) {
            if ( room.GetComponent<UniqueId>().uniqueId == a_id )
                return room;
        }
        return null;
    }

    public void LoadGame() {
        LoadGame( "save slot " + SaveSlot );
    }

    public void LoadGame( string a_saveName, bool a_reportLoad = true ) {
        var json = PlayerPrefs.GetString( a_saveName, "" );
        if ( string.IsNullOrEmpty( json ) ) {
            MessageWindow.instance.ShowMessage( "No save in '" + a_saveName + "'" );
            return;
        }

        var gameData = JsonUtility.FromJson<GameData>( json );
        Debug.Log( "Load data:\n" + json );

        Debug.LogFormat( "Loading {0} items", gameData.items.Length );

        // remove all items from rooms and containers so capacity handles correctly
        foreach ( var item in m_itemList ) {
            if ( item.location != null )
                item.location.RemoveItem( item );
        }

        foreach ( var itemData in gameData.items ) {
            if ( string.IsNullOrEmpty( itemData.itemId ) )
                continue;

            Item targetItem = FindItemById( itemData.itemId );
            if ( targetItem == null ) {
                Debug.LogErrorFormat( "Failed to load. Cannot find item id '{0}'.", itemData.itemId );
                return;
            }


            targetItem.transform.position = new Vector2( itemData.posX, itemData.posY );
            targetItem.GetComponent<SpriteRenderer>().sortingOrder = itemData.sortingOrder;

            Room targetRoom = FindRoomById( itemData.roomId );
            if ( targetRoom != null ) targetRoom.AddItem( targetItem );

            targetItem.gameObject.SetActive( itemData.isActive );
            Debug.LogFormat( "Added {4} item {0} to room {1} at ({2}, {3}).",
                targetItem, targetRoom, itemData.posX, itemData.posY, itemData.isActive ? "active" : "inactive" );
        }

        GoToRoom( gameData.playerRoomId );
        Debug.LogFormat( "Start in room {0}", Room.Current.name );

        CloseContainer();
        var container = FindItemById( gameData.openContainerId );
        OpenContainer( container );

        MessageWindow.instance.Clear();
        if ( a_reportLoad )
            MessageWindow.instance.ShowMessage( "Game loaded from '" + a_saveName + "'." );
        Debug.LogFormat( "Game loaded from '{0}'", a_saveName );

        m_isGameOver = false;
        ShouldEndGame = false;
        GameOverRoom = null;
        m_gameOverHud.SetActive( false );
    }

    // TODO make a stack so we can handle multiple open items at once
    private Item m_currentOpenItem = null;
    public Item CurrentOpenItem {  get { return m_currentOpenItem; } }

    public void CloseContainerIfNotHeld() {
        if ( m_currentOpenItem == null ) return;
        if ( m_currentOpenItem.location != m_inventoryRoom )
            CloseContainer();
    }

    private void CloseContainer() {
        if ( m_currentOpenItem == null ) return;

        if ( m_currentOpenItem.ContainerRoom == null ) {
            Debug.LogErrorFormat( "Open item container missing child room in {0}", m_currentOpenItem.name );
            return;
        }

        m_currentOpenItem.ContainerRoom.AddItemsFrom( m_containerRoom );
        m_containerRoom.gameObject.SetActive( false );
        m_currentOpenItem = null;
        m_containerLabel.enabled = false;
    }

    public void OpenContainer( Item a_item ) {
        if ( a_item == null ) return;

        if ( m_currentOpenItem == a_item ) {
            CloseContainer();
            return;
        }

        if ( m_currentOpenItem != null ) {
            CloseContainer();
        }

        if ( a_item.ContainerRoom == null ) {
            Debug.LogErrorFormat( "Item container missing child room in {0}", a_item.name );
            return;
        }

        StartCoroutine( OpenContainerCoroutine( a_item ) );
    }

    private IEnumerator OpenContainerCoroutine( Item a_item ) {
        m_containerRoom.gameObject.SetActive( true );
        //m_containerRoom.GetComponent<SpriteRenderer>().sortingOrder = TopSortingOrder;
        m_currentOpenItem = a_item;
        m_containerLabel.enabled = true;
        m_containerLabel.text = m_currentOpenItem.PrintedName.ToUpper();

        // let the container room initialize
        yield return null;

        m_containerRoom.AddItemsFrom( a_item.ContainerRoom );
    }

    public void PlayMusic( AudioClip a_clip ) {
        var source = GetComponent<AudioSource>();
        if ( source.isPlaying && source.clip == a_clip ) return;

        if ( source.clip == null ) {
            source.clip = a_clip;
            source.loop = true;
            source.Play();
            return;
        }

        StartCoroutine( TransitionMusic( a_clip ) );
    }

    public void RemoveItemFromContainer( Item a_item ) {
        if ( m_currentOpenItem == null ) return;
        m_containerRoom.RemoveItem( a_item );
    }

    public void NewGame() {
        SceneManager.LoadScene( SceneManager.GetActiveScene().buildIndex );
        SceneManager.sceneLoaded += NewGameLoaded;
    }

    private void NewGameLoaded( Scene arg0, LoadSceneMode arg1 ) {
        SceneManager.sceneLoaded -= NewGameLoaded;
        var go = new GameObject( "in game" );
    }

    public void SaveGame() {
        SaveGame( "save slot " + SaveSlot );
    }

    public void SaveGame( string a_saveName, bool a_reportSave = true ) {
        if ( IsInGame == false ) {
            Debug.LogWarning( "Tried to save while not in-game." );
            return;
        }

        var gameData = new GameData {
            openContainerId = m_currentOpenItem == null ? "" : m_currentOpenItem.GetComponent<UniqueId>().uniqueId,
            playerRoomId = Room.Current.GetComponent<UniqueId>().uniqueId,
            items = new ItemData[m_itemList.Count]
        };

        var prevOpenItem = m_currentOpenItem;
        CloseContainer();

        for ( var i = 0; i < gameData.items.Length; ++i ) {
            if ( m_itemList[i].IncludeInSave == false ) continue;

            var itemData = new ItemData {
                isActive = m_itemList[i].gameObject.activeSelf,
                itemId = m_itemList[i].GetComponent<UniqueId>().uniqueId,
                roomId = m_itemList[i].location?.GetComponent<UniqueId>().uniqueId,
                posX = m_itemList[i].transform.position.x,
                posY = m_itemList[i].transform.position.y,
                sortingOrder = m_itemList[i].GetComponent<SpriteRenderer>().sortingOrder
            };
            gameData.items[i] = itemData;
        }

        var json = JsonUtility.ToJson( gameData );
        Debug.Log( "Save data:\n" + json );
        PlayerPrefs.SetString( a_saveName, json );

        OpenContainer( prevOpenItem );

        if ( a_reportSave ) {
            MessageWindow.instance.Clear();
            MessageWindow.instance.ShowMessage( "Game saved to '" + a_saveName + "'." );
        }

        Debug.LogFormat( "Game saved to '{0}'", a_saveName );
    }

    public void TempLoad() {
        LoadGame( "temp", false );
    }

    public void TempSave() {
        SaveGame( "temp", false );
    }

    public void GoToRoom( string a_id ) {
        var room = FindRoomById( a_id );
        GoToRoom( room );
    }

    public void GoToRoom( Room a_room ) {
        if ( a_room == null ) {
            Debug.LogErrorFormat( "Tried to go to null room from {0}.",
                Room.Current == null ? "nowhere" : Room.Current.name );
            return;
        }

        if ( Room.Current == a_room ) {
            Debug.LogWarningFormat( "Tried to go to room but we're already there." );
            return;
        }

        StartCoroutine( TransitionToRoom( a_room ) );
    }

    private IEnumerator TransitionToRoom( Room a_room ) {
        while ( MessageWindow.instance.HasMore )
            yield return null;

        if ( Room.Current != null ) {
            if ( a_room.IsCutscene == false )
                TempSave();
            Room.Current.gameObject.SetActive( false );
        }

        // wait for the save to finish
        yield return new WaitForSeconds( 0.1f );

        // TODO transition

        Room.Current = a_room;
        Room.Current.gameObject.SetActive( true );
        instance.BackEnabled = Room.Current.HasBackRoom;
        Room.Current.PlayMusic();

        instance.CloseContainerIfNotHeld();
        if ( Item.Selected != null )
            Item.Selected.Deselect();

        if ( instance.Verbose )
            Room.Current.Describe();
    }

    private void Awake() {
        if ( instance != null ) {
            Debug.LogWarningFormat( "Duplicate control manager in {0}. Destroying.", name );
            Destroy( this );
            return;
        }
        instance = this;

        Cursor.lockState = CursorLockMode.Confined;

        // initialize items / rooms
        var scene = SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();
        foreach ( var go in rootObjects ) {
            m_itemList.AddRange( go.GetComponentsInChildren<Item>( true ) );
            m_roomList.AddRange( go.GetComponentsInChildren<Room>( true ) );
        }

        Debug.LogFormat( "Game has {0} rooms and {1} items.", m_roomList.Count, m_itemList.Count );
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube( m_inventoryRect.center, m_inventoryRect.size );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube( m_roomRect.center, m_roomRect.size );
    }

    private bool IsInGame {
        get { return GameObject.Find( "in game" ) != null; }
    }

    public Vector2 MouseWorldPos {
        get { return Camera.main.ScreenToWorldPoint( Input.mousePosition ); }
    }

    private List<Item> ItemsUnderMouse {
        get {
            var hitArr = Physics2D.RaycastAll( MouseWorldPos, Vector2.zero, 0f );
            var hitList = new List<RaycastHit2D>( hitArr );

            var itemList = new List<Item>();
            foreach ( var hit in hitList ) {
                var item = hit.collider.GetComponent<Item>();
                if ( item == null ) continue;
                itemList.Add( item );
            }

            return itemList.OrderByDescending( i => i.GetComponent<SpriteRenderer>().sortingOrder ).ToList();
        }
    }

    private void Start() {
        HideActiveRooms();

        if ( m_skipSystemRooms || IsInGame ) GoToRoom( GameStartRoom );
        else GoToRoom( m_systemStartRoom );

        m_inventoryRoom.gameObject.SetActive( true );
        m_sortingOrderTop = m_sortingOrderTakeable;

        m_gameOverHud.SetActive( false );
    }

    private bool m_isGameOver = false;
    // handle clicking anything that is NOT an item
    private void Update() {
        foreach ( var timer in m_timerList )
            timer.Update( Time.deltaTime );

        if ( m_isGameOver == false && ShouldEndGame && MessageWindow.instance.HasMore == false ) {
            GoToRoom( GameOverRoom );
            m_gameOverHud.SetActive( true );
            m_isGameOver = true;
            return;
        }

        HandleHotkeys();

        if ( m_menu.activeInHierarchy || m_helpMenu.activeInHierarchy)
            return;

        if ( MessageWindow.instance != null && MessageWindow.instance.HasMore ) {
            Time.timeScale = 0f;
            if ( Input.GetMouseButtonDown( 0 ) )
                MessageWindow.instance.HandleClick();
            return;
        }

        Time.timeScale = 1f;

        if ( Room.Current == null || Room.Current.IsCutscene )
            return;

        /*
        var mouseWorldPos = Camera.main.ScreenToWorldPoint( Input.mousePosition );
        var hitArr = Physics2D.RaycastAll( mouseWorldPos, Vector2.zero, 0f );

        if ( hitArr.Length > 0 ) {
            Item target = null;
            if ( hitArr.Length == 1 )
                target = hitArr[0].collider.GetComponent<Item>();
            else {
                var highestOrder = -1;
                foreach ( var hit in hitArr ) {
                    var order = hit.collider.GetComponent<SpriteRenderer>().sortingOrder;
                    if ( order > highestOrder ) {
                        highestOrder = order;
                        target = hit.collider.GetComponent<Item>();
                    }
                }
            }

            //Debug.LogFormat( "Mouse targeting {0} (out of {1} items hit)", target.name, hitArr.Length );
            target.HandleMouse();
            return;
        }
        */

        if( m_debugClicks) {
            if( Input.GetMouseButtonDown( 0 ) ) {
                var msg = "Items under mouse: ";
                foreach ( var item in ItemsUnderMouse )
                    msg += item.PrintedName + ", ";
                Debug.Log( msg );
            }
        }

        if( ItemsUnderMouse.Count > 0 ) {
            ItemsUnderMouse[0].HandleMouse();
            return;
        }

        if ( m_inventoryRect.Contains( MouseWorldPos ) ) {
            if ( Input.GetMouseButtonDown( 0 ) ) {
                MessageWindow.instance.Clear();
                if ( m_currentOpenItem == null || MouseWorldPos.y > m_inventoryRect.center.y )
                    m_inventoryRoom.Describe();
                else m_containerRoom.Describe();

                if ( Item.Selected != null )
                    Item.Selected.Deselect();
            }
            return;
        }

        if ( m_roomRect.Contains( MouseWorldPos ) ) {
            if ( Input.GetMouseButtonDown( 0 ) ) {
                MessageWindow.instance.Clear();
                if ( Item.Selected != null )
                    Item.Selected.Deselect();
                Room.Current.Describe();

                if ( Item.Selected != null )
                    Item.Selected.Deselect();
            }

            if ( Input.GetMouseButtonDown( 1 ) ) {
                MessageWindow.instance.Clear();
                MessageWindow.instance.ShowMessage( "There's nothing you can use there." );
            }

            if ( Input.GetMouseButtonDown( 2 ) ) {
                MessageWindow.instance.Clear();
                MessageWindow.instance.ShowMessage( "You can't go there." );
            }
        }
    }

    private void HandleHotkeys() {
        if ( Input.GetKeyDown( KeyCode.Space ) ) {
            if ( m_menu.activeSelf == false )
                MessageWindow.instance.NextPage();
        }

        if ( Input.GetKeyDown( KeyCode.Escape ) ) {
            if ( m_helpMenu.activeSelf ) m_helpMenu.SetActive( false );
            m_menu.SetActive( !m_menu.activeSelf );
        }

        if ( Input.GetKeyDown( KeyCode.F1 ) ) {
            m_helpUi.enabled = !m_helpUi.enabled;
        }

        if ( Input.GetKeyDown( KeyCode.F6 ) ) {
            if ( m_helpMenu.activeSelf ) m_helpMenu.SetActive( false );
            if ( m_menu.activeSelf ) m_menu.SetActive( false );
            SaveGame();
        }

        if ( Input.GetKeyDown( KeyCode.F9 ) ) {
            if ( m_helpMenu.activeSelf ) m_helpMenu.SetActive( false );
            if ( m_menu.activeSelf ) m_menu.SetActive( false );
            LoadGame();
        }

        if ( Input.GetKeyDown( KeyCode.F12 ) ) {
            if ( m_helpMenu.activeSelf ) m_helpMenu.SetActive( false );
            if ( m_menu.activeSelf ) m_menu.SetActive( false );
            NewGame();
        }
    }

    private IEnumerator TransitionMusic( AudioClip a_clip ) {
        var source = GetComponent<AudioSource>();

        var timeElapsed = 0f;
        while ( timeElapsed < m_musicTransitionTimeSec * 0.5f ) {
            timeElapsed += Time.deltaTime;
            source.volume = 1f - timeElapsed;
            yield return null;
        }

        source.clip = a_clip;
        source.loop = true;
        source.Play();

        if ( a_clip == null ) {
            source.Stop();
            yield break;
        }

        timeElapsed = 0f;
        while ( timeElapsed < m_musicTransitionTimeSec * 0.5f ) {
            timeElapsed += Time.deltaTime;
            source.volume = timeElapsed;
            yield return null;
        }
    }
}
