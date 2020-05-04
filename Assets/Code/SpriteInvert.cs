using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteInvert : MonoBehaviour
{
    [SerializeField] private bool m_isInverted = false;

    private SpriteRenderer m_spriteRenderer = null;

    private Texture2D m_texture = null;
    private Texture2D m_textureInverted = null;

    private Sprite m_sprite = null;
    private Sprite m_spriteInverted = null;
    
    //private Color[] m_originalPixels = null;
    //private Color[] m_invertedPixels = null;

    public bool Inverted {
        set {
            m_isInverted = value;
            m_spriteRenderer.sprite = m_isInverted ? m_spriteInverted : m_sprite;
        }
    }

    private Sprite CreateSprite( Texture2D a_texture ) {
        var rect = new Rect( 0f, 0f, a_texture.width, a_texture.height );
        return Sprite.Create( a_texture, rect, Vector2.one * 0.5f, 32 );
    }

    private void Awake() {
        m_spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start() {
        var originalTexture = m_spriteRenderer.sprite.texture;
        var originalPixels = originalTexture.GetPixels();
        m_texture = new Texture2D( originalTexture.width, originalTexture.height );
        m_texture.SetPixels( originalPixels );
        m_texture.filterMode = FilterMode.Point;
        m_texture.Apply();
        m_sprite = CreateSprite( m_texture );

        var invertedPixels = new Color[originalPixels.Length];
        for ( var i = 0; i < originalPixels.Length; ++i )
            invertedPixels[i] = InvertColor( originalPixels[i] );
        m_textureInverted = new Texture2D( originalTexture.width, originalTexture.height );
        m_textureInverted.SetPixels( invertedPixels );
        m_textureInverted.filterMode = FilterMode.Point;
        m_textureInverted.Apply();
        m_spriteInverted = CreateSprite( m_textureInverted );

        Inverted = m_isInverted;
    }

    private void OnDestroy() {
        Destroy( m_sprite );
        Destroy( m_spriteInverted );
        Destroy( m_texture );
        Destroy( m_textureInverted );
    }

    private Color InvertColor(Color a_color ) {
        if ( a_color.a < 1.0f ) return Color.clear;
        return new Color( 1f - a_color.r, 1f - a_color.g, 1f - a_color.b );
    }
}
