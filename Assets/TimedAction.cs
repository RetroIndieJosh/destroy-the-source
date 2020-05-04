using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class Timer
{
    private float m_timeSec = 1f;
    private List<ItemCombination> m_actionList = new List<ItemCombination>();
    private bool m_isRunning = false;
    private float m_timeElapsed = 0f;
    private Room m_containingRoom = null;

    public List<ItemCombination> ActionList {
        set {
            m_actionList.Clear();
            foreach ( var action in value )
                m_actionList.Add( action );
        }
    }
    public Room ContainingRoom {  set { m_containingRoom = value; } }
    public float Time { set { m_timeSec = value; } }

    public void Pause() {
        m_isRunning = false;
    }

    public void Start() {
        m_isRunning = true;
    }

    public void Stop() {
        m_isRunning = false;
        m_timeElapsed = 0.0f;
    }

    public void Update( float dt ) {
        if ( m_isRunning == false ) return;

        m_timeElapsed += dt;
        if ( m_timeElapsed < m_timeSec ) return;

        var sortedActionList = m_actionList.OrderByDescending( a => a.priority );
        foreach( var action in sortedActionList) 
            Item.ExecuteAction( action, Room.Current == m_containingRoom );
        Stop();
    }
}

public class TimedAction : MonoBehaviour
{
    [SerializeField] private bool m_startImmediately = false;
    [SerializeField] private float m_timeSec = 1f;
    [SerializeField] private List<ItemCombination> m_actionList = new List<ItemCombination>();

    private Timer m_timer = null;
    private Timer Timer {
        get {
            if ( m_timer == null ) CreateTimer();
            return m_timer;
        }
    }

    public void PauseTimer() { Timer.Pause(); }
    public void StartTimer() { Timer.Start(); }
    public void StopTimer() { Timer.Stop(); }

    public void UpdateInspector() {
        if ( m_actionList.Count == 0 ) m_actionList.Add( new ItemCombination() );
        foreach( var action in m_actionList) 
            action.UpdateInspector();
    }

    private void OnDestroy() {
        ControlManager.instance.RemoveTimer( m_timer );
    }

    private void Start() {
        if ( m_startImmediately ) StartTimer();
    }

    private void CreateTimer() {
        m_timer = ControlManager.instance.AddTimer( m_timeSec, m_actionList );

        // TODO HACK for now assume timed actions only in items or rooms
        var item = GetComponent<Item>();
        if ( item == null ) m_timer.ContainingRoom = GetComponent<Room>();
        else m_timer.ContainingRoom = item.location;
    }
}
