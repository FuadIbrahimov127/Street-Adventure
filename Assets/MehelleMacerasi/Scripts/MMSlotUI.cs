using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Çanta və əşya panelindəki bir xana: klik (seç), üzərinə gəlmə (təsvir), sürükləyib-buraxma (yerini dəyiş).
public class MMSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int Index;
    public bool InBag;
    public Image Background;
    public Image Icon;
    public Text CountText;

    static int dragFrom = -1;

    static MMUI UI => MMGame.I != null ? MMGame.I.UI : null;

    public void OnPointerClick(PointerEventData e)
    {
        var inv = MMInventory.I;
        if (inv == null || UI == null) return;
        if (Index < MMInventory.HotbarSize) inv.Select(Index);
        if (InBag) UI.ShowItemInfo(Index);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (InBag && UI != null) UI.ShowItemInfo(Index);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        var inv = MMInventory.I;
        if (!InBag || inv == null || inv.Slots[Index].Empty) return;
        dragFrom = Index;
        UI.BeginDrag(Icon.sprite, e.position);
        Icon.color = new Color(1, 1, 1, 0.35f);
    }

    public void OnDrag(PointerEventData e)
    {
        if (dragFrom >= 0 && UI != null) UI.MoveDrag(e.position);
    }

    public void OnEndDrag(PointerEventData e)
    {
        dragFrom = -1;
        Icon.color = Color.white;
        if (UI != null) UI.EndDrag();
    }

    public void OnDrop(PointerEventData e)
    {
        if (!InBag || dragFrom < 0 || MMInventory.I == null) return;
        MMInventory.I.Move(dragFrom, Index);
        MMAudio.Play("blip");
        UI.ShowItemInfo(Index);
        dragFrom = -1;
    }
}
