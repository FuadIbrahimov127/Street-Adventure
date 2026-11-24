using UnityEngine;
using UnityEngine.EventSystems;

// Fasilə menyusundakı sətir: siçanla üzərinə gələndə seçilir, kliklə işləyir.
public class MMPauseRow : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public MMPauseMenu Menu;
    public int Index;

    public void OnPointerClick(PointerEventData e) { if (Menu != null) Menu.Activate(Index); }
    public void OnPointerEnter(PointerEventData e) { if (Menu != null) Menu.Select(Index); }
}
