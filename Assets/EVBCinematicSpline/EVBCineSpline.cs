using System.Collections.Generic;
using UnityEngine;

public class EVBCineSpline : MonoBehaviour
{
    [Range(-3,1)]
    public float Tension = -1f;
    [Range(-3,1)]
    public float AngularTension = -1f;
    [Range(-3, 1)]
    public float ZAngularTension = 1;
    public bool Play = false;
    [Range(0,1)]
    public float Position = 0f;
    [Tooltip("Meters/Second unless DoNotUseFixedSpeed==true then Speed becomes the Duration of the Dolly Move")]
    public float Speed = 1f;
    public ZMode ZRotationMode = ZMode.Smooth;
    public bool DoNotUseFixedSpeed = false;
    [Range(0.01f,1f)]
    public float StepResolution = 0.1f;
    
    public List<Transform> Keyframes;
    public Transform Dolly;
    public Transform OverrideUp;
    public Mesh GizmoMesh;

    // Update is called once per frame
    void Update()
    {
        if(Keyframes.Count == 0 || !Dolly) return;
        if (Position == 0)
        {
            Dolly.position = Keyframes[0].position;
            Dolly.rotation = Keyframes[0].rotation;
        }
        if (Position == 1)
        {
            Dolly.position = Keyframes[Keyframes.Count-1].position;
            Dolly.rotation = Keyframes[Keyframes.Count-1].rotation;
        }
        if(Play){
            float startingTime = Position;
            float stepDistance = Speed * Time.deltaTime;
            while (Position < 1)
            {
                var step = DoNotUseFixedSpeed ? Time.deltaTime / Speed : (StepResolution / Keyframes.Count) * Time.deltaTime;
                Position = Mathf.Min(step + Position, 1);
                float utime = Position * (Keyframes.Count - 1);
                var seg = GetSegmentForTime(utime);
                var between = utime - Mathf.Floor(utime);
                var tfrm = CatmulPro(seg, between);
                if(DoNotUseFixedSpeed) {
                    Dolly.position = tfrm.position;
                    Dolly.rotation = tfrm.rotation;
                    break;
                }
                float dist = (Dolly.position - tfrm.position).magnitude;
                if (Position < 1 && dist < Speed * Time.deltaTime)
                {
                    continue;
                }
                else if (dist > 0)
                {
                    var deltaT = Position - startingTime;
                    Position = Mathf.Min(startingTime + (stepDistance / dist) * deltaT, 1);

                    utime = Position * (Keyframes.Count - 1);
                    seg = GetSegmentForTime(utime);
                    between = utime - Mathf.Floor(utime);
                    tfrm = CatmulPro(seg, between);
                }
                Dolly.position = tfrm.position;
                Dolly.rotation = tfrm.rotation;
                break;
            }
        }else{
            float utime = Position * (Keyframes.Count - 1);
            Segment seg = GetSegmentForTime(utime);
            float between = utime - Mathf.Floor(utime);
            Tfrm tfrm = CatmulPro(seg, between);
            Dolly.position = tfrm.position;
            Dolly.rotation = tfrm.rotation;
        }
    }

    private Tfrm CatmulPro(Segment s, float t){
        Vector3 p0 = s.a.position, p1 = s.b.position, p2 = s.c.position, p3 = s.d.position;
        float t01 = 1;//(p0-p1).magnitude * Alpha;
        float t12 = 1;//(p1-p2).magnitude * Alpha;
        float t23 = 1;//(p2-p3).magnitude * Alpha;

        Vector3 m1 = (1.0f - Tension) * (p2 - p1 + t12 * ((p1 - p0) / t01 - (p2 - p0) / (t01 + t12)));
        Vector3 m2 = (1.0f - Tension) * (p2 - p1 + t12 * ((p3 - p2) / t23 - (p3 - p1) / (t12 + t23)));
        Vector3 a = 2f * (p1 - p2) + m1 + m2, b = -3f * (p1 - p2) - m1 - m1 - m2, c = m1, d = p1;
        Vector3 point = a * t * t * t + b * t * t + c * t + d;

        Vector3 r0 = s.a.rotation* Vector3.forward, r1 = s.b.rotation* Vector3.forward, r2 = s.c.rotation* Vector3.forward, r3 = s.d.rotation* Vector3.forward;

        m1 = (1.0f - AngularTension) * (r2 - r1 + t12 * ((r1 - r0) / t01 - (r2 - r0) / (t01 + t12)));
        m2 = (1.0f - AngularTension) * (r2 - r1 + t12 * ((r3 - r2) / t23 - (r3 - r1) / (t12 + t23)));
        a = 2f * (r1 - r2) + m1 + m2; b = -3f * (r1 - r2) - m1 - m1 - m2; c = m1; d = r1;
        Vector3 forward = a * t * t * t + b * t * t + c * t + d;

        Quaternion rotation = Quaternion.identity;

        if (ZRotationMode == ZMode.World)
        {
            rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
        else if (ZRotationMode == ZMode.Override)
        {
            rotation = Quaternion.LookRotation(forward, OverrideUp ? OverrideUp.up : Vector3.up);
        }
        else if(ZRotationMode == ZMode.Artistic)
        {
            r0 = s.a.rotation * Vector3.up; r1 = s.b.rotation * Vector3.up; r2 = s.c.rotation * Vector3.up; r3 = s.d.rotation * Vector3.up;

            m1 = (1.0f - ZAngularTension) * (r2 - r1 + t12 * ((r1 - r0) / t01 - (r2 - r0) / (t01 + t12)));
            m2 = (1.0f - ZAngularTension) * (r2 - r1 + t12 * ((r3 - r2) / t23 - (r3 - r1) / (t12 + t23)));
            a = 2f * (r1 - r2) + m1 + m2; b = -3f * (r1 - r2) - m1 - m1 - m2; c = m1; d = r1;
            Vector3 up = a * t * t * t + b * t * t + c * t + d;

            rotation = Quaternion.LookRotation(forward, up);
        }
        else if(ZRotationMode == ZMode.Smooth)
        {
            float z0 = s.a.rotation.eulerAngles.z, z1 = s.b.rotation.eulerAngles.z, z2 = s.c.rotation.eulerAngles.z, z3 = s.d.rotation.eulerAngles.z;
            float _m1 = (1.0f - ZAngularTension) * (z2 - z1 + t12 * ((z1 - z0) / t01 - (z2 - z0) / (t01 + t12)));
            float _m2 = (1.0f - ZAngularTension) * (z2 - z1 + t12 * ((z3 - z2) / t23 - (z3 - z1) / (t12 + t23)));
            float _a = 2f * (z1 - z2) + _m1 + _m2, _b = -3f * (z1 - z2) - _m1 - _m1 - _m2, _c = _m1, _d = z1;
            float z = _a * t * t * t + _b * t * t + _c * t + _d;

            rotation = Quaternion.LookRotation(forward, Vector3.up);
            rotation.eulerAngles += Vector3.forward * z;
        }
        else if(ZRotationMode == ZMode.Linear)
        {
            float z0 = s.a.rotation.eulerAngles.z, z1 = s.b.rotation.eulerAngles.z, z2 = s.c.rotation.eulerAngles.z, z3 = s.d.rotation.eulerAngles.z;
            rotation = Quaternion.LookRotation(forward, Vector3.up);
            rotation.eulerAngles += Vector3.forward * Mathf.LerpAngle(z0, z1, t);//z;
        }

        return new Tfrm {
            position = point,
            rotation = rotation
        };
    }

    private Segment GetSegmentForTime(float utime){
        if(Keyframes.Count < 1) return new Segment {
            a = new Tfrm { position = Vector3.zero, rotation = Quaternion.identity },
            b = new Tfrm { position = Vector3.zero, rotation = Quaternion.identity },
            c = new Tfrm { position = Vector3.zero, rotation = Quaternion.identity },
            d = new Tfrm { position = Vector3.zero, rotation = Quaternion.identity }
        };
        int i = Mathf.FloorToInt(utime);
        return new Segment
        {
            a = new Tfrm
            {
                position = i - 1 < Keyframes.Count ? Keyframes[Mathf.Max(i - 1, 0)].position : Keyframes[Keyframes.Count - 1].position,
                rotation = i - 1 < Keyframes.Count ? Keyframes[Mathf.Max(i - 1, 0)].rotation : Keyframes[Keyframes.Count - 1].rotation
            },
            b = new Tfrm
            {
                position = i < Keyframes.Count ? Keyframes[Mathf.Max(i, 0)].position : Keyframes[Keyframes.Count - 1].position,
                rotation = i < Keyframes.Count ? Keyframes[Mathf.Max(i, 0)].rotation : Keyframes[Keyframes.Count - 1].rotation
            },
            c = new Tfrm
            {
                position = i + 1 < Keyframes.Count ? Keyframes[Mathf.Max(i + 1, 0)].position : Keyframes[Keyframes.Count - 1].position,
                rotation = i + 1 < Keyframes.Count ? Keyframes[Mathf.Max(i + 1, 0)].rotation : Keyframes[Keyframes.Count - 1].rotation
            },
            d = new Tfrm
            {
                position = i + 2 < Keyframes.Count ? Keyframes[Mathf.Max(i + 2, 0)].position : Keyframes[Keyframes.Count - 1].position,
                rotation = i + 2 < Keyframes.Count ? Keyframes[Mathf.Max(i + 2, 0)].rotation : Keyframes[Keyframes.Count - 1].rotation,
            }
        };
    }

    private void OnDrawGizmos() {
        // Loop Through Path
        if(Keyframes.Count >= 2){
            // Step Distance
            float stepDistance = Mathf.Max(Speed,0.1f);
            int every = Mathf.Max(Mathf.FloorToInt(((1 / StepResolution) * Keyframes.Count) / Speed),1);

            Vector3 startingPos = Keyframes[0].position;
            float startingTime = 0;
            float time = 0;

            Gizmos.color = Color.gray;
            if (GizmoMesh) Gizmos.DrawWireMesh(GizmoMesh, Keyframes[0].position, Keyframes[0].rotation * Quaternion.Euler(0, -90, 0));

            int cnt = 0;
            while(time < 1){
                var step = StepResolution / Keyframes.Count;
                time = Mathf.Min(step+time,1);
                float utime = time * (Keyframes.Count-1);
                var seg = GetSegmentForTime(utime);
                var between = utime - Mathf.Floor(utime);
                var tfrm = CatmulPro(seg, between);
                if (DoNotUseFixedSpeed)
                {
                    cnt++;
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(startingPos, tfrm.position);
                    startingPos = tfrm.position;
                    startingTime = time;

                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(startingPos, startingPos + tfrm.rotation * Vector3.forward * 1f);
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(startingPos, startingPos + tfrm.rotation * Vector3.right * 0.1f);
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(startingPos, startingPos + tfrm.rotation * Vector3.up * 1f);

                    Gizmos.color = Color.gray;
                    if (GizmoMesh && cnt % every == 0) Gizmos.DrawWireMesh(GizmoMesh, startingPos, tfrm.rotation * Quaternion.Euler(0, -90, 0));
                    continue;
                }
                float dist = (startingPos - tfrm.position).magnitude;
                if(time < 1 && dist < stepDistance){
                    continue;
                }else if(dist > 0){
                    var deltaT = time-startingTime;
                    time = Mathf.Min(startingTime + (stepDistance/dist)*deltaT,1);

                    utime = time * (Keyframes.Count -1);
                    seg = GetSegmentForTime(utime);
                    between = utime - Mathf.Floor(utime);
                    tfrm = CatmulPro(seg, between);
                }

                cnt++;
                Gizmos.color = Color.red;
                Gizmos.DrawLine(startingPos, tfrm.position);
                startingPos = tfrm.position;
                startingTime = time;

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(startingPos, startingPos + tfrm.rotation * Vector3.forward * 1f);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(startingPos, startingPos + tfrm.rotation * Vector3.right * 0.1f);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(startingPos, startingPos + tfrm.rotation * Vector3.up * 1f);

                Gizmos.color = Color.gray;
                if(GizmoMesh) Gizmos.DrawWireMesh(GizmoMesh, startingPos, tfrm.rotation*Quaternion.Euler(0,-90,0));
            }
        }
    }

    public struct Tfrm {
        public Vector3 position;
        public Quaternion rotation;
    }
    public struct Segment {
        public Tfrm a;
        public Tfrm b;
        public Tfrm c;
        public Tfrm d; 
    }
    public enum ZMode {
        Artistic, Smooth, Linear, Override, World
    }
}

public class MathUtil
{
    /// <summary>
    /// Wrapper for Math.Pow()
    /// Can handle cases like (-8)^(1/3) or  (-1/64)^(1/3)
    /// </summary>
    public static float Pow(float expBase, float power)
    {
        bool sign = (expBase < 0);
        if (sign && HasEvenDenominator(power))
            return float.NaN;  //sqrt(-1) = i
        else
        {
            if (sign && HasOddDenominator(power))
                return -1 * Mathf.Pow(Mathf.Abs(expBase), power);
            else
                return Mathf.Pow(expBase, power);
        }
    }

    private static bool HasEvenDenominator(float input)
    {
        if (input == 0)
            return false;
        else if (input % 1 == 0)
            return false;

        float inverse = 1 / input;
        if (inverse % 2 < float.Epsilon)
            return true;
        else
            return false;
    }

    private static bool HasOddDenominator(float input)
    {
        if (input == 0)
            return false;
        else if (input % 1 == 0)
            return false;

        float inverse = 1 / input;
        if ((inverse + 1) % 2 < float.Epsilon)
            return true;
        else
            return false;
    }
}