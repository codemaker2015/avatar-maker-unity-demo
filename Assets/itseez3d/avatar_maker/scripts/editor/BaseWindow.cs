/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, January 2019
*/

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ItSeez3D.AvatarMaker.Editor
{
	public abstract class BaseWindow : EditorWindow
	{
		protected readonly float cameraOffset = 0.5f;

		protected GUIStyle titleStyle, titleLeftStyle, errorStyle, redButtonStyle, greenBoldButtonStyle, buttonSkin, linkStyle;
		protected GUIStyle avatarsFoldoutStyle;

		/// <summary>
		/// Checks if GUI styles are initialized and initializes them if needed
		/// </summary>
		protected virtual void InitUI()
		{
			if (titleStyle == null)
			{
				titleStyle = new GUIStyle(EditorStyles.boldLabel)
				{
					alignment = TextAnchor.MiddleCenter
				};
			}
			if (titleLeftStyle == null)
			{
				titleLeftStyle = new GUIStyle(EditorStyles.boldLabel);
			}
			if (errorStyle == null)
			{
				errorStyle = new GUIStyle(EditorStyles.boldLabel);
				errorStyle.normal.textColor = Color.red;
				errorStyle.alignment = TextAnchor.MiddleCenter;
			}
			if (linkStyle == null)
			{
				linkStyle = new GUIStyle(EditorStyles.boldLabel);
				linkStyle.normal.textColor = new Color(0.2f, 0.4f, 0.73f);
				linkStyle.fontSize = 14;
				linkStyle.alignment = TextAnchor.MiddleCenter;
			}
			if (redButtonStyle == null)
			{
				redButtonStyle = new GUIStyle("Button");
				redButtonStyle.normal.textColor = new Color(255f / 255f, 69f / 255f, 0f);
			}
			if (greenBoldButtonStyle == null)
			{
				greenBoldButtonStyle = new GUIStyle("Button");
				greenBoldButtonStyle.normal.textColor = new Color(107f / 255f, 142f / 255f, 35f / 255f);
				greenBoldButtonStyle.fontStyle = FontStyle.Bold;
			}
			if (buttonSkin == null)
			{
				buttonSkin = new GUIStyle(GUI.skin.button);
				buttonSkin.margin = new RectOffset(2, 2, 2, 2);
			}
			if (avatarsFoldoutStyle == null)
			{
				avatarsFoldoutStyle = new GUIStyle(EditorStyles.foldout);
				avatarsFoldoutStyle.fontStyle = FontStyle.Bold;
				avatarsFoldoutStyle.onActive.textColor = titleStyle.onActive.textColor;
				avatarsFoldoutStyle.onFocused.textColor = avatarsFoldoutStyle.onActive.textColor;
				avatarsFoldoutStyle.active.textColor = avatarsFoldoutStyle.onActive.textColor;
				avatarsFoldoutStyle.focused.textColor = avatarsFoldoutStyle.onActive.textColor;
			}
		}

		protected void ShowAvatarMakerProLink()
		{
			if (GUILayout.Button("Avatar Maker Pro - 3D avatar from a single selfie", linkStyle))
				Application.OpenURL("http://u3d.as/1oKv");
		}
	}
}
