using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// TODO get this to work - currently displays correctly but doesn't allow edito
/*
[CustomPropertyDrawer(typeof(ItemCombination))]
public class ItemCombinationDrawer: PropertyDrawer
{
    const float TEXT_BOX_HEIGHT_MULT = 4f;

    private Rect ShowProperty( SerializedProperty a_property, Rect a_rect, string a_name, bool a_isTextBox = false ) {
        var prop = a_property.FindPropertyRelative( a_name );

        var width = EditorGUILayout.GetControlRect().width;
        var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        if ( a_isTextBox ) height *= TEXT_BOX_HEIGHT_MULT;

        var rect = new Rect( a_rect.x, a_rect.y, width, height );
        EditorGUI.PropertyField( rect, prop );

        if ( a_isTextBox ) m_elementCount += Mathf.FloorToInt( TEXT_BOX_HEIGHT_MULT );
        else ++m_elementCount;

        return new Rect( a_rect.x, a_rect.y + height, a_rect.width, a_rect.height );
    }

    private int m_elementCount = 0;

    public override float GetPropertyHeight( SerializedProperty property, GUIContent label ) {
        var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        if ( property.isExpanded ) height *= 10;
        //if ( property.isExpanded ) height *= m_elementCount;
        return height;
    }

    public override void OnGUI( Rect a_rect, SerializedProperty property, GUIContent label ) {
        var unitHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.BeginProperty( a_rect, label, property );

        m_elementCount = 1;

        var item = ( property.FindPropertyRelative( "item" ).objectReferenceValue ) as Item;
        var name = item == null ? "UNASSIGNED" : item.name;
        property.isExpanded = EditorGUI.Foldout( a_rect, property.isExpanded, name );

        if ( property.isExpanded ) {
            var rect = a_rect;
            rect.y += unitHeight;
            rect = ShowProperty( property, rect, "item" );
            rect = ShowProperty( property, rect, "action" );

            var action = (UseAction)property.FindPropertyRelative( "action" ).enumValueIndex;
            if ( action == UseAction.ChangeExit || action == UseAction.Open || action == UseAction.Open )
                rect = ShowProperty( property, rect, "targetItem" );

            if ( action == UseAction.GameOver || action == UseAction.ChangeExit )
                rect = ShowProperty( property, rect, "targetRoom" );

            if ( action == UseAction.Replace ) {
                rect = ShowProperty( property, rect, "replaceTargetItem" );
                rect = ShowProperty( property, rect, "replaceUsedItem" );
            }

            if ( action == UseAction.ChangeDescription )
                rect = ShowProperty( property, rect, "newDescription", true );

            if ( action != UseAction.None ) {
                rect = ShowProperty( property, rect, "message", true );
                rect = ShowProperty( property, rect, "sound" );
            }
        }

        EditorGUI.EndProperty();
    }
}
*/

[CustomEditor( typeof( TimedAction ) ), CanEditMultipleObjects]
public class TimedActionEditor : Editor
{
    public override void OnInspectorGUI() {
        if ( EditorApplication.isPlaying == false ) {
            foreach ( TimedAction timedAction in targets )
                timedAction.UpdateInspector();
        }

        DrawDefaultInspector();
    }
}

[CustomEditor( typeof( Item ) ), CanEditMultipleObjects]
public class ItemEditor : Editor
{
    public override void OnInspectorGUI() {
        if ( EditorApplication.isPlaying ) {
            if ( targets.Length == 1 ) {
                if ( GUILayout.Button( "Move to Inventory" ) ) {
                    var item = targets[0] as Item;
                    FindObjectOfType<ControlManager>().AddToInventory( item );
                }
            }
        } else {
            foreach ( Item item in targets )
                item.UpdateInspector();

            if ( GUILayout.Button( "Clone" ) ) {
                foreach ( Item item in targets ) {
                    var copy = Instantiate( item ).gameObject;
                    DestroyImmediate( copy.GetComponent<UniqueId>() );
                    copy.AddComponent<UniqueId>();

                    copy.transform.parent = item.transform.parent;
                    copy.transform.SetAsLastSibling();

                    copy.name = item.name;
                    Selection.activeObject = copy;
                }
            }

            if ( GUILayout.Button( "Convert to Container" ) ) {
                foreach ( Item item in targets ) {
                    var containerCombination = new ItemCombination {
                        action = UseAction.Open
                    };
                    item.Initialize( containerCombination );

                    if ( item.GetComponentInChildren<Room>() != null )
                        continue;

                    var containerRoom = EditorUtility.CreateRoom();
                    containerRoom.transform.parent = item.transform;
                    containerRoom.name = "container";
                }
            }

            if ( GUILayout.Button( "Initialize" ) ) {
                foreach ( Item item in targets )
                    item.Initialize();
            }
        }

        DrawDefaultInspector();

        if ( EditorApplication.isPlaying == false ) {
            if ( targets.Length == 1 ) {
                if ( GUILayout.Button( "Add Combination" ) ) {
                    var item = targets[0] as Item;
                    item.AddCombination();
                }

                if ( GUILayout.Button( "Clear All 'None' Action Combinations" ) ) {
                    var item = targets[0] as Item;
                    item.ClearNoneCombinations();
                }
            }
        }
    }
}
