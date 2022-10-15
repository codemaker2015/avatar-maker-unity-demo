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
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ItSeez3D.AvatarMaker.WebCamera;
using ItSeez3D.AvatarMaker.MotionCapture;
using System.IO;
using System;

namespace ItSeez3D.AvatarMaker.Editor
{
	public class MotionCaptureWindow : BaseWindow
	{
		class SliderWithTextValues
		{
			public SliderWithTextValues(float value)
			{
				sliderValue = value;
				textValue = value.ToString();
			}

			public void SetValue(float value)
			{
				sliderValue = value;
				textValue = value.ToString();
			}

			public float sliderValue;
			public string textValue;
		}


		private AnimationClip recordedAnimationClip = null;
		private AnimationClip playingAnimationClip = null;
		private AnimationClipModifier animationModifier = null;
		private bool isCapturing = false;
		private string capturingButtonLabel = string.Empty;
		private string recordingButtonLabel = string.Empty;
		private string capturingErrorLabel = string.Empty;
		private string exportErrorLabel = string.Empty;
		private string avatarErrorLabel = string.Empty;
		private float animationTime = 0.0f;
		private bool isAutoPlayAnimation = true;
		private int cameraId = 0;

		private Dictionary<string, SliderWithTextValues> blendshapesImpactControls = new Dictionary<string, SliderWithTextValues>();
		private Dictionary<string, float> blendshapesImpactValues = new Dictionary<string, float>(); 
		private bool showBlendshapeCoefficients = false;
		private readonly float maxImpactValue = 3f;

		private AvatarInfo avatarInfo = null;

		private AvatarAnimator avatarAnimator = null;

		private Vector2 scrollPosition;
		private Vector2 blendshapesScrollPosition;

		private bool isEditAnimationMode = false;

		private void OnEnable()
		{
			if (!AvatarMakerInitializer.IsPlatformSupported())
			{
				Debug.LogError("Avatar plugin supports only Windows platform and works in the Editor mode.");
				return;
			}

			if (!AvatarMakerInitializer.IsInitialized)
				AvatarMakerInitializer.StartInitialization();

#if UNITY_2018_1_OR_NEWER
			EditorApplication.hierarchyChanged += OnHierarchyChanged;
#else
			EditorApplication.hierarchyWindowChanged += OnHierarchyChanged;
#endif
			OnHierarchyChanged();
		}

		void OnGUI()
		{
			InitUI();

			GUILayout.Label("Facial Motion Capture", titleStyle);
			GUILayout.Space(20);

			if (!AvatarMakerInitializer.IsPlatformSupported())
			{
				EditorGUILayout.HelpBox("Avatar Maker plugin supports only Windows platform and works in the Editor mode.", MessageType.Error);
				return;
			}

			if (EditorApplication.isPlaying)
			{
				GUILayout.Space(20);
				EditorGUILayout.HelpBox("Avatar Maker plugin doesn't work in the Play mode.", MessageType.Error);
				return;
			}

			if (AvatarMakerInitializer.IsInitializationInProgress)
			{
				EditorGUILayout.HelpBox("Loading...", MessageType.Info);
				return;
			}

			if (!AvatarMakerInitializer.IsMotionCaptureSupported)
			{
				EditorGUILayout.HelpBox("Your CPU doesn't have AVX extension required for Motion Tracking.", MessageType.Error);
				return;
			}

			if (isEditAnimationMode)
			{
				if (animationModifier != null && animationModifier.IsValidAvatarAnimation)
				{
					OnGuiEditAnimation();
					return;
				}
				else
					isEditAnimationMode = false;
			}

			if (avatarInfo == null)
			{
				if (isCapturing)
				{
					avatarAnimator.StopCapturing();
					avatarAnimator = null;
					isCapturing = false;
				}

				if (AnimationMode.InAnimationMode())
					ToggleAnimationMode();

				EditorGUILayout.HelpBox(avatarErrorLabel, MessageType.Info);
				return;
			}

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			WebCamDevice[] cameraDevices = WebCamTexture.devices;
			if (cameraDevices != null && cameraDevices.Length > 0)
			{
				if (!isCapturing)
				{
					if (cameraDevices.Length > 1)
					{
						string[] cameraNames = cameraDevices.Select(d => { return d.name; }).ToArray();
						cameraId = GUILayout.SelectionGrid(cameraId, cameraNames, 1, "toggle");
					}
					else
						cameraId = 0;
				}

				EditorGUI.BeginDisabledGroup(avatarInfo == null);
				capturingButtonLabel = isCapturing ? "Stop capturing" : "Start capturing";
				isCapturing = GUILayout.Toggle(isCapturing, capturingButtonLabel, "Button");
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUILayout.HelpBox("There is no available web camera.", MessageType.Info);
			}

			if (isCapturing)
			{
				capturingErrorLabel = string.Empty;
				if (avatarAnimator == null)
				{
					avatarAnimator = new AvatarAnimator(avatarInfo.transform, avatarInfo.headMeshRenderer, cameraOffset);
					isCapturing = avatarAnimator.StartCapturing(WebCamTexture.devices[cameraId].name, avatarInfo.code);
					if (!isCapturing)
					{
						capturingErrorLabel = "Unable to start motion capture.";
						Debug.LogError(capturingErrorLabel);
						avatarAnimator = null;
						return;
					}
					ApplyBlendshapesImpact();

					if (AvatarAnimator.RecordAtStart)
						StartRecording();

					if (AnimationMode.InAnimationMode())
						ToggleAnimationMode();
				}
				Texture2D tex = avatarAnimator.HandleCapturedFrame();
				DisplayFrameTexture(tex);
			}
			else
			{
				if (avatarAnimator != null)
				{
					avatarAnimator.StopCapturing();
					avatarAnimator = null;
				}
			}

			if (!string.IsNullOrEmpty(capturingErrorLabel))
				EditorGUILayout.HelpBox(capturingErrorLabel, MessageType.Error);
			GUILayout.Space(20);

			EditorGUILayout.BeginVertical("Box");
			{
				EditorGUILayout.LabelField("Recording options", titleStyle);
				GUILayout.Space(5);
				if (isCapturing)
				{
					recordingButtonLabel = avatarAnimator.IsRecording ? "Stop recording" : "Start recording";
					if (avatarAnimator.IsRecording != GUILayout.Toggle(avatarAnimator.IsRecording, recordingButtonLabel, "Button"))
					{
						if (avatarAnimator.IsRecording)
							StopRecording();
						else
							StartRecording();
					}
					GUILayout.Space(5);
				}

				AvatarAnimator.RecordAtStart = GUILayout.Toggle(AvatarAnimator.RecordAtStart, "Record at start");

				recordedAnimationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation file: ", recordedAnimationClip, typeof(AnimationClip), false);

				AvatarAnimator.ApplyTranslation = GUILayout.Toggle(AvatarAnimator.ApplyTranslation, "Capture translation");
				AvatarAnimator.ApplyRotation = GUILayout.Toggle(AvatarAnimator.ApplyRotation, "Capture rotation");
			}
			EditorGUILayout.EndVertical();
			GUILayout.Space(10);

			OnGuiPlayingAnimation();

			if (!string.IsNullOrEmpty(exportErrorLabel))
			{
				EditorGUILayout.HelpBox(exportErrorLabel, MessageType.Error);
				ShowAvatarMakerProLink();
			}
			GUILayout.Space(10);

			showBlendshapeCoefficients = EditorGUILayout.Foldout(showBlendshapeCoefficients, "Blendshapes Impact");
			if (showBlendshapeCoefficients)
			{
				if (blendshapesImpactControls.Count == 0)
				{
					List<string> blendshapeNames = AvatarAnimator.GetBlendshapesNames();
					for (int i = 0; i < blendshapeNames.Count; i++)
					{
						blendshapesImpactControls.Add(blendshapeNames[i], new SliderWithTextValues(1.0f));
						blendshapesImpactValues.Add(blendshapeNames[i], 1f);
					}
				}

				blendshapesScrollPosition = GUILayout.BeginScrollView(blendshapesScrollPosition, GUILayout.Height(Mathf.Max(200f, position.height - 200f)));
				var blendshapesNames = blendshapesImpactControls.Keys.ToList<string>();
				for (int i = 0; i < blendshapesNames.Count; i++)
				{
					SliderWithTextValues controlsValues = blendshapesImpactControls[blendshapesNames[i]];
					
					GUILayout.BeginHorizontal();
					GUILayout.Label(blendshapesNames[i] + ":", GUILayout.MaxWidth(100));
					float blendshapeImpactVal = GUILayout.HorizontalSlider(controlsValues.sliderValue, 0, maxImpactValue);
					if (blendshapeImpactVal != controlsValues.sliderValue)
					{
						controlsValues.SetValue(blendshapeImpactVal);
						blendshapesImpactValues[blendshapesNames[i]] = blendshapeImpactVal;
						ApplyBlendshapesImpact();
					}

					string modifiedValueStr = GUILayout.TextField(controlsValues.textValue, GUILayout.Width(100));
					if (modifiedValueStr != blendshapeImpactVal.ToString())
					{
						controlsValues.textValue = modifiedValueStr;
						if (float.TryParse(modifiedValueStr, out blendshapeImpactVal) && blendshapeImpactVal >= 0 && blendshapeImpactVal <= maxImpactValue)
						{
							controlsValues.sliderValue = blendshapeImpactVal;
							blendshapesImpactValues[blendshapesNames[i]] = blendshapeImpactVal;
							ApplyBlendshapesImpact();
						}
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.Space(10);
				GUILayout.BeginHorizontal();
				if (GUILayout.Button("Save", buttonSkin))
				{
					SaveBlendshapesImpactValues();
				}
				if (GUILayout.Button("Load", buttonSkin))
				{
					LoadBlendshapesImpactValues();
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(10);
				GUILayout.EndScrollView();
			}

			EditorGUILayout.EndScrollView();

			if (isCapturing)
				Repaint();
		}

		private void OnGuiPlayingAnimation()
		{
			if (!isCapturing)
			{
				EditorGUILayout.BeginVertical("Box");
				{
					EditorGUILayout.LabelField("Animation", titleStyle);
					GUILayout.Space(5);

					playingAnimationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation to play: ", playingAnimationClip, typeof(AnimationClip), false);

					if (playingAnimationClip != null)
					{
						EditorGUI.BeginChangeCheck();
						GUILayout.Toggle(AnimationMode.InAnimationMode(), "Play recorded animation");
						if (EditorGUI.EndChangeCheck())
							ToggleAnimationMode();

						if (AnimationMode.InAnimationMode())
						{
							isAutoPlayAnimation = GUILayout.Toggle(isAutoPlayAnimation, "Automatically play in loop");

							animationTime = EditorGUILayout.Slider(animationTime, 0.0f, playingAnimationClip.length);
						}

						GUILayout.BeginHorizontal();
						{
							if (GUILayout.Button("Export to FBX"))
							{
								if (AvatarMakerInitializer.IsProVersion)
								{
									string fbxFilePath = Export.ExportAvatarAsFbx(avatarInfo, true);
									if (!string.IsNullOrEmpty(fbxFilePath))
									{
										int result = AvatarAnimator.AddAnimationToFbx(fbxFilePath, playingAnimationClip);
										if (result != 0)
											Debug.LogErrorFormat("Unable to create FBX animation: {0}", result);
									}
								}
								else
								{
									exportErrorLabel = "FBX export is available only in the Avatar Maker Pro version!";
								}
							}

							if (GUILayout.Button("Edit"))
							{
								animationModifier = new AnimationClipModifier(playingAnimationClip);
								if (animationModifier.IsValidAvatarAnimation)
								{
									isEditAnimationMode = true;
									Repaint();
								}
								else
									EditorUtility.DisplayDialog("Error", "This animation can't be modified.", "OK");
							}
						}
						GUILayout.EndHorizontal();
					}
				}
				EditorGUILayout.EndVertical();
				GUILayout.Space(10);
			}
		}

		private Vector2 blendshapesEditorScrollPosition = Vector2.zero;
		private void OnGuiEditAnimation()
		{
			EditorGUILayout.BeginVertical();
			{
				AnimationClip selectedAnimationClip = (AnimationClip)EditorGUILayout.ObjectField("Animation: ", playingAnimationClip, typeof(AnimationClip), false);
				if (selectedAnimationClip != playingAnimationClip)
				{
					animationModifier.SetAnimationClip(selectedAnimationClip);
					if (!animationModifier.IsValidAvatarAnimation)
					{
						EditorUtility.DisplayDialog("Error", "This animation can't be modified.", "OK");
						isEditAnimationMode = false;
						Repaint();
						return;
					}
					else
						playingAnimationClip = selectedAnimationClip;
				}
				GUILayout.Space(10);

				if (animationModifier.PoseModificationsExist)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label("Translation/Rotation Object Name:", GUILayout.Width(200f));
						animationModifier.PoseObjectName = EditorGUILayout.TextField(animationModifier.PoseObjectName, GUILayout.Height(18));
						if (GUILayout.Button("Delete", buttonSkin, GUILayout.Width(80), GUILayout.Height(18)))
							animationModifier.DeletePoseModifications();
					}
					GUILayout.EndHorizontal();
				}

				if (animationModifier.BlendshapesCount > 0)
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label("Blendshapes Object Name:", GUILayout.Width(200f));
						animationModifier.BlendshapesObjectName = EditorGUILayout.TextField(animationModifier.BlendshapesObjectName, GUILayout.Height(18));
						if (GUILayout.Button("Delete", buttonSkin, GUILayout.Width(80), GUILayout.Height(18)))
							animationModifier.DeleteAllBlendshapesModifications();
					}
					GUILayout.EndHorizontal();

					GUILayout.Label("Blendshapes list:");
					blendshapesEditorScrollPosition = EditorGUILayout.BeginScrollView(blendshapesEditorScrollPosition, GUILayout.Height(350));
					{
						EditorGUILayout.BeginVertical("Box");
						{
							for (int i = 0; i < animationModifier.BlendshapesCount; i++)
							{
								GUILayout.BeginHorizontal();
								{
									string blendshapeName = animationModifier.GetBlendshapeName(i);
									string editedBlendshapeName = EditorGUILayout.TextField(blendshapeName, GUILayout.Height(18));
									if (blendshapeName != editedBlendshapeName)
										animationModifier.SetBlendshapeName(i, string.IsNullOrEmpty(editedBlendshapeName) ? blendshapeName : editedBlendshapeName);
									if (GUILayout.Button("Delete", buttonSkin, GUILayout.Width(80), GUILayout.Height(18)))
										animationModifier.DeleteBlendshapeModifications(i);
								}
								GUILayout.EndHorizontal();
							}
						}
						EditorGUILayout.EndVertical();
					}
					EditorGUILayout.EndScrollView();
				}

				GUILayout.Space(10);
				GUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					if (GUILayout.Button("Revert", buttonSkin, GUILayout.Width(100), GUILayout.Height(18)))
					{
						animationModifier.RevertChanges();
					}
					if (GUILayout.Button("Apply", buttonSkin, GUILayout.Width(100), GUILayout.Height(18)))
					{
						animationModifier.ApplyChanges();
					}
					if (GUILayout.Button("Cancel", buttonSkin, GUILayout.Width(100), GUILayout.Height(18)))
					{
						isEditAnimationMode = false;
					}
				}
				GUILayout.EndHorizontal();

			}
			EditorGUILayout.EndVertical();
		}

		float lastUpdateTime = 0;
		private void Update()
		{
			if (playingAnimationClip == null)
				return;

			if (AnimationMode.InAnimationMode())
			{
				if (avatarInfo == null)
				{
					avatarInfo = FindAvatarObject();
					if (avatarInfo == null)
					{
						ToggleAnimationMode();
						return;
					}
				}

				AnimationMode.BeginSampling();
				AnimationMode.SampleAnimationClip(avatarInfo.gameObject, playingAnimationClip, animationTime);
				AnimationMode.EndSampling();
				SceneView.RepaintAll();

				if (isAutoPlayAnimation)
				{
					animationTime += (Time.realtimeSinceStartup - lastUpdateTime);
					if (animationTime >= playingAnimationClip.length)
						animationTime = 0.0f;
					Repaint();
				}
			}
			lastUpdateTime = Time.realtimeSinceStartup;
		}


		void OnHierarchyChanged()
		{
			avatarInfo = FindAvatarObject();
		}

		private void DisplayFrameTexture(Texture2D cameraTexture)
		{
			float previewAspect = (float)cameraTexture.height / (float)cameraTexture.width;
			int newWidth = (int)Mathf.Min(position.width, Mathf.Min(cameraTexture.width, 360));
			Vector2 previewSize = new Vector2(newWidth, (previewAspect * newWidth));
			GUI.DrawTexture(new Rect(new Vector2((position.width - previewSize.x) * 0.5f, 30.0f), previewSize), cameraTexture);
			GUILayout.Space((int)Mathf.Max(20, previewSize.y + 10));
		}

		private AvatarInfo FindAvatarObject()
		{
			avatarErrorLabel = string.Empty;

			List<UnityEngine.Object> avatarInfoObjects = GameObject.FindObjectsOfType(typeof(AvatarInfo)).ToList();
			if (avatarInfoObjects.Count == 0)
			{
				avatarErrorLabel = "There is no avatar on the scene to animate!";
				return null;
			}
			if (avatarInfoObjects.Count > 1)
			{
				avatarErrorLabel = "There are multiple avatars on the scene! Motion capture works only for a single avatar.";
				return null;
			}

			return avatarInfoObjects[0] as AvatarInfo;
		}

		private void StartRecording()
		{
			if (!avatarAnimator.IsRecordingEnabled)
			{
				exportErrorLabel = "You are allowed to record animation only in the Avatar Maker Pro version!";
				return;
			}

			if (recordedAnimationClip == null)
				recordedAnimationClip = CreateAnimationFile();
			avatarAnimator.StartRecording(recordedAnimationClip);
		}

		private void StopRecording()
		{
			avatarAnimator.FinishRecording();
			playingAnimationClip = recordedAnimationClip;
		}

		private AnimationClip CreateAnimationFile()
		{
			string animationsFolder = "itseez3d_animations";
			string animationsFolderWithAssets = string.Format("Assets/{0}", animationsFolder);
			if (!AssetDatabase.IsValidFolder(animationsFolderWithAssets))
				AssetDatabase.CreateFolder("Assets", animationsFolder);

			int idx = 0;
			string animationName = string.Empty;
			while(true)
			{
				animationName = string.Format("avatar_animation_{0}", idx);
				if (AssetDatabase.FindAssets(animationName).Length == 0)
					break;
				idx++;
			}

			AnimationClip animation = new AnimationClip();
			string animationFileName = string.Format("{0}/{1}.anim", animationsFolderWithAssets, animationName);
			AssetDatabase.CreateAsset(animation, animationFileName);

			return animation;
		}

		private void ToggleAnimationMode()
		{
			if (AnimationMode.InAnimationMode())
				AnimationMode.StopAnimationMode();
			else
				AnimationMode.StartAnimationMode();
		}

		private void ApplyBlendshapesImpact()
		{
			if (avatarAnimator != null)
				avatarAnimator.SetBlendshapesImpact(blendshapesImpactValues);
		}

		private void SaveBlendshapesImpactValues()
		{
			string filePath = EditorUtility.SaveFilePanel("Blendshapes Impact Values", "", "blendshapes_impact", "txt");
			if (!string.IsNullOrEmpty(filePath))
			{
				List<string> lines = new List<string>();
				foreach (var blendshapesValue in blendshapesImpactValues)
					lines.Add(string.Format("{0}={1}", blendshapesValue.Key, blendshapesValue.Value));
				File.WriteAllLines(filePath, lines.ToArray());
			}
		}

		private void LoadBlendshapesImpactValues()
		{
			string filePath = EditorUtility.OpenFilePanel("Blendshapes Impact Values", "", "txt");
			if (!string.IsNullOrEmpty(filePath))
			{
				string[] lines = File.ReadAllLines(filePath);
				foreach (string line in lines)
				{
					try
					{
						string[] splits = line.Split('=');
						if (splits.Length == 2)
						{
							string name = splits[0];
							float value = float.Parse(splits[1]);
							blendshapesImpactValues[name] = value;
							blendshapesImpactControls[name].SetValue(value);
						}
					}
					catch (Exception exc)
					{
						Debug.LogErrorFormat("Unable parse line {0}: {1}", line, exc);
					}
				}
			}
		}
	}
}
