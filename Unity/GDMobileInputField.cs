using System;
using NiceJson;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using AwesomeGolf;
using AwesomeGolfEngine;


namespace Mopsicus.AG.Modified
{
    /// <summary>
    /// Wrapper for Unity InputField
    /// Add this as a custom control in UI Builder (UI Toolkit)
    /// </summary>
    [UxmlElement]
    public partial class GDMobileInputField : GDMobileInputReceiver
    {
        /// <summary>
        /// Config structure
        /// </summary>
        private struct MobileInputConfig
        {
            public bool Multiline;
            public Color TextColor;
            public Color BackgroundColor;
            public string ContentType;
            public string InputType;
            public string KeyboardType;
            public float FontSize;
            public string Align;
            public string Placeholder;
            public Color PlaceholderColor;
            public int CharacterLimit;
        }

        /// <summary>
        /// Button type
        /// </summary>
        public enum ReturnKeyType
        {
            Default,
            Next,
            Done,
            Search,
            Send
        }

        
        
        //***************************************************************************
        // Public Properties
        //***************************************************************************
        
        /// <summary>
        /// Custom font name
        /// </summary>
        [UxmlAttribute]
        public string pCustomFont = "SairaSemiCondensed-Regular";

        /// <summary>
        /// Hide and deselect input manually
        /// </summary>
        [UxmlAttribute]
        public bool pIsManualHideControl = false;

        /// <summary>
        /// "Done" button visible (for iOS)
        /// </summary>
        [UxmlAttribute]
        public bool pIsWithDoneButton = false;

        /// <summary>
        /// "(x)" button visible (for iOS)
        /// </summary>
        [UxmlAttribute]
        public bool pIsWithClearButton = false;

        /// <summary>
        /// Type for return button
        /// </summary>
        public ReturnKeyType pReturnKey;

        /// <summary>
        /// Action when Return pressed, for subscribe
        /// </summary>
        public Action OnReturnPressed = delegate { };

        /// <summary>
        /// Action when Focus changed
        /// </summary>
        public Action<bool> OnFocusChanged = delegate { };

        /// <summary>
        /// Action when Ready 
        /// </summary>
        public Action OnReady = delegate { };

        /// <summary>
        /// Event when Return pressed, for Unity inspector
        /// </summary>
        public UnityEvent OnReturnPressedEvent;
        
        
        
        //***************************************************************************
        // Private Properties
        //***************************************************************************

        /// <summary>
        /// Mobile input creation flag
        /// </summary>
        private bool mIsMobileInputCreated = false;

        /// <summary>
        /// InputField object
        /// </summary>
        private TextField mInputObject;

        /// <summary>
        /// InputField object
        /// </summary>
        private VisualElement mUnityTextInput;
        
        /// <summary>
        /// PlaceHolderText object
        /// </summary>
        private VisualElement mPlaceHolderText;

        /// <summary>
        /// Set focus on create flag
        /// </summary>
        private bool mIsFocusOnCreate;

        /// <summary>
        /// Set visible on create flag
        /// </summary>
        private bool mIsVisibleOnCreate = true;

        /// <summary>
        /// Last field position cache
        /// </summary>
        private Rect mLastRect;

        /// <summary>
        /// Current config
        /// </summary>
        private MobileInputConfig mConfig;
        
        
        
        //***************************************************************************
        // Constants
        //***************************************************************************
        
        /// <summary>
        /// InputField create event
        /// </summary>
        const string cCREATE = "CREATE_EDIT";

        /// <summary>
        /// InputField remove event
        /// </summary>
        const string cREMOVE = "REMOVE_EDIT";

        /// <summary>
        /// Set text to InputField
        /// </summary>
        const string cSET_TEXT = "SET_TEXT";

        /// <summary>
        /// Set palceholder to InputField
        /// </summary>
        const string cSET_PLACEHOLDER = "SET_PLACEHOLDER";

        /// <summary>
        /// Set new Rect, position, size
        /// </summary>
        const string cSET_RECT = "SET_RECT";

        /// <summary>
        /// Set focus to InputField
        /// </summary>
        const string cSET_FOCUS = "SET_FOCUS";

        /// <summary>
        /// Event when InputField is focused
        /// </summary>
        const string cON_FOCUS = "ON_FOCUS";

        /// <summary>
        /// Event when InputField is unfocused
        /// </summary>
        const string cON_UNFOCUS = "ON_UNFOCUS";

        /// <summary>
        /// Set visible to InputField
        /// </summary>
        const string cSET_VISIBLE = "SET_VISIBLE";

        /// <summary>
        /// Event when text changing in InputField
        /// </summary>
        const string cTEXT_CHANGE = "TEXT_CHANGE";

        /// <summary>
        /// Event when text end changing in InputField
        /// </summary>
        const string cTEXT_END_EDIT = "TEXT_END_EDIT";

        /// <summary>
        /// Event for Android
        /// </summary>
        const string cANDROID_KEY_DOWN = "ANDROID_KEY_DOWN";

        /// <summary>
        /// Event when Return key pressed
        /// </summary>
        const string cRETURN_PRESSED = "RETURN_PRESSED";

        /// <summary>
        /// Ready event
        /// </summary>
        const string cREADY = "READY";

        
        
        //***************************************************************************
        // Getters/Setters
        //***************************************************************************
        
        /// <summary>
        /// Current InputField for external access
        /// </summary>
        public TextField pInputField
        {
            get { return mInputObject; }
        }

        /// <summary>
        /// Current Unity Text Input for external access
        /// </summary>
        public VisualElement pUnityTextField
        {
            get { return mUnityTextInput; }
        }
        
        /// <summary>
        /// MobileInput visible
        /// </summary>
        public bool pVisible { get; private set; }
        
        /// <summary>
        /// Ratio from UI Toolkit to devices aspect ratio
        /// </summary>
        public float pRatioX { get; private set; }
        
        /// <summary>
        /// Ratio from UI Toolkit to devices aspect ratio
        /// </summary>
        public float pRatioY { get; private set; }



        //***************************************************************************
        // Always On Top Hack Properties
        //***************************************************************************
        
        /// <summary>
        /// Reference to Unity Text element used as replacement when native field is hidden
        /// </summary>
        private Label mForceBehindReplacementText;
        
        /// <summary>
        /// Cached placeholder text for the hack
        /// </summary>
        private string mMobileHackPlaceholderText;
        
        /// <summary>
        /// Cached input text for the hack
        /// </summary>
        private string mMobileHackInputText;
        
        /// <summary>
        /// Counter for nested always-on-top hack activations
        /// </summary>
        private int mAlwaysOnTopHackCount;
        
        
        
        //***************************************************************************
        // Input Field - Initialisation
        //***************************************************************************
        
        public void InitialiseTextField()
        {
            this.style.flexDirection = FlexDirection.Column;
            
            // Equivalent to awake in actual package
            mInputObject = this.Q<TextField>("tfTextField");

            // Disable use if in UI Builder
            if (mInputObject == null)
            {
                return;
            }

            mUnityTextInput = mInputObject.Q<VisualElement>("unity-text-input");
            
            foreach (var child in mUnityTextInput.Children())
            {
                mPlaceHolderText = child;
            }
            
            VisualElement current = this;
            while (current.hierarchy.parent != null)
            {
                current = current.hierarchy.parent;
            }

            var root = current;
            pRatioX = Screen.width / root.resolvedStyle.width;
            pRatioY = Screen.height / root.resolvedStyle.height;
      
            mInputObject.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            
            OnAttachToPanel();
        }

        private void OnAttachToPanel()
        {
            InitializeOnNextFrame();
        }

        /// <summary>
        /// Initialization coroutine
        /// </summary>
        private void InitializeOnNextFrame()
        {
            // Resolve the objects after the panel is attached
            this.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            this.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    
            this.PrepareNativeEdit();

            
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            this.CreateNativeEdit();
            this.SetTextNative(this.mInputObject.text);
#endif
        }

        
        //***************************************************************************
        // Monobehaviours
        //***************************************************************************
        
        /// <summary>
        /// Check position on each frame
        /// If changed - send to plugin
        /// It's need when app rotate on input field change position
        /// </summary>
        public override void Update () 
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            this.UpdateForceKeyeventForAndroid ();
#endif
            if (mIsMobileInputCreated && this.mInputObject != null) {
#if !UNITY_EDITOR
                int touchCount = Input.touchCount;
                if (touchCount > 0)
                {
                    Rect inputRect = GetScreenRectFromVisualElement(this.mUnityTextInput);
                    for (int i = 0; i < touchCount; i++) {
                        if (!inputRect.Contains (Input.GetTouch(i).position)) {
                            if (!pIsManualHideControl) {
                                Hide();
                            }
                            return;
                        }
                    }
                }
#endif
                SetRectNative();
            }
        }
        
        
        //***************************************************************************
        // Utility Methods
        //***************************************************************************
        
        /// <summary>
        /// Get sizes and convert to current screen size
        /// </summary>
        /// <param name="rect">RectTranform from Gameobject</param>
        /// <returns>Rect</returns>
        public Rect GetScreenRectFromVisualElement(VisualElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));
            
            Rect worldRect = element.worldBound;
            
            worldRect.x *= pRatioX;
            worldRect.width *= pRatioX;
            worldRect.y *= pRatioY;
            worldRect.height *= pRatioY;
            
            float x = worldRect.x;
            float y = worldRect.y;
            float width = worldRect.width;
            float height = worldRect.height;
            
            Rect rect = new Rect(x, y, width, height);
            return rect;
        }
        
        
        
        //***************************************************************************
        // Native Keyboard - Creation/Deletion
        //***************************************************************************
        
        /// <summary>
        /// Prepare config
        /// </summary>
        private void PrepareNativeEdit()
        {
            if (mInputObject == null)
            {
                GdLogger.LogE("The provided input object is not a TextField.");
                return;
            }
            
            mConfig.Placeholder = "";
            mConfig.PlaceholderColor = Color.clear;
            mConfig.CharacterLimit = mInputObject.maxLength;
            mConfig.FontSize = mPlaceHolderText.resolvedStyle.fontSize - 12.5f;
            mConfig.TextColor = mInputObject.resolvedStyle.color;
            mConfig.Align = mInputObject.resolvedStyle.unityTextAlign.ToString();
            mConfig.ContentType = "Standard";
            mConfig.BackgroundColor = mInputObject.resolvedStyle.backgroundColor;
            mConfig.Multiline = mInputObject.multiline;
            mConfig.KeyboardType = mInputObject.keyboardType.ToString();
            mConfig.InputType = "Standard";
        }

        /// <summary>
        /// Create native input field
        /// </summary>
        private void CreateNativeEdit()
        {
            Rect rect = GetScreenRectFromVisualElement(mUnityTextInput);

            JsonObject data = new JsonObject();
            data["msg"] = cCREATE;
            data["x"] = InvariantCultureString(rect.x / Screen.width);
            data["y"] = InvariantCultureString(rect.y / Screen.height);
            data["width"] = InvariantCultureString(rect.width / Screen.width);
            data["height"] = InvariantCultureString(rect.height / Screen.height);
            data["character_limit"] = mConfig.CharacterLimit;
            data["text_color_r"] = InvariantCultureString(mConfig.TextColor.r);
            data["text_color_g"] = InvariantCultureString(mConfig.TextColor.g);
            data["text_color_b"] = InvariantCultureString(mConfig.TextColor.b);
            data["text_color_a"] = InvariantCultureString(1.0f);
            data["back_color_r"] = InvariantCultureString(mConfig.BackgroundColor.r);
            data["back_color_g"] = InvariantCultureString(mConfig.BackgroundColor.g);
            data["back_color_b"] = InvariantCultureString(mConfig.BackgroundColor.b);
            data["back_color_a"] = InvariantCultureString(0.0f); // Use unity text field instead
            data["font_size"] = InvariantCultureString(mConfig.FontSize);
            data["content_type"] = mConfig.ContentType;
            data["align"] = mConfig.Align;
            data["with_done_button"] = this.pIsWithDoneButton;
            data["with_clear_button"] = this.pIsWithClearButton;
            data["placeholder"] = mConfig.Placeholder;
            data["placeholder_color_r"] = InvariantCultureString(mConfig.PlaceholderColor.r);
            data["placeholder_color_g"] = InvariantCultureString(mConfig.PlaceholderColor.g);
            data["placeholder_color_b"] = InvariantCultureString(mConfig.PlaceholderColor.b);
            data["placeholder_color_a"] = InvariantCultureString(mConfig.PlaceholderColor.a);
            data["multiline"] = mConfig.Multiline;
            data["font"] = this.pCustomFont;
            data["input_type"] = mConfig.InputType;
            data["keyboard_type"] = mConfig.KeyboardType;
            switch (pReturnKey)
            {
                case ReturnKeyType.Next:
                    data["return_key_type"] = "Next";
                    break;
                case ReturnKeyType.Done:
                    data["return_key_type"] = "Done";
                    break;
                case ReturnKeyType.Search:
                    data["return_key_type"] = "Search";
                    break;
                case ReturnKeyType.Send:
                    data["return_key_type"] = "Send";
                    break;
                default:
                    data["return_key_type"] = "Default";
                    break;
            }
            this.Execute(data);
        }
        
        /// <summary>
        /// Remove field
        /// </summary>
        private void RemoveNative()
        {
            JsonObject data = new JsonObject();
            data["msg"] = cREMOVE;
            this.Execute(data);
        }
        
        
        
        //***************************************************************************
        // Native Keyboard - Message Handling
        //***************************************************************************
        
        /// <summary>
        /// Sending data to plugin
        /// </summary>
        /// <param name="data">JSON</param>
        public override void Send(JsonObject data)
        {
            PluginsMessageRoutine(data);
        }

        /// <summary>
        /// Remove focus, keyboard when app lose focus
        /// </summary>
        public override void Hide()
        {
            this.SetFocus(false);
        }
        
        /// <summary>
        /// Coroutine for send, so its not freeze main thread
        /// </summary>
        /// <param name="data">JSON</param>
        private void PluginsMessageRoutine(JsonObject data)
        {
            string msg = data["msg"];
            if (msg.Equals(cTEXT_CHANGE))
            {
                string text = data["text"];
                this.OnTextChange(text);
            }
            else if (msg.Equals(cREADY))
            {
                this.Ready();
                OnReady();
            }
            else if (msg.Equals(cON_FOCUS))
            {
                OnFocusChanged(true);
            }
            else if (msg.Equals(cON_UNFOCUS))
            {
                OnFocusChanged(false);
            }
            else if (msg.Equals(cTEXT_END_EDIT))
            {
                string text = data["text"];
                this.OnTextEditEnd(text);
            }
            else if (msg.Equals(cRETURN_PRESSED))
            {
                OnReturnPressed();
                if (OnReturnPressedEvent != null)
                {
                    OnReturnPressedEvent.Invoke();
                }
            }
        }

        /// <summary>
        /// New field successfully added
        /// </summary>
        void Ready()
        {
            mIsMobileInputCreated = true;
            if (!mIsVisibleOnCreate)
            {
                SetVisible(false);
            }

            if (mIsFocusOnCreate)
            {
                SetFocus(true);
            }
        }

        /// <summary>
        /// Set text to field
        /// </summary>
        /// <param name="text">New text</param>
        void SetTextNative(string text)
        {
            JsonObject data = new JsonObject();
            data["msg"] = cSET_TEXT;
            data["text"] = text;
            this.Execute(data);
        }

        /// <summary>
        /// Set placeholder to field
        /// </summary>
        /// <param name="text">New placeholder</param>
        void SetPlaceholderNative(string text)
        {
            JsonObject data = new JsonObject();
            data["msg"] = cSET_PLACEHOLDER;
            data["placeholder"] = text;
            this.Execute(data);
        }

        /// <summary>
        /// Set new size and position
        /// </summary>
        /// <param name="inputRect">RectTransform</param>
        public void SetRectNative()
        {
            Rect rect = GetScreenRectFromVisualElement(mUnityTextInput);
            if (mLastRect == rect)
            {
                return;
            }

            mLastRect = rect;
            JsonObject data = new JsonObject();
            data["msg"] = cSET_RECT;
            data["x"] = InvariantCultureString(rect.x / Screen.width);
            data["y"] = InvariantCultureString(rect.y / Screen.height);
            data["width"] = InvariantCultureString(rect.width / Screen.width);
            data["height"] = InvariantCultureString(rect.height / Screen.height);
            
            this.Execute(data);
        }

        /// <summary>
        /// Set focus on field
        /// </summary>
        /// <param name="isFocus">true | false</param>
        public void SetFocus(bool isFocus)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (!mIsMobileInputCreated)
            {
                mIsFocusOnCreate = isFocus;
                return;
            }

            JsonObject data = new JsonObject();
            data["msg"] = cSET_FOCUS;
            data["is_focus"] = isFocus;
            this.Execute(data);
#else
            
            // Handle focus in UI Toolkit
            if (mInputObject is VisualElement inputElement)
            {
                if (isFocus)
                {
                    // For TextField, use Focus() to set focus
                    if (mInputObject is TextField textField)
                    {
                        textField.Focus();
                    }
                    else
                    {
                        // If it's another type of VisualElement, handle accordingly
                        inputElement.Focus();
                    }
                }
                else
                {
                    // Blur (remove focus) from the input element
                    inputElement.Blur();
                }
            }
            else
            {
                mIsFocusOnCreate = isFocus;
            }
#endif
        }

        /// <summary>
        /// Set field visible
        /// </summary>
        /// <param name="isVisible">true | false</param>
        public void SetVisible(bool isVisible)
        {
            if (!mIsMobileInputCreated)
            {
                mIsVisibleOnCreate = isVisible;
                return;
            }

            JsonObject data = new JsonObject();
            data["msg"] = cSET_VISIBLE;
            data["is_visible"] = isVisible;
            this.Execute(data);
            this.pVisible = isVisible;
        }
        
        
        
        //***************************************************************************
        // Native Keyboard - Event Handling
        //***************************************************************************
        
        /// <summary>
        /// Handler for app focus lost
        /// </summary>
        private void OnApplicationFocus(bool hasFocus) {
            if (!mIsMobileInputCreated)
                this.SetVisible (hasFocus);
        }
        
        /// <summary>
        /// Text change callback
        /// </summary>
        /// <param name="text">new text</param>
        private void OnTextChange(string text)
        {
            if (text == this.mInputObject.text) return;
            this.mInputObject.value = text;
            
            // Set Unity text alpha based on whether native keyboard has text
            if (mUnityTextInput != null)
            {
                float alpha = string.IsNullOrEmpty(text) ? 1.0f : 0.0f;
                Color currentColor = mConfig.TextColor;
                mUnityTextInput.style.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            }
        }

        /// <summary>
        /// Text change end callback
        /// </summary>
        /// <param name="text">text</param>
        private void OnTextEditEnd(string text)
        {
            this.mInputObject.value = text;
            SetFocus(false);
        }
        
        
        
        //***************************************************************************
        // Native Keyboard - Android Specific
        //***************************************************************************
        
#if UNITY_ANDROID && !UNITY_EDITOR

        /// <summary>
        /// Send android button state
        /// </summary>
        /// <param name="key">Code</param>
        private void ForceSendKeydownAndroid(string key)
        {
            JsonObject data = new JsonObject();
            data["msg"] = cANDROID_KEY_DOWN;
            data["key"] = key;
            this.Execute(data);
        }

        /// <summary>
        /// Keyboard handler
        /// </summary>
        private void UpdateForceKeyeventForAndroid()
        {
            if (UnityEngine.Input.anyKeyDown)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace))
                {
                    this.ForceSendKeydownAndroid("backspace");
                }
                else
                {
                    foreach (char c in UnityEngine.Input.inputString)
                    {
                        if (c == '\n')
                        {
                            this.ForceSendKeydownAndroid("enter");
                        }
                        else
                        {
                            this.ForceSendKeydownAndroid(Input.inputString);
                        }
                    }
                }
            }
        }
#endif
        
        
        
        //***************************************************************************
        // Cleanup
        //***************************************************************************
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (mIsMobileInputCreated) {
                this.SetFocus (false);
                this.SetVisible (false);
            }
            
            RemoveNative(); 
        }
        
        
        
        //***************************************************************************
        // Utilities
        //***************************************************************************
        
        /// <summary>
        /// Set the replacement label reference for the always-on-top hack
        /// </summary>
        /// <param name="replacementLabel">Label to use as replacement</param>
        public void SetReplacementLabel(Label replacementLabel)
        {
            mForceBehindReplacementText = replacementLabel;
        }
        
        /// <summary>
        /// On iOS, editViews are created in the native plugin and added to the main view
        /// controller (i.e. Unity's game view). This means that UITextFields are always
        /// drawn on top. They can also be part of scroll views and be under dialog boxes.
        /// This hack switches them off when they might be 'under' other UI elements.
        /// </summary>
        public void ReplaceNativeTextFieldWithUnityView()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (mForceBehindReplacementText == null)
            {
                GdLogger.LogW("ReplaceNativeTextFieldWithUnityView called but no replacement label set");
                return;
            }
            
            var clearTexts = false;
            // We only want/need to do this for the first time its required
            // (i.e. multiple things can happen at the same time that force the hack)
            if (mAlwaysOnTopHackCount == 0)
            {
                SetFocus(false);
                mForceBehindReplacementText.style.display = DisplayStyle.Flex;

                mMobileHackPlaceholderText = mConfig.Placeholder;
                mMobileHackInputText = mInputObject.text;

                // If there is no text entered, we want to 'copy' the placeholder
                if (string.IsNullOrEmpty(mMobileHackInputText))
                {
                    mForceBehindReplacementText.text = mMobileHackPlaceholderText;
                    mForceBehindReplacementText.style.color = mConfig.PlaceholderColor;
                }
                else
                {
                    mForceBehindReplacementText.text = mMobileHackInputText;
                    mForceBehindReplacementText.style.color = mConfig.TextColor;
                }

                clearTexts = true;
            }

            mAlwaysOnTopHackCount++;

            // Do the above before this. ALWAYS!
            if (clearTexts)
            {
                SetVisible(false);
            }
#endif
        }

        /// <summary>
        /// Update the replacement text when input changes while hack is active
        /// </summary>
        /// <param name="text">New text value</param>
        private void UpdateReplacementText(string text)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (mForceBehindReplacementText == null) return;
            
            mMobileHackInputText = text;
            if (string.IsNullOrEmpty(text))
            {
                mForceBehindReplacementText.text = mMobileHackPlaceholderText;
                mForceBehindReplacementText.style.color = mConfig.PlaceholderColor;
            }
            else
            {
                mForceBehindReplacementText.text = text;
                mForceBehindReplacementText.style.color = mConfig.TextColor;
            }
#endif
        }

        /// <summary>
        /// Restore the native text field after the overlay is removed
        /// </summary>
        public void ReplaceUnityViewWithNativeTextField()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (mAlwaysOnTopHackCount > 0)
            {
                mAlwaysOnTopHackCount--;
                if (mInputObject != null && mAlwaysOnTopHackCount == 0)
                {
                    SetVisible(true);
                    if (mForceBehindReplacementText != null)
                    {
                        mForceBehindReplacementText.style.display = DisplayStyle.None;
                    }
                }
            }
#endif
        }
        
        /// <summary>
        /// Get current text, accounting for the always-on-top hack state
        /// </summary>
        public string Text
        {
            get
            {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
                if (mAlwaysOnTopHackCount > 0)
                    return mMobileHackInputText;
#endif
                return mInputObject?.text ?? "";
            }
            set
            {
                if (mInputObject != null)
                {
                    mInputObject.value = value;
                    SetTextNative(value);
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
                    if (mAlwaysOnTopHackCount > 0)
                        UpdateReplacementText(value);
#endif
                }
            }
        }
        
        
        
        //***************************************************************************
        // Utilities
        //***************************************************************************
        
        /// <summary>
        /// Convert float value to InvariantCulture string
        /// </summary>
        /// <param name="value">float value</param>
        /// <returns></returns>
        private string InvariantCultureString(float value)
        {
            return value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}