using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(MeshFilter))]
public class GridOverlayMap : MonoBehaviour
{
  public ComputeShader gridShader;
  public float OutlineMult = 0.1f;

  ComputeBuffer gridSquares;
  ComputeBuffer vertBuffer;
  ComputeBuffer colBuffer;
  ComputeBuffer indexBuffer;

  MapTerrain mapRef;
  MeshFilter meshFilter;

  int[] gridOverride;

  public IEnumerator SetupGrid(MapTerrain map)
  {
    yield return new WaitForSeconds(0.1f);
    mapRef = map;

    int numSquares = map.gridWidth * map.gridHeight;

    gridSquares = new ComputeBuffer(numSquares, sizeof(int), ComputeBufferType.Default);

    vertBuffer = new ComputeBuffer(numSquares * 4 * 2, sizeof(float) * 3, ComputeBufferType.Default);
    colBuffer = new ComputeBuffer(numSquares * 4 * 2, sizeof(float) * 4, ComputeBufferType.Default);
    indexBuffer = new ComputeBuffer(numSquares * 4 * 3 * 2, sizeof(int), ComputeBufferType.Default);

    UpdateMesh();
  }

  public IEnumerator AnimateSquaresAtStartOfGame()
  {
    // Copy 
    var gdSrc = mapRef.m_gridValues;
    gridOverride = new int[gdSrc.Length];
    for (int i = 0; i < gdSrc.Length; ++i)
      gridOverride[i] = MapTerrain.Hidden; 
    

    UpdateMesh();
    yield return new WaitForSeconds(0.5f);

    // Show Squares one at a time     
    Queue<int> oldPosTrail = new Queue<int>();
    for (int y = mapRef.gridHeight - 1; y >= 0; y--)
    {
      for (int x = 0; x < mapRef.gridWidth; x++)
      {
        int i = x + y * mapRef.gridWidth;
        gridOverride[i] = MapTerrain.Normal;
        oldPosTrail.Enqueue(i);

        // Set Trailing Square to Real Value
        if (oldPosTrail.Count > 3)
        {
          int iOld = oldPosTrail.Dequeue();
          gridOverride[iOld] = gdSrc[iOld];
        }

        UpdateMesh();
        yield return new WaitForSeconds(0.02f);
      }
    }
    
    // Finish Sequence
    while (oldPosTrail.Count > 0)
    {
      int iOld = oldPosTrail.Dequeue();
      gridOverride[iOld] = gdSrc[iOld];

      UpdateMesh();
      yield return new WaitForSeconds(0.02f);
    }

    gridOverride = null;
    UpdateMesh();
  }

  public void UpdateMesh()
  {
    if (mapRef == null)
      return;

    // Setup Grid Numbers
    int[] squareSizes = new int[4] {
        mapRef.subWidth,
      mapRef.subHeight,
      mapRef.gridWidth,
      mapRef.gridHeight,
      };

    // Build Mesh
    if(gridOverride != null)
    {
      gridSquares.SetData(gridOverride);      
    } else
    {
      gridSquares.SetData(mapRef.m_gridValues);
    }
    

    gridShader.SetInts("SquareSize", squareSizes);
    gridShader.SetFloat("OutlineAmount", OutlineMult);

    int kernelHandle = gridShader.FindKernel("CSGridMain");
    gridShader.SetBuffer(kernelHandle, "SquareData", gridSquares);
    gridShader.SetBuffer(kernelHandle, "VertBuf", vertBuffer);
    gridShader.SetBuffer(kernelHandle, "ColBuf", colBuffer);
    gridShader.SetBuffer(kernelHandle, "IndxBuf", indexBuffer);
    gridShader.Dispatch(kernelHandle, squareSizes[2], squareSizes[3], 1);

    Vector3[] vData = new Vector3[vertBuffer.count];
    Color[] cData = new Color[colBuffer.count];
    int[] iData = new int[indexBuffer.count];

    vertBuffer.GetData(vData);
    colBuffer.GetData(cData);
    indexBuffer.GetData(iData);


    meshFilter.mesh.vertices = vData;
    meshFilter.mesh.colors = cData;
    meshFilter.mesh.SetIndices(iData, MeshTopology.Triangles, 0);

    meshFilter.mesh.RecalculateBounds();

    Debug.Log("Grid Mesh Updated");
  }

  // Use this for initialization
  void Start()
  {
    meshFilter = GetComponent<MeshFilter>();
    meshFilter.mesh.MarkDynamic();

  }

  private void OnDestroy()
  {
    if (gridSquares != null) gridSquares.Release();

    if (vertBuffer != null) vertBuffer.Release();
    if (colBuffer != null) colBuffer.Release();
    if (indexBuffer != null) indexBuffer.Release();
  }

  // Update is called once per frame
  void Update()
  {

  }
}
