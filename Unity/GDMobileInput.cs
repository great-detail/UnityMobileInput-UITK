using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using NiceJson;
using System.IO;
using AwesomeGolf;
using UnityEngine.Networking;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif



namespace Mopsicus.AG.Modified
{

    /// <summary>
    /// Base class for InputField using Visual Elements
    /// </summary>
    public abstract class GDMobileInputReceiver : VisualElement
    {
        //***************************************************************************
        // Class Properties
        //***************************************************************************

        /// <summary>
        /// Current input id
        /// </summary>
        private int mID;

        /// <summary>
        /// Root Visual Element (e.g., assigned from UI Document or created programmatically)
        /// </summary>
        protected VisualElement mRootElement { get; set; }
        
        

        //***************************************************************************
        // Initialisation
        //***************************************************************************

        protected GDMobileInputReceiver()
        {
            RegisterCallback<AttachToPanelEvent>(evt => Initialise());
            RegisterCallback<DetachFromPanelEvent>(evt => Destroy());
        }

        /// <summary>
        /// Initialize input and register interface
        /// </summary>
        protected virtual void Initialise()
        {
            mID = GDMobileInput.Register(this);
        }

        
        
        //***************************************************************************
        // Monobehaviours
        //***************************************************************************

        /// <summary>
        /// Show native on enable
        /// </summary>
        public virtual void OnEnable()
        {
        }

        /// <summary>
        /// Fix touch input issue - AGJP Modified
        /// </summary>
        public virtual void Update()
        {
        }
        
        
        
        //***************************************************************************
        // Data Handling
        //***************************************************************************

        /// <summary>
        /// Send data to plugin
        /// </summary>
        /// <param name="data">Data</param>
        protected void Execute(JsonObject data)
        {
            GDMobileInput.Execute(mID, data);
        }

        /// <summary>
        /// Send data to plugin manually
        /// </summary>
        /// <param name="data">Data</param>
        public abstract void Send(JsonObject data);

        /// <summary>
        /// Hide input
        /// </summary>
        public abstract void Hide();

        
        
        //***************************************************************************
        // Event handling
        //***************************************************************************

        /// <summary>
        /// Fix Application Focus - AGJP Modified
        /// </summary>
        public virtual void OnApplicationFocus() { }
        
        
        
        //***************************************************************************
        // Cleanup
        //***************************************************************************

        /// <summary>
        /// Action on destroy
        /// </summary>
        public virtual void Destroy()
        {
            GDMobileInput.RemoveReceiver(mID);
        }
    }

    
    
    
    /// <summary>
    /// Mobile native input plugin
    /// </summary>
    public class GDMobileInput : MonoBehaviour, IPlugin
    {

        //***************************************************************************
        // Public Properties
        //***************************************************************************

        /// <summary>
        /// Delegate for show/hide keyboard action
        /// </summary>
        public delegate void ShowDelegate(bool isShow, int height);

        /// <summary>
        /// Handler for ShowDelegate
        /// </summary>
        public static ShowDelegate OnShowKeyboard = delegate { };

        /// <summary>
        /// Delegate for prepare font error action
        /// </summary>
        public delegate void PrepareFontErrorDelegate();

        /// <summary>
        /// Handler for PrepareFontErrorDelegate
        /// </summary>
        public static PrepareFontErrorDelegate OnPrepareFontError = delegate { };



        //***************************************************************************
        // Private Properties
        //***************************************************************************

        /// <summary>
        /// Mobile fields dictionary
        /// </summary>
        private Dictionary<int, GDMobileInputReceiver> _inputs = new Dictionary<int, GDMobileInputReceiver>();

        /// <summary>
        /// Current instance
        /// </summary>
        private static GDMobileInput _instance;

        /// <summary>
        /// Cache data for hidden app state
        /// </summary>
        private JsonObject _data;

        /// <summary>
        /// Cache error for hidden app state
        /// </summary>
        private JsonObject _error;

        /// <summary>
        /// MobileInput counter
        /// </summary>
        private int _counter = 0;

#if UNITY_IOS
        /// <summary>
        /// Send data to plugin input
        /// </summary>
        [DllImport ("__Internal")]
        private static extern void inputExecute (int id, string json);

        /// <summary>
        /// Init MobileInput plugin
        /// </summary>
        [DllImport ("__Internal")]
        private static extern void inputInit ();

        /// <summary>
        /// Destroy MobileInput plugin
        /// </summary>
        [DllImport ("__Internal")]
        private static extern void inputDestroy ();
#endif


        //***************************************************************************
        // Constants
        //***************************************************************************

        /// <summary>
        /// Event name for keyboard show/hide
        /// </summary>
        const string KEYBOARD_ACTION = "KEYBOARD_ACTION";

        /// <summary>
        /// Key name for settings save
        /// </summary>
        const string INIT_KEY = "mobileinput_inited";



        //***************************************************************************
        // Getters/Setters
        //***************************************************************************

        /// <summary>
        /// Plugin name
        /// </summary>
        public string Name
        {
            get { return GetType().Name.ToLower(); }
        }

        /// <summary>
        /// Current instance for external access
        /// </summary>
        public static GDMobileInput Plugin
        {
            get { return _instance; }
        }



        //***************************************************************************
        // Monobehaviours
        //***************************************************************************

        /// <summary>
        /// Constructor
        /// </summary>
        private void Awake()
        {
            if ((object)_instance == null)
            {
                _instance = GetComponent<GDMobileInput>();
                Init();
            }
        }

        /// <summary>
        /// Fix touch input issue - AGJP Modified
        /// </summary>
        private void Update()
        {
            foreach (GDMobileInputReceiver receiver in _inputs.Values)
            {
                receiver.Update();
            }
        }



        //***************************************************************************
        // Initialisation
        //***************************************************************************

        /// <summary>
        /// Init plugin
        /// </summary>
        public static void Init()
        {
            bool fontPrepSuccess = true;
            int state = PlayerPrefs.GetInt(INIT_KEY, 0);
            if (state == 0)
            {
                string path = Application.streamingAssetsPath;
                if (Directory.Exists(path))
                {
                    string[] files = Directory.GetFiles(path, "*.ttf");
                    foreach (string filePath in files)
                    {
                        fontPrepSuccess = PrepareFontsAssets(Path.GetFileName(filePath));
                        if (!fontPrepSuccess)
                            break;
                    }
                }

                if (fontPrepSuccess)
                {
                    PlayerPrefs.SetInt(INIT_KEY, 1);
                    PlayerPrefs.Save();
                }
                else
                {
                    _instance = null;
                    return;
                }
            }

#if UNITY_EDITOR
#elif UNITY_ANDROID
            using (AndroidJavaClass plugin =
 new AndroidJavaClass (string.Format (GDPlugins.ANDROID_CLASS_MASK, _instance.Name))) {
                plugin.CallStatic ("init");
            }
#elif UNITY_IOS
            inputInit();
#endif
        }



        //***************************************************************************
        // Reciever Handling
        //***************************************************************************

        /// <summary>
        /// Init and save new MobileInput
        /// </summary>
        /// <param name="receiver">Receiver</param>
        /// <returns>Id</returns>
        public static int Register(GDMobileInputReceiver receiver)
        {
            if (receiver == null || _instance == null)
                return -1;

            int index = _instance._counter;
            _instance._counter++;
            _instance._inputs[index] = receiver;
            return index;
        }

        /// <summary>
        /// Remove MobileInput
        /// </summary>
        /// <param name="id">Input id</param>
        public static void RemoveReceiver(int id)
        {
            _instance?._inputs?.Remove(id);
        }

        /// <summary>
        /// Get MobileInput by index
        /// </summary>
        /// <param name="id">Input id</param>
        /// <returns>Receiver</returns>
        public static GDMobileInputReceiver GetReceiver(int id)
        {
            return _instance._inputs[id];
        }



        //***************************************************************************
        // Event Handling
        //***************************************************************************

        /// <summary>
        /// Send data to plugin
        /// </summary>
        /// <param name="id">id</param>
        /// <param name="data">json</param>
        public static void Execute(int id, JsonObject data)
        {
            data["id"] = id;
            string json = data.ToJsonString();
#if !UNITY_EDITOR
    #if UNITY_ANDROID
            using (AndroidJavaClass plugin = new AndroidJavaClass (GDPlugins.ANDROID_CLASS_MASK)) {
                plugin.CallStatic ("execute", id, json);
            }
    #elif UNITY_IOS
            inputExecute (id, json);
    #endif
#endif
        }

        /// <summary>
        /// Callback on data
        /// </summary>
        public void OnData(JsonObject data)
        {
            _data = data;

            try
            {
                JsonObject response = (JsonObject)JsonNode.ParseJsonString(data["data"]);
                string code = response["msg"];
                switch (code)
                {
                    case KEYBOARD_ACTION:
                        bool isShow = response["show"];
                        int height = 0;
                        height = response["height"];
                        OnShowKeyboard(isShow, height);
                        break;
                    default:
                        int id = response["id"];
                        if (_inputs.ContainsKey(id))
                        {
                            GetReceiver(id).Send(response);
                        }

                        break;
                }

                _data = null;
            }
            catch (Exception e)
            {
                AgLogger.LogE(string.Format("{0} plugin OnData error: {1}", GetType().Name, e.Message));
            }
        }

        /// <summary>
        /// Callback on error
        /// </summary>
        public void OnError(JsonObject data)
        {
            AgLogger.LogE(string.Format("{0} plugin OnError: {0}", GetType().Name,
                data.ToJsonPrettyPrintString()));
            _error = data;
            try
            {
                _error = null;
            }
            catch (Exception e)
            {
                AgLogger.LogE(string.Format("{0} plugin OnError error: {1}", GetType().Name, e.Message));
            }
        }

        /// <summary>
        /// Handler to check data on focus change
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                if (_data != null)
                {
                    OnData(_data);
                }
                else if (_error != null)
                {
                    OnError(_error);
                }
            }
        }

        /// <summary>
        /// Fix touch input issue - AGJP Modified
        /// </summary>
        void OnApplicationFocus(bool focusStatus)
        {
            foreach (GDMobileInputReceiver receiver in _inputs.Values)
            {
                receiver.OnApplicationFocus();
            }

            if (!focusStatus)
            {
                foreach (var item in _instance._inputs.Values)
                {
                    item.Hide();
                }
            }
        }



        //***************************************************************************
        // Cleanup
        //***************************************************************************

        /// <summary>
        /// Destructor
        /// </summary>
        public static void Destroy()
        {
#if UNITY_EDITOR
#elif UNITY_ANDROID
            using (AndroidJavaClass plugin =
 new AndroidJavaClass (string.Format (GDPlugins.ANDROID_CLASS_MASK, _instance.Name))) {
                plugin.CallStatic ("destroy");
            }
#elif UNITY_IOS
            inputDestroy ();
#endif
        }



        //***************************************************************************
        // Utilities
        //***************************************************************************

        /// <summary>
        /// Copy files from StreamingAssets to device path
        /// </summary>
        /// <param name="fileName">File name</param>
        static bool PrepareFontsAssets(string fileName)
        {
            bool success = true;
            string folder = Application.dataPath;
            string filepath = string.Format("{0}/{1}", Application.persistentDataPath, fileName);

            try
            {
#if UNITY_EDITOR
                string data = string.Format("{0}/{1}", Application.streamingAssetsPath, fileName);
                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }

                File.Copy(data, filepath);
#elif UNITY_ANDROID
                using (UnityWebRequest www =
 UnityWebRequest.Get (string.Format ("jar:file://{0}!/assets/{1}", folder, fileName))) {
                    www.SendWebRequest ();
                    while (!www.isDone) { }
                    File.WriteAllBytes (filepath, www.downloadHandler.data);
                }
#elif UNITY_IOS
                string data = string.Format ("{0}/Raw/{1}", folder, fileName);
                if (File.Exists (filepath)) {
                    File.Delete (filepath);
                }
                File.Copy (data, filepath);
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"MobileInput Exception - PrepareFontAssets() - {e.ToString()}");
                OnPrepareFontError();
                success = false;
            }

            return success;
        }
    }
}