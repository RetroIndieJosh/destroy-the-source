using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MessageWindow : MonoBehaviour
{
    static public MessageWindow instance = null;

    [Header("Components")]
    [SerializeField] private TextMeshProUGUI m_messageTextMesh = null;
    [SerializeField] private TextMeshProUGUI m_speakerTextMesh = null;
    [SerializeField] private Image m_speakerImage = null;

    [Header("Visual Settings")]
    [SerializeField] private bool m_allCaps = false;
    [SerializeField] private Image m_moreCursor = null;
    [SerializeField] private float m_cursorBlinkTime = 0.5f;

    private float m_timeSinceLastBlinkSec = 0.0f;

    public bool HasMore {
        get {
            m_messageTextMesh.ForceMeshUpdate();
            return m_messageTextMesh.pageToDisplay < m_messageTextMesh.textInfo.pageCount;
        }
    }

    private Sprite SpeakerSprite {
        set {
            m_speakerImage.sprite = value;
            m_speakerImage.enabled = ( m_speakerImage.sprite != null );
        }
    }

    public void Clear() {
        SpeakerSprite = null;
        m_speakerTextMesh.text = "";
        m_messageTextMesh.text = "";

        m_messageTextMesh.pageToDisplay = 1;
    }

    private Rect WorldRect {
        get {
            var corners = new Vector3[4];
            var rectTransform = GetComponent<RectTransform>();

            rectTransform.GetWorldCorners( corners );

            var cs = FindObjectOfType<CanvasScaler>();
            var widthMult = 1.0f / 16.0f; // why 16? PPU is 32...
            var heightMult = 1.0f / 16.0f;
            var size = new Vector2( widthMult * rectTransform.rect.size.x, heightMult * rectTransform.rect.size.y );
            return new Rect( corners[0], size );
        }
    }

    public void HandleClick() {
        if( WorldRect.Contains( ControlManager.instance.MouseWorldPos ) )
            NextPage();
    }

    public void ShowMessage(string a_msg) {
        if ( m_allCaps ) a_msg = a_msg.ToUpper();
        if ( string.IsNullOrEmpty( m_messageTextMesh.text ) )
            m_messageTextMesh.text = a_msg;
        else
            m_messageTextMesh.text += " " + a_msg;
    }

    private void Awake() {
        if( instance != null ) {
            Debug.LogWarningFormat( "Duplicate message window in {0}. Destroying.", name );
            Destroy( this );
            return;
        }
        instance = this;
    }

    private void Start() {
        Clear();
    }

    private void Update() {
        if ( Input.GetKeyDown( KeyCode.Space ) )
            NextPage();

        if ( HasMore ) {
            m_timeSinceLastBlinkSec += Time.unscaledDeltaTime;
            if ( m_timeSinceLastBlinkSec > m_cursorBlinkTime ) {
                m_moreCursor.enabled = !m_moreCursor.enabled;
                m_timeSinceLastBlinkSec = 0f;
            }
        }
        else m_moreCursor.enabled = false;
    }

    public void NextPage() {
        if ( HasMore == false ) return;
        ++m_messageTextMesh.pageToDisplay;
        m_messageTextMesh.ForceMeshUpdate();
    }
}
