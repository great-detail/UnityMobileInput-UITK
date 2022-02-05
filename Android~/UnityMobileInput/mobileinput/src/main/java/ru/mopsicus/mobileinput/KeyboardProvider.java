// ----------------------------------------------------------------------------
// The MIT License
// UnityMobileInput https://github.com/mopsicus/UnityMobileInput
// Copyright (c) 2018 Mopsicus <mail@mopsicus.ru>
// ----------------------------------------------------------------------------

package ru.mopsicus.mobileinput;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.res.Configuration;
import android.content.res.Resources;
import android.graphics.Point;
import android.graphics.Rect;
import android.graphics.drawable.ColorDrawable;
import android.os.Handler;
import android.view.Gravity;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.ViewTreeObserver;
import android.view.WindowManager;
import android.widget.PopupWindow;
import android.util.Log;
import android.view.Display;
import android.view.Window;
import android.content.Context;
import android.util.DisplayMetrics;

public class KeyboardProvider extends PopupWindow {

    private KeyboardObserver observer;
    private int keyboardLandscapeHeight;
    private int keyboardPortraitHeight;
    private View popupView;
    private View parentView;
    private Activity activity;

    // Constructor
    public KeyboardProvider(Activity activity, ViewGroup parent, KeyboardObserver listener) {
        super(activity);
        this.observer = listener;
        this.activity = activity;
        Resources resources = this.activity.getResources();
        String packageName = this.activity.getPackageName();
        int id = resources.getIdentifier("popup", "layout", packageName);
        LayoutInflater inflator = (LayoutInflater) activity.getSystemService(Activity.LAYOUT_INFLATER_SERVICE);
        this.popupView = inflator.inflate(id, null, false);
        setContentView(popupView);
        setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE | WindowManager.LayoutParams.SOFT_INPUT_STATE_ALWAYS_VISIBLE);
        setInputMethodMode(PopupWindow.INPUT_METHOD_NEEDED);
        parentView = parent;
        setWidth(0);
        setHeight(WindowManager.LayoutParams.MATCH_PARENT);
        setBackgroundDrawable(new ColorDrawable(0));
        showAtLocation(parentView, Gravity.NO_GRAVITY, 0, 0);

        navBarHeight = getNavigationBarHeight();

        popupView.getViewTreeObserver().addOnGlobalLayoutListener(new ViewTreeObserver.OnGlobalLayoutListener() {
            @Override
            public void onGlobalLayout() {
                if (popupView != null) {
                    handleOnGlobalLayout();
                }
            }
        });
    }

    // Close fake popup
    public void disable() {
        dismiss();
    }

    // Return screen orientation
    private int getScreenOrientation() {
        return activity.getResources().getConfiguration().orientation;
    }

    private int heightMax = 0;
    private int navBarHeight = 0;

    // Handler to get keyboard height
    private void handleOnGlobalLayout() {

		Rect rect = new Rect();
        popupView.getWindowVisibleDisplayFrame(rect);
        if (rect.bottom > heightMax) {
            heightMax = rect.bottom;
        }

        int keyboardHeight = heightMax - rect.bottom;


        // BF - 5/2/22
        // Took this out!  It works for 'permanentlyOn nav bars', but now I think most modern devices overlay.
        // Could improve by potentially assessing which type of nav bar is used.  But, for my purposes, this is best for now.
/*
        if (keyboardHeight > 0) {
        	keyboardHeight += navBarHeight;
        }
*/

        // BF - 5/2/22
        // Added this log statement
/*
        @SuppressLint("DefaultLocale")
        String message = String.format(
              "Rect = [%d, %d, %d, %d], HeightMax = %d,KeyboardHeight = %d, NavBarHeight = %d",
              rect.left, rect.right, rect.top, rect.bottom, heightMax, keyboardHeight, navBarHeight);
        Log.d("AGS", message);
*/

        // BF - 5/2/22
        // This is what was happening on some devices.  Get two events, the first is wrong, the second is right
        // These almost at the same time 0.025s between each one.
        // Rect = [0, 1920, 0, 78], HeightMax = 1200, KeyboardHeight = 1194, NavBarHeight = 72, Ori = 2
        // Rect = [0, 1920, 0, 639], HeightMax = 1200, KeyboardHeight = 633, NavBarHeight = 72, Ori = 2

        // Because of the situation described above, we now wait for a second event, before
        // calling the listener method.  We do all this in updateKeyboardHeight and call notifyKeyboardHeight()
        // from there instead of here..

        //notifyKeyboardHeight(keyboardHeight, keyboardHeight, getScreenOrientation());
        updateKeyboardHeight(keyboardHeight);
    }

    // BF - 5/2/22
    // Add this method, and two working vars
    boolean mWaitForAnotherKeyboardHeight = false;
    int mKeyboardHeight = 0;
    private void updateKeyboardHeight(int keyboardHeight)
    {
        mKeyboardHeight = keyboardHeight;
        if(!mWaitForAnotherKeyboardHeight)
        {
            mWaitForAnotherKeyboardHeight = true;
            new Handler().postDelayed(new Runnable() {
                @Override
                public void run() {
                    int orientation = getScreenOrientation();
                    notifyKeyboardHeight(mKeyboardHeight, mKeyboardHeight, orientation);
                    mWaitForAnotherKeyboardHeight = false;
                }
            }, 100);
        }

    }

    private int getNavigationBarHeight() {
    	if (!hasSoftKeys())
    	{
    		return 0;
    	}
        Resources resources = activity.getResources();
        int resourceId = resources.getIdentifier("navigation_bar_height", "dimen", "android");
        if (resourceId > 0) {
            return resources.getDimensionPixelSize(resourceId);
        }
        return 0;
    }

    public boolean hasSoftKeys() {
        Display d = activity.getWindowManager().getDefaultDisplay();

        DisplayMetrics realDisplayMetrics = new DisplayMetrics();
        d.getRealMetrics(realDisplayMetrics);

        int realHeight = realDisplayMetrics.heightPixels;
        int realWidth = realDisplayMetrics.widthPixels;

        DisplayMetrics displayMetrics = new DisplayMetrics();
        d.getMetrics(displayMetrics);

        int displayHeight = displayMetrics.heightPixels;
        int displayWidth = displayMetrics.widthPixels;

        boolean hasSoftwareKeys =  (realWidth - displayWidth) > 0 || (realHeight - displayHeight) > 0;
	    return hasSoftwareKeys;
	}

    // Send data observer
    private void notifyKeyboardHeight(float height, int keyboardHeight, int orientation) {
        if (observer != null) {
            observer.onKeyboardHeight(height, keyboardHeight, orientation);
        }
    }
}