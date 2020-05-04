using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class PageHandler : MonoBehaviour
{
    private TextMeshProUGUI m_textMesh = null;

    private int Page {
        get { return m_textMesh.pageToDisplay; }
        set {
            var maxPages = m_textMesh.textInfo.pageCount;
            m_textMesh.pageToDisplay = Mathf.Clamp( value, 1, maxPages );
        }
    }

    public void NextPage() {
        ++Page;
    }

    public void PrevPage() {
        --Page;
    }

    private void Awake() {
        m_textMesh = GetComponent<TextMeshProUGUI>();
    }

    private void Start() {
        if ( m_textMesh.overflowMode != TextOverflowModes.Page )
            Debug.LogWarningFormat( "TMP in {0} is not in Page overflow mode, PageHandler will do nothing.", name );
    }
}
