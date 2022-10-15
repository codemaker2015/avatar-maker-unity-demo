/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, January 2019
*/

using Coroutines;
using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarSdk.Offline;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ItSeez3D.AvatarMaker
{
	public class AvatarMakerInitializer
	{
		private static bool isInitialized = false;

		private static bool isProVersion = false;

		private static bool isMotionCaptureSupported = true;

		private static CoroutineHandle initRoutine;

		private static IAvatarProvider avatarProvider = null;

		private static object initSyncMutex = new object();

		public static bool IsInitialized
		{
			get { return isInitialized; }
		}

		public static bool IsInitializationInProgress
		{
			get { return initRoutine.IsRunning; }
		}

		public static IAvatarProvider AvatarProvider
		{
			get { return avatarProvider; }
		}

		public static bool IsProVersion
		{
			get { return isProVersion; }
		}

		public static bool IsMotionCaptureSupported
		{
			get { return isMotionCaptureSupported; }
		}

		public static void StartInitialization()
		{
			lock (initSyncMutex)
			{
				InitAvatarSdkMgrIfNeeded();

				if (!IsInitialized && !IsInitializationInProgress)
				{
					avatarProvider = AvatarSdkMgr.IoCContainer.Create<IAvatarProvider>();
					initRoutine = EditorRunner.instance.Run(InitRoutine());
				}
			}
		}

		public static bool IsPlatformSupported()
		{
#if UNITY_EDITOR_WIN
			return true;
#else
			return false;
#endif
		}

		public static IEnumerator ResetResourcesAsync()
		{
			InitAvatarSdkMgrIfNeeded();

			var resourcesPath = AvatarSdkMgr.Storage().GetResourcesDirectory();
			Debug.LogFormat("Resource directory: {0}", resourcesPath);
			Directory.Delete(resourcesPath, true);
			Directory.CreateDirectory(resourcesPath);
			yield return OfflineSdkUtils.EnsureSdkResourcesUnpacked(resourcesPath);
			isProVersion = AvatarMakerPlugin.IsProVersion();
		}

		private static void InitAvatarSdkMgrIfNeeded()
		{
			if (!AvatarSdkMgr.IsInitialized)
			{
				AvatarSdkMgr.Init(sdkType: SdkType.Offline);
				AvatarSdkMgr.Settings.SeparateHeadAndFaceResources = false;
			}
		}

		private static IEnumerator InitRoutine()
		{
			yield return avatarProvider.InitializeAsync();
			isProVersion = AvatarMakerPlugin.IsProVersion();
			isMotionCaptureSupported = AvatarMakerPlugin.IsHardwareSupportedForMotionCapture();
			isInitialized = true;
		}


	}
}
