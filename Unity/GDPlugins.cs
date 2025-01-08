// ----------------------------------------------------------------------------
// The MIT License
// UnityMobileInput https://github.com/mopsicus/UnityMobileInput
// Copyright (c) 2018 Mopsicus <mail@mopsicus.ru>
// ----------------------------------------------------------------------------

using UnityEngine;
using System;
using System.Collections.Generic;
using Mopsicus.Plugins;
using NiceJson;
#if UNITY_IOS
using System.Runtime.InteropServices;
#endif

namespace Mopsicus.AG.Modified {

    /// <summary>
    /// Mobile plugin interface
    /// Each plugin must implement it
    /// </summary>
    public interface IPlugin {

        /// <summary>
        /// Plaugin name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Callback on get data
        /// </summary>
        void OnData (JsonObject data);

        /// <summary>
        /// Callback on get error
        /// </summary>
        void OnError (JsonObject data);
    }
    
    
    
    /// <summary>
    /// Plugin service to manager all mobile plugins
    /// </summary>
	public class GDPlugins : MonoBehaviour
    {
        //***************************************************************************
        // Class Properties
        //***************************************************************************
        
#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Mask for Java classes
        /// </summary>
        public const string ANDROID_CLASS_MASK = "ru.mopsicus.mobileinput.Plugin";
#elif UNITY_IOS && !UNITY_EDITOR
        /// <summary>
        /// Init iOS plugins
        /// </summary>
        [DllImport ("__Internal")]
        private static extern void pluginsInit (string data);
#endif		

        /// <summary>
        /// Gameobject name on scene to receive data
        /// ACHTUNG! Do not change it
        /// </summary>
        const string cDataObject = "Plugins";

        /// <summary>
        /// Function name to receive data
        /// ACHTUNG! Do not change it
        /// </summary>
        const string cDataReceiver = "OnDataReceive";

        /// <summary>
        /// Dictionary of plugins
        /// </summary>
        private Dictionary<string, IPlugin> mPlugins;
        
        
        
        //***************************************************************************
        // Monobehaviours
        //***************************************************************************
        
		private void Awake () {
			name = cDataObject;
			DontDestroyOnLoad (gameObject);
			InitPlugins ();
		}

		private void OnDestroy () {
			mPlugins = null;
		}

        
        
        //***************************************************************************
        // Initialisation
        //***************************************************************************
        
		/// <summary>
        /// Init all plugins in app
        /// </summary>
        void InitPlugins ()
        {
            gameObject.AddComponent<GDMobileInput> ();
            //
            // other plugins
            //			
            IPlugin[] plugins = GetComponents<IPlugin> ();
            mPlugins = new Dictionary<string, IPlugin> (plugins.Length);
            foreach (var item in plugins) {
                mPlugins.Add (item.Name, item);
            }
            JsonObject data = new JsonObject ();
            data["object"] = cDataObject;
            data["receiver"] = cDataReceiver;
#if UNITY_IOS && !UNITY_EDITOR
            pluginsInit (data.ToJsonString ());
#endif
            //Debug.Log ("Plugins init");
        }

        
        
        //***************************************************************************
        // Data Handling
        //***************************************************************************
        
        /// <summary>
        /// Handler to process data to plugin
        /// </summary>
        /// <param name="data">data from plugin</param>
        void OnDataReceive (string data) {
            try {
                JsonObject info = (JsonObject) JsonNode.ParseJsonString (data);
                
                // GDMobileInput overrides the mobile input
                if (info["name"] == "mobileinput")
                {
                    info["name"] = "gdmobileinput";
                }
                
                if (mPlugins.ContainsKey (info["name"])) {
                    IPlugin plugin = mPlugins[info["name"]];
                    if (info.ContainsKey ("error")) {
                        plugin.OnError (info);
                    } else {
                        plugin.OnData (info);
                    }
                } else {
                    Debug.LogError (string.Format ("{0} plugin does not exists", info["name"]));
                }
            } catch (Exception e) {
                Debug.LogError (string.Format ("Plugins receive error: {0}, stack: {1}", e.Message, e.StackTrace));
            }
        }
	}
}