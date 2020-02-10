

using System.Diagnostics;
using System.Threading;
using Debug = UnityEngine.Debug;
using File = UnityEngine.Windows.File;
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace UnityToolbarExtender
{

    public class DrawToolBarConfig
    {
        public enum MODE { DEFAULT, SVN }
        public static MODE mode;
    }
    public class DrawToolBarRightConfig
    {
        public enum MODE { DEFAULT, DEVELOP }
        public static MODE mode;
    }

    public class DrawItems
    {
        public string title;
        public System.Action callback;
    }

    public class DrawList
    { 
        public List<DrawItems> list = new List<DrawItems>();
        public DrawToolBarConfig.MODE mode;
        public int Max {get=>list.Count;}
        public int current = 0; 
        public void AddItem(string title, System.Action callback) { list.Add(new DrawItems() { callback = callback, title = title });}
        public void PrevPrev()
        {
            current = 0;
        }
        public void NextNext()
        {
            current = Max - 3;
        }
        public void Prev()
        {
            current = Mathf.Clamp(current - 1, 0, Max - 3);
        }
        public void Next()
        {
            current = Mathf.Clamp(current + 1, 0, Max - 3 );
        }
        public void Draw()
        {
            //DRAW ARROW
            if (Max > 3 && current > 0)
                DrawButton("<<", null, GUIStyleManager.GetCustomStyle(32, btnTexture: GUIStyleManager.ui_red_bt), PrevPrev);
            if (Max > 3 && current > 0)
                DrawButton("<", null, GUIStyleManager.GetCustomStyle(24, btnTexture: GUIStyleManager.ui_red_bt), Prev);



            //DRAW Coontent
            for (int i = current; i< current + 3; i++)
            {
                DrawButton(list[i].title,null, GUIStyleManager.GetCustomStyle(100, btnTexture: GUIStyleManager.ui_blue_bt), list[i].callback);
            }


            //DRAW ARROW
            if (Max > 3 && current+3 != Max )
                DrawButton(">", null, GUIStyleManager.GetCustomStyle(24, btnTexture: GUIStyleManager.ui_red_bt), Next);
            if (Max > 3 && current+3 != Max)
                DrawButton(">>", null, GUIStyleManager.GetCustomStyle(32, btnTexture: GUIStyleManager.ui_red_bt), NextNext);


            //DRAW Exit
            DrawButton("X", "TOOLTIP", GUIStyleManager.GetCustomStyle(32, btnTexture: GUIStyleManager.ui_red_bt, fontStyle: FontStyle.Bold), () =>
            {
                DrawToolBarConfig.mode = DrawToolBarConfig.MODE.DEFAULT;
            });
        }
        void DrawButton(string title, string tooltip, GUIStyle guiStyle, System.Action onClick)
        {
            if (GUILayout.Button(new GUIContent(title, tooltip), guiStyle))
            {
                onClick?.Invoke();
            }
        }
    }



    public static class SVNEditorTool
    {
        public static string svnPath = null;
        private const string COMMAND_UPDATE = "/command:update /path:{path} /closeonend:0";
        private const string COMMAND_COMMIT = "/command:commit /path:{path}";
        
        
        /// <summary>
        /// SVN의 경로를 자동으로 탐색합니다. 찾는데 실패한경우 null리턴
        /// </summary>
        /// <returns> null : cannot found. </returns>
        public static string FindSvnPathAuto()
        {

            List<string> pathList = new List<string>()
            {
                @"{Drive}:\Program Files\TortoiseSVN\bin\TortoiseProc.exe",
                @"{Drive}:\Program Files (x86)\TortoiseSVN\bin\TortoiseProc.exe", 
                @"{Drive}:\TortoiseSVN\bin\TortoiseProc.exe",
                @"{Drive}:\TortoiseSVN\bin\TortoiseProc.exe"
            };
            
            List<string> makeRealPathList = new List<string>();
            for (int i = 0; i < pathList.Count; i++)
            {
                for (int j = 65; j < 90; j++)
                {
                    var tryPath = pathList[i].Replace("{Drive}",  ((char)j).ToString() ); 
                    if (File.Exists(tryPath))
                    {
                        Debug.Log("SVN.exe Auto Found! " + tryPath);
                        return tryPath;
                         
                    }
                }
            }

            return null;
        }

        static string GetUpdateCMD(string path)
        {
            return COMMAND_UPDATE.Replace("{path}", path);
        }
        static string GetCommitCMD(string path)
        {
            return COMMAND_COMMIT.Replace("{path}", path);
        }
        public static void Update(string path)
        {
            FindSvnPath();
            if (!string.IsNullOrEmpty(svnPath))
            { 
                Debug.Log("SVN :: Update Try " + path);
                Process proc = new Process();
                proc.StartInfo = new ProcessStartInfo(svnPath, GetUpdateCMD(path)); 
                // Unity Thread의 Safe를 보장하기 위함.
                ThreadStart ths = new ThreadStart(delegate() { proc.Start(); });
                Thread th = new Thread(ths);
                th.IsBackground = true;
                th.Start();
            }
            else
            {
                EditorUtility.DisplayDialog("", "Can't find svn. re-install svn default path. ", "ok");
            }
        }

        static void FindSvnPath()
        { 
            if (string.IsNullOrEmpty(svnPath))
            {
                svnPath = FindSvnPathAuto();
            }

            if (string.IsNullOrEmpty(svnPath))
            {
                svnPath = PlayerPrefs.GetString("SVN_PATH", null);
            }
        }
        
        
        public static void Commit(string path)
        {
            FindSvnPath();

            if (!string.IsNullOrEmpty(svnPath))
            { 
                Debug.Log("SVN :: Commit Try " + path);
                Process proc = new Process();
                proc.StartInfo = new ProcessStartInfo(svnPath, GetCommitCMD(path)); 
                // Unity Thread의 Safe를 보장하기 위함.
                ThreadStart ths = new ThreadStart(delegate() { proc.Start(); });
                Thread th = new Thread(ths);
                th.IsBackground = true;
                th.Start();
            }
            else
            {
                EditorUtility.DisplayDialog("", "Can't find svn. re-install svn default path. ", "ok");  
            }
        }
    }
    [InitializeOnLoad]
    public class DrawToolbar
    {
        private static bool enabled_;
        
        private const string MENU_NAME = "SVN/ShowHide";
        [MenuItem(DrawToolbar.MENU_NAME)]
        private static void ToggleAction() {
  
            /// Toggling action
            PerformAction( !DrawToolbar.enabled_);
        }
  
        public static void PerformAction(bool enabled) {
  
            /// Set checkmark on menu item
            Menu.SetChecked(DrawToolbar.MENU_NAME, enabled);
            /// Saving editor state
            EditorPrefs.SetBool(DrawToolbar.MENU_NAME, enabled);
  
            DrawToolbar.enabled_ = enabled;
  
            /// Perform your logic here...
        }
        
        
        
        static DrawList draw_debug_view = new DrawList(); 
        static DrawList draw_features = new DrawList(); 
        static DrawToolbar()
        { 
            
            DrawToolbar.enabled_ = EditorPrefs.GetBool(DrawToolbar.MENU_NAME, false);
  
            /// Delaying until first editor tick so that the menu
            /// will be populated before setting check state, and
            /// re-apply correct action
            EditorApplication.delayCall += () => {
                PerformAction(DrawToolbar.enabled_);
            };
            
            ToolbarExtender.LeftToolbarGUI.Add(OnLeftUI); 
            ToolbarExtender.RightToolbarGUI.Add(OnRightUI);

        
            draw_debug_view.AddItem("Commit", () =>
            {
      
                SVNEditorTool.Commit(Environment.CurrentDirectory+"/Assets");
            });
            draw_debug_view.AddItem("Update", () =>
            {         
                bool commitBreak = false; 
                if (EditorPrefs.GetBool("UPDATE_GAME_ART_TEAM"))
                {
                    var v = EditorUtility.DisplayDialog("슬픔방지메세지", "에디터 상태에서 하던 작업이 있다면, 업데이트 받기전에 반드시 먼저 작업내용을 저장을 해야 합니다. 문제가 없다면 '업데이트 받기'를 클릭하세요.", "업데이트 받기", "업데이트 안받기");
                    if (!v)
                    {
                        commitBreak = true;
                    }
                } 
                if (commitBreak == false)
                {
                    SVNEditorTool.Update(Environment.CurrentDirectory+"/Assets");
                }
            }); 
            draw_debug_view.AddItem("Example", () =>
            {
                Debug.Log("You Click Example Button.");
                EditorUtility.DisplayDialog("", "You can add your custom method. Check it DrawToolbar.cs", "-"); 
            });    

        }
         
        static void DrawButton(string title, string tooltip, GUIStyle guiStyle, System.Action onClick)
        {
            if (GUILayout.Button(new GUIContent(title, tooltip), guiStyle))
            {
                onClick?.Invoke();
            }
        }
        static void DrawCloseButton(System.Action onClick)
        {
            DrawButton(null, null, GUIStyleManager.GetCustomStyle(btnTexture: GUIStyleManager.ui_close, width: 25), onClick);
        }

        static void OnRightUI()
        { 
            GUILayout.FlexibleSpace(); 
            DrawDefault(); 
            DrawUIDebugView();
        }

        static void DrawDefault()
        {  
            var style = GUIStyleManager.GetCustomStyle(40, btnTexture: GUIStyleManager.ui_red_bt,
                fontStyle: FontStyle.Normal); 
            if (DrawToolBarConfig.mode == DrawToolBarConfig.MODE.DEFAULT)
            {
                DrawButton("Svn", ":D", GUIStyleManager.GetCustomStyle(80, btnTexture: GUIStyleManager.ui_red_bt, fontStyle: FontStyle.Normal), () =>
                {
 
                    if (DrawToolbar.enabled_ == false)
                    {
                        var v = EditorUtility.DisplayDialog("svn",
                            "Turn on svn manager?",
                            "yes", "no");

                        if (v)
                        {
                            EditorUtility.DisplayDialog("Ok!",
                                "enabled svn manager!",
                                "happy!");
                            DrawToolBarConfig.mode = DrawToolBarConfig.MODE.SVN;
                            PerformAction(true);
                        }
                        return;
                    }

                    DrawToolBarConfig.mode = DrawToolBarConfig.MODE.SVN;
                });
            }

        }


        static void DrawUIDebugView()
        { 
            if (DrawToolBarConfig.mode == DrawToolBarConfig.MODE.SVN)
            {
                draw_debug_view.Draw();
            } 
        }  


        static void OnLeftUI()
        {
 
        }
    }


} 
#endif