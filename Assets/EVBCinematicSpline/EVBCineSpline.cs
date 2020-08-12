using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EVBCineSpline : MonoBehaviour
{
    [Range(0,1)]
    public float Tension = 0f;
    public bool Play = false;
    [Range(0,1)]
    public float Position = 0f;
    [Tooltip("Meters/Second")]
    public float Speed = 1f;
    [Range(0,1)]
    public float Alpha = 0.5f;
    public GameObject PreviewCameraPrefab;
    
    public List<Transform> points;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private Tfrm Catmul(Segment s, float t){
        Vector3 p0 = s.a.position, p1 = s.b.position, p2 = s.c.position, p3 = s.d.position;
        float t01 = Mathf.Pow((p0-p1).magnitude, Alpha);
        float t12 = Mathf.Pow((p1-p2).magnitude, Alpha);
        float t23 = Mathf.Pow((p2-p3).magnitude, Alpha);
        Vector3 m1 = (1.0f - Tension) * (p2 - p1 + t12 * ((p1 - p0) / t01 - (p2 - p0) / (t01 + t12)));
        Vector3 m2 = (1.0f - Tension) * (p2 - p1 + t12 * ((p3 - p2) / t23 - (p3 - p1) / (t12 + t23)));
        Vector3 point = p0 * t * t * t + p1 * t * t + p2 * t + p3;

        Vector3 r0 = s.a.rotation.eulerAngles, r1 = s.b.rotation.eulerAngles, r2 = s.c.rotation.eulerAngles, r3 = s.d.rotation.eulerAngles;
        Vector3 rotation = Vector3.zero;

        return new Tfrm {
            position = point,
            rotation = Quaternion.Euler()
        };
    }

    private Segment GetSegmentForTime(float utime){
        if(points.Count == 1) return new Segment {
            a = new Tfrm { position = points[0].position, rotation = points[0].rotation },
            b = new Tfrm { position = points[0].position, rotation = points[0].rotation },
            c = new Tfrm { position = points[0].position, rotation = points[0].rotation },
            d = new Tfrm { position = points[0].position, rotation = points[0].rotation }
        };
        if(points.Count < 1) return new Segment {
            a = new Tfrm { position = Vector3.zero, rotation = Quaternion.identity },
            b = new Tfrm { position = Vector3.zero, rotation = Quaternion.identity },
            c = new Tfrm { position = Vector3.zero, rotation = Quaternion.identity },
            d = new Tfrm { position = Vector3.zero, rotation = Quaternion.identity }
        };
        int i = Mathf.FloorToInt(utime);
        return new Segment {
            a = new Tfrm {
                position = points.Count > 0 ? points[i-1].position : points[0].position,
                rotation = points.Count > 0 ? points[i-1].rotation : points[0].rotation
            },
            b = new Tfrm {
                position = i < points.Count ? points[i].position : points[points.Count-1].position,
                rotation = i < points.Count ? points[i].rotation : points[points.Count-1].rotation
            },
            c = new Tfrm {
                position = i+1 < points.Count ? points[i+1].position : points[points.Count-1].position,
                rotation = i+1 < points.Count ? points[i+1].rotation : points[points.Count-1].rotation
            },
            d = new Tfrm {
                position = i+2 < points.Count ? points[i+2].position : points[points.Count-1].position,
                rotation = i+2 < points.Count ? points[i+2].rotation : points[points.Count-1].rotation,
            }
        };
    }

    private void OnDrawGizmos() {
        // Loop Through Path
        if(points.Count >= 2){
            // Step Distance
            float stepDistance = 0.1f;
            Gizmos.Color = Golor.red;


            Vector3 startingPos = points[0].position;
            float time = 0;
        }
    }

    public struct Tfrm {
        Vector3 position;
        Quaternion rotation;
    }
    public struct Segment {
        Tfrm a;
        Tfrm b;
        Tfrm c;
        Tfrm d; 
    }
}
