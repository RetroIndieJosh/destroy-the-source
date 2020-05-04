using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[CustomEditor( typeof( ControlManager ) )]
public class ControlManagerEditor : Editor
{
    public override void OnInspectorGUI() {
        var cm = target as ControlManager;

        if ( EditorApplication.isPlaying == false ) {
            EditorGUILayout.LabelField( "Game Data", EditorStyles.boldLabel );

            if ( GUILayout.Button( "Clear Game Data" ) ) {
                PlayerPrefs.DeleteAll();
                Debug.Log( "Cleared all game data" );
            }

            for ( var i = 0; i < 10; ++i ) {
                var save = PlayerPrefs.GetString( "save slot " + i );
                if ( string.IsNullOrEmpty( save ) ) continue;

                var gameData = JsonUtility.FromJson<GameData>( save );
                var playerRoom = FindRoomById( gameData.playerRoomId );
                var msg = string.Format( "Save Slot {0}: {1} chars, {2} items in {3} ({4})",
                    i, save.Length, gameData.items.Length, playerRoom == null ? "nowhere" : playerRoom.name,
                    gameData.playerRoomId );
                EditorGUILayout.LabelField( msg );
            }

            var tempSave = PlayerPrefs.GetString( "temp" );
            if ( string.IsNullOrEmpty( tempSave ) == false ) {
                var gameData = JsonUtility.FromJson<GameData>( tempSave );
                var playerRoom = FindRoomById( gameData.playerRoomId );
                var msg = string.Format( "Temp Save: {0} chars, {1} items in {2} ({3})",
                    tempSave.Length, gameData.items.Length, playerRoom == null ? "nowhere" : playerRoom.name,
                    gameData.playerRoomId );
                EditorGUILayout.LabelField( msg );
            }

            EditorGUILayout.LabelField( "Rooms", EditorStyles.boldLabel );

            if ( GUILayout.Button( "Create Room" ) ) {
                var room = EditorUtility.CreateRoom();
                FocusRoom( room );
            }

            if ( GUILayout.Button( "Show Start Room" ) )
                FocusRoom( cm.GameStartRoom.gameObject );
        }

        DrawDefaultInspector();
    }

    private Room FindRoomById( string a_id ) {
        var scene = SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();
        var roomList = new List<Room>();
        foreach ( var go in rootObjects )
            roomList.AddRange( go.GetComponentsInChildren<Room>(true) );

        foreach ( var room in roomList ) {
            if ( room.GetComponent<UniqueId>().uniqueId == a_id )
                return room;
        }

        return null;
    }

    private void FocusRoom( GameObject a_room ) {
        var cm = target as ControlManager;
        cm.HideActiveRooms();
        a_room.gameObject.SetActive( true );
        Selection.activeObject = a_room;
    }
}
