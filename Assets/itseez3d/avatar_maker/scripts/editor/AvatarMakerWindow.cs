/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using Coroutines;
using ItSeez3D.AvatarSdk.Offline;
using ItSeez3D.AvatarSdk.Core;
using ItSeez3D.AvatarMaker.WebCamera;

namespace ItSeez3D.AvatarMaker.Editor
{
	public class AvatarMakerWindow : BaseWindow
	{
		#region Settings
		private const string BALD_HAIRCUT_NAME = "bald";
		private const string HEAD_OBJECT_NAME = "ItSeez3D Head";
		private const string HAIRCUT_OBJECT_NAME = "ItSeez3D Haircut";
		private const string AVATAR_OBJECT_NAME = "ItSeez3D Avatar";

		#endregion

		#region Internal classes
		/// <summary>
		/// Avatar states for a simple "state machine" implemented within GallerySample class.
		/// </summary>
		protected enum GalleryAvatarState
		{
			UNKNOWN,
			GENERATING,
			COMPLETED,
			FAILED
		}

		/// <summary>
		/// internal class stored avatar code, name and state
		/// </summary>
		protected class GalleryAvatar
		{
			public string name;
			public string code;
			public GalleryAvatarState state;
		}

		[Serializable]
		public class AvatarColor
		{
			public int blue;

			public int green;

			public int red;
		}

		#endregion

		#region Init Parameters

		private PipelineType pipelineType = PipelineType.FACE;

		private IAvatarProvider avatarProvider;

		#endregion

		#region WebCam

		private bool isCameraCapturing = false;

		private WebCameraController webCameraController = new WebCameraController();

		private WebCamDevice selectedWebCam;

		private int selectedWebCamIndex = 0;

		private string webcamWidthString;

		private string webcamHeightString;

		Texture2D photoPreview;

		float cameraPreviewYOffset = 50f;

		#endregion

		#region Data

		private AvatarInfo selectedAvatarInfo;

		private string selectedAvatarName;

		private List<GalleryAvatar> loadedAvatars = new List<GalleryAvatar>();

		private Vector2 scrollWindow, scrollPosBlendshapes, scrollHaircuts, scrollWebCams;

		private bool needGUIRepaint;

		private bool exportWithBlendshapes = true;

		private bool showExportError = false;

		private bool showLODError = false;

		private string progressText;
		protected string ProgressText
		{
			get
			{
				return progressText;
			}

			set
			{
				needGUIRepaint = true;
				progressText = value;
			}
		}

		private bool showHaircutsFoldout = false;
		private bool showBlendshapesFoldout = false;
		private bool showAvatarsFoldout = true;

		private string[] availableLODs = 
		{
			"LOD0 (13043 vertices; 24479 faces)",
			"LOD1 (9898  vertices; 18674 faces)",
			"LOD2 (8122 vertices; 15122 faces)",
			"LOD3 (6107 vertices; 11128 faces)",
			"LOD4 (5363 vertices; 9772 faces)",
			"LOD5 (3859 vertices; 6844 faces)",
			"LOD6 (3062 vertices; 5430 faces)",
			"LOD7 (2342 vertices; 3996 faces)",
			"LOD8 (1425 vertices; 2680 faces)",
		};

		#endregion

		#region Unity Events
		private void Awake()
		{
			EditorApplication.update += Update;
		}

		private void OnEnable()
		{
			if (!AvatarMakerInitializer.IsPlatformSupported())
			{
				Debug.LogError("Avatar plugin supports only Windows platform and works in the Editor mode.");
				return;
			}

			if (!AvatarMakerInitializer.IsInitialized)
			{ 
				AvatarMakerInitializer.StartInitialization();
			}

			avatarProvider = AvatarMakerInitializer.AvatarProvider;

			UpdateAvatarList();
		}

		private void OnDestroy()
		{
			if (webCameraController.IsCapturing)
				webCameraController.StopCapturing();

			EditorApplication.update -= Update;
		}

		private void Update()
		{
			if (needGUIRepaint)
				Repaint();
		}
		#endregion

		#region Window GUI Methods
		/// <summary>
		/// Draw editor window interface.
		/// </summary>
		void OnGUI()
		{
			InitUI();

			if (!AvatarMakerInitializer.IsPlatformSupported())
			{
				GUILayout.Space(20);
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
				GUILayout.Space(20);
				EditorGUILayout.HelpBox("Loading...", MessageType.Info);
				return;
			}

			GUILayout.BeginVertical();
			{
				scrollWindow = GUILayout.BeginScrollView(scrollWindow);
				{
					OnGUIAvatarGenerationSection();

					GUILayout.Space(20);

					OnGUISelectedAvatarSection();

					GUILayout.Space(20);

					OnGUIAvatarsListSection();

				}
				GUILayout.EndScrollView();
			}
			GUILayout.EndVertical();

			if (!string.IsNullOrEmpty(ProgressText))
				GUILayout.TextArea("Progress: " + ProgressText, GUILayout.MinHeight(40), GUILayout.MaxHeight(40));
			else
				GUILayout.Space(40);
		}

		private void OnGUIAvatarGenerationSection()
		{
			GUILayout.Label("Generate Avatar: ", titleStyle);

			if (!AvatarMakerInitializer.IsProVersion)
			{
				int countAvailableAvatars = AvatarMakerPlugin.CountAvailableAvatars();
				if (countAvailableAvatars > 0)
					EditorGUILayout.HelpBox(string.Format("You have {0} avatar(s) to generate in the Free-version.", countAvailableAvatars), MessageType.Info);
				else
					EditorGUILayout.HelpBox("You exceeded the number of generated avatars available in the Free-version!", MessageType.Error);
				cameraPreviewYOffset = 95f;
			}

			GUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Select Photo"))
				{
					string path = EditorUtility.OpenFilePanel("Select image", "", "jpg,png");
					if (path.Length != 0)
					{
						var fileContent = File.ReadAllBytes(path);
						Debug.Log(fileContent.Length + " bytes file selected");
						EditorRunner.instance.Run(GenerateAndDisplayHeadRoutine(fileContent, pipelineType));
					}
				}
				isCameraCapturing = GUILayout.Toggle(isCameraCapturing, "Web Camera", "Button");
			}
			GUILayout.EndHorizontal();

			if (isCameraCapturing)
			{
				OnGUICameraCapturing();
			}
			else
			{
				if (webCameraController.IsCapturing)
					webCameraController.StopCapturing();
			}
		}

		private void OnGUISelectedAvatarSection()
		{
			if (Selection.activeGameObject != null && Selection.activeGameObject.scene.name != null)
			{
				var selectedAvatarInfoPrev = selectedAvatarInfo;

				selectedAvatarInfo = Selection.activeGameObject.GetComponent<AvatarInfo>();

				if (selectedAvatarInfo != null)
				{
					GUILayout.Label("Selected Avatar: ", titleStyle);

					if (selectedAvatarInfoPrev != selectedAvatarInfo)
					{
						selectedAvatarName = selectedAvatarInfo.name;
					}

					GUILayout.BeginHorizontal();
					{
						selectedAvatarName = EditorGUILayout.TextField(selectedAvatarName, GUILayout.Height(18));
						if (!string.IsNullOrEmpty(selectedAvatarName) && selectedAvatarName != selectedAvatarInfo.name)
						{
							if (GUILayout.Button("Rename", buttonSkin, GUILayout.Width(60), GUILayout.Height(18)))
							{
								selectedAvatarInfo.name = selectedAvatarName;
								WriteAvatarNameByCode(selectedAvatarInfo.code, selectedAvatarName);
								UpdateAvatarList();
							}
						}
						else
						{
							GUILayout.Button("Rename", buttonSkin, GUILayout.Width(60), GUILayout.Height(18));
						}
					}
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					{
						GUILayout.Label("Level Of Details:", GUILayout.Width(100f));
						int selectedLOD = EditorGUILayout.Popup(selectedAvatarInfo.levelOfDetails, availableLODs, GUILayout.Width(250f));
						if (selectedLOD != selectedAvatarInfo.levelOfDetails)
						{
							if (AvatarMakerInitializer.IsProVersion)
							{
								selectedAvatarInfo.levelOfDetails = selectedLOD;
								EditorRunner.instance.Run(UpdateAvatarMesh(selectedAvatarInfo));
							}
							else
							{
								selectedLOD = 0;
								showLODError = true;
							}
						}
					}
					GUILayout.EndHorizontal();

					showHaircutsFoldout = EditorGUILayout.Foldout(showHaircutsFoldout, "Hairstyles");
					if (showHaircutsFoldout)
					{
						EditorGUILayout.BeginVertical("Box");
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Label("Haircut Color:", GUILayout.MaxWidth(100f));
								selectedAvatarInfo.HaircutColor = EditorGUILayout.ColorField(selectedAvatarInfo.HaircutColor);
								if (GUILayout.Button("Default", buttonSkin, GUILayout.Width(100), GUILayout.Height(16)))
									selectedAvatarInfo.HaircutColor = selectedAvatarInfo.avgHaircutColor;
							}
							GUILayout.EndHorizontal();

							scrollHaircuts = GUILayout.BeginScrollView(scrollHaircuts, GUILayout.Height(500));
							{
								bool selected = (selectedAvatarInfo.selectedHairstyle == -1);

								if (GUILayout.Toggle(selected, "None") != selected)
								{
									selectedAvatarInfo.selectedHairstyle = -1;
									EditorRunner.instance.Run(ChangeHairRoutine(selectedAvatarInfo, -1));
								}

								for (int i = 0; i < selectedAvatarInfo.haircuts.Length; i++)
								{
									var haircut = selectedAvatarInfo.haircuts[i];
									selected = (selectedAvatarInfo.selectedHairstyle == i);
									if (GUILayout.Toggle(selected, haircut) != selected)
									{
										selectedAvatarInfo.selectedHairstyle = i;
										EditorRunner.instance.Run(ChangeHairRoutine(selectedAvatarInfo, i));
									}
								}
							}
							GUILayout.EndScrollView();
						}
						EditorGUILayout.EndVertical();
					}

					showBlendshapesFoldout = EditorGUILayout.Foldout(showBlendshapesFoldout, "Blendshapes");
					if (showBlendshapesFoldout)
					{
						EditorGUILayout.BeginVertical("Box");
						{
							scrollPosBlendshapes = GUILayout.BeginScrollView(scrollPosBlendshapes, GUILayout.Height(500));
							{
								var blendShapeCount = selectedAvatarInfo.headMeshRenderer.sharedMesh.blendShapeCount;
								for (int i = 1; i < blendShapeCount; i++)
								{
									GUILayout.BeginHorizontal();
									{
										GUILayout.Label(selectedAvatarInfo.headMeshRenderer.sharedMesh.GetBlendShapeName(i) + ":", GUILayout.MaxWidth(100));
										var blendWeight = GUILayout.HorizontalSlider(selectedAvatarInfo.headMeshRenderer.GetBlendShapeWeight(i), 0, 100);
										selectedAvatarInfo.headMeshRenderer.SetBlendShapeWeight(i, blendWeight);
									}
									GUILayout.EndHorizontal();
								}
							}
							GUILayout.EndScrollView();
						}
						EditorGUILayout.EndVertical();
					}

					EditorGUILayout.BeginVertical("Box");
					{
						GUILayout.Label("Export to:", titleLeftStyle);
						exportWithBlendshapes = GUILayout.Toggle(exportWithBlendshapes, "Include blendshapes (for Prefab and FBX)");

						GUILayout.BeginHorizontal();
						{
							if (GUILayout.Button("Prefab"))
							{
								if (AvatarMakerInitializer.IsProVersion)
								{
									AvatarPrefabParameters prefabParameters = new AvatarPrefabParameters(selectedAvatarInfo.code, HEAD_OBJECT_NAME)
									{
										haircutObjectName = HAIRCUT_OBJECT_NAME,
										haircutId = selectedAvatarInfo.SelectedHairstyleName,
										mergeHeadAndTextureMeshes = true,
										withBlendshapes = exportWithBlendshapes,
										levelOfDetails = selectedAvatarInfo.levelOfDetails
									};
									AvatarPrefabBuilder.Instance.CreateAvatarPrefab(selectedAvatarInfo.gameObject, prefabParameters);
								}
								else
								{
									Debug.LogError("Unable to create prefab in the Free-version!");
									showExportError = true;
								}
							}

							if (GUILayout.Button("FBX"))
							{
								if (AvatarMakerInitializer.IsProVersion)
								{
									Export.ExportAvatarAsFbx(selectedAvatarInfo, exportWithBlendshapes);
								}
								else
								{
									Debug.LogError("Export to FBX is inaccessible in the Free-version!");
									showExportError = true;
								}
							}

							if (GUILayout.Button("OBJ"))
							{
								if (AvatarMakerInitializer.IsProVersion)
									Export.ExportAvatarAsObj(selectedAvatarInfo);
								else
								{
									Debug.LogError("Export to OBJ is inaccessible in the Free-version!");
									showExportError = true;
								}
							}
						}
						GUILayout.EndHorizontal();
					}
					EditorGUILayout.EndVertical();

					if (showExportError || showLODError)
					{
						if (showExportError)
							EditorGUILayout.HelpBox("Export feature is available only in the Avatar Maker Pro version!", MessageType.Error);
						if (showLODError)
							EditorGUILayout.HelpBox("Level Of Details is available only in the Avatar Maker Pro version", MessageType.Error);
						ShowAvatarMakerProLink();
					}
				}
			}
			else if (selectedAvatarInfo != null)
			{
				selectedAvatarInfo = null;
			}
		}

		private void OnGUIAvatarsListSection()
		{
			if (loadedAvatars.Count > 0)
			{
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				showAvatarsFoldout = EditorGUILayout.Foldout(showAvatarsFoldout, "Avatars:", avatarsFoldoutStyle);
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				if (showAvatarsFoldout)
				{
					foreach (var avatar in loadedAvatars)
					{
						if (avatar != null)
						{
							string avatarName = avatar.name;
							GUIStyle buttonStyle = (GUI.skin.button);
							if (avatar.state != GalleryAvatarState.COMPLETED)
							{
								avatarName = "Failed " + avatarName;
								buttonStyle = redButtonStyle;
							}
							else if (selectedAvatarInfo != null && selectedAvatarInfo.code == avatar.code)
							{
								buttonStyle = greenBoldButtonStyle;
							}

							GUILayout.BeginHorizontal();

							if (GUILayout.Button(avatarName, buttonStyle, GUILayout.Width(position.width - 150)))
								EditorRunner.instance.Run(ShowAvatarByCode(avatar.code));
							
							if (GUILayout.Button("Delete"))
								OnDeleteAvatarButton(avatar);
							

							GUILayout.EndHorizontal();
						}

					}
					if (loadedAvatars.Count >= 2 && GUILayout.Button("Delete All"))
					{
						if (EditorUtility.DisplayDialog("Delete all saved avatars?", "Are you sure you want to delete all saved avatars? ", "Yes", "No"))
						{
							foreach (var avatar in loadedAvatars)
							{
								EditorRunner.instance.Run(DeleteAvatarRoutine(avatar));
							}
						}
					}
				}
			}
		}

		private void OnGUICameraCapturing()
		{
			WebCamDevice[] devices = webCameraController.Devices;

			if (devices.Length > 0)
			{
				scrollWebCams = GUILayout.BeginScrollView(scrollWebCams, GUILayout.Width(150), GUILayout.Height(200));
				{
					for (int i = 0; i < devices.Length; i++)
					{
						WebCamDevice device = devices[i];
						if (GUILayout.Button(device.name))
						{
							selectedWebCamIndex = i;
							StartCapturing(devices[i]);
						}
					}
				}
				GUILayout.EndScrollView();

				if (!webCameraController.IsCapturing)
				{
					if (selectedWebCamIndex < devices.Length)
						StartCapturing(devices[selectedWebCamIndex]);
				}
				else
				{
					var texture = webCameraController.Texture;
					if (photoPreview != null)
					{
						OnGUIPhotoPreview(photoPreview);

						GUILayout.BeginHorizontal();
						{
							if (GUILayout.Button("Reset"))
							{
								DestroyImmediate(photoPreview);
								photoPreview = null;
							}
							if (GUILayout.Button("Generate avatar!"))
							{
								byte[] bytes = photoPreview.EncodeToPNG();
								EditorRunner.instance.Run(GenerateAndDisplayHeadRoutine(bytes, pipelineType, asyncResult => { DestroyImmediate(photoPreview); photoPreview = null; }));
								isCameraCapturing = false;
							}
						}
						GUILayout.EndHorizontal();
					}
					else
					{
						OnGUIPhotoPreview(texture);

						GUILayout.BeginHorizontal();
						{
							GUILayout.Label("WebCam: " + selectedWebCam.name, GUILayout.Width(180));

							GUILayout.BeginHorizontal();
							{
								GUILayout.FlexibleSpace();

								GUILayout.Label("Resolution: ", GUILayout.Width(80));

								webcamWidthString = GUILayout.TextField(webcamWidthString, "Label", GUILayout.Width(40));
								webcamHeightString = GUILayout.TextField(webcamHeightString, "Label", GUILayout.Width(40));

								int parsedWidth = 640;
								int parsedHeight = 480;

								GUILayout.FlexibleSpace();

								if (int.TryParse(webcamWidthString, out parsedWidth))
								{
									//webcamWidth = parsedWidth;
								}
								else
								{
									webcamWidthString = texture.width.ToString();
								}

								if (int.TryParse(webcamHeightString, out parsedHeight))
								{
									//webcamHeight = parsedHeight;
								}
								else
								{
									webcamHeightString = texture.height.ToString();
								}

								if (parsedWidth != texture.width || parsedHeight != texture.height)
								{
									if (GUILayout.Button("Apply", GUILayout.Width(60)))
									{
										webCameraController.StartCapturing(selectedWebCam.name, parsedWidth, parsedHeight);
										webcamWidthString = webcamHeightString = "";
									}
								}
								else
								{
									GUILayout.Space(60);
								}
							}
							GUILayout.EndHorizontal();
						}
						GUILayout.EndHorizontal();

						if (GUILayout.Button("Capture Photo"))
							OnCapturePhotoButton();

						Repaint();
					}
				}
			}
			else
				GUILayout.Label("No webcam available");
		}

		/// <summary>
		/// Display texture preview in this EditorWindow
		/// </summary>
		private void OnGUIPhotoPreview(Texture texture)
		{
			float previewAspect = (float)texture.height / (float)texture.width;

			float previewLeftOffset = 150f;

			int newWidth = (int)Mathf.Min(position.width - previewLeftOffset, texture.width) - 8;

			Vector2 previewSize = new Vector2(-newWidth, (previewAspect * newWidth));

			GUI.DrawTexture(new Rect(new Vector2((previewLeftOffset + position.width - previewSize.x) * 0.5f, cameraPreviewYOffset), previewSize), texture);

			GUILayout.Space(Math.Max(0, previewSize.y - 190));
		}

		private void OnDeleteAvatarButton(GalleryAvatar avatar)
		{
			if (EditorUtility.DisplayDialog("Delete avatar?", "Are you sure you want to delete " + avatar.code + "?", "Yes", "No"))
				EditorRunner.instance.Run(DeleteAvatarRoutine(avatar));
		}

		private void OnCapturePhotoButton()
		{
			photoPreview = webCameraController.CapturePhoto();
			if (photoPreview != null)
			{
				byte[] photoBytes = photoPreview.EncodeToPNG();
				string filePath = Path.Combine(Application.persistentDataPath, "Photo.png");
				Debug.Log("SAVE TO  " + filePath);
				File.WriteAllBytes(filePath, photoBytes);
				Debug.Log(photoBytes.Length + " bytes saved");
			}
			else
			{
				Debug.LogError("Unable to capture photo!");
			}
		}
		#endregion

		#region Avatar Methods

		/// <summary>
		/// Updates avatar list and repaints GUI
		/// </summary>
		public void UpdateAvatarList()
		{
			var routine = UpdateAvatarListRoutine(b =>
			{
				Repaint();
			});
			EditorRunner.instance.Run(routine);
		}

		protected AsyncRequest<GalleryAvatar[]> GetAllAvatarsAsync(int maxItems)
		{
			var request = new AsyncRequest<GalleryAvatar[]>(AvatarSdkMgr.Str(Strings.GettingAvatarState));
			EditorRunner.instance.Run(GetAllAvatarsRoutine(maxItems, request));
			return request;
		}

		private IEnumerator GetAllAvatarsRoutine(int maxItems, AsyncRequest<GalleryAvatar[]> request)
		{
			var avatarsRequest = avatarProvider.GetAllAvatarsAsync(maxItems);
			yield return AwaitRoutine(avatarsRequest, null);
			if (avatarsRequest.IsError)
				yield break;

			GalleryAvatar[] avatars = new GalleryAvatar[avatarsRequest.Result.Length];
			for (int i = 0; i < avatars.Length; i++)
			{
				string avatarCode = avatarsRequest.Result[i];
				string avatarName = ReadAvatarNameByCode(avatarCode);
				avatars[i] = new GalleryAvatar() { name = avatarName, code = avatarCode, state = GetAvatarState(avatarCode) };
			}

			request.Result = avatars;
			request.IsDone = true;
		}

		/// <summary>
		/// Destroy the existing avatar in the scene. Disable the buttons.
		/// Wait until coroutine finishes and then enable buttons again.
		/// </summary>
		protected virtual IEnumerator GenerateAvatarRoutine(byte[] photoBytes)
		{
			var avatarObject = GameObject.Find(AVATAR_OBJECT_NAME);
			Destroy(avatarObject);
			yield return EditorRunner.instance.Run(GenerateAndDisplayHeadRoutine(photoBytes, pipelineType));
		}

		/// <summary>
		/// To make Getting Started sample as simple as possible all code required for creating and
		/// displaying an avatar is placed here in a single function. This function is also a good example of how to
		/// chain asynchronous requests, just like in traditional sequential code.
		/// </summary>
		protected virtual IEnumerator GenerateAndDisplayHeadRoutine(byte[] photoBytes, PipelineType pipeline, Action<AsyncRequest> callback = null)
		{
			if (avatarProvider == null)
			{
				Debug.LogError("avatarProvider null");
				yield break;
			}

			// Choose all available parameters
			var parametersRequest = avatarProvider.GetParametersAsync(ComputationParametersSubset.ALL, pipeline);
			yield return AwaitRoutine(parametersRequest);

			ComputationParameters allParameters = parametersRequest.Result;
			ComputationParameters parameters = ComputationParameters.Empty;
			parameters.haircuts = allParameters.haircuts;
			parameters.blendshapes = allParameters.blendshapes;
			parameters.modelInfo = allParameters.modelInfo;
			parameters.modelInfo.hairColor.Value = true;
			parameters.modelInfo.predictHaircut.Value = true;

			// generate avatar from the photo and get its code in the Result of request
			var initializeRequest = avatarProvider.InitializeAvatarAsync(photoBytes, "name", "description", pipeline, parameters);
			yield return AwaitRoutine(initializeRequest);

			var avatarCode = initializeRequest.Result;

			var calculateRequest = avatarProvider.StartAndAwaitAvatarCalculationAsync(avatarCode);
			yield return AwaitRoutine(calculateRequest);

			string avatarName = "Avatar " + System.DateTime.Now.ToString("dd/MM/yy HH:mm:ss");

			WriteAvatarNameByCode(avatarCode, avatarName);

			if (callback != null)
			{
				callback.Invoke(calculateRequest);
			}

			UpdateAvatarList();

			if (!calculateRequest.IsError)
				yield return ShowAvatarByCode(avatarCode);
			else
				Debug.LogErrorFormat("Unable to calculate avatar: {0}", avatarName);
		}

		public string ReadAvatarNameByCode(string sCode)
		{
			string avatarName = sCode;
			try
			{
				avatarName = File.ReadAllText(Path.Combine(AvatarSdkMgr.Storage().GetAvatarDirectory(sCode), "avatarName.txt"));
			}
			catch { }
			return avatarName;
		}

		public void WriteAvatarNameByCode(string avatarCode, string avatarName)
		{
			File.WriteAllText(Path.Combine(AvatarSdkMgr.Storage().GetAvatarDirectory(avatarCode), "avatarName.txt"), avatarName);
		}

		private IEnumerator ShowAvatarByCode(string avatarCode)
		{
			// with known avatar code we can get TexturedMesh for head in order to show it further
			var avatarHeadRequest = avatarProvider.GetHeadMeshAsync(avatarCode, true);
			yield return AwaitRoutine(avatarHeadRequest);
			TexturedMesh headTexturedMesh = avatarHeadRequest.Result;

			TexturedMesh haircutTexturedMesh = null;
			// get identities of all haircuts available for the generated avatar
			var haircutsIdRequest = avatarProvider.GetHaircutsIdAsync(avatarCode);
			yield return AwaitRoutine(haircutsIdRequest);

			ModelInfo modelInfo = CoreTools.GetAvatarModelInfo(avatarCode);

			// select predicted haircut
			var haircuts = haircutsIdRequest.Result.ToList();
			var haircutIdx = 0;
			if (haircuts != null && haircuts.Count > 0 && !string.IsNullOrEmpty(modelInfo.haircut_name))
			{
				haircutIdx = haircuts.FindIndex(h => h.Contains(modelInfo.haircut_name));

				if (haircutIdx >= 0)
				{
					// load TexturedMesh for the chosen haircut 
					var haircutRequest = avatarProvider.GetHaircutMeshAsync(avatarCode, haircuts[haircutIdx]);
					yield return AwaitRoutine(haircutRequest);
					haircutTexturedMesh = haircutRequest.Result;
				}
			}

			var avatarInfo = CreateHead(avatarCode, headTexturedMesh, haircutTexturedMesh, modelInfo);
			avatarInfo.code = avatarCode;
			avatarInfo.name = ReadAvatarNameByCode(avatarCode);
			avatarInfo.haircuts = haircutsIdRequest.Result;
			avatarInfo.selectedHairstyle = haircutIdx;
			avatarInfo.transform.position = Vector3.zero;
			avatarInfo.transform.localRotation = Quaternion.identity;

			Selection.activeGameObject = avatarInfo.gameObject;

			SceneView sceneView = SceneView.lastActiveSceneView;
			if (sceneView == null)
			{
				Type GameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
				sceneView = EditorWindow.GetWindow<SceneView>(new Type[] { GameViewType });
			}

			if (sceneView != null)
			{
				Camera sceneCam = sceneView.camera;
				sceneView.pivot = Vector3.zero;
				sceneView.size = cameraOffset;
				sceneView.LookAt(Vector3.zero, Quaternion.identity);
			}

			Tools.current = Tool.Rotate;
		}

		/// <summary>
		/// Displays head mesh and harcut on the scene
		/// </summary>
		private AvatarInfo CreateHead(string avatarCode, TexturedMesh headMesh, TexturedMesh haircutMesh, ModelInfo modelInfo)
		{
			// create parent avatar object in a scene, attach a script to it to allow rotation by mouse
			var avatarObject = new GameObject(AVATAR_OBJECT_NAME);

			// create head object in the scene
			Debug.LogFormat("Generating Unity mesh object for head...");
			var headObject = new GameObject(HEAD_OBJECT_NAME);
			var headMeshRenderer = headObject.AddComponent<SkinnedMeshRenderer>();
			headMeshRenderer.sharedMesh = headMesh.mesh;
			var headMaterial = new Material(Shader.Find("AvatarUnlitShader"))
			{
				mainTexture = headMesh.texture
			};
			headMeshRenderer.material = headMaterial;
			headObject.transform.SetParent(avatarObject.transform);

			MeshRenderer haircutMeshRenderer = null;

			if (haircutMesh != null)
				haircutMeshRenderer = CreateHaircutObject(haircutMesh, avatarObject);

			var avatarInfo = avatarObject.AddComponent<AvatarInfo>();
			avatarInfo.headMeshRenderer = headMeshRenderer;
			avatarInfo.hairMeshRenderer = haircutMeshRenderer;
			avatarInfo.predictedHaircutColor = modelInfo.hair_color.ToUnityColor();
			if (haircutMesh != null)
			{
				avatarInfo.avgHaircutColor = CoreTools.CalculateAverageColor(haircutMeshRenderer.sharedMaterial.mainTexture as Texture2D);
				avatarInfo.HaircutColor = avatarInfo.predictedHaircutColor;
			}

            return avatarInfo;
		}

		private IEnumerator UpdateAvatarMesh(AvatarInfo avatarInfo)
		{
			var meshRequest = avatarProvider.GetHeadMeshAsync(avatarInfo.code, true, avatarInfo.levelOfDetails);
			yield return meshRequest;

			avatarInfo.headMeshRenderer.sharedMesh = meshRequest.Result.mesh;
		}

		/// <summary>
		/// Determinates state of the avatar. It simply checks existence of the mesh and texture files. 
		/// </summary>
		private GalleryAvatarState GetAvatarState(string avatarCode)
		{
			OfflineAvatarProvider offlineAvatarProvider = avatarProvider as OfflineAvatarProvider;

			GalleryAvatarState avatarState = GalleryAvatarState.UNKNOWN;

			var session = offlineAvatarProvider.Session;

			if (session.IsAvatarCalculating(avatarCode))
				avatarState = GalleryAvatarState.GENERATING;
			else
			{
				string meshFilePath = AvatarSdkMgr.Storage().GetAvatarFilename(avatarCode, AvatarFile.MESH_PLY);
				string textureFilePath = AvatarSdkMgr.Storage().GetAvatarFilename(avatarCode, AvatarFile.TEXTURE);
				if (File.Exists(meshFilePath) && File.Exists(textureFilePath))
					avatarState = GalleryAvatarState.COMPLETED;
				else
					avatarState = GalleryAvatarState.FAILED;
			}

			return avatarState;
		}

		/// <summary>
		/// Delete local avatar files and request server to delete all data permanently. Can't undo this.
		/// </summary>
		private IEnumerator DeleteAvatarRoutine(GalleryAvatar avatar)
		{
			GameObject sceneAvatar = GameObject.Find(avatar.name);
			if (sceneAvatar != null && sceneAvatar.GetComponent<AvatarInfo>() != null)
				DestroyImmediate(sceneAvatar);

			var deleteRequest = avatarProvider.DeleteAvatarAsync(avatar.code);
			yield return deleteRequest;
			yield return UpdateAvatarListRoutine();
		}

		/// <summary>
		/// Detects created avatars and displays them in the gallery.
		/// </summary>
		protected IEnumerator UpdateAvatarListRoutine(Action<bool> onComplete = null)
		{
			Debug.LogFormat("Updating avatar list...");

			// For this sample we basically get all avatars created by the current player (but no more than a 1000,
			// just in case). Then pagination is done locally.
			// This should be all right for almost all practical situations. However if this is not suitable for your app
			// you can implement custom pagination logic using the low-level Connection API.
			const int maxAvatars = 1000;
			var avatarsRequest = GetAllAvatarsAsync(maxAvatars);
			yield return AwaitRoutine(avatarsRequest, null);
			if (avatarsRequest.IsError)
				yield break;

			loadedAvatars = new List<GalleryAvatar>(avatarsRequest.Result);

			if (onComplete != null)
				onComplete.Invoke(true);
		}

		private IEnumerator ChangeHairRoutine(AvatarInfo avatarInfo, int n)
		{
			var haircutIdx = n;
            
			if (n < 0)
			{
				Debug.Log("Haricut None selected");
				var hairObject = avatarInfo.transform.Find(HAIRCUT_OBJECT_NAME);
				if (hairObject != null)
				{
					//DestroyImmediate(hairObject.gameObject);
					var haircutMeshFilter = hairObject.GetComponent<MeshFilter>();
					haircutMeshFilter.sharedMesh = null;
					yield break;
				}
			}
			else
			{
				if (n >= avatarInfo.haircuts.Length)
				{
					Debug.LogWarning(avatarInfo + " does not contain haircut #" + n);
					yield break;
				}
				var haircut = avatarInfo.haircuts[haircutIdx];
				// load TexturedMesh for the chosen haircut 
				var haircutRequest = avatarProvider.GetHaircutMeshAsync(avatarInfo.code, haircut);
				yield return AwaitRoutine(haircutRequest);

				// create haircut object in the scene
				MeshRenderer haircutMeshRenderer = null;
				var haircutObject = avatarInfo.transform.Find(HAIRCUT_OBJECT_NAME);
				if (haircutObject == null)
					haircutMeshRenderer = CreateHaircutObject(haircutRequest.Result, avatarInfo.gameObject);
				else
				{
					var meshFilter = haircutObject.GetComponent<MeshFilter>();
					haircutMeshRenderer = haircutObject.GetComponent<MeshRenderer>();
					meshFilter.sharedMesh = haircutRequest.Result.mesh;
					var haircutMaterial = new Material(Shader.Find("AvatarUnlitHairShader"))
					{
						mainTexture = haircutRequest.Result.texture
					};
					haircutMeshRenderer.material = haircutMaterial;
				}

				avatarInfo.hairMeshRenderer = haircutMeshRenderer;
				avatarInfo.avgHaircutColor = CoreTools.CalculateAverageColor(haircutRequest.Result.texture);
				avatarInfo.HaircutColor = avatarInfo.predictedHaircutColor;
			}

			avatarInfo.selectedHairstyle = n;
		}

		private MeshRenderer CreateHaircutObject(TexturedMesh haircutMesh, GameObject avatarObject)
		{
			// create haircut object in the scene
			var haircutObject = new GameObject(HAIRCUT_OBJECT_NAME);
			MeshFilter mf = haircutObject.AddComponent<MeshFilter>();
			MeshRenderer haircutMeshRenderer = haircutObject.AddComponent<MeshRenderer>();
			mf.sharedMesh = haircutMesh.mesh;
			var haircutMaterial = new Material(Shader.Find("AvatarUnlitHairShader"))
			{
				mainTexture = haircutMesh.texture
			};
			haircutMeshRenderer.material = haircutMaterial;
			haircutObject.transform.SetParent(avatarObject.transform);
			return haircutMeshRenderer;
		}

		#endregion

		#region WebCam Methods
		private void StartCapturing(WebCamDevice webCam, int requestedWidth = 1920, int requestedHeight = 1080)
		{
			selectedWebCam = webCam;
			webCameraController.StartCapturing(webCam.name, requestedWidth, requestedHeight);
			webcamWidthString = webcamHeightString = "";
		}

		#endregion

		#region Misc Methods

		/// <summary>
		/// Helper function that allows to yield on multiple async requests in a coroutine.
		/// It also tracks progress on the current request(s) and updates it in UI.
		/// </summary>
		protected IEnumerator AwaitRoutine(params AsyncRequest[] requests)
		{
			foreach (var r in requests)
				while (r != null && !r.IsDone)
				{
					// yield null to wait until next frame (to avoid blocking the main thread)
					yield return null;

					// This function will throw on any error. Such primitive error handling only provided as
					// an example, the production app probably should be more clever about it.
					if (r.IsError)
					{
						Debug.LogError(r.ErrorMessage);
						throw new Exception(r.ErrorMessage);
					}

					// Each requests may or may not contain "subrequests" - the asynchronous subtasks needed to
					// complete the request. The progress for the requests can be tracked overall, as well as for
					// every subtask. The code below shows how to recursively iterate over current subtasks
					// to display progress for them.
					var progress = new List<string>();
					AsyncRequest request = r;
					while (request != null)
					{
						progress.Add(string.Format("{0}: {1}%", request.State, request.ProgressPercent.ToString("0.0")));
						request = request.CurrentSubrequest;
					}

					ProgressText = string.Join("\n", progress.ToArray());
				}
		}
#endregion
	}
}
