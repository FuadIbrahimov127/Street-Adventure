using UnityEngine;

// Yaddaşda (Collected) müəyyən açar olanda obyekti göstərir.
// Məsələn: şənlik bəzəkləri ("festival"), evə qayıdan pişik balaları ("q_kit_done").
public class MMShowIf : MonoBehaviour
{
    public string Key;
    public GameObject Target;

    void Update()
    {
        var g = MMGame.I;
        if (g == null || Target == null) return;
        bool on = g.Collected.Contains(Key);
        if (Target.activeSelf != on) Target.SetActive(on);
    }
}
