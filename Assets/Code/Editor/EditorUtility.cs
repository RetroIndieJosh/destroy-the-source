using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

class EditorUtility
{
    public static GameObject CreateItem( Transform a_parent, bool a_usePolygonCollider = false ) {
        var item = new GameObject();

        item.AddComponent<SpriteRenderer>();
        item.AddComponent<SpriteInvert>();

        if ( a_usePolygonCollider ) {
            var collider = item.AddComponent<PolygonCollider2D>();
            collider.offset = Vector2.zero;
        } else {
            var collider = item.AddComponent<BoxCollider2D>();
            collider.offset = Vector2.zero;
            collider.size = Vector2.one;
        }

        var itemComp = item.AddComponent<Item>();
        itemComp.Initialize();

        item.AddComponent<UniqueId>();

        item.transform.parent = a_parent;
        item.transform.SetAsLastSibling();

        item.name = "New Item";
        return item;
    }

    public static GameObject CreateRoom() {
        var room = new GameObject();
        room.AddComponent<Room>();
        room.AddComponent<UniqueId>();

        room.transform.SetAsLastSibling();

        room.name = "New Room";
        return room;
    }
}
