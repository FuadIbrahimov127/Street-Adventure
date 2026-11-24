using UnityEngine;

// Qapı: oyunçu qapıya doğru yeriyəndə ekran qaralır və o biri tərəfə keçir (evə girir və ya çıxır).
public class MMDoor : MonoBehaviour
{
    public Vector2 Target;                    // keçəndən sonra oyunçunun yeri
    public Vector2 EnterDirection = Vector2.up;
    public Vector2 FaceAfter = Vector2.up;
    public MMRoom TargetRoom;                 // null = küçəyə çıxış
    public MMZone TargetZone;                 // başqa xəritəyə keçid (null = eyni xəritə)
    public string Message;                    // girəndə göstərilən bildiriş

    void OnTriggerStay2D(Collider2D other)
    {
        var p = other.GetComponentInParent<MMPlayer>();
        var g = MMGame.I;
        if (p == null || g == null || g.State != MMState.Playing || !p.IsMoving) return;
        if (Vector2.Dot(p.Facing, EnterDirection) < 0.7f) return;
        g.UseDoor(this);
    }
}
