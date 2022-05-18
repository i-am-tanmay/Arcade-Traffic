using UnityEngine;
using UnityEngine.Rendering;

public class SkidMarksManager : MonoBehaviour
{
    #region Singleton
    private static SkidMarksManager _instance;
    public static SkidMarksManager Instance { get { return _instance; } }

    private void Awake()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else { _instance = this; }
    }
    #endregion

    [SerializeField] private Material skidmarksMaterial;

	private const int MAX_MARKS = 2048;
	private const float MARK_WIDTH = 0.15f;
	private const float GROUND_OFFSET = 0.02f;
	private const float MIN_DISTANCE = 0.25f;
	private const float MIN_SQR_DISTANCE = MIN_DISTANCE * MIN_DISTANCE;
	private const float MAX_OPACITY = 1.0f;

	class MarkSection
	{
		public Vector3 Pos = Vector3.zero;
		public Vector3 Normal = Vector3.zero;
		public Vector4 Tangent = Vector4.zero;
		public Vector3 Posl = Vector3.zero;
		public Vector3 Posr = Vector3.zero;
		public Color32 Colour;
		public int LastIndex;
	};

	private int markIndex;
	private MarkSection[] skidmarks;
	private Mesh marksMesh;
	private MeshRenderer meshRenderer;
	private MeshFilter meshFilter;

	private Vector3[] vertices;
	private Vector3[] normals;
	private Vector4[] tangents;
	private Color32[] colors;
	private Vector2[] uvs;
	private int[] triangles;

	private bool meshUpdated;
	private bool haveSetBounds;

	private Color32 clr_black = Color.black;

	protected void Start()
	{
		skidmarks = new MarkSection[MAX_MARKS];
		for (int i = 0; i < MAX_MARKS; i++) skidmarks[i] = new MarkSection();

        vertices = new Vector3[MAX_MARKS * 4];
        normals = new Vector3[MAX_MARKS * 4];
        tangents = new Vector4[MAX_MARKS * 4];
        colors = new Color32[MAX_MARKS * 4];
        uvs = new Vector2[MAX_MARKS * 4];
        triangles = new int[MAX_MARKS * 6];

        meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
		marksMesh = new Mesh();
		marksMesh.MarkDynamic();
		meshFilter.sharedMesh = marksMesh;
		meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
		meshRenderer.receiveShadows = false;
		meshRenderer.material = skidmarksMaterial;
		meshRenderer.lightProbeUsage = LightProbeUsage.Off;
	}

	protected void LateUpdate()
	{
		if (!meshUpdated) return;
		meshUpdated = false;

		marksMesh.vertices = vertices;
		marksMesh.normals = normals;
		marksMesh.tangents = tangents;
		marksMesh.triangles = triangles;
		marksMesh.colors32 = colors;
		marksMesh.uv = uvs;

		if (!haveSetBounds)
		{
			//marksMesh.RecalculateBounds(); //SLOW
			marksMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
			haveSetBounds = true;
		}

		meshFilter.sharedMesh = marksMesh;
	}

	public int AddSkidMark(Vector3 pos, Vector3 normal, float opacity, int lastIndex)
	{
		if (opacity > 1) opacity = 1.0f;
		else if (opacity <= 0) return -1;

		clr_black.a = (byte)(opacity * 255);
		return AddSkidMark(pos, normal, clr_black, lastIndex);
	}
	
	public int AddSkidMark(Vector3 pos, Vector3 normal, Color32 colour, int lastIndex)
	{
		if (colour.a == 0) return -1;

		MarkSection lastSection = null;
		Vector3 distAndDirection = Vector3.zero;
		Vector3 newPos = pos + normal * GROUND_OFFSET;
		if (lastIndex != -1)
		{
			lastSection = skidmarks[lastIndex];
			distAndDirection = newPos - lastSection.Pos;
			if (distAndDirection.sqrMagnitude < MIN_SQR_DISTANCE)
			{
				return lastIndex;
			}

			if (distAndDirection.sqrMagnitude > MIN_SQR_DISTANCE * 10)
			{
				lastIndex = -1;
				lastSection = null;
			}
		}

		colour.a = (byte)(colour.a * MAX_OPACITY);

		MarkSection curSection = skidmarks[markIndex];

		curSection.Pos = newPos;
		curSection.Normal = normal;
		curSection.Colour = colour;
		curSection.LastIndex = lastIndex;

		if (lastSection != null)
		{
			Vector3 xDirection = Vector3.Cross(distAndDirection, normal).normalized;
			curSection.Posl = curSection.Pos + xDirection * MARK_WIDTH * 0.5f;
			curSection.Posr = curSection.Pos - xDirection * MARK_WIDTH * 0.5f;
			curSection.Tangent = new Vector4(xDirection.x, xDirection.y, xDirection.z, 1);

			if (lastSection.LastIndex == -1)
			{
				lastSection.Tangent = curSection.Tangent;
				lastSection.Posl = curSection.Pos + xDirection * MARK_WIDTH * 0.5f;
				lastSection.Posr = curSection.Pos - xDirection * MARK_WIDTH * 0.5f;
			}
		}

		UpdateSkidmarksMesh();

		int curIndex = markIndex;
		markIndex = ++markIndex % MAX_MARKS;

		return curIndex;
	}
	void UpdateSkidmarksMesh()
	{
		MarkSection curr = skidmarks[markIndex];

		if (curr.LastIndex == -1) return;

		MarkSection last = skidmarks[curr.LastIndex];
		vertices[markIndex * 4 + 0] = last.Posl;
		vertices[markIndex * 4 + 1] = last.Posr;
		vertices[markIndex * 4 + 2] = curr.Posl;
		vertices[markIndex * 4 + 3] = curr.Posr;

		normals[markIndex * 4 + 0] = last.Normal;
		normals[markIndex * 4 + 1] = last.Normal;
		normals[markIndex * 4 + 2] = curr.Normal;
		normals[markIndex * 4 + 3] = curr.Normal;

		tangents[markIndex * 4 + 0] = last.Tangent;
		tangents[markIndex * 4 + 1] = last.Tangent;
		tangents[markIndex * 4 + 2] = curr.Tangent;
		tangents[markIndex * 4 + 3] = curr.Tangent;

		colors[markIndex * 4 + 0] = last.Colour;
		colors[markIndex * 4 + 1] = last.Colour;
		colors[markIndex * 4 + 2] = curr.Colour;
		colors[markIndex * 4 + 3] = curr.Colour;

		uvs[markIndex * 4 + 0] = new Vector2(0, 0);
		uvs[markIndex * 4 + 1] = new Vector2(1, 0);
		uvs[markIndex * 4 + 2] = new Vector2(0, 1);
		uvs[markIndex * 4 + 3] = new Vector2(1, 1);

		triangles[markIndex * 6 + 0] = markIndex * 4 + 0;
		triangles[markIndex * 6 + 2] = markIndex * 4 + 1;
		triangles[markIndex * 6 + 1] = markIndex * 4 + 2;

		triangles[markIndex * 6 + 3] = markIndex * 4 + 2;
		triangles[markIndex * 6 + 5] = markIndex * 4 + 1;
		triangles[markIndex * 6 + 4] = markIndex * 4 + 3;

		meshUpdated = true;
	}

	public void ResetMesh()
    {
		marksMesh.Clear(false);
        //marksMesh = new Mesh();
        //marksMesh.MarkDynamic();
        //meshFilter.sharedMesh = marksMesh;

		markIndex = 0;                                                                                          
        skidmarks = new MarkSection[MAX_MARKS];
        for (int i = 0; i < MAX_MARKS; i++) skidmarks[i] = new MarkSection();

        vertices = new Vector3[MAX_MARKS * 4];
        normals = new Vector3[MAX_MARKS * 4];
        tangents = new Vector4[MAX_MARKS * 4];
        colors = new Color32[MAX_MARKS * 4];
        uvs = new Vector2[MAX_MARKS * 4];
        triangles = new int[MAX_MARKS * 6];
    }
}