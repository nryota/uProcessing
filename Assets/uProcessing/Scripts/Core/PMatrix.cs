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
	public Matrix4x4 m;// { get; set; }

	public PMatrix() { this.m = new Matrix4x4(); }
	public PMatrix(Matrix4x4 source) { this.m = source; }
	public PMatrix(PMatrix source) { this.m = source.m; }
	public override string ToString () { return m.ToString(); }

	#region Processing Members
	public float m00 { get { return m.m00; } set { m.m00 = value; } }
	public float m01 { get { return m.m01; } set { m.m01 = value; } }
	public float m02 { get { return m.m02; } set { m.m02 = value; } }
	public float m03 { get { return m.m03; } set { m.m03 = value; } }
	public float m10 { get { return m.m10; } set { m.m10 = value; } }
	public float m11 { get { return m.m11; } set { m.m11 = value; } }
	public float m12 { get { return m.m12; } set { m.m12 = value; } }
	public float m13 { get { return m.m13; } set { m.m13 = value; } }
	public float m20 { get { return m.m20; } set { m.m20 = value; } }
	public float m21 { get { return m.m21; } set { m.m21 = value; } }
	public float m22 { get { return m.m22; } set { m.m22 = value; } }
	public float m23 { get { return m.m23; } set { m.m23 = value; } }
	public float m30 { get { return m.m30; } set { m.m30 = value; } }
	public float m31 { get { return m.m31; } set { m.m31 = value; } }
	public float m32 { get { return m.m32; } set { m.m32 = value; } }
	public float m33 { get { return m.m33; } set { m.m33 = value; } }

	public PMatrix get() { return new PMatrix(this); }
	public void set(Matrix4x4 source) { m = source; }
	public void set(PMatrix source) { m = source.m; }
	public void applay(PMatrix source) { m *= source.m; }
	public void preApplay(PMatrix left) { set(left.m * m); }
	public void reset() { set(Matrix4x4.zero); }

	public bool invert() {
		var im = m.inverse;
		m = im;
		return true;
	}
	public void transpose() { m = m.transpose; }

	public Vector3 mult(Vector3 source, Vector3 target) {
		target = m.MultiplyPoint(source);
		return target;
	}

	public void scale(Vector3 v) { scale(v.x, v.y, v.z); }
	public void scale(float x, float y, float z = 1.0f) {
		m.m00 *= x; m.m01 *= x; m.m02 *= x;
		m.m10 *= y; m.m11 *= y; m.m12 *= y;
		m.m20 *= z; m.m21 *= z; m.m22 *= z;
	}
	#endregion

	#region Processing Extra Members
	public Vector3 mult(Vector3 source) { return m.MultiplyPoint(source); }

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

	public void set3x3(PMatrix pm) {
		m.m00 = pm.m00; m.m01 = pm.m01; m.m02 = pm.m02;
		m.m10 = pm.m10; m.m11 = pm.m11; m.m12 = pm.m12;
		m.m20 = pm.m20; m.m21 = pm.m21; m.m22 = pm.m22;
	}
	#endregion
}
