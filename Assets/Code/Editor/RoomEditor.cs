using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor( typeof( Room ) ), CanEditMultipleObjects]
public class RoomEditor : Editor
{
    public override void OnInspectorGUI() {
        if ( targets.Length == 1 ) {
            var room = targets[0] as Room;
            if ( EditorApplication.isPlaying ) {
                if ( GUILayout.Button( "Go Here" ) )
                    FindObjectOfType<ControlManager>().GoToRoom( room );
            } else {
                EditorGUILayout.LabelField( "Item Creation", EditorStyles.boldLabel );

                if ( GUILayout.Button( "Add Item (Box Collider)" ) ) {
                    var item = EditorUtility.CreateItem( room.transform );
                    Selection.activeObject = item;
                }

                if ( GUILayout.Button( "Add Item (Polygon Collider)" ) ) {
                    var item = EditorUtility.CreateItem( room.transform, true );
                    Selection.activeObject = item;
                }

                if ( GUILayout.Button( "Add Door (Box Collider)" ) ) {
                    var door = CreateDoor( room.transform, false );
                    Selection.activeObject = door;
                }

                if ( GUILayout.Button( "Add Door (Polygon Collider)" ) ) {
                    var door = CreateDoor( room.transform, true );
                    Selection.activeObject = door;
                }

                if ( GUILayout.Button( "Add Lockable Door (Box Collider)" ) ) {
                    var door = CreateDoor( room.transform, false, true );
                    Selection.activeObject = door;
                }

                if ( GUILayout.Button( "Add Lockable Door (Polygon Collider)" ) ) {
                    var door = CreateDoor( room.transform, true, true );
                    Selection.activeObject = door;
                }

                if ( GUILayout.Button( "Add Container" ) ) {
                    var item = EditorUtility.CreateItem( room.transform );
                    var containerRoom = EditorUtility.CreateRoom();
                    containerRoom.transform.parent = item.transform;
                    containerRoom.name = "container";
                    Selection.activeObject = item;
                }

                if ( room.transform.parent == null ) {
                    EditorGUILayout.LabelField( "Room Controls", EditorStyles.boldLabel );
                    if ( GUILayout.Button( "Focus" ) )
                        Focus();
                    if ( GUILayout.Button( "Start Here" ) ) {
                        FindObjectOfType<ControlManager>().GameStartRoom = room;
                        Focus();
                    }
                }
            }
        }

        DrawDefaultInspector();
    }

    private GameObject CreateDoor( Transform a_parent, bool a_polygonCollider, bool a_isLockable = false ) {
        var door = new GameObject();
        door.transform.parent = a_parent;
        door.name = "door";

        if( a_isLockable ) {
            var lockedDoor = EditorUtility.CreateItem( door.transform, a_polygonCollider );
            lockedDoor.name = "locked door";
            lockedDoor.transform.parent = door.transform;
        }

        var closedDoor = EditorUtility.CreateItem( door.transform, a_polygonCollider );
        closedDoor.name = "closed door";
        closedDoor.transform.parent = door.transform;
        if ( a_isLockable ) closedDoor.SetActive( false );

        var openDoor = EditorUtility.CreateItem( door.transform, a_polygonCollider );
        openDoor.name = "open door";
        openDoor.transform.parent = door.transform;
        openDoor.SetActive( false );
        
        openDoor.GetComponent<Item>().Initialize( new ItemCombination() {
            action = UseAction.Replace,
            replaceTargetWith = closedDoor.GetComponent<Item>()
        } );

        closedDoor.GetComponent<Item>().Initialize( new ItemCombination() {
            action = UseAction.Replace,
            replaceTargetWith = openDoor.GetComponent<Item>()
        } );

        return door;
    }

    private void Focus() {
        var room = target as Room;
        FindObjectOfType<ControlManager>().HideActiveRooms();
        room.gameObject.SetActive( true );
        Selection.activeObject = room.gameObject;
    }
}
