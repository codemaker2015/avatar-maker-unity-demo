/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@itseez3D.com>, April 2017
*/

using ItSeez3D.AvatarSdk.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItSeez3D.AvatarMaker
{
	public class AvatarInfo : MonoBehaviour
	{

		[HideInInspector]
		public string code;

		public string[] haircuts;

		public int selectedHairstyle;

		public Color avgHaircutColor;

		public Color predictedHaircutColor;

		private Color haircutColor;

		public SkinnedMeshRenderer headMeshRenderer;

		public MeshRenderer hairMeshRenderer;

		public int levelOfDetails = 0;

		public string SelectedHairstyleName
		{
			get
			{
				if (selectedHairstyle >= 0 && haircuts != null && haircuts.Length > selectedHairstyle && string.Compare(haircuts[selectedHairstyle], "bald") != 0)
				{
					return haircuts[selectedHairstyle];
				}
				else
					return string.Empty;
			}
		}

		public Color HaircutColor
		{
			get
			{
				return haircutColor;
			}

			set
			{
				haircutColor = value;

				if (hairMeshRenderer != null)
				{
					HaircutColorTint = CoreTools.CalculateTint(haircutColor, avgHaircutColor);

					hairMeshRenderer.sharedMaterial.SetVector("_ColorTarget", haircutColor);
					hairMeshRenderer.sharedMaterial.SetVector("_ColorTint", HaircutColorTint);
					hairMeshRenderer.sharedMaterial.SetFloat("_TintCoeff", 0.8f);
				}
			}
		}

		public Color HaircutColorTint { get; private set; }
	}
}
