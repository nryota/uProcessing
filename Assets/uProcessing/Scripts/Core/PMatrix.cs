using UnityEngine;
using System.Collections;

static class PMatrixExtensions
{
	public static void set(this Transform transform, PMatrix matrix)
	{
		transform.localScale = matrix.getScale();
		transform.rotation = matrix.getRotation();
		transform.position = matrix.getPosition();
	}
}

public class PMatrix {
	Matrix4x4 m { get; set; }

	public PMatrix() {
		this.m = new Matrix4x4();
	}

	public PMatrix(Matrix4x4 m) {
		this.m = m;
	}

	#region Processing Extra Members
	public Quaternion getRotation() {
		var qw = Mathf.Sqrt(1.0f + m.m00 + m.m11 + m.m22) * 0.5f;
		var w = 4.0f * qw;
		var qx = (m.m21 - m.m12) / w;
		var qy = (m.m02 - m.m20) / w;
		var qz = (m.m10 - m.m01) / w;
		return new Quaternion(qx, qy, qz, qw);
	}
	
	public Vector3 getPosition() {
		var x = m.m03;
		var y = m.m13;
		var z = m.m23;
		return new Vector3(x, y, z);
	}

	public Vector3 getScale()
	{
		var x = Mathf.Sqrt(m.m00 * m.m00 + m.m01 * m.m01 + m.m02 * m.m02);
		var y = Mathf.Sqrt(m.m10 * m.m10 + m.m11 * m.m11 + m.m12 * m.m12);
		var z = Mathf.Sqrt(m.m20 * m.m20 + m.m21 * m.m21 + m.m22 * m.m22);
		return new Vector3(x, y, z);
	}
	#endregion
}
