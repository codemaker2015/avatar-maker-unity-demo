/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, November 2019
*/

using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ItSeez3D.AvatarMaker.Editor
{
	public class AnimationClipModifier
	{
		class AnimationCurveWithBinding
		{
			public AnimationCurveWithBinding(EditorCurveBinding curveBinding, AnimationCurve curve)
			{
				this.curveBinding = curveBinding;
				this.curve = curve;
			}
			public EditorCurveBinding curveBinding;
			public AnimationCurve curve;
		}


		private AnimationClip animationClip = null;

		private List<AnimationCurveWithBinding> blendshapesCurveBindings = new List<AnimationCurveWithBinding>();
		private List<string> blendshapesNamesList = new List<string>();
		private List<AnimationCurveWithBinding> poseCurveBindings = new List<AnimationCurveWithBinding>();

		public AnimationClipModifier(AnimationClip animationClip)
		{
			this.animationClip = animationClip;
			ReadAnimationClip();
		}

		public void SetAnimationClip(AnimationClip animationClip)
		{
			this.animationClip = animationClip;
			ReadAnimationClip();
		}

		public string PoseObjectName { get; set; }

		public bool PoseModificationsExist
		{
			get { return poseCurveBindings.Count > 0; }
		}

		public string BlendshapesObjectName { get; set; }

		public int BlendshapesCount
		{
			get { return blendshapesCurveBindings.Count; }
		}

		public void DeletePoseModifications()
		{
			poseCurveBindings.Clear();
		}

		public void DeleteAllBlendshapesModifications()
		{
			blendshapesCurveBindings.Clear();
			blendshapesNamesList.Clear();
		}

		public string GetBlendshapeName(int idx)
		{
			return blendshapesNamesList[idx];
		}

		public void SetBlendshapeName(int idx, string name)
		{
			blendshapesNamesList[idx] = name;
			blendshapesCurveBindings[idx].curveBinding.propertyName = string.Format("blendshape.{0}", name);
		}

		public void DeleteBlendshapeModifications(int idx)
		{
			blendshapesCurveBindings.RemoveAt(idx);
			blendshapesNamesList.RemoveAt(idx);
		}

		public void RevertChanges()
		{
			ReadAnimationClip();
		}

		public void ApplyChanges()
		{
			animationClip.ClearCurves();

			foreach (AnimationCurveWithBinding curveWithBinding in poseCurveBindings)
				animationClip.SetCurve(PoseObjectName, curveWithBinding.curveBinding.type, curveWithBinding.curveBinding.propertyName, curveWithBinding.curve);

			foreach (AnimationCurveWithBinding curveWithBinding in blendshapesCurveBindings)
				animationClip.SetCurve(BlendshapesObjectName, curveWithBinding.curveBinding.type, curveWithBinding.curveBinding.propertyName, curveWithBinding.curve);

			AssetDatabase.SaveAssets();

			ReadAnimationClip();
		}

		public bool IsValidAvatarAnimation { get; set; }

		private void ReadAnimationClip()
		{
			try
			{
				IsValidAvatarAnimation = false;

				EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(animationClip);

				blendshapesCurveBindings.Clear();
				blendshapesNamesList.Clear();
				poseCurveBindings.Clear();

				string blendshapeNameSuffix = "blendShape.";
				foreach (EditorCurveBinding curveBinding in curveBindings)
				{
					string propertyName = curveBinding.propertyName;
					if (propertyName.Contains(blendshapeNameSuffix))
					{
						blendshapesCurveBindings.Add(new AnimationCurveWithBinding(curveBinding, AnimationUtility.GetEditorCurve(animationClip, curveBinding)));
						string blendshapeName = propertyName.Substring(blendshapeNameSuffix.Length);
						blendshapesNamesList.Add(blendshapeName);
					}
					else if (propertyName.Contains("m_LocalPosition") || propertyName.Contains("m_LocalRotation"))
						poseCurveBindings.Add(new AnimationCurveWithBinding(curveBinding, AnimationUtility.GetEditorCurve(animationClip, curveBinding)));
				}

				PoseObjectName = string.Empty;
				foreach(var curve in poseCurveBindings)
				{
					if (PoseObjectName == string.Empty)
						PoseObjectName = curve.curveBinding.path;
					else if (PoseObjectName != curve.curveBinding.path)
					{
						Debug.LogError("Different paths in translation curves");
						return;
					}
				}

				BlendshapesObjectName = string.Empty;
				foreach (var curve in blendshapesCurveBindings)
				{
					if (BlendshapesObjectName == string.Empty)
						BlendshapesObjectName = curve.curveBinding.path;
					else if (BlendshapesObjectName != curve.curveBinding.path)
					{
						Debug.LogError("Different paths in blendshapes curves");
						return;
					}
				}

				IsValidAvatarAnimation = true;
			}
			catch (Exception exc)
			{
				Debug.LogErrorFormat("Unable to read animation: {0}", exc);
				IsValidAvatarAnimation = false;
			}
		}

	}
}
