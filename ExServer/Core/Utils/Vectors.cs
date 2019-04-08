#if UNITY_2017 || UNITY_2018 || UNITY_2019
#define UNITY
#endif

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !UNITY
using static Ex.Utils.Mathf;

namespace Ex.Utils {
	#region Mathf
	/// <summary> Like UnityEngine.Mathf, Wrap <see cref="System.Math"/> functions to deal with float/int, and some custom functions. </summary>
	public struct Mathf {
		public const float PI = 3.14159274f;
		public const float EPSILON = 1E-05f;
		public const float SQR_EPSILON = 1E-15f;
		public const float COMPARE_EPSILON = 9.99999944E-11f;
		public const float Infinity = float.PositiveInfinity;
		public const float NegativeInfinity = float.NegativeInfinity;
		public const float Deg2Rad = (2f * PI) / 360f;
		public const float Rad2Deg = 360f / (PI * 2f);
		public static float Sin(float f) { return (float)Math.Sin(f); }
		public static float Cos(float f) { return (float)Math.Cos(f); }
		public static float Tan(float f) { return (float)Math.Tan(f); }
		public static float Asin(float f) { return (float)Math.Asin(f); }
		public static float Acos(float f) { return (float)Math.Acos(f); }
		public static float Atan(float f) { return (float)Math.Atan(f); }
		public static float Atan2(float y, float x) { return (float) Math.Atan2(y, x); }
		public static float Sqrt(float f) { return (float) Math.Sqrt(f); }
		public static float Abs(float f) { return Math.Abs(f); }
		public static int Abs(int f) { return Math.Abs(f); }

		public static float Pow(float f, float p) { return (float)Math.Pow(f, p); }
		public static float Exp(float power) { return (float)Math.Exp(power); }
		public static float Log(float f, float b) { return (float)Math.Log(f, b); }
		public static float Log(float f) { return (float)Math.Log(f); }
		public static float Log10(float f) { return (float)Math.Log10(f); }

		public static float Ceil(float f) { return (float)Math.Ceiling(f); }
		public static int CeilToInt(float f) { return (int)Math.Ceiling(f); }
		public static float Floor(float f) { return (float)Math.Floor(f); }
		public static int FloorToInt(float f) { return (int)Math.Floor(f); }
		public static float Round(float f) { return (float)Math.Round(f); }
		public static int RoundToInt(float f) { return (int)Math.Round(f); }

		public static float Min(float a, float b) { return a < b ? a : b; }
		public static float Min(float a, float b, float c) { return a < b ? (a < c ? a : c) : (b < c ? b : c); }
		public static float Max(float a, float b) { return a > b ? a : b; }
		public static float Max(float a, float b, float c) { return a > b ? (a > c ? a : c) : (b > c ? b : c); }
		public static int Min(int a, int b) { return a < b ? a : b; }
		public static int Min(int a, int b, int c) { return a < b ? (a < c ? a : c) : (b < c ? b : c); }
		public static int Max(int a, int b) { return a > b ? a : b; }
		public static int Max(int a, int b, int c) { return a > b ? (a > c ? a : c) : (b > c ? b : c); }

		public static float Repeat(float f, float length) { return Clamp(f - Floor(f / length) * length, 0, length); }
		public static float PingPong(float f, float length) { f = Repeat(f, length*2f); return length - Abs(f - length); }
		
		public static float Sign(float f) { return (f < 0) ? -1f : 1f; }
		public static float Clamp01(float f) { return f < 0 ? 0 : f > 1 ? 1 : f; }
		public static float Clamp(float f, float min, float max) { return f < min ? min : f > max ? max : f; }
		public static int Clamp(int f, int min, int max) { return f < min ? min : f > max ? max : f; }
		public static float DeltaAngle(float current, float target) {
			float angle = Repeat(target - current, 360f);
			if (angle > 180f) { angle -= 360f; }
			return angle;
		}
		public static float Map(float a, float b, float val, float x, float y) { return Lerp(x, y, InverseLerp(a, b, val)); }
		public static float Lerp(float a, float b, float f) { return a + (b-a) * Clamp01(f); }
		public static float InverseLerp(float a, float b, float value) { return (a != b) ? Clamp01((value-a) / (b-a)) : 0f; }
		public static float LerpUnclamped(float a, float b, float f) { return a + (b-a) * f; }
		public static float SmoothStep(float a, float b, float f) {
			f = Clamp01(f);
			f = -2f * f * f * f + 3f * f * f;
			return a * f + b * (1f - f);
		}
		public static float LerpAngle(float a, float b, float f) {
			float angle = Repeat(b - a, 360f);
			if (angle > 180f) { angle -= 360f; }
			return a + angle * Clamp01(f);
		}
		public static float MoveTowards(float current, float target, float maxDelta) {
			return (Abs(target - current) <= maxDelta) ? target : (current + Sign(target-current) * maxDelta);
		}
		public static float MoveTowardsAngle(float current, float target, float maxDelta) {
			float delta = DeltaAngle(current, target);
			return (-maxDelta < delta && delta < maxDelta) ? target : MoveTowards(current, current+delta, maxDelta);
		}
		public static float Gamma(float value, float absmax, float gamma) {
			bool negative = value < 0f;
			float abs = Abs(value);
			if (abs > absmax) { return negative ? -abs : abs; }
			float pow =  Pow(abs / absmax, gamma) * absmax;
			return negative ? -pow : pow;
		}
		
		public static float Damp(float current, float target, ref float currentVelocity, float smoothTime, float deltaTime, float maxSpeed = Infinity) {
			smoothTime = Max(.0001f, smoothTime);
			float step = 2f / smoothTime;
			float d = step*deltaTime;
			float smoothed = 1f / (1f + d + 0.48f * d * d + 0.235f * d * d * d);

			float desired = target;
			float maxDelta = maxSpeed * smoothTime;
			float diff = Clamp(current - target, -maxDelta, maxDelta);
			target = current - diff;
			
			float velocityStep = (currentVelocity + step * diff) * deltaTime;
			currentVelocity = (currentVelocity - step * velocityStep) * smoothed;
			float result = target + (diff + velocityStep) * smoothed;
			if (desired - current > 0f == result > desired) {
				result = desired;
				currentVelocity = (result - desired) / deltaTime;
			}
			return result;
		}
		public static float DampAngle(float current, float target, ref float currentVelocity, float smoothTime, float deltaTime, float maxSpeed = Infinity) {
			target = current + DeltaAngle(current, target);
			return Damp(current, target, ref currentVelocity, smoothTime, deltaTime, maxSpeed);
		}

		public static float Spring(float value, float target, ref float velocity, float deltaTime, float strength = 100, float dampening = 1) {
			velocity += (target - value) * strength * deltaTime;
			velocity *= Pow(dampening * .0001f, deltaTime);
			value += velocity * deltaTime;
			return value;
		}
	}
	#endregion Mathf
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	#region Vector2
	/// <summary> Surrogate class, similar to UnityEngine.Vector2 </summary>
	public struct Vector2 {
		public static Vector2 zero { get { return new Vector2(0, 0); } }
		public static Vector2 one { get { return new Vector2(1, 1); } }
		public static Vector2 up{ get { return new Vector2(0, 1); } }
		public static Vector2 down { get { return new Vector2(0, -1); } }
		public static Vector2 left { get { return new Vector2(-1, 0); } }
		public static Vector2 right { get { return new Vector2(1, 0); } }
		public static Vector2 negativeInfinity { get { return new Vector2(float.NegativeInfinity, float.NegativeInfinity); } }
		public static Vector2 positiveInfinity { get { return new Vector2(float.PositiveInfinity, float.PositiveInfinity); } }
		
		public float x, y;
		public Vector2(float x, float y) { this.x = x; this.y = y; }

		public float magnitude { get { return Mathf.Sqrt(x*x + y*y); } }
		public float sqrMagnitude { get { return (x*x) + (y*y); } }
		public Vector2 normalized { get { float m = magnitude; if (m > EPSILON) { return this / m; } return zero; } }
		public float this[int i] { 
			get { if (i == 0) { return x; } if (i == 1) { return y; } throw new IndexOutOfRangeException($"Vector2 has length=2, {i} is out of range."); } 
			set { if (i == 0) { x = value; } if (i == 1) { y = value; } throw new IndexOutOfRangeException($"Vector2 has length=2, {i} is out of range."); }
		}
		
		public override bool Equals(object other) { return other is Vector2 && Equals((Vector2)other); }
		public bool Equals(Vector2 other) { return x.Equals(other.x) && y.Equals(other.y); }
		public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2; }
		public override string ToString() { return $"({x:F2}, {y:F2})"; }

		public void Normalize() { float m = magnitude; if (m > EPSILON) { this /= m; } else { this = zero; } }
		public void Set(float x, float y) { this.x = x; this.y = y; }
		public void Scale(float a, float b) { x *= a; y *= b; }
		public void Scale(Vector2 s) { x *= s.x; y *= s.y; }
		public void Clamp(Vector2 min, Vector2 max) {
			x = Mathf.Clamp(x, min.x, max.x);
			y = Mathf.Clamp(y, min.y, max.y);
		}
		
		public static float Dot(Vector2 a, Vector2 b) { return a.x * b.x + a.y * b.y; }
		public static Vector2 Min(Vector2 a, Vector2 b) { return new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y)); }
		public static Vector2 Max(Vector2 a, Vector2 b) { return new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)); }

		public static Vector2 Lerp(Vector2 a, Vector2 b, float f) { f = Clamp01(f); return new Vector2(a.x + (b.x-a.x) * f, a.y + (b.y-a.y) * f); }
		public static Vector2 LerpUnclamped(Vector2 a, Vector2 b, float f) { return new Vector2(a.x + (b.x-a.x) *f, a.y + (b.y-a.y) * f); }
		public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistanceDelta) {
			Vector2 a = target - current;
			float m = a.magnitude;
			return (m < maxDistanceDelta || m == 0f) ? target : (current + a / m * maxDistanceDelta);
		}
		public static Vector2 Scale(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
		public static Vector2 ClampMagnitude(Vector2 vector, float maxLength) {
			return (vector.sqrMagnitude > maxLength * maxLength) ? vector.normalized * maxLength : vector;
		}
		public static Vector2 Reflect(Vector2 dir, Vector2 normal) { return -2f * Dot(normal, dir) * normal + dir; }
		public static Vector2 Project(Vector2 dir, Vector2 normal) {
			float len = Dot(normal, normal);
			return (len < SQR_EPSILON) ? zero : normal * Dot(dir, normal) / len;
		}
		public static Vector2 Perpendicular(Vector2 dir) { return new Vector2(-dir.y, dir.x); }

		public static float Distance(Vector2 a, Vector2 b) { return (a-b).magnitude; }
		public static float Angle(Vector2 from, Vector2 to) { 
			float e = Sqrt(from.sqrMagnitude * to.sqrMagnitude);
			if (e < SQR_EPSILON) { return 0; }
			float f = Mathf.Clamp(Dot(from, to) / e, -1f, 1f);
			return Acos(f) * Rad2Deg;
		}
		public static float SignedAngle(Vector2 from, Vector2 to) {
			float angle = Angle(from, to);
			float sign = Sign(from.x * to.y - from.y * to.x);
			return sign * angle;
		}

		public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
		public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
		public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
		public static Vector2 operator *(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
		public static Vector2 operator /(Vector2 a, Vector2 b) { return new Vector2(a.x / b.x, a.y / b.y); }
		public static Vector2 operator *(Vector2 a, float f) { return new Vector2(a.x * f, a.y * f); }
		public static Vector2 operator *(float f, Vector2 a) { return new Vector2(a.x * f, a.y * f); }
		public static Vector2 operator /(Vector2 a, float f) { return new Vector2(a.x / f, a.y / f); }
		public static Vector2 operator /(float f, Vector2 a) { return new Vector2(a.x / f, a.y / f); }
		public static bool operator ==(Vector2 a, Vector2 b) { return (a - b).sqrMagnitude < COMPARE_EPSILON; }
		public static bool operator !=(Vector2 a, Vector2 b) { return !(a == b); }
		public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
		public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0f); }
	}
	#endregion
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	#region Vector2Int
	/// <summary> Surrogate class, similar to UnityEngine.Vector2Int </summary>
	public struct Vector2Int : IEquatable<Vector2Int> {
		public static Vector2Int zero { get { return new Vector2Int(0, 0); } }
		public static Vector2Int one { get { return new Vector2Int(1, 1); } }
		public static Vector2Int up { get { return new Vector2Int(0, 1); } }
		public static Vector2Int down { get { return new Vector2Int(0, -1); } }
		public static Vector2Int left { get { return new Vector2Int(-1, 0); } }
		public static Vector2Int right { get { return new Vector2Int(1, 0); } }

		public int x, y;
		public Vector2Int(int x, int y) { this.x = x; this.y = y; }

		public int this[int i] { 
			get { if (i == 0) { return x; } if (i == 1) { return y; } throw new IndexOutOfRangeException($"Vector2Int has length=2, {i} is out of range."); }
			set { if (i == 0) { x = value; } if (i == 1) { y = value; } throw new IndexOutOfRangeException($"Vector2Int has length=2, {i} is out of range."); }
		}

		public override bool Equals(object other) { return other is Vector2Int && Equals((Vector2Int)other); }
		public bool Equals(Vector2Int other) { return x.Equals(other.x) && y.Equals(other.y); }
		public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode() << 2; }
		public override string ToString() { return $"({x}, {y})"; }
		
		public float magnitude { get { return Sqrt(x * x + y * y); } }
		public int sqrMagnitude { get { return x * x + y * y; } }

		public void Set(int a, int b) { x = a; y = b; }
		public void Scale(Vector2Int scale) { x *= scale.x; y *= scale.y; }
		public void Clamp(Vector2 min, Vector2 max) {
			x = (int) Mathf.Clamp(x, min.x, max.x);
			y = (int) Mathf.Clamp(y, min.y, max.y);
		}

		public static Vector2Int Min(Vector2Int a, Vector2Int b) { return new Vector2Int(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y)); }
		public static Vector2Int Max(Vector2Int a, Vector2Int b) { return new Vector2Int(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)); }
		public static Vector2Int Scale(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x * b.x, a.y * b.y); }
		public static float Distance(Vector2Int a, Vector2Int b) { return (b-a).magnitude; }

		public static Vector2Int FloorToInt(Vector2 v) { return new Vector2Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y)); }
		public static Vector2Int CeilToInt(Vector2 v) { return new Vector2Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y)); }
		public static Vector2Int RoundToInt(Vector2 v) { return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y)); }
		
		public static Vector2Int operator -(Vector2Int a) { return new Vector2Int(-a.x, -a.y); }
		public static Vector2Int operator +(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x + b.x, a.y + b.y); }
		public static Vector2Int operator -(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x - b.x, a.y - b.y); }
		public static Vector2Int operator *(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x * b.x, a.y * b.y); }
		public static Vector2Int operator /(Vector2Int a, Vector2Int b) { return new Vector2Int(a.x / b.x, a.y / b.y); }
		public static Vector2Int operator *(Vector2Int a, int i) { return new Vector2Int(a.x * i, a.y * i); }
		public static Vector2Int operator *(int i, Vector2Int a) { return new Vector2Int(a.x * i, a.y * i); }
		public static Vector2Int operator /(Vector2Int a, int i) { return new Vector2Int(a.x / i, a.y / i); }
		public static Vector2Int operator /(int i, Vector2Int a) { return new Vector2Int(a.x / i, a.y / i); }
		public static bool operator ==(Vector2Int a, Vector2Int b) { return a.x == b.x && a.y == b.y; }
		public static bool operator !=(Vector2Int a, Vector2Int b) { return !(a == b); }
		
		public static implicit operator Vector2(Vector2Int v) { return new Vector2(v.x, v.y); }
		public static explicit operator Vector3Int(Vector2Int v) { return new Vector3Int(v.x, v.y, 0); }
		
	}
	#endregion
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	#region Vector3
	/// <summary> Surrogate class, similar to UnityEngine.Vector3 </summary>
	public struct Vector3 {
		public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
		public static Vector3 one { get { return new Vector3(1, 1, 1); } }
		public static Vector3 right { get { return new Vector3(1, 0, 0); } }
		public static Vector3 left { get { return new Vector3(-1, 0, 0); } }
		public static Vector3 up { get { return new Vector3(0, 1, 0); } }
		public static Vector3 down { get { return new Vector3(0, -1, 0); } }
		public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
		public static Vector3 back { get { return new Vector3(0, 0, -1); } }
		public static Vector3 positiveInfinity { get { return new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity); } }
		public static Vector3 negativeInfinity { get { return new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity); } }

		public float x,y,z;
		public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
		public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
		public float this[int i] {
			get { if (i == 0) { return x; } if (i == 1) { return y; } if (i == 2) { return z; } throw new IndexOutOfRangeException($"Vector3 has length=3, {i} is out of range."); }
			set { if (i == 0) { x = value; } if (i == 1) { y = value; } if (i == 2) { z = value; } throw new IndexOutOfRangeException($"Vector3 has length=3, {i} is out of range."); }
		}

		public override bool Equals(object other) { return other is Vector3 && Equals((Vector3)other); }
		public bool Equals(Vector3 other) { return x.Equals(other.x) && y.Equals(other.y) && z.Equals(other.z); }
		public override int GetHashCode() { return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2); }
		public override string ToString() { return $"({x:F2}, {y:F2}, {z:F2})"; }
		
		public Vector3 normalized { get { float m = magnitude; if (m > EPSILON) { return this / m; } return zero; } }
		public float magnitude { get { return Sqrt(x * x + y * y + z * z); } }
		public float sqrMagnitude { get { return x * x + y * y + z * z; } }

		public void Set(float a, float b, float c) { x = a; y = b; z = c; }
		public void Normalize() { float m = magnitude; if (m > EPSILON) { this /= m; } else { this = zero; } }
		public void Scale(Vector3 s) { x *= s.x; y *= s.y; z *= s.z; }
		public void Clamp(Vector3 min, Vector3 max) {
			x = Mathf.Clamp(x, min.x, max.x);
			y = Mathf.Clamp(y, min.y, max.y);
			z = Mathf.Clamp(z, min.z, max.z);
		}

		public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z)); }
		public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z)); }

		
		public static Vector3 Cross(Vector3 a, Vector3 b) {
			return new Vector3(a.y * b.z - a.z * b.y, 
								a.z * b.x - a.x * b.y,
								a.x * b.y * a.y * b.x);
		}
		public static float Dot(Vector3 a, Vector3 b) { return a.x * b.x + a.y * b.y + a.z * b.z; }
		public static Vector3 Reflect(Vector3 dir, Vector3 normal) { return -2f * Dot(normal, dir) * normal + dir; }
		public static Vector3 Project(Vector3 dir, Vector3 normal) {
			float len = Dot(normal, normal);
			return (len < SQR_EPSILON) ? zero : normal * Dot(dir, normal) / len;
		}
		public static Vector3 ProjectOnPlane(Vector3 v, Vector3 normal) { return v - Project(v, normal); }
		public static float Angle(Vector3 from, Vector3 to) {
			float e = Sqrt(from.sqrMagnitude * to.sqrMagnitude);
			if (e < SQR_EPSILON) { return 0; }
			float f = Mathf.Clamp(Dot(from, to) / e, -1f, 1f);
			return Acos(f) * Rad2Deg;
		}
		public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis) {
			float angle = Angle(from, to);
			float sign = Sign(Dot(axis, Cross(from, to)));
			return sign * angle;
		}
		public static float Distance(Vector3 a, Vector3 b) {
			Vector3 v = new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
			return Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
		}
		public static Vector3 ClampMagnitude(Vector3 vector, float maxLength) {
			return (vector.sqrMagnitude > maxLength * maxLength) ? vector.normalized * maxLength : vector; 
		}
		public static Vector3 operator -(Vector3 a) { return new Vector3(-a.x, -a.y, -a.z); }
		public static Vector3 operator +(Vector3 a, Vector3 b) { return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); }
		public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
		public static Vector3 operator *(Vector3 a, Vector3 b) { return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z); }
		public static Vector3 operator /(Vector3 a, Vector3 b) { return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z); }
		public static Vector3 operator *(Vector3 a, float f) { return new Vector3(a.x * f, a.y * f, a.z * f); }
		public static Vector3 operator *(float f, Vector3 a) { return new Vector3(a.x * f, a.y * f, a.z * f); }
		public static Vector3 operator /(Vector3 a, float f) { return new Vector3(a.x / f, a.y / f, a.z / f); }
		public static Vector3 operator /(float f, Vector3 a) { return new Vector3(a.x / f, a.y / f, a.z / f); }
		public static bool operator ==(Vector3 a, Vector3 b) { return (a - b).sqrMagnitude < COMPARE_EPSILON; }
		public static bool operator !=(Vector3 a, Vector3 b) { return !(a == b); }

	}
	#endregion
	//////////////////////////////////////////////////////////////////////////////////////////////////////////
	/////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////
	#region Vector3Int
	/// <summary> Surrogate class, similar to UnityEngine.Vector3Int </summary>
	public struct Vector3Int : IEquatable<Vector3Int> {
		public static Vector3Int zero { get { return new Vector3Int(0, 0, 0); } }
		public static Vector3Int one { get { return new Vector3Int(0, 0, 0); } }
		public static Vector3Int right { get { return new Vector3Int(1, 0, 0); } }
		public static Vector3Int left { get { return new Vector3Int(-1, 0, 0); } }
		public static Vector3Int up { get { return new Vector3Int(0, 1, 0); } }
		public static Vector3Int down { get { return new Vector3Int(0, -1, 0); } }
		public static Vector3Int forward { get { return new Vector3Int(0, 0, 1); } }
		public static Vector3Int back { get { return new Vector3Int(0, 0, -1); } }

		public int x,y,z;
		public Vector3Int(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
		public int this[int i] {
			get { if (i == 0) { return x; } if (i == 1) { return y; } if (i == 2) { return z; } throw new IndexOutOfRangeException($"Vector3 has length=3, {i} is out of range."); }
			set { if (i == 0) { x = value; } if (i == 1) { y = value; } if (i == 2) { z = value; } throw new IndexOutOfRangeException($"Vector3 has length=3, {i} is out of range."); }
		}

		public override bool Equals(object other) { return other is Vector3Int && Equals((Vector3Int)other); }
		public bool Equals(Vector3Int other) { return this == other; }
		public override int GetHashCode() { 
			int yy = y.GetHashCode(); int zz = z.GetHashCode(); int xx = x.GetHashCode();
			return xx ^ (yy << 4) ^ (yy >> 28) ^ (zz >> 4) ^ (zz << 28);
		}
		public override string ToString() { return $"({x}, {y}, {z})"; }

		public float magnitude { get { return Sqrt(x * x + y * y + z * z); } }
		public int sqrMagnitude { get { return x * x + y * y + z * z; } }

		public void Set(int a, int b, int c) { x = a; y = b; z = c; }
		public void Scale(Vector3Int scale) { x *= scale.x; y *= scale.y; z *= scale.z; }
		public void Clamp(Vector3 min, Vector3 max) {
			x = (int) Mathf.Clamp(x, min.x, max.x);
			y = (int) Mathf.Clamp(y, min.y, max.y);
			z = (int) Mathf.Clamp(z, min.z, max.z);
		}

		public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z)); }
		public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z)); }
		public static Vector3Int Scale(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z); }
		public static float Distance(Vector3Int a, Vector3Int b) { return (a - b).magnitude; }

		public static Vector3Int FloorToInt(Vector3 v) { return new Vector3Int(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y), Mathf.FloorToInt(v.z)); }
		public static Vector3Int CeilToInt(Vector3 v) { return new Vector3Int(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y), Mathf.CeilToInt(v.z)); }
		public static Vector3Int RoundToInt(Vector3 v) { return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z)); }

		public static Vector3Int operator -(Vector3Int a) { return new Vector3Int(-a.x, -a.y, -a.z); }
		public static Vector3Int operator +(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z); }
		public static Vector3Int operator -(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z); }
		public static Vector3Int operator *(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z); }
		public static Vector3Int operator /(Vector3Int a, Vector3Int b) { return new Vector3Int(a.x / b.x, a.y / b.y, a.z / b.z); }
		public static Vector3Int operator *(Vector3Int a, int i) { return new Vector3Int(a.x * i, a.y * i, a.z * i); }
		public static Vector3Int operator *(int i, Vector3Int a) { return new Vector3Int(a.x * i, a.y * i, a.z * i); }
		public static Vector3Int operator /(Vector3Int a, int i) { return new Vector3Int(a.x / i, a.y / i, a.z / i); }
		public static Vector3Int operator /(int i, Vector3Int a) { return new Vector3Int(a.x / i, a.y / i, a.z / i); }
		public static bool operator ==(Vector3Int a, Vector3Int b) { return a.x == b.x && a.y == b.y && a.z == b.z; }
		public static bool operator !=(Vector3Int a, Vector3Int b) { return !(a == b); }

		public static implicit operator Vector3(Vector3Int v) { return new Vector3(v.x, v.y, v.z); }
		public static explicit operator Vector2Int(Vector3Int v) { return new Vector2Int(v.x, v.y); }
	}
	#endregion


}

#endif
