using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
[ExecuteInEditMode]
public class SdfBaker : MonoBehaviour {
	public enum BakeSdfMode { 
	RAY_CAST,RENDER_DEPTH
	}
	[Header("烘焙9700 +2060s 需要3分钟左右")]
	public bool ckickToBake;
	public BakeSdfMode bakeSdfMode = BakeSdfMode.RENDER_DEPTH;
	  RenderTexture tempDepth;
 	  RenderTexture   rt;
	private Camera camera;
	public Shader renderShader;
	public ComputeShader calMinDistance;
	ComputeBuffer resBuff;
 
 
	Dictionary<int, float> posCacheMap;
 
	public Texture3D sdfTex;

	//public bool sdfTree;
	[ContextMenu("bakesdf")]
	void bakesdf()
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		 
		posCacheMap = new Dictionary<int, float>();
		if (bakeSdfMode == BakeSdfMode.RENDER_DEPTH)
		{
			initCamera();
			print(calMinDistance.FindKernel("CSMain"));
			resBuff = new ComputeBuffer(1, 4);
			calMinDistance.SetTexture(0, "depthTex", tempDepth);
			calMinDistance.SetBuffer(0, "rstBuff", resBuff);
		}
	  
		int scale =1;
		int xCount =  100* scale;
		int zCount = 100* scale;
		int yCount = 20* scale;
		float step = 1.0f/ scale;
		sdfTex = new Texture3D(xCount, zCount, yCount, TextureFormat.RFloat, false);
		sdfTex.filterMode = FilterMode.Trilinear;
		sdfTex.wrapMode = TextureWrapMode.Clamp;
	 
	    var bakingColors= new Color[xCount * zCount * yCount];
         for (int x = 0; x < xCount; x++)
        {
            for (int z = 0; z < zCount; z++)
            {
                for (int y = 0; y < yCount; y++)
                {
					 if(bakeSdfMode== BakeSdfMode.RAY_CAST)
				 	bakingColors[y * xCount * zCount + z * xCount + x].r = getSdfFromRay(new Vector3(x, y, z)*step + Vector3.one * step/2 - new Vector3(xCount/scale/2, 0, zCount / scale / 2)); 
					else
				 	bakingColors[y * xCount * zCount + z * xCount + x].r = getSdfFromCamera(new Vector3(x, y, z)*step + Vector3.one * step/2 - new Vector3(xCount/scale/2, 0, zCount / scale / 2)); 
				 
				}

            }
			 
        }

		sdfTex.SetPixels(bakingColors);
		sdfTex.Apply();
	 
		Shader.SetGlobalTexture("_TestSdfTex", sdfTex);
		sw.Stop();
		print("baking time:"+sw.ElapsedMilliseconds/1000+" s");
 
	}


    private float getSdfFromCamera(Vector3 wpos)
    {
	 int posIndex=	((int)wpos.z/3 + 500) * 1000 * 1000 + ((int)wpos.y/3 + 500) * 1000 + ((int)wpos.x/3 + 500);
		 if (posCacheMap.ContainsKey(posIndex)) {
		 return posCacheMap[posIndex];
		 }
		transform.position = wpos;
		camera.RenderToCubemap(tempDepth);
		if (rt == null)
		{
			rt = RenderTexture.GetTemporary(tempDepth.width, tempDepth.height, tempDepth.depth, tempDepth.format, RenderTextureReadWrite.Linear);
			rt.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
			rt.volumeDepth = 6;
          rt.Create();
          
			 
		}

		for (int i = 0; i < 6; i++)
		{
			Graphics.CopyTexture(tempDepth, i, rt, i);
		}
		calMinDistance.SetTexture(0, "depthTex", rt);
	

		 calMinDistance.Dispatch(0, 1, 1, 1);

		 float[] rstArray = new float[1];
		 resBuff.GetData(rstArray);
		
		// print(rstArray[0]);
		 if(rstArray[0]>4)   posCacheMap[posIndex]= rstArray[0]-3;
		return rstArray[0];
	}

    private void initCamera()
    {
		camera = GetComponent<Camera>();
		camera.enabled = false;
		camera.SetReplacementShader(renderShader, "");
		if (tempDepth == null)
		{
			tempDepth = new RenderTexture(32, 32, 16, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);// .GetTemporary(32, 32, 16, RenderTextureFormat.ARGBHalf);
			
			tempDepth.dimension = UnityEngine.Rendering.TextureDimension.Cube;
			tempDepth.Create();
		}

	}
 
    // Update is called once per frame
    void Update () {
		if (ckickToBake) {
			ckickToBake = false;
			bakesdf();
		}
		if(sdfTex!=null)
		Shader.SetGlobalTexture("_TestSdfTex", sdfTex);
	}
 
	 
	float getSdfFromRay(Vector3 wpos) {
		int posIndex = ((int)wpos.z / 3 + 500) * 1000 * 1000 + ((int)wpos.y / 3 + 500) * 1000 + ((int)wpos.x / 3 + 500);
		 if (posCacheMap.ContainsKey(posIndex))
		{
		 	return posCacheMap[posIndex];
		}

		//正向求交 存最近表面距离
		float minDis = 100;
		int sign = 1;
		int rayRow = 32;
		for (int i = 0; i < rayRow; i++)
		{
			for (int j = 0; j <= rayRow; j++)
			{
				Vector3 dir = Vector3.zero;
				float yLen = Mathf.Sin(Mathf.Deg2Rad * (j - rayRow/2) * 180 / rayRow);
				float xzLen = Mathf.Sqrt(1 - yLen * yLen);
				dir.y = yLen;
				dir.x = Mathf.Cos(Mathf.Deg2Rad * i * 360 / rayRow) * xzLen;
				dir.z = Mathf.Sin(Mathf.Deg2Rad * i * 360 / rayRow) * xzLen;
			 
				RaycastHit hitInfo;

				if (Physics.Raycast(wpos, dir, out hitInfo))
				{
					if (minDis > hitInfo.distance)
					{

						minDis = hitInfo.distance;
					}
				}

			}

		}

		//反向求交 存最近表面距离
		for (int i = 0; i < rayRow; i++)
		{
			for (int j = 0; j <= rayRow; j++)
			{
				Vector3 dir = Vector3.zero;
				float yLen = Mathf.Sin(Mathf.Deg2Rad * (j - rayRow/2) * 180 / rayRow);
				float xzLen = Mathf.Sqrt(1 - yLen * yLen);
				dir.y = yLen;
				dir.x = Mathf.Cos(Mathf.Deg2Rad * i * 360 / rayRow) * xzLen;
				dir.z = Mathf.Sin(Mathf.Deg2Rad * i * 360 / rayRow) * xzLen;

				RaycastHit[] hitInfos = Physics.RaycastAll(wpos + dir * minDis, -dir, minDis);
				float maxRevertDis = 0;
				foreach (var hitInfo in hitInfos)
				{
					maxRevertDis = Mathf.Max(maxRevertDis, hitInfo.distance);
					 sign = -1;
				}
				if (maxRevertDis != 0) {
					minDis = minDis - maxRevertDis;
				}

			}

		}

		minDis *= sign;
		
			 if (minDis > 4) return posCacheMap[posIndex] = minDis - 3;
		return minDis;
	}

}
