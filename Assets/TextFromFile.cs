using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextFromFile : MonoBehaviour
{
    [SerializeField]
    private TextAsset m_textFile = null;

    private void Start() {
        if ( m_textFile == null ) return;
        GetComponent<TextMeshProUGUI>().text = m_textFile.text;
    }
}
