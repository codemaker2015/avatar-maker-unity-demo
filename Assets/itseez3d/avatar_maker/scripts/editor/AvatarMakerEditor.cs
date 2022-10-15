/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, July 2017
*/

using Coroutines;
using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdk.Offline;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ItSeez3D.AvatarMaker.Editor
{
	[InitializeOnLoad]
	public class AvatarMakerEditor
	{
		[MenuItem("Window/Avatar Maker/Avatars")]
		public static void ShowAvatarsWindow()
		{
			var window = (AvatarMakerWindow)EditorWindow.GetWindow(typeof(AvatarMakerWindow));
			window.titleContent.text = "Avatar Maker";
			window.minSize = new Vector2(480, 550);
			window.Show();
		}

		[MenuItem("Window/Avatar Maker/Facial Motion Capture")]
		public static void ShowMotionCaptureWindow()
		{
			var window = (MotionCaptureWindow)EditorWindow.GetWindow(typeof(MotionCaptureWindow));
			window.titleContent.text = "Facial Capture";
			window.minSize = new Vector2(480, 550);
			window.Show();
		}

		[MenuItem("Window/Avatar Maker/Reset license and resources", false, 2000)]
		public static void ResetResources()
		{
			EditorRunner.instance.Run(AvatarMakerInitializer.ResetResourcesAsync());
		}
	}
}
