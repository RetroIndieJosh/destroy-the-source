using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : MonoBehaviour
{
    static public Room Current;

    [SerializeField] private bool m_isCutscene = false;
    [SerializeField] private bool m_alignItemsToGrid = false;
    [SerializeField] private Vector2Int m_size = Vector2Int.zero;
    [SerializeField] private AudioClip m_music = null;

    [Header("Back Room")]
    [SerializeField] private Room m_backRoom = null;
    [SerializeField, TextArea] private string m_backMessage = "You head back.";

    [Header( "Strings (JSON)" )]
    [SerializeField, TextArea] private string description = "A very bland, normal room.";

    public bool IsCutscene {  get { return m_isCutscene; } }
    public bool HasBackRoom {  get { return m_backRoom != null; } }
    public bool IsContainer { get { return transform.parent != null; } }

    private List<Item> m_itemList = new List<Item>();
    private Item[,] m_gridOccupant = null;

    public bool AddItem(Item a_item ) {
        if ( m_isCutscene || m_itemList.Contains( a_item ) ) return false;

        if ( m_alignItemsToGrid ) {
            if ( SnapItem( a_item ) == false ) {
                Debug.LogWarningFormat( "{0} doesn't fit in {1}.", a_item.name, name );
                return false;
            }
        } else {
            if ( a_item.location != null )
                a_item.location.RemoveItem( a_item );
        }

        a_item.transform.SetParent( transform, true );
        a_item.location = this;
        m_itemList.Add( a_item );

        // put item on top of room if it renders
        var sr = GetComponent<SpriteRenderer>();
        if ( sr != null )
            a_item.GetComponent<SpriteRenderer>().sortingOrder = sr.sortingOrder + 1;

        // hide behind camera if in container
        var pos = a_item.transform.position;
        if ( IsContainer )
            pos.z = Camera.main.transform.position.z - 100f;
        else pos.z = 0f;
        a_item.transform.position = pos;

        Debug.LogFormat( "Add {0} to {1}", a_item.name, name );
        return true;
    }

    public void AddItemsFrom(Room a_room ) {

        // clone since we're going to modify the room's item list
        var itemList = new List<Item>( a_room.m_itemList );
        foreach( var item in itemList ) 
            AddItem( item );
    }

    public void Describe() {
        MessageWindow.instance.ShowMessage( description );
        if ( ControlManager.instance.DebugIds )
            MessageWindow.instance.ShowMessage( "ID " + GetComponent<UniqueId>().uniqueId );
    }

    public void GoBack() {
        if ( m_backRoom == null ) return;
        MessageWindow.instance.ShowMessage( m_backMessage );
        ControlManager.instance.GoToRoom( m_backRoom );
    }

    public void PlayMusic() {
        ControlManager.instance.PlayMusic( m_music );
    }

    public void RemoveItem( Item a_item ) {
        if ( m_alignItemsToGrid ) {
            for ( var x = 0; x < m_size.x; ++x ) {
                for ( var y = 0; y < m_size.y; ++y ) {
                    if ( m_gridOccupant[x, y] == a_item ) {
                        m_gridOccupant[x, y] = null;
                        Debug.LogFormat( "Remove from ({0}, {1})", x, y );
                    }
                }
            }
        }

        m_itemList.Remove( a_item );
        a_item.location = ControlManager.instance.NowhereRoom;

        Debug.LogFormat( "Remove {0} from {1}", a_item.name, name );
    }

    private void Start() {
        if ( m_alignItemsToGrid == false ) return;

        if ( m_size == null || m_size == Vector2Int.zero ) {
            Debug.LogErrorFormat( "Must set room size for grid alignment in {0}", name );
            return;
        }

        m_gridOccupant = new Item[m_size.x, m_size.y];
        for( var x = 0; x < m_size.x; ++x ) {
            for ( var y = 0; y < m_size.y; ++y ) {
                m_gridOccupant[x, y] = null;
            }
        }
    }

    private Vector2 SlotPosition( int a_x, int a_y ) {
        var left = Mathf.FloorToInt( transform.position.x - m_size.x / 2 );
        var top = Mathf.FloorToInt( transform.position.y + m_size.y / 2 );

        var x = left + a_x;
        var y = top - a_y;

        return new Vector2( x, y );
    }

    private bool CanFit( Item a_item, Vector2Int a_startSlotPos ) {
        var endSlotPos = a_startSlotPos + a_item.Size;
        for( var tileX = a_startSlotPos.x; tileX < endSlotPos.x; ++tileX ) {
            if ( tileX >= m_size.x ) return false;
            for( var tileY = a_startSlotPos.y; tileY < endSlotPos.y; ++tileY ) {
                if ( tileY >= m_size.y ) return false;
                if ( m_gridOccupant[tileX, tileY] == true ) return false;
            }
        }
        return true;
    }

    private bool SnapItem( Item a_item ) {
        var x = 0;
        var y = 0;

        var loops = 0;
        while( CanFit(a_item, new Vector2Int(x, y)) == false ) {
            ++x;
            if( x >= m_size.x ) {
                x = 0;
                ++y;
            }

            // no more inventory space
            if ( y >= m_size.y ) {
                var msg = string.Format( "No space in {0} for {1}.", name, a_item.name );
                MessageWindow.instance.ShowMessage( msg );
                return false;
            }

            ++loops;
            if( loops > 1000 ) {
                Debug.LogError( "Infinite loop detected" );
                return false;
            }
        }

        if( a_item.location != null )
            a_item.location.RemoveItem( a_item );

        Debug.LogFormat( "Drop {0} at index ({1}, {2})", a_item.name, x, y );

        var offset = new Vector2( a_item.Size.x * 0.5f, a_item.Size.y * -0.5f );

        // HACK shouldn't need this?
        offset.y += 0.5f;

        var slotPos = SlotPosition( x, y ) + offset;
        var z = a_item.transform.position.z;
        a_item.transform.position = new Vector3( slotPos.x, slotPos.y, z );

        for( var tileX = x; tileX < x + a_item.Size.x; ++tileX ) {
            for( var tileY = y; tileY < y + a_item.Size.y; ++tileY ) {
                m_gridOccupant[tileX, tileY] = a_item;
                Debug.LogFormat( "Occupy ({0}, {1})", tileX, tileY );
            }
        }

        // TODO handle sizes

        return true;
    }
}
