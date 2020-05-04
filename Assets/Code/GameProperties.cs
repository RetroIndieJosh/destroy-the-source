using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Properties")]
public class GameProperties : ScriptableObject {
    [SerializeField]
    private string m_gameName = "";

    [SerializeField]
    private string m_executableName = "";

    [SerializeField]
    private int m_versionMajor = 0;

    [SerializeField]
    private int m_versionMinor = 0;

    [SerializeField]
    private int m_versionRelease = 0;

    [SerializeField]
    private string m_bitbucketUrl = "";

    [Header( "Extra Files" )]

    [SerializeField]
    [Tooltip("Filename for readme to be copied to release directories. Starts searching in base Unity project dir.")]
    private string m_readmeFileName = "README.md";

    [SerializeField]
    [Tooltip("Filename for changelog to be copied to release directories. Starts searching in base Unity project dir.")]
    private string m_changelogFileName = "changelog.md";

    [Header( "Scenes" )]

    [SerializeField]
    private string m_sceneFolder = "Assets";

    [SerializeField]
    private string[] m_sceneList = new string[0];

    [Header( "itch.io" )]

    [SerializeField]
    private string m_itchUserNane = "joshua-mclean";

    [SerializeField]
    private string m_itchChannel = "";

    public string BitBucketUrl {  get { return m_bitbucketUrl; } }
    public string GameName {  get { return m_gameName; } }
    public string FileName {  get { return m_executableName + ".exe"; } }
    public string ReadmeFileName {  get { return m_readmeFileName; } }
    public string ChangelogFileName {  get { return m_changelogFileName; } }
    public string ItchUserName {  get { return m_itchUserNane; } }
    public string ItchChannel {  get { return m_itchChannel; } }
    public string[] SceneList {
        get {
            var sceneList = new string[m_sceneList.Length];
            for ( int i = 0; i < sceneList.Length; ++i )
               sceneList[i] = m_sceneFolder + "/" + m_sceneList[i] + ".unity";
            return sceneList;
        }
    }
    public string VersionString {
        get {
            return string.Format( "{0}.{1}.{2}", m_versionMajor, m_versionMinor, m_versionRelease );
        }
    }
}
