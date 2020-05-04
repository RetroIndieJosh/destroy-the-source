using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using MyBox;
using System.Linq;

public enum UseAction
{
    None,
    Destroy,
    GameOver,
    Message,
    Replace,
    Open,
    ChangeDescription,
    ChangeExit,
    MovePlayer,
    MoveItemToRoom,
    StartTimer,
    StopTimer,
    PauseTimer,
    ScaleItem
}

[System.Serializable]
public class ItemCombination
{
    public void UpdateInspector() {
        name = item == null ? "UNASSIGNED" : item.name;

        needsDescription = action == UseAction.ChangeDescription;
        needsPosOrScale = action == UseAction.MoveItemToRoom || action == UseAction.ScaleItem;
        needsReplace = action == UseAction.Replace;
        needsRoom = action == UseAction.GameOver || action == UseAction.ChangeExit || action == UseAction.MoveItemToRoom 
            || action == UseAction.MovePlayer;
        needsTarget = action != UseAction.GameOver && action != UseAction.Message && action != UseAction.MovePlayer;
        needsTimer = action == UseAction.StartTimer || action == UseAction.StopTimer || action == UseAction.PauseTimer;
        hasAction = action != UseAction.None;

        hasSecondMessage = action == UseAction.Open;

        hasItem = item != null;
    }

    [HideInInspector]
    public string name = "test";

    public Item item = null;

    public int priority = 0;

    // this keeps actions clean in Item but breaks TimedAction
    //[ConditionalField("hasItem")]
    public UseAction action = UseAction.None;

    [HideInInspector] public bool hasItem = false;
    [HideInInspector] public bool hasAction = false;
    [HideInInspector] public bool hasSecondMessage = false;
    [HideInInspector] public bool needsPosOrScale = false;
    [HideInInspector] public bool needsReplace = false;
    [HideInInspector] public bool needsRoom = false;
    [HideInInspector] public bool needsTarget = false;
    [HideInInspector] public bool needsTimer = false;
    [HideInInspector] public bool needsDescription = false;

    [ConditionalField( "needsTimer" )]
    public TimedAction targetTimer = null;

    [ConditionalField("needsTarget")]
    public Item targetItem = null;

    [ConditionalField("needsReplace")]
    public Item replaceUsedWith = null;

    [ConditionalField("needsReplace")]
    public Item replaceTargetWith = null;

    [ConditionalField( "needsPosOrScale" )]
    public Vector2 targetPosOrScale = Vector2.zero;

    [ConditionalField("needsRoom")]
    public Room targetRoom = null;

    [ConditionalField("needsDescription")]
    public string newDescription = "A new description.";

    [ConditionalField( "hasAction" )]
    public bool clrMsgBefore = false;

    [ConditionalField( "hasAction" ), 
        Tooltip( "Move Player: Triggers AFTER moving player (after room desc)\n"
           +  "Open: open message")]
    public string message = "";

    [ConditionalField( "hasSecondMessage"), Tooltip("Open: Close message")]
    public string message2 = "";

    [ConditionalField("hasAction")]
    public AudioClip sound = null;
}

[System.Serializable]
public class ItemData
{
    public bool isActive = true;
    public string itemId = null;
    public string roomId = null;
    public float posX = 0f;
    public float posY = 0f;
    public int sortingOrder = 0;
}

[RequireComponent( typeof( Collider2D ) )]
[RequireComponent( typeof( SpriteRenderer ) )]
[RequireComponent( typeof( SpriteInvert ) )]
public class Item : MonoBehaviour
{

    static public Item Selected { get; private set; }

    [SerializeField] private bool m_includeInSave = true;

    public Room location = null;

    [Header( "Strings (JSON)" )]
    [SerializeField] private string printedName = "Item";
    [SerializeField, TextArea] private string msgExamine = "Nothing special.";
    [SerializeField, TextArea] private string msgNoGo = "You can't go through [item].";
    [SerializeField, TextArea] private string msgUseFail = "You can't use [item].";
    [SerializeField, TextArea] private string msgComboFail = "You can't use [item] on [target].";

    [Header( "Physical" )]
    [SerializeField, Tooltip( "Equivalent to 'can combine'" )] private bool m_canSelect = false;
    [SerializeField] private bool m_canTake = false;
    [SerializeField] private Vector2Int m_size = Vector2Int.one;
    [SerializeField] private Room m_targetRoom = null;

    [Header( "Audio" )]
    [SerializeField] private AudioClip m_dropSound = null;

    [Header( "Actions" )]
    [SerializeField] private ItemCombination m_takeAction = new ItemCombination();

    [Header( "Combinations" )]
    [SerializeField] private List<ItemCombination> m_combinationList = new List<ItemCombination>();

    private bool m_isDragging = false;
    private Vector3 m_dragStartPos = Vector3.zero;
    private bool m_canDrag = false;

    public Room ContainerRoom { get; private set; }
    public bool IsContainer {  get { return ContainerRoom != null; } }
    public bool IncludeInSave {  get { return m_includeInSave; } }
    public string PrintedName {  get { return printedName; } }
    public Vector2Int Size {  get { return m_size; } }

    public void AddCombination( ItemCombination a_combination = null ) {
        if( a_combination == null )
            a_combination = new ItemCombination();

        if ( a_combination.targetItem == null )
            a_combination.targetItem = this;

        m_combinationList.Add( a_combination );
    }

    public void ClearNoneCombinations() {
        var toRemove = new List<ItemCombination>();
        foreach( var combination in m_combinationList) {
            if ( combination.item == this ) continue;
            if ( combination.action == UseAction.None )
                toRemove.Add( combination );
        }

        foreach( var combination in toRemove)
            m_combinationList.Remove( combination );
    }

    public void HandleMouse() {
        if ( Input.GetMouseButtonDown( 0 ) == false
            && Input.GetMouseButtonDown( 1 ) == false
            && Input.GetMouseButtonDown( 2 ) == false ) {

            return;
        }

        MessageWindow.instance.Clear();
        if ( Input.GetMouseButtonDown( 0 ) ) HandleLeftClick();
        if ( Input.GetMouseButtonDown( 1 ) ) HandleRightClick();
        if ( Input.GetMouseButtonDown( 2 ) ) HandleMiddleClick();
    }

    public bool IsOpenable {
        get { return SelfCombination != null && SelfCombination.action == UseAction.Open; }
    }

    private ItemCombination SelfCombination {
        get {
            foreach ( var combination in m_combinationList )
                if ( combination.item == this ) return combination;
            return null;
        }
    }

    // in-editor initialization
    public void Initialize( ItemCombination a_selfCombination = null ) {
        if ( a_selfCombination == null )
            a_selfCombination = new ItemCombination();

        a_selfCombination.item = this;
        AddCombination( a_selfCombination );
    }

    // sync combination labels in inspector to item name
    public void UpdateInspector() {
        m_takeAction.UpdateInspector();
        foreach ( var combination in m_combinationList ) {
            combination.UpdateInspector();

            // always have a target
            if ( combination.hasAction && combination.targetItem == null )
                combination.targetItem = this;
        }

        if ( m_canSelect == false )
            m_canTake = false;

        if ( m_canTake ) {
            var cm = FindObjectOfType<ControlManager>();
            GetComponent<SpriteRenderer>().sortingOrder = cm.SortingOrderTakeable;
        }

        GetComponent<SpriteInvert>().enabled = m_canTake;
    }

    private void Start() {
        if( IsOpenable ) {
            ContainerRoom = GetComponentInChildren<Room>();
            if ( ContainerRoom == null ) {
                Debug.LogErrorFormat( "Openable {0} has no container room. Disabling open.", name );
                SelfCombination.action = UseAction.None;
            }
        }

        var current = transform;
        while ( current.parent != null ) {
            location = current.parent.GetComponent<Room>();
            if ( location != null ) {
                location.AddItem( this );
                break;
            }
            current = current.parent;
        }
    }

    private void Update() {
        HandleDrag();
    }

    private void HandleDrag() {
        if ( m_canTake == false ) return;

        if ( Input.GetMouseButtonUp( 0 ) ) {
            m_canDrag = false;

            if ( m_isDragging ) {
                m_isDragging = false;
                Deselect();
                var validDrop = ControlManager.instance.DropItem( this );
                if ( validDrop ) {
                    if ( m_dropSound != null )
                        AudioSource.PlayClipAtPoint( m_dropSound, transform.position );
                    ExecuteAction( m_takeAction );
                    if( m_takeAction.action == UseAction.None )
                        ControlManager.instance.AdvanceTurn();
                } else transform.position = m_dragStartPos;
            }
        }

        if ( m_isDragging == false ) {
            if ( m_canDrag ) {
                var mousePos = Camera.main.ScreenToWorldPoint( Input.mousePosition );
                var distance = Vector2.Distance( mousePos, transform.position );
                if ( distance > ControlManager.instance.DragThreshold ) {
                    m_isDragging = true;
                    m_canDrag = false;
                    MessageWindow.instance.Clear();
                }
            } else {
                m_dragStartPos = transform.position;
                return;
            }
        }

        if ( m_isDragging == false ) return;

        var pos = Camera.main.ScreenToWorldPoint( Input.mousePosition );
        pos.z = transform.position.z;
        transform.position = pos;
    }

    private void ReplaceWith(Item a_item ) {
        if ( a_item == this ) return;

        if( a_item == null ) {
            RemoveFromGame();
            return;
        }

        a_item.gameObject.SetActive( true );
        var loc = location;
        RemoveFromGame();
        if( loc != null )
            loc.AddItem( a_item );
    }

    private void RemoveFromGame() {
        if ( Selected == this )
            Selected.Deselect();

        gameObject.SetActive( false );
        if ( location != null ) location.RemoveItem( this );
        ControlManager.instance.RemoveItemFromContainer( this );
        //transform.parent = null;
    }

    private void ResetDrag() {
        m_isDragging = false;
        m_canDrag = false;
        m_dragStartPos = transform.position;
    }

    private List<ItemCombination> GetCombinationsWith( Item a_item ) {
        var list = new List<ItemCombination>();
        foreach ( var combination in m_combinationList ) {
            if ( combination.item == a_item )
                list.Add( combination );
        }
        return list;
    }

    public static void ExecuteAction( ItemCombination a_combination, bool a_showRawMessage = false) {
        bool usesTurn = false;

        switch ( a_combination.action ) {
            case UseAction.None:
                return;
            case UseAction.Destroy:
                a_combination.targetItem.RemoveFromGame();
                usesTurn = true;
                break;
            case UseAction.GameOver:
                ControlManager.instance.ShouldEndGame = true;
                ControlManager.instance.GameOverRoom = a_combination.targetRoom;
                break;
            case UseAction.Message: break;
            case UseAction.Open:
                if ( Selected == a_combination.targetItem )
                    Selected.Deselect();
                ControlManager.instance.OpenContainer( a_combination.targetItem );
                break;
            case UseAction.Replace:
                if( a_combination.item != null )
                    a_combination.item.ReplaceWith( a_combination.replaceUsedWith );
                if( a_combination.targetItem != null )
                    a_combination.targetItem.ReplaceWith( a_combination.replaceTargetWith );
                usesTurn = true;
                break;
            case UseAction.MoveItemToRoom:
                if ( Selected == a_combination.targetItem )
                    Selected.Deselect();

                if ( a_combination.targetRoom == null ) {
                    a_combination.targetItem.RemoveFromGame();
                } else {
                    a_combination.targetRoom.AddItem( a_combination.targetItem );

                    a_combination.targetItem.ResetDrag();

                    var pos = (Vector3)a_combination.targetPosOrScale;
                    pos.z = a_combination.targetItem.transform.position.z;
                    a_combination.targetItem.transform.position = pos;

                    a_combination.targetItem.gameObject.SetActive( true );
                }
                usesTurn = true;
                break;
            case UseAction.MovePlayer:
                if ( Selected != null && Selected == a_combination.targetItem )
                    Selected.Deselect();
                ControlManager.instance.GoToRoom( a_combination.targetRoom );
                if ( a_combination.clrMsgBefore )
                    MessageWindow.instance.Clear();
                usesTurn = true;
                break;
            case UseAction.ScaleItem:
                if ( Selected == a_combination.targetItem )
                    Selected.Deselect();
                if ( a_combination.targetItem != null )
                    a_combination.targetItem.transform.localScale = a_combination.targetPosOrScale;
                usesTurn = true;
                break;
            case UseAction.StartTimer:
                a_combination.targetTimer.StartTimer();
                break;
            case UseAction.StopTimer:
                a_combination.targetTimer.StopTimer();
                break;
            case UseAction.PauseTimer:
                a_combination.targetTimer.PauseTimer();
                break;
        }

        if ( a_combination.sound != null )
            AudioSource.PlayClipAtPoint( a_combination.sound, Vector3.zero );

        if ( Selected != null )
            Selected.Deselect();

        if ( usesTurn )
            ControlManager.instance.AdvanceTurn();

        if ( a_showRawMessage )
            MessageWindow.instance.ShowMessage( a_combination.message );
    }

    public void ActionCombine( Item a_item ) {
        var combinationList = GetCombinationsWith( a_item ).OrderByDescending( combo => combo.priority ).ToList();

        if ( combinationList == null || combinationList.Count == 0 ) {
            if ( a_item == this )
                ShowMessage( msgUseFail );
            else
                a_item.ShowMessage( msgComboFail, this );
            return;
        }

        var msg = "";
        foreach ( var combination in combinationList ) {
            ExecuteAction( combination );
            if ( combination.action == UseAction.None ) {
                ShowMessage( msgUseFail );
            } else if ( string.IsNullOrEmpty( combination.message ) == false ) {
                var message = combination.message;

                // we've closed it so it's no longer the open item
                if ( combination.action == UseAction.Open && ControlManager.instance.CurrentOpenItem != this )
                    message = combination.message2;

                if ( string.IsNullOrEmpty( msg ) == false )
                    msg += " ";
                msg += message;
            }
        }

        a_item.ShowMessage( msg, this );
    }

    private void ActionExamine() {
        var msg = msgExamine;
        if ( ControlManager.instance.DebugIds )
            msg += " ID " + GetComponent<UniqueId>().uniqueId;
        ShowMessage( msg );
    }

    private void ActionGo() {
        ControlManager.instance.GoToRoom( m_targetRoom );
    }

    private void Select() {
        MessageWindow.instance.Clear();

        m_canDrag = true;

        if( Selected == this ) {
            ActionExamine();
            return;
        }

        if ( Selected != null ) Selected.Deselect();

        if ( m_canSelect ) {
            Selected = this;
            GetComponent<SpriteInvert>().Inverted = true;

            if ( m_canTake )
                GetComponent<SpriteRenderer>().sortingOrder = ControlManager.instance.SortingOrderDragging;
        }

        ActionExamine();
    }

    public void Deselect() {
        if ( m_canTake )
            ControlManager.instance.SortOnTop( this );

        Selected = null;
        GetComponent<SpriteInvert>().Inverted = false;
    }

    private void HandleLeftClick() {
        Select();
    }

    private void HandleMiddleClick() {
        MessageWindow.instance.Clear();
        ShowMessage( msgNoGo );
        if ( m_targetRoom != null )
            ActionGo();
    }

    private void HandleRightClick() {
        if ( Selected == null )
            ActionCombine( this );
        else {
            ActionCombine( Selected );
        }
    }

    private string ParseMessage(string a_msg, Item a_target = null) {
        var msg = a_msg.Replace( "[item]", printedName );
        if( a_target != null ) msg = msg.Replace( "[target]", a_target.printedName );
        return msg;
    }

    private void ShowMessage(string a_msg, Item a_target = null) {
        var msg = ParseMessage( a_msg, a_target );
        MessageWindow.instance.ShowMessage( msg );
    }
}

