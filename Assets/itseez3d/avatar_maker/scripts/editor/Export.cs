/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, November 2019
*/

using ItSeez3D.AvatarSdk.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ItSeez3D.AvatarMaker.Editor
{
	public static class Export
	{
		public static string ExportAvatarAsObj(AvatarInfo avatarInfo)
		{
			string objFilePath = EditorUtility.SaveFilePanel("Export As", "", "model", "obj");
			if (!string.IsNullOrEmpty(objFilePath))
			{
				string haircutName = avatarInfo.SelectedHairstyleName;
				if (!string.IsNullOrEmpty(haircutName))
				{
					CoreTools.SaveAvatarMesh(avatarInfo.headMeshRenderer, avatarInfo.code, objFilePath, MeshFileFormat.OBJ, true, false, haircutName,
						avatarInfo.HaircutColor, avatarInfo.HaircutColorTint, avatarInfo.levelOfDetails);
				}
				else
					CoreTools.SaveAvatarMesh(avatarInfo.headMeshRenderer, avatarInfo.code, objFilePath, MeshFileFormat.OBJ, true, false,
						levelOfDetails: avatarInfo.levelOfDetails);
			}
			return objFilePath;
		}

		public static string ExportAvatarAsFbx(AvatarInfo avatarInfo, bool includeBlendshapes)
		{
			string fbxFilePath = EditorUtility.SaveFilePanel("Export As", "", "model", "fbx");
			if (!string.IsNullOrEmpty(fbxFilePath))
			{
				string haircutName = avatarInfo.SelectedHairstyleName;
				if (!string.IsNullOrEmpty(haircutName))
				{
					CoreTools.SaveAvatarMesh(avatarInfo.headMeshRenderer, avatarInfo.code, fbxFilePath, MeshFileFormat.FBX, !includeBlendshapes, includeBlendshapes,
						haircutName, avatarInfo.HaircutColor, avatarInfo.HaircutColorTint, avatarInfo.levelOfDetails);
				}
				else
					CoreTools.SaveAvatarMesh(avatarInfo.headMeshRenderer, avatarInfo.code, fbxFilePath, MeshFileFormat.FBX,
						!includeBlendshapes, includeBlendshapes, levelOfDetails: avatarInfo.levelOfDetails);
			}
			return fbxFilePath;
		}
	}
}
