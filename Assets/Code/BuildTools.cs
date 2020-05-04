#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityEditor;

public class BuildTools
{
    public static GameProperties GameProperties {
        get {
            // TODO don't load this every time!
            var gameProperties = AssetDatabase.LoadAssetAtPath<GameProperties>( "Assets/Game Properties.asset" );
            if ( gameProperties == null ) UnityEngine.Debug.LogError( "No game properties set. Aborting upload." );
            return gameProperties;
        }
    }

    private static string GetBuildPath(bool a_latest, BuildTarget a_target ) {
        switch ( a_target ) {
            case BuildTarget.StandaloneLinuxUniversal: return "build/" + GetTargetPathLinux( a_latest );
            case BuildTarget.StandaloneOSX: return "build/" + GetTargetPathOsx( a_latest );
            case BuildTarget.StandaloneWindows: return "build/" + GetTargetPathWindows32( a_latest );
            case BuildTarget.StandaloneWindows64: return "build/" + GetTargetPathWindows64( a_latest );
        }

        UnityEngine.Debug.LogErrorFormat( "Unsupported build target {0}. Aborting build.", a_target );
        return null;
    }
    
    private static string GetItchPathLinux( GameProperties a_gameProperties, bool a_latest = false ) {
        return string.Format( "{0}/{1}:{2}",
            a_gameProperties.ItchUserName,
            a_gameProperties.ItchChannel,
            GetTargetPathLinux(a_latest)
        );
    }

    private static string GetItchPathOsx( GameProperties a_gameProperties, bool a_latest = false ) {
        return string.Format( "{0}/{1}:{2}",
            a_gameProperties.ItchUserName,
            a_gameProperties.ItchChannel,
            GetTargetPathOsx(a_latest)
        );
    }

    private static string GetItchPathWindows32(GameProperties a_gameProperties, bool a_latest = false ) {
        return string.Format( "{0}/{1}:{2}",
            a_gameProperties.ItchUserName,
            a_gameProperties.ItchChannel,
            GetTargetPathWindows32(a_latest)
        );
    }

    private static string GetItchPathWindows64(GameProperties a_gameProperties, bool a_latest = false ) {
        return string.Format( "{0}/{1}:{2}",
            a_gameProperties.ItchUserName,
            a_gameProperties.ItchChannel,
            GetTargetPathWindows64(a_latest)
        );
    }

    private static string GetTargetPathLinux(bool a_latest ) {
        return ( a_latest ? "latest" : GameProperties.VersionString ) + "-linux";
    }

    private static string GetTargetPathOsx(bool a_latest ) {
        return ( a_latest ? "latest" : GameProperties.VersionString ) + "-osx";
    }

    private static string GetTargetPathWindows32(bool a_latest ) {
        return ( a_latest ? "latest" : GameProperties.VersionString ) + "-windows32";
    }

    private static string GetTargetPathWindows64(bool a_latest ) {
        return ( a_latest ? "latest" : GameProperties.VersionString ) + "-windows64";
    }

    [MenuItem("Edit/Project Settings/Game Properties (JMTools)")]
    public static void EditGameProperties() {
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = GameProperties;
    }

    [MenuItem( "Build/Windows x64" )]
    public static void BuildWindows64() { Build( true, BuildTarget.StandaloneWindows64 ); }

    [MenuItem( "Build/All Latest" )]
    public static void BuildLatest() { BuildAll( true ); }

    [MenuItem( "Build/All Releases" )]
    public static void BuildRelease() { BuildAll( false ); }

    [MenuItem( "Upload/All Latest" )]
    public static void UploadLatest() { UploadAll( true ); }

    [MenuItem( "Upload/Most Recent Releases" )]
    public static void UploadRelease() { UploadAll( false ); }

    private static void UploadAll( bool a_latest ) {
        Butler( "push " + GetBuildPath( a_latest, BuildTarget.StandaloneLinuxUniversal ) + " " 
            + GetItchPathLinux( GameProperties, a_latest ) );
        Butler( "push " + GetBuildPath( a_latest, BuildTarget.StandaloneOSX ) + " " 
            + GetItchPathOsx( GameProperties, a_latest ) );
        Butler( "push " + GetBuildPath( a_latest, BuildTarget.StandaloneWindows ) + " " 
            + GetItchPathWindows32( GameProperties, a_latest ) );
        Butler( "push " + GetBuildPath( a_latest, BuildTarget.StandaloneWindows64 ) + " " 
            + GetItchPathWindows64( GameProperties, a_latest ) );
    }

    private static void BuildAll( bool a_latest ) {
        // TODO do we need this?
        var gameProperties = AssetDatabase.LoadAssetAtPath<GameProperties>( "Assets/Game Properties.asset" );
        if( gameProperties == null ) {
            UnityEngine.Debug.LogError( "No game properties set. Aborting upload." );
            return;
        }

        Build( a_latest, BuildTarget.StandaloneLinuxUniversal );
        Build( a_latest, BuildTarget.StandaloneOSX );
        Build( a_latest, BuildTarget.StandaloneWindows );
        Build( a_latest, BuildTarget.StandaloneWindows64 );
    }

    private static void Build( bool a_latest, BuildTarget a_target ) {

        string dirPath = GetBuildPath( a_latest, a_target );
        if ( dirPath == null ) return;

        string filePath = dirPath + "/" + GameProperties.FileName;
        UnityEngine.Debug.Log( "Building " + filePath + " for " + a_target );

        BuildPipeline.BuildPlayer( GameProperties.SceneList, filePath, a_target, BuildOptions.None );
        CopyFiles( dirPath );
    }

    private static void CopyFiles( string a_path ) {
        if ( !string.IsNullOrEmpty( GameProperties.ReadmeFileName ) )
            FileUtil.ReplaceFile( GameProperties.ReadmeFileName, a_path + "/" + GameProperties.ReadmeFileName );

        if ( !string.IsNullOrEmpty( GameProperties.ChangelogFileName ) )
            FileUtil.ReplaceFile( GameProperties.ChangelogFileName, a_path + "/" + GameProperties.ChangelogFileName );
    }

    private static void Butler( string args ) {
        var info = new ProcessStartInfo( "butler", args );
        var process = Process.Start( info );

        process.OutputDataReceived += ( object s, DataReceivedEventArgs e ) => {
            UnityEngine.Debug.Log( "Butler: " + e.Data );
        };
        process.ErrorDataReceived += ( object s, DataReceivedEventArgs e ) => {
            UnityEngine.Debug.LogError( "Butler: " + e.Data );
        };

        process.WaitForExit();
    }

}

#endif // UNITY_EDITOR
